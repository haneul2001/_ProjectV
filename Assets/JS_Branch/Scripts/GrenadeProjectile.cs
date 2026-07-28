using UnityEngine;

public class GrenadeProjectile : MonoBehaviour
{
    [Header("폭발 오브젝트 연결")]
    public GameObject explosionPrefab;      // 폭발 범위 및 데미지를 처리할 프리팹

    private bool hasExploded = false;

    // 오브젝트가 다른 콜라이더와 물리적으로 충돌했을 때 자동으로 호출되는 함수
    void OnCollisionEnter(Collision collision)
    {
        // 이미 폭발했다면 중복 실행 방지
        if (hasExploded) return;

        Explode();
    }

    private void Explode()
    {
        hasExploded = true;

        // 1. 충돌한 위치에 폭발 프리팹 생성
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, transform.rotation);
        }

        // 2. 수류탄 자신은 파괴
        Destroy(gameObject);
    }
}