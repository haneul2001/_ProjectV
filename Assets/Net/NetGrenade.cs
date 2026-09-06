using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 네트워크로 동기화되는 투척물(수류탄 / 연막탄).
///
/// 권위 구조:
///  - 물리(Rigidbody)는 서버에서만 굴립니다. 클라이언트는 NetworkTransform이 보내주는
///    위치를 받아 그리기만 합니다. 양쪽에서 물리를 돌리면 서로 다른 곳에서 터집니다.
///  - 터지는 판정과 데미지도 전부 서버가 내립니다.
///  - 폭발/연막 이펙트는 네트워크 오브젝트가 아니라, 서버가 "여기서 터졌다"고 알려주면
///    각자 자기 화면에 로컬로 생성합니다. (이펙트까지 동기화할 이유가 없습니다)
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkObject))]
public class NetGrenade : NetworkBehaviour
{
    public enum Kind { Frag, Smoke }

    [Header("종류")]
    public Kind kind = Kind.Frag;

    [Header("터질 때 생성할 이펙트 프리팹")]
    [Tooltip("수류탄이면 ExplosionArea, 연막탄이면 SmokeArea. 네트워크 오브젝트가 아니어도 됩니다.")]
    public GameObject effectPrefab;

    [Header("폭발 데미지 (연막탄은 무시됨)")]
    [Tooltip("폭심지에서 받는 최대 데미지")]
    public float maxDamage = 90f;

    [Tooltip("데미지가 들어가는 반경. 폭심지에서 멀어질수록 선형으로 줄어듭니다. " +
             "이펙트 프리팹의 시각적 반경과는 별개입니다.")]
    public float damageRadius = 6f;

    [Header("타이밍")]
    [Tooltip("이 시간이 지나면 부딪히지 않아도 터집니다.")]
    public float fuseTime = 3f;

    [Tooltip("던진 직후 이 시간 동안은 충돌해도 안 터집니다. " +
             "던진 사람 몸에 스치자마자 터지는 걸 막아줍니다.")]
    public float armDelay = 0.12f;

    private Rigidbody rb;
    private Collider[] myColliders;
    private bool exploded;
    private float spawnTime;

    // 던진 사람. 폭발로 맞췄을 때 이 사람에게만 히트마커를 보냅니다.
    private ulong attackerId = ulong.MaxValue;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        myColliders = GetComponentsInChildren<Collider>(true);
    }

    public override void OnNetworkSpawn()
    {
        spawnTime = Time.time;

        // 클라이언트에서는 순수 표시용입니다.
        // 물리를 끄고 콜라이더도 꺼서, 내 캐릭터를 밀거나 총알을 막지 않게 합니다.
        if (!IsServer)
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
            foreach (var c in myColliders)
                if (c != null) c.enabled = false;
        }
    }

    /// <summary>서버가 스폰 직후 호출합니다.</summary>
    public void ServerInitialize(ulong throwerId, Vector3 velocity)
    {
        // 던진 사람과는 충돌하지 않게 해서, 발밑에서 바로 터지는 걸 막습니다.
        if (NetworkManager.ConnectedClients.TryGetValue(throwerId, out var client) &&
            client.PlayerObject != null)
        {
            foreach (var mine in myColliders)
            {
                if (mine == null) continue;
                foreach (var theirs in client.PlayerObject.GetComponentsInChildren<Collider>(true))
                {
                    if (theirs == null) continue;
                    Physics.IgnoreCollision(mine, theirs, true);
                }
            }
        }

        attackerId = throwerId;
        rb.linearVelocity = velocity;
    }

    void Update()
    {
        if (!IsServer || exploded) return;

        if (Time.time - spawnTime >= fuseTime)
            Explode();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!IsServer || exploded) return;
        if (Time.time - spawnTime < armDelay) return;

        Explode();
    }

    // ------------------------------------------------------------------
    private void Explode()
    {
        if (exploded) return;
        exploded = true;

        Vector3 point = transform.position;

        if (kind == Kind.Frag)
            ApplyDamage(point);

        // 모두의 화면에 이펙트를 띄웁니다. (Despawn 전에 보내야 도착합니다)
        SpawnEffectRpc(point);

        if (NetworkObject != null && NetworkObject.IsSpawned)
            NetworkObject.Despawn(true);
        else
            Destroy(gameObject);
    }

    private void ApplyDamage(Vector3 point)
    {
        var hits = Physics.OverlapSphere(point, damageRadius);
        var alreadyHit = new System.Collections.Generic.HashSet<PlayerHealth>();

        foreach (var col in hits)
        {
            var hp = col.GetComponentInParent<PlayerHealth>();
            if (hp == null || alreadyHit.Contains(hp)) continue;
            alreadyHit.Add(hp);

            // 폭심지에서 멀수록 데미지가 줄어듭니다 (선형 감쇠).
            float dist = Vector3.Distance(point, hp.transform.position);
            float falloff = Mathf.Clamp01(1f - dist / damageRadius);
            float damage = maxDamage * falloff;

            if (damage > 0.5f) hp.ServerApplyDamage(damage, attackerId);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void SpawnEffectRpc(Vector3 point)
    {
        if (effectPrefab == null) return;
        Instantiate(effectPrefab, point, Quaternion.identity);
    }

    void OnDrawGizmosSelected()
    {
        if (kind != Kind.Frag) return;
        Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, damageRadius);
    }
}
