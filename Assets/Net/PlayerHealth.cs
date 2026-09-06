using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 플레이어 체력. 서버 권위(server authoritative)로 관리합니다.
///
/// PlayerFire.cs는 레이캐스트로 맞은 콜라이더에서 IDamageable을 찾아 TakeDamage를 부릅니다.
/// 그 호출은 "쏜 사람의 클라이언트"에서 "맞은 사람의 복제본"을 대상으로 일어나므로,
/// 여기서 바로 체력을 깎지 않고 서버로 RPC를 보내 서버만 값을 바꾸게 합니다.
/// (그래서 RequireOwnership = false 가 필요합니다 - 내 것이 아닌 오브젝트에 보내는 RPC라서)
/// </summary>
[DisallowMultipleComponent]
public class PlayerHealth : NetworkBehaviour, IDamageable
{
    public float maxHp = 100f;

    public NetworkVariable<float> Hp = new NetworkVariable<float>(
        100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> IsDead = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public float Normalized => maxHp <= 0f ? 0f : Mathf.Clamp01(Hp.Value / maxHp);

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Hp.Value = maxHp;
            IsDead.Value = false;
        }
    }

    // IDamageable - 쏜 쪽 클라이언트에서 호출됨
    public void TakeDamage(float damage)
    {
        if (!IsSpawned) return;
        ApplyDamageRpc(damage);
    }

    // InvokePermission.Everyone: 이 오브젝트의 소유자가 아니어도 RPC를 보낼 수 있게 합니다.
    // 데미지는 "쏜 사람"이 "맞은 사람"의 복제본에 보내는 것이라 반드시 필요합니다.
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ApplyDamageRpc(float damage)
    {
        if (IsDead.Value) return;

        // 라운드 사이(리스폰 대기 중)에는 데미지를 무시합니다.
        var round = RoundManager.Instance;
        if (round != null && !round.RoundActive.Value) return;

        Hp.Value = Mathf.Max(0f, Hp.Value - damage);

        if (Hp.Value <= 0f)
        {
            IsDead.Value = true;
            if (round != null) round.OnPlayerDied(OwnerClientId);
        }
    }

    /// <summary>서버에서만 호출. 라운드 리셋용.</summary>
    /// <summary>
    /// 서버에서만 호출. 폭발처럼 이미 서버가 판정을 끝낸 데미지를 바로 적용합니다.
    /// (총알과 달리 RPC를 한 번 더 거칠 필요가 없습니다)
    /// </summary>
    public void ServerApplyDamage(float damage)
    {
        if (!IsServer || IsDead.Value) return;

        var round = RoundManager.Instance;
        if (round != null && !round.RoundActive.Value) return;

        Hp.Value = Mathf.Max(0f, Hp.Value - damage);

        if (Hp.Value <= 0f)
        {
            IsDead.Value = true;
            if (round != null) round.OnPlayerDied(OwnerClientId);
        }
    }

    /// <summary>서버에서만 호출. 라운드 리셋용.</summary>
    public void ServerReset()
    {
        if (!IsServer) return;
        Hp.Value = maxHp;
        IsDead.Value = false;
    }
}
