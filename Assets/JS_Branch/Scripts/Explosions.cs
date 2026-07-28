using UnityEngine;

public class Explosion : MonoBehaviour
{
    [Header("폭발 설정")]
    public float explosionRadius = 5f;      // 폭발 반경
    public float destroyTime = 2f;          // 폭발 이펙트가 유지되는 시간

    void Start()
    {
        // 1. 데미지 판정
        ApplyExplosionDamage();

        // 2. 설정한 시간(destroyTime)이 지나면 폭발 프리팹 전체(이펙트 포함)를 파괴
        Destroy(gameObject, destroyTime);
    }

    private void ApplyExplosionDamage()
    {
        // 폭발 반경(explosionRadius) 내에 있는 모든 콜라이더를 가져옵니다.
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
    }
}