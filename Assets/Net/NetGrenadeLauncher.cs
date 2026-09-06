using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 플레이어의 투척물 담당. 기존 GrenadeThrower.cs의 네트워크 버전입니다.
/// (GrenadeThrower는 로컬에서 Instantiate만 하기 때문에 상대 화면에 안 보였습니다)
///
/// 흐름: 소유자가 키를 누름 -> 서버에 "이 위치에서 이 방향으로 던지겠다" 요청
///       -> 서버가 개수를 검사하고 차감한 뒤 NetworkObject로 스폰
///       -> 모두의 화면에 보이고, 데미지도 서버가 판정
///
/// 개수는 NetworkVariable이라 서버가 진짜 값을 들고 있고, HUD는 그걸 읽기만 합니다.
/// </summary>
public class NetGrenadeLauncher : NetworkBehaviour
{
    [Header("투척 위치")]
    [Tooltip("기존 GrenadeThrower가 쓰던 ThrowPoint를 그대로 연결하세요.")]
    public Transform throwPoint;

    [Header("프리팹 (NetworkObject가 붙은 네트워크용)")]
    public GameObject fragPrefab;
    public GameObject smokePrefab;

    [Header("키")]
    public KeyCode fragKey = KeyCode.E;
    public KeyCode smokeKey = KeyCode.Q;

    [Header("보유 개수 (라운드마다 이 값으로 리필됩니다)")]
    public int maxFrag = 1;
    public int maxSmoke = 3;

    [Header("투척 힘")]
    public float throwForce = 15f;
    public float upwardForce = 4f;

    [Tooltip("던지는 사람 몸 안에서 생성되지 않도록 앞으로 밀어내는 거리")]
    public float spawnOffset = 0.7f;

    [Header("연사 방지")]
    public float throwCooldown = 0.6f;

    // --- 서버가 쓰고 모두가 읽습니다 (HUD가 이 값을 표시) ---
    public NetworkVariable<int> FragLeft = new NetworkVariable<int>(
        1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> SmokeLeft = new NetworkVariable<int>(
        3, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private float nextThrowTime;
    private PlayerHealth health;

    void Awake()
    {
        health = GetComponent<PlayerHealth>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer) ServerRefill();
    }

    /// <summary>서버 전용. 라운드가 시작될 때 RoundManager가 호출합니다.</summary>
    public void ServerRefill()
    {
        if (!IsServer) return;
        FragLeft.Value = maxFrag;
        SmokeLeft.Value = maxSmoke;
    }

    void Update()
    {
        if (!IsOwner) return;
        if (throwPoint == null) return;

        // 죽었거나 라운드 사이면 못 던집니다.
        if (health != null && health.IsDead.Value) return;
        var round = RoundManager.Instance;
        if (round != null && !round.RoundActive.Value) return;

        if (Time.time < nextThrowTime) return;

        if (Input.GetKeyDown(fragKey) && FragLeft.Value > 0)
        {
            nextThrowTime = Time.time + throwCooldown;
            RequestThrow(NetGrenade.Kind.Frag);
        }
        else if (Input.GetKeyDown(smokeKey) && SmokeLeft.Value > 0)
        {
            nextThrowTime = Time.time + throwCooldown;
            RequestThrow(NetGrenade.Kind.Smoke);
        }
    }

    private void RequestThrow(NetGrenade.Kind kind)
    {
        // 조준 방향은 소유자만 정확히 알고 있으므로(카메라가 소유자에게만 켜져 있음)
        // 위치와 방향을 같이 보냅니다.
        ThrowRpc((int)kind, throwPoint.position, throwPoint.forward, throwPoint.up);
    }

    [Rpc(SendTo.Server)]
    private void ThrowRpc(int kindValue, Vector3 origin, Vector3 forward, Vector3 up, RpcParams rpcParams = default)
    {
        var kind = (NetGrenade.Kind)kindValue;
        ulong sender = rpcParams.Receive.SenderClientId;

        // 서버가 최종 판정. 클라이언트 값을 믿지 않습니다.
        if (kind == NetGrenade.Kind.Frag)
        {
            if (FragLeft.Value <= 0) return;
            FragLeft.Value--;
        }
        else
        {
            if (SmokeLeft.Value <= 0) return;
            SmokeLeft.Value--;
        }

        var prefab = kind == NetGrenade.Kind.Frag ? fragPrefab : smokePrefab;
        if (prefab == null)
        {
            Debug.LogError("[NetGrenadeLauncher] " + kind + " 프리팹이 비어있습니다.");
            return;
        }

        Vector3 spawnPos = origin + forward.normalized * spawnOffset;
        var go = Instantiate(prefab, spawnPos, Quaternion.LookRotation(forward));

        var netObj = go.GetComponent<NetworkObject>();
        netObj.Spawn(true);

        var grenade = go.GetComponent<NetGrenade>();
        if (grenade != null)
        {
            Vector3 velocity = forward.normalized * throwForce + up.normalized * upwardForce;
            grenade.ServerInitialize(sender, velocity);
        }
    }
}
