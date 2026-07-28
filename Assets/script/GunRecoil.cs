using UnityEngine;

public class GunRecoil : MonoBehaviour
{
    [Header("반동 설정")]
    public float recoilAmount = 2f;        // 반동 강도 (위로 올라가는 각도)
    public float recoilSpeed = 10f;        // 반동 발생 속도
    public float returnSpeed = 5f;         // 원위치로 돌아오는 속도
    public float maxRecoil = 15f;          // 최대 반동 누적 각도

    [Header("흔들림 (선택)")]
    public float horizontalRecoil = 0.5f;  // 좌우 흔들림 (0이면 없음)

    private float currentRecoil = 0f;      // 현재 반동 값
    private float targetRecoil = 0f;       // 목표 반동 값
    private Camera playerCamera;

    void Start()
    {
        playerCamera = Camera.main;
    }

    void Update()
    {
        // 반동이 목표값으로 빠르게 올라감
        currentRecoil = Mathf.Lerp(currentRecoil, targetRecoil, Time.deltaTime * recoilSpeed);

        // 카메라 X축 회전 적용 (위로 올라가는 효과)
        playerCamera.transform.localEulerAngles = new Vector3(-currentRecoil, 0f, 0f);

        // 목표값을 천천히 0으로 복귀
        targetRecoil = Mathf.Lerp(targetRecoil, 0f, Time.deltaTime * returnSpeed);
        currentRecoil = Mathf.Lerp(currentRecoil, 0f, Time.deltaTime * returnSpeed);
    }

    // 총 발사 시 호출
    public void ApplyRecoil()
    {
        float horizontal = Random.Range(-horizontalRecoil, horizontalRecoil);
        targetRecoil += recoilAmount;
        targetRecoil = Mathf.Clamp(targetRecoil, 0f, maxRecoil);

        // 좌우 흔들림 적용 (선택)
        if (horizontalRecoil > 0f)
        {
            playerCamera.transform.Rotate(0f, horizontal, 0f);
        }
    }
}