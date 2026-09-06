using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 매치메이킹 전반을 담당하는 영속 싱글톤.
/// Unity Gaming Services(익명 로그인) -> Session(내부적으로 Lobby + Relay) 순으로 붙습니다.
/// Relay를 쓰기 때문에 포트포워딩 없이 서로 다른 인터넷 회선끼리 접속됩니다.
///
/// 이 오브젝트는 매치메이킹 씬에 놓여있고, DontDestroyOnLoad로 게임 씬까지 살아남습니다.
/// (NetworkManager도 마찬가지로 자기 자신을 DontDestroyOnLoad 처리합니다)
/// </summary>
public class MatchSession : MonoBehaviour
{
    public const int MaxPlayers = 2;
    public const string GameSceneName = "GameScene";
    public const string LobbySceneName = "MatchmakingScene";

    /// <summary>
    /// "빠른 매칭" 버튼이 쓰는 고정 세션 ID.
    /// 먼저 누른 사람이 방을 만들고, 두 번째 사람이 같은 ID로 들어옵니다.
    /// 여러 팀이 동시에 테스트하면 서로 섞이므로, 그럴 땐 아래 값을 팀마다 다르게 바꾸거나
    /// "방 만들기 / 코드로 참가"를 쓰세요.
    /// </summary>
    public string quickMatchSessionId = "quickmatch-1v1";

    public static MatchSession Instance { get; private set; }

    public ISession Session { get; private set; }
    public string StatusMessage { get; private set; } = "";
    public bool Busy { get; private set; }
    public bool SignedIn { get; private set; }

    /// <summary>UI가 다시 그려야 할 때 호출됩니다.</summary>
    public event Action Changed;

    private bool gameStarting;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    async void Start()
    {
        await SignInAsync();
    }

    private void Notify(string message = null)
    {
        if (message != null) StatusMessage = message;
        Changed?.Invoke();
    }

    // ------------------------------------------------------------------
    // 1. UGS 초기화 + 익명 로그인
    // ------------------------------------------------------------------
    public async Task SignInAsync()
    {
        if (SignedIn || Busy) return;

        try
        {
            Busy = true;
            Notify("Unity 서비스에 연결하는 중...");

            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            SignedIn = true;
            Notify("준비 완료. 매칭을 시작하세요.");
        }
        catch (Exception e)
        {
            Notify("서비스 연결 실패: " + e.Message +
                   "\n(Edit > Project Settings > Services 에서 Unity 클라우드 프로젝트를 연결했는지 확인하세요)");
            Debug.LogException(e);
        }
        finally
        {
            Busy = false;
            Notify();
        }
    }

    // ------------------------------------------------------------------
    // 2. 매칭 진입 경로 3가지
    // ------------------------------------------------------------------

    /// <summary>고정 ID로 만들거나 들어갑니다. 버튼 하나로 1대1 매칭.</summary>
    public async void QuickMatch()
    {
        if (!await EnsureReady()) return;

        try
        {
            Busy = true;
            Notify("상대를 찾는 중...");

            var options = new SessionOptions
            {
                Name = "1v1",
                MaxPlayers = MaxPlayers
            }.WithRelayNetwork();

            var session = await MultiplayerService.Instance
                .CreateOrJoinSessionAsync(quickMatchSessionId, options);

            OnJoined(session);
        }
        catch (Exception e)
        {
            FailWith(e, "빠른 매칭 실패");
        }
        finally
        {
            Busy = false;
            Notify();
        }
    }

    /// <summary>비공개 방을 만들고 참가 코드를 받습니다.</summary>
    public async void CreateRoom()
    {
        if (!await EnsureReady()) return;

        try
        {
            Busy = true;
            Notify("방을 만드는 중...");

            var options = new SessionOptions
            {
                Name = "1v1 Room",
                MaxPlayers = MaxPlayers,
                IsPrivate = true
            }.WithRelayNetwork();

            var session = await MultiplayerService.Instance.CreateSessionAsync(options);
            OnJoined(session);
        }
        catch (Exception e)
        {
            FailWith(e, "방 생성 실패");
        }
        finally
        {
            Busy = false;
            Notify();
        }
    }

