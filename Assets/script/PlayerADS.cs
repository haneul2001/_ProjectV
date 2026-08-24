using UnityEngine;

/// <summary>
/// 정조준(ADS, Aim Down Sight) 스크립트.
/// 손 IK 없이 "야매"로 구현: 총의 조준경 위치에 미리 빈 오브젝트(ADS_Point)를 자식으로
/// 붙여두면, 팔/총이 어떻게 움직이든 그 오브젝트의 월드 좌표는 항상 조준경 위치를 따라갑니다.
/// 우클릭을 누르면 카메라를 그 위치로 부드럽게 이동시키고, 떼면 원래 자리로 돌아옵니다.
///
/// 전제: 이 스크립트는 Camera_Rotate.cs와 같은 오브젝트(Main Camera)에 붙입니다.
/// test_had.cs가 카메라의 "회전"을 매 프레임 복원하는 것과 별개로,
/// 이 스크립트는 카메라의 "위치"만 다루므로 서로 간섭하지 않습니다.
/// </summary>
public class PlayerADS : MonoBehaviour
{
    [Header("조준경 위치")]
    [Tooltip("총의 조준경(가늠자) 위치에 자식으로 붙여둔 빈 오브젝트를 연결하세요. " +
             "정확한 위치는 플레이 모드에서 눈으로 보면서 미세 조정하면 됩니다.")]
    public Transform adsPoint;

    [Header("조준 설정")]
    [Tooltip("정조준 입력 버튼. 기본은 마우스 우클릭(1)")]
    public int aimMouseButton = 1;

    [Tooltip("조준 상태로 전환되는 속도. 값이 클수록 빠르게 카메라가 이동함")]
    public float adsSpeed = 8f;

    [Header("FOV 줌 (선택)")]
    [Tooltip("정조준 시 카메라 시야각(FOV)도 같이 좁혀서 줌인 효과를 줍니다. 0이면 기능 끔")]
    public float adsFieldOfView = 40f;
    private float defaultFieldOfView;

    [Header("UI")]
    [Tooltip("힙파이어(평소) 크로스헤어 오브젝트. 정조준 중에는 자동으로 꺼지고, " +
             "정조준을 풀면 다시 켜집니다.")]
    public GameObject crosshair;

    private Camera cam;
    private Vector3 defaultLocalPosition; // 카메라의 원래(평소/힙파이어) 로컬 위치
    private float aimWeight = 0f;         // 0 = 평소, 1 = 완전 정조준

    // 다른 스크립트(PlayerFire 등)가 "지금 조준 중인지" 읽어갈 수 있게 공개
    public bool IsAiming => aimWeight > 0.01f;
    public float AimWeight => aimWeight;

    void Start()
    {
        cam = GetComponent<Camera>();
        defaultLocalPosition = transform.localPosition;

        if (cam != null)
        {
            defaultFieldOfView = cam.fieldOfView;
        }

        if (adsPoint == null)
        {
            Debug.LogWarning("[PlayerADS] ADS Point가 연결되지 않았습니다! " +
                              "총 조준경 위치에 빈 오브젝트를 만들어 인스펙터에 연결하세요.");
        }
    }

    void LateUpdate()
    {
        // 카메라 회전을 다루는 test_had.cs / Camera_Rotate.cs와 실행 순서가 겹쳐도,
        // 이 스크립트는 위치(position)만 건드리므로 서로 값을 덮어쓰지 않습니다.

        // 1. 조준 입력에 따라 aimWeight를 부드럽게 0~1로 보간
        bool aimInput = Input.GetMouseButton(aimMouseButton);
        float targetWeight = aimInput ? 1f : 0f;
        aimWeight = Mathf.MoveTowards(aimWeight, targetWeight, Time.deltaTime * adsSpeed);

        // 2. [핵심] 먼저 카메라 로컬 위치를 원래 자리로 복원합니다.
        //    지난 프레임에 transform.position(월드 좌표)을 직접 바꿨다면, 그 순간 Unity가
        //    내부적으로 로컬 위치도 같이 덮어써버려서 그대로 두면 오프셋이 영구적으로 틀어집니다.
        //    (test_had.cs가 카메라 "회전"을 매 프레임 복원하는 것과 완전히 같은 이유/원리)
        transform.localPosition = defaultLocalPosition;

        // 3. 조준 중이면, 방금 복원한 "평소 위치"에서 ADS_Point 위치로 aimWeight만큼 보간
        if (aimWeight > 0f && adsPoint != null)
        {
            Vector3 hipFireWorldPos = transform.position; // 방금 복원했으니 지금 값 = 평소 위치
            transform.position = Vector3.Lerp(hipFireWorldPos, adsPoint.position, aimWeight);
        }

        // 4. FOV 줌 (선택 기능)
        if (cam != null && adsFieldOfView > 0f)
        {
            cam.fieldOfView = Mathf.Lerp(defaultFieldOfView, adsFieldOfView, aimWeight);
        }

        // 5. 크로스헤어 표시/숨김 (정조준 중엔 숨김)
        if (crosshair != null)
        {
            bool shouldShowCrosshair = !IsAiming;
            if (crosshair.activeSelf != shouldShowCrosshair)
            {
                crosshair.SetActive(shouldShowCrosshair);
            }
        }
    }
}