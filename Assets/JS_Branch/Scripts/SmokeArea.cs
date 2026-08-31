using UnityEngine;

public class SmokeArea : MonoBehaviour
{
    [Header("연막 설정")]
    public float smokeDuration = 10f; // 연막이 유지되는 시간

    void Start()
    {
        Destroy(gameObject, smokeDuration); // 연막 유지시간이 끝나면 연막 파괴
    }
}