    /// <summary>친구가 알려준 참가 코드로 들어갑니다.</summary>
    public async void JoinByCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            Notify("참가 코드를 입력하세요.");
            return;
        }

        if (!await EnsureReady()) return;

        try
        {
            Busy = true;
            Notify("방에 들어가는 중...");

            var session = await MultiplayerService.Instance
                .JoinSessionByCodeAsync(code.Trim().ToUpperInvariant());

            OnJoined(session);
        }
        catch (Exception e)
        {
            FailWith(e, "참가 실패 (코드를 다시 확인하세요)");
        }
        finally
        {
            Busy = false;
            Notify();
        }
    }

    public async void Leave()
    {
        gameStarting = false;

        if (Session == null)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                NetworkManager.Singleton.Shutdown();
            return;
        }

        try
        {
            Busy = true;
            Notify("나가는 중...");
            var s = Session;
            Session = null;
            await s.LeaveAsync();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        finally
        {
            Busy = false;
            Notify("세션에서 나왔습니다.");

            if (SceneManager.GetActiveScene().name != LobbySceneName)
                SceneManager.LoadScene(LobbySceneName);
        }
    }

    // ------------------------------------------------------------------
    // 3. 인원이 다 모이면 호스트가 게임 씬으로 넘깁니다.
    // ------------------------------------------------------------------
    void Update()
    {
        if (gameStarting || Session == null) return;
        if (!Session.IsHost) return;

        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer) return;

        // 로비 기준 인원과 실제 Relay 연결 인원이 모두 찼을 때만 시작합니다.
        // (로비에는 등록됐지만 아직 Relay 핸드셰이크가 안 끝난 순간에 씬을 넘기면
        //  들어오는 쪽이 씬 동기화를 놓칩니다)
        if (Session.PlayerCount < MaxPlayers) return;
        if (nm.ConnectedClientsIds.Count < MaxPlayers) return;

        gameStarting = true;
        Notify("상대를 찾았습니다! 게임을 시작합니다...");

        var status = nm.SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
        if (status != SceneEventProgressStatus.Started)
        {
            gameStarting = false;
            Notify("게임 씬 로드 실패: " + status +
                   "\n(File > Build Profiles 의 Scene List에 GameScene이 들어있는지 확인하세요)");
        }
    }

    // ------------------------------------------------------------------
    // 내부 헬퍼
    // ------------------------------------------------------------------
    private async Task<bool> EnsureReady()
    {
        if (Busy) return false;

        if (!SignedIn)
        {
            await SignInAsync();
            if (!SignedIn) return false;
        }

        if (Session != null)
        {
            Notify("이미 세션에 들어와 있습니다.");
            return false;
        }

        return true;
    }

    private void OnJoined(ISession session)
    {
        Session = session;
        gameStarting = false;

        session.RemovedFromSession += () =>
        {
            Session = null;
            gameStarting = false;
            Notify("세션이 종료되었습니다.");
        };

        session.Changed += () => Notify();

        Notify(session.IsHost
            ? "방을 만들었습니다. 상대를 기다리는 중..."
            : "방에 들어왔습니다. 곧 시작합니다...");
    }

    private void FailWith(Exception e, string prefix)
    {
        Session = null;
        Notify(prefix + ": " + e.Message);
        Debug.LogException(e);
    }

    /// <summary>UI 표시용. 아직 세션이 없으면 0을 돌려줍니다.</summary>
    public int PlayerCount => Session?.PlayerCount ?? 0;

    /// <summary>비공개 방의 참가 코드. 없으면 빈 문자열.</summary>
    public string JoinCode => Session?.Code ?? "";
}
