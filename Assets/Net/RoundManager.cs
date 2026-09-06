using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 5판 3선승제 1대1 라운드 진행. 서버(호스트)가 모든 판정을 내립니다.
/// 게임 씬에 하나만 놓여있는 씬 오브젝트입니다.
/// </summary>
public class RoundManager : NetworkBehaviour
{
    public static RoundManager Instance { get; private set; }

    [Header("규칙")]
    [Tooltip("먼저 이 판수를 이기면 매치 승리 (5판 3선승 = 3)")]
    public int roundsToWin = 3;

    [Tooltip("라운드가 끝나고 다음 라운드가 시작되기까지의 대기 시간(초)")]
    public float roundResetDelay = 3f;
    [Tooltip("매치가 끝나고 결과를 보여준 뒤 로비로 돌아가기까지의 시간(초)")]
    public float returnToLobbyDelay = 5f;


    [Header("스폰")]
    [Tooltip("NetworkObject가 붙은 플레이어 프리팹")]
    public GameObject playerPrefab;

    [Tooltip("스폰 지점. 최소 2개. 0번=호스트, 1번=게스트")]
    public Transform[] spawnPoints;

    // --- 모두가 읽는 상태 ---
    public NetworkVariable<int> ScoreHost = new NetworkVariable<int>(0);
    public NetworkVariable<int> ScoreGuest = new NetworkVariable<int>(0);
    public NetworkVariable<int> RoundNumber = new NetworkVariable<int>(0);
    public NetworkVariable<bool> RoundActive = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> MatchOver = new NetworkVariable<bool>(false);

    /// <summary>HUD가 구독합니다. (표시할 문구, 표시 시간)</summary>
    public static event Action<string, float> BannerRequested;

    private ulong guestClientId = ulong.MaxValue;
    private bool matchStarted;

    public override void OnNetworkSpawn()
    {
        Instance = this;

        if (!IsServer) return;

        NetworkManager.OnClientDisconnectCallback += OnClientDisconnect;
        StartCoroutine(ServerStartMatch());
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager != null)
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnect;

