using UnityEngine;

public class GrenadeThrower : MonoBehaviour
{
    [Header("투척 오브젝트 연결")]
    public Transform throwPoint;       // 수류탄이 생성될 위치 (Main Camera의 자식)
    public GameObject grenadePrefab;   // 던질 수류탄 프리팹 (GrenadeObj)

    [Header("수류탄 궤도 설정")]
    public float throwForce = 15f;     // 앞으로 날아가는 힘 (사거리)
    public float upwardForce = 4f;     // 위로 던지는 힘 (포물선의 높이)

    void Update()
    {
        // G키를 누르면 투척
        if (Input.GetKeyDown(KeyCode.G))
        {
            ThrowGrenade();
        }
    }

    private void ThrowGrenade()
    {
        // 1. ThrowPoint 위치에 수류탄 생성
        GameObject grenade = Instantiate(grenadePrefab, throwPoint.position, throwPoint.rotation);

        // 2. 생성된 수류탄의 물리 엔진(Rigidbody) 컴포넌트를 가져옴
        Rigidbody rb = grenade.GetComponent<Rigidbody>();
        
        if (rb != null)
        {
            // 3. 앞쪽으로 밀어내는 힘(throwForce)과 위로 띄우는 힘(upwardForce)을 합칩니다.
            Vector3 forceToAdd = throwPoint.forward * throwForce + throwPoint.up * upwardForce;
            
            // 4. ForceMode.Impulse를 사용하여 순간적으로 강한 힘을 줍니다.
            rb.AddForce(forceToAdd, ForceMode.Impulse);
        }
    }
}