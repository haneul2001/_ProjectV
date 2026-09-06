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
    private bool leaving;
    private bool disconnectHooked;

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

    /// <summary>
    /// 세션에서 나가고 로비로 돌아갑니다.
    /// 매치 종료, ESC 메뉴의 "매치 나가기", 상대 이탈 감지 등 모든 경로가 여기로 모입니다.
    /// </summary>
    public async void Leave()
    {
        if (leaving) return;   // 여러 경로에서 동시에 불려도 한 번만 처리합니다.
        leaving = true;
        gameStarting = false;

        var s = Session;
        Session = null;

        try
        {
            Busy = true;
            Notify("나가는 중...");

            if (s != null) await s.LeaveAsync();
        }
        catch (Exception e)
        {
            // 이미 끊긴 세션에서 나가려 하면 예외가 날 수 있는데, 그래도 로비로는 가야 합니다.
            Debug.LogWarning("[MatchSession] 세션 나가기 실패(무시하고 로비로 이동): " + e.Message);
        }
        finally
        {
            // Relay 연결이 남아있으면 다음 매칭이 꼬입니다. 확실히 내려줍니다.
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                NetworkManager.Singleton.Shutdown();

            Busy = false;
            Notify("세션에서 나왔습니다.");

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            if (SceneManager.GetActiveScene().name != LobbySceneName)
                SceneManager.LoadScene(LobbySceneName);

            leaving = false;
        }
    }

    /// <summary>로비의 "게임 종료" 버튼.</summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ------------------------------------------------------------------
    // 접속 끊김 감지
    // ------------------------------------------------------------------
    /// <summary>
    /// 매치 도중 상대(또는 호스트)가 사라졌을 때 로비로 돌려보냅니다.
    ///
    /// 호스트가 나간 경우: 게스트는 서버와의 연결 자체가 끊기므로
    /// RoundManager(서버에만 있음)가 알려줄 방법이 없습니다. 그래서 여기서 직접 잡습니다.
    /// 게스트가 나간 경우: 서버 쪽 RoundManager가 배너를 띄우고 5초 뒤 양쪽을 로비로 보냅니다.
    /// </summary>
    private void OnClientDisconnected(ulong clientId)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        // 서버는 RoundManager가 처리하므로 여기서는 클라이언트만 신경 씁니다.
        if (nm.IsServer) return;

        // 내가 끊겼거나 호스트가 사라진 경우
        if (clientId == nm.LocalClientId || clientId == NetworkManager.ServerClientId)
        {
            StatusMessage = "상대와의 연결이 끊어졌습니다.";
            Leave();
        }
    }

    // ------------------------------------------------------------------
    // 3. 인원이 다 모이면 호스트가 게임 씬으로 넘깁니다.
    // ------------------------------------------------------------------
    void Update()
    {
        var nm = NetworkManager.Singleton;

        // NetworkManager는 세션에 들어갈 때 준비되므로, 준비되면 한 번만 연결합니다.
        if (!disconnectHooked && nm != null)
        {
            nm.OnClientDisconnectCallback += OnClientDisconnected;
            disconnectHooked = true;
        }

        if (gameStarting || Session == null) return;
        if (!Session.IsHost) return;

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

    void OnDestroy()
    {
        if (disconnectHooked && NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
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
