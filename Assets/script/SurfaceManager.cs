using UnityEngine;

public class SurfaceManager : MonoBehaviour
{
    public static SurfaceManager Instance; // 어디서든 접근 가능하게 싱글톤 설정

    [Header("임팩트 이펙트들")]
    public GameObject defaultImpact; // 기본 탄착군 (GIF/VFX)
    public GameObject fleshImpact;   // 적 피격용 (혈흔)
    public GameObject metalImpact;   // 철 소리/스파크용

    void Awake()
    {
        Instance = this;
    }

    public void PlayImpact(RaycastHit hit)
    {
        GameObject effectPref = defaultImpact;

        // 맞은 물체의 태그에 따라 이펙트 변경
        //if (hit.collider.CompareTag("Enemy"))
            //effectPref = fleshImpact;

        if (effectPref != null)
        {
            // 1. 이펙트 생성 (맞은 지점에서 살짝 띄워서 생성)
            GameObject effect = Instantiate(effectPref, hit.point + hit.normal * 0.01f, Quaternion.LookRotation(hit.normal));

            // 2. 맞은 물체가 움직일 수도 있으므로 부모로 설정
            effect.transform.SetParent(hit.transform);

            // 3. 2초 뒤 자동 삭제 (메모리 최적화)
            Destroy(effect, 2f);
        }
    }
}