        if (Instance == this) Instance = null;
    }

    // ------------------------------------------------------------------
    // 매치 시작 - 두 명이 씬에 다 들어올 때까지 기다렸다가 플레이어를 스폰합니다.
    // ------------------------------------------------------------------
    private IEnumerator ServerStartMatch()
    {
        float timeout = Time.time + 20f;

        while (NetworkManager.ConnectedClientsIds.Count < 2 && Time.time < timeout)
            yield return null;

        // 씬 동기화가 완전히 끝나도록 한 박자 쉽니다.
        yield return new WaitForSeconds(0.5f);

        foreach (var id in NetworkManager.ConnectedClientsIds)
        {
            if (id != NetworkManager.ServerClientId) guestClientId = id;
            SpawnPlayerFor(id);
        }

        matchStarted = true;
        yield return StartCoroutine(BeginRound(1));
    }

    private void SpawnPlayerFor(ulong clientId)
    {
        if (playerPrefab == null)
        {
            Debug.LogError("[RoundManager] playerPrefab이 비어있습니다.");
            return;
        }

        var client = NetworkManager.ConnectedClients[clientId];
        if (client.PlayerObject != null) return; // 이미 있음

        var point = GetSpawnPoint(clientId);
        var go = Instantiate(playerPrefab, point.position, point.rotation);
        var netObj = go.GetComponent<NetworkObject>();
        netObj.SpawnAsPlayerObject(clientId);
    }

    private Transform GetSpawnPoint(ulong clientId)
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return transform;
        int index = clientId == NetworkManager.ServerClientId ? 0 : 1;
        return spawnPoints[index % spawnPoints.Length];
    }

    // ------------------------------------------------------------------
    // 라운드 진행
    // ------------------------------------------------------------------
    private IEnumerator BeginRound(int number)
    {
        RoundNumber.Value = number;

        foreach (var id in NetworkManager.ConnectedClientsIds)
            ResetPlayer(id);

        // 리셋이 반영될 한 프레임
        yield return null;

        RoundActive.Value = true;
        ShowBannerRpc($"라운드 {number} 시작!", 1.5f);
    }

    private void ResetPlayer(ulong clientId)
    {
        if (!NetworkManager.ConnectedClients.TryGetValue(clientId, out var client)) return;
        var playerObject = client.PlayerObject;
        if (playerObject == null) return;

        var hp = playerObject.GetComponent<PlayerHealth>();
        if (hp != null) hp.ServerReset();

        // 수류탄 / 연막탄 개수도 매 라운드 다시 채워줍니다.
        var launcher = playerObject.GetComponent<NetGrenadeLauncher>();
        if (launcher != null) launcher.ServerRefill();

        var net = playerObject.GetComponent<NetPlayer>();
        var point = GetSpawnPoint(clientId);
        if (net != null) net.RespawnRpc(point.position, point.rotation.eulerAngles.y);
    }

    /// <summary>PlayerHealth가 서버에서 호출합니다.</summary>
    public void OnPlayerDied(ulong deadClientId)
    {
        if (!IsServer || !RoundActive.Value || MatchOver.Value) return;

        RoundActive.Value = false;

        ulong winnerId = deadClientId == NetworkManager.ServerClientId
            ? guestClientId
            : NetworkManager.ServerClientId;

        if (winnerId == NetworkManager.ServerClientId) ScoreHost.Value++;
        else ScoreGuest.Value++;

        RoundResultRpc(winnerId);

        if (ScoreHost.Value >= roundsToWin || ScoreGuest.Value >= roundsToWin)
            StartCoroutine(EndMatch(winnerId));
        else
            StartCoroutine(NextRoundAfterDelay());
    }

    private IEnumerator NextRoundAfterDelay()
    {
        yield return new WaitForSeconds(roundResetDelay);
        yield return StartCoroutine(BeginRound(RoundNumber.Value + 1));
    }

    private IEnumerator EndMatch(ulong winnerId)
    {
        MatchOver.Value = true;
        yield return new WaitForSeconds(2f);
        MatchResultRpc(winnerId);

        // 결과를 보여준 뒤 양쪽 모두 로비로 돌려보냅니다.
        yield return new WaitForSeconds(returnToLobbyDelay);
        ReturnToLobbyRpc();
    }

    private void OnClientDisconnect(ulong clientId)
    {
        if (!IsServer || !matchStarted || MatchOver.Value) return;
        if (clientId == NetworkManager.ServerClientId) return;

        RoundActive.Value = false;
        MatchOver.Value = true;
        ShowBannerRpc("상대가 나갔습니다. 로비로 돌아갑니다...", returnToLobbyDelay);
        StartCoroutine(ReturnToLobbyAfterDelay());
    }

    private IEnumerator ReturnToLobbyAfterDelay()
    {
        yield return new WaitForSeconds(returnToLobbyDelay);
        ReturnToLobbyRpc();
    }

    // ------------------------------------------------------------------
    // 클라이언트 표시용 RPC
    // ------------------------------------------------------------------
    [Rpc(SendTo.Everyone)]
    private void ShowBannerRpc(string message, float duration)
    {
        BannerRequested?.Invoke(message, duration);
    }

    [Rpc(SendTo.Everyone)]
    private void RoundResultRpc(ulong winnerClientId)
    {
        bool iWon = NetworkManager.Singleton.LocalClientId == winnerClientId;
        BannerRequested?.Invoke(iWon ? "라운드 승리!" : "라운드 패배", roundResetDelay);
    }

    [Rpc(SendTo.Everyone)]
    private void MatchResultRpc(ulong winnerClientId)
    {
        bool iWon = NetworkManager.Singleton.LocalClientId == winnerClientId;
        BannerRequested?.Invoke(iWon ? "매치 승리!" : "매치 패배", returnToLobbyDelay + 2f);
    }

    /// <summary>
    /// 매치 종료. 양쪽이 각자 세션을 정리하고 매치메이킹 씬으로 돌아갑니다.
    /// 호스트가 NGO로 씬만 강제로 넘기면 Relay 세션이 그대로 살아있어서
    /// 다음 매칭이 안 잡히기 때문에, 반드시 각자 Leave()를 거쳐야 합니다.
    /// </summary>
    [Rpc(SendTo.Everyone)]
    private void ReturnToLobbyRpc()
    {
        // 게임 중에 잠겨있던 커서를 풀어줍니다. 안 풀면 로비에서 버튼을 못 누릅니다.
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (MatchSession.Instance != null)
            MatchSession.Instance.Leave();
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(MatchSession.LobbySceneName);
    }

    // ------------------------------------------------------------------
    // HUD 편의 함수
    // ------------------------------------------------------------------
    /// <summary>내 점수 (로컬 클라이언트 기준)</summary>
    public int MyScore =>
        NetworkManager.Singleton.LocalClientId == NetworkManager.ServerClientId
            ? ScoreHost.Value : ScoreGuest.Value;

    /// <summary>상대 점수</summary>
    public int OpponentScore =>
        NetworkManager.Singleton.LocalClientId == NetworkManager.ServerClientId
            ? ScoreGuest.Value : ScoreHost.Value;
}
