using UnityEngine;

public class test_had : MonoBehaviour
{
    private Animator anim;

    // 메인 카메라에 붙어있는 스크립트 컴포넌트
    private Camera_Rotate cameraScript;

    private Transform headBone;
    private Transform leftShoulderBone;
    private Transform rightShoulderBone;

    [Header("상하 회전 범위 제한")]
    [Range(0f, 90f)] public float maxLookAngleX = 45f;

    [Header("좌우 회전 범위 제한")]
    [Range(0f, 90f)] public float maxLookAngleY = 50f;

    [Header("미세 조정 (오프셋)")]
    public float offsetAngleX = 0f;
    public float offsetAngleY = 0f;

    private Vector3 defaultHeadEuler;
    private Vector3 defaultLeftShoulderEuler;
    private Vector3 defaultRightShoulderEuler;
    private bool wasReloading = false; // 재장전 상태 전환 감지용 (로그 찍을 때만 사용)

    [Header("재장전 - 왼쪽 어깨 연동")]
    [Tooltip("PlayerFire.cs가 재장전 중일 때 true로 설정합니다. true인 동안에는 " +
             "왼쪽 어깨의 평소 조준 연동(상하좌우 따라가기)을 끄고, 아래 재장전 자세로 대체합니다.")]
    public bool isReloading = false;

    [Tooltip("재장전 시 왼쪽 어깨 오프셋. X는 절대각(항상 이 값으로 고정, 예: 180.6), " +
             "Y/Z는 현재 조준 각도 위에 더해지는 상대값입니다. " +
             "게임 실행 중 인스펙터에서 값을 바꿔가며 원하는 자세를 찾은 뒤 최종값으로 고정하세요.")]
    public Vector3 reloadShoulderEulerOffset = new Vector3(180.6f, -30f, 31.263f);

    [Tooltip("PlayerFire.cs가 매 프레임 넘겨주는 재장전 진행도 (0=원위치, 1=재장전 자세 최대치). " +
             "코드에서 자동으로 채워지므로 직접 건드릴 필요 없습니다.")]
    [Range(0f, 1f)] public float reloadProgress = 0f;

    [Header("좌우 흔들림(반동) 안정화")]
    [Tooltip("실제로 반영할 좌우 흔들림 최대치. 카메라가 여러 본 아래 중첩돼 있어서 " +
             "localEulerAngles.y를 그대로 쓰면 특정 각도 구간에서 값이 순간적으로 튀기 때문에, " +
             "먼저 이 정도의 작은 범위로 눌러줍니다.")]
    public float maxSwayY = 10f;
    [Tooltip("튀는 값을 얼마나 빨리 따라갈지. 값이 낮을수록 더 부드럽지만 반응이 느려집니다.")]
    public float swaySmoothSpeed = 15f;
    private float smoothedAngleY = 0f;

    void Start()
    {
        anim = GetComponent<Animator>();

        // 메인 카메라에서 Camera_Rotate 스크립트를 정확하게 찾아 연결합니다.
        if (Camera.main != null)
        {
            cameraScript = Camera.main.GetComponent<Camera_Rotate>();
        }

        if (anim != null)
        {
            headBone = anim.GetBoneTransform(HumanBodyBones.Head);
            leftShoulderBone = anim.GetBoneTransform(HumanBodyBones.LeftShoulder);
            rightShoulderBone = anim.GetBoneTransform(HumanBodyBones.RightShoulder);
        }

        if (headBone != null) defaultHeadEuler = headBone.localEulerAngles;
        if (leftShoulderBone != null)
        {
            defaultLeftShoulderEuler = leftShoulderBone.localEulerAngles;
        }
        else
        {
            Debug.LogWarning("[test_had] LeftShoulder 본을 Humanoid Avatar에서 찾지 못했습니다. " +
                              "Avatar 설정의 Muscles & Settings에서 Left Shoulder가 매핑되어 있는지 확인하세요. " +
                              "매핑이 안 되어 있으면 재장전 애니메이션도, 기존 조준 연동도 작동하지 않습니다.");
        }
        if (rightShoulderBone != null) defaultRightShoulderEuler = rightShoulderBone.localEulerAngles;
    }

    void LateUpdate()
    {
        // 카메라 스크립트 연결이 안 되었거나 누락되었다면 실행 중지 (안전 장치)
        if (cameraScript == null) return;

        // [핵심 수정] 카메라는 Head 본의 자식이라서, 아래에서 Head/어깨 본을 회전시키면
        // Camera_Rotate가 이번 프레임에 이미 세팅해둔 카메라의 "의도된" 월드 회전값도
        // 같이 틀어져 버립니다 (부모가 도는 만큼 자식도 같이 돌아가므로).
        // 이 상태로 방치하면 화면을 돌리거나 반동이 걸릴 때마다 몸-카메라 정렬이
        // 조금씩 어긋나며 계속 누적됩니다 (보고해주신 증상의 원인).
        // 그래서 Head/어깨를 건드리기 전에 카메라의 현재(=Camera_Rotate가 의도한) 월드 회전을
        // 미리 저장해두고, 아래에서 본들을 다 회전시킨 뒤 마지막에 이 값으로 다시 고정합니다.
        Quaternion intendedCameraRotation = Camera.main.transform.rotation;

        // 1. [상하 각도] public으로 연 길을 통해 순수 마우스 입력축(tempX)을 다이렉트로 가져옵니다.
        float angleX = cameraScript.tempX;

        // 2. [좌우 각도] 카메라가 월드에서 회전 중인 실시간 Y축 각도를 가져옵니다.
        //    ※ 카메라가 Head -> headfront 등 여러 본 아래 중첩되어 있어서, 이 로컬 Y값은
        //    오일러 각으로 분해되는 과정에서 특정 상하(피치) 구간을 지날 때 순간적으로
        //    크게 튀는 경우가 있습니다 (팔이 옆으로 확 벌어지는 잔상의 원인).
        //    실제 좌우 흔들림은 원래 작은 값이어야 하므로, 우선 좁은 범위로 눌러준 뒤
        //    부드럽게 따라가도록 감쇠시켜서 사용합니다.
        float rawAngleY = Camera.main.transform.localEulerAngles.y;
        if (rawAngleY > 180f) rawAngleY -= 360f; // -180 ~ 180 범위 보정
        rawAngleY = Mathf.Clamp(rawAngleY, -maxSwayY, maxSwayY);
        smoothedAngleY = Mathf.Lerp(smoothedAngleY, rawAngleY, Time.deltaTime * swaySmoothSpeed);
        float angleY = smoothedAngleY;

        // 3. 회전 작동 한계치 제한 (Clamping)
        angleX = Mathf.Clamp(angleX, -maxLookAngleX, maxLookAngleX);
        angleY = Mathf.Clamp(angleY, -maxLookAngleY, maxLookAngleY);

        // 4. 머리(Head) 상하좌우 연동 적용
        if (headBone != null)
        {
            float finalHeadX = NormalizeAngle(defaultHeadEuler.x) + angleX + offsetAngleX;
            float finalHeadY = NormalizeAngle(defaultHeadEuler.y) + angleY + offsetAngleY;
            headBone.localRotation = Quaternion.Euler(finalHeadX, finalHeadY, defaultHeadEuler.z);
        }

        // 5. 양쪽 어깨 상하좌우 연동 적용 (상하 부호 반전 유지)
        if (leftShoulderBone != null)
        {
            // 재장전 상태가 바뀐 순간에만 로그 (매 프레임 찍으면 콘솔 도배됨) - 디버깅용
            if (isReloading != wasReloading)
            {
                Debug.Log($"[test_had] isReloading = {isReloading} 로 전환됨 (leftShoulderBone: {leftShoulderBone.name})");
                wasReloading = isReloading;
            }

            // 재장전 여부와 상관없이, 어깨는 항상 "현재 조준 방향" 오일러 각을 기준으로 계산합니다.
            float finalLeftX = NormalizeAngle(defaultLeftShoulderEuler.x) - angleX - offsetAngleX;
            float finalLeftY = NormalizeAngle(defaultLeftShoulderEuler.y) + angleY + offsetAngleY;
            float finalLeftZ = defaultLeftShoulderEuler.z;

            if (isReloading)
            {
                // X축(reloadShoulderEulerOffset.x)만 "절대각"으로 취급합니다.
                // reloadProgress가 0→1로 갈 때는 "지금 조준하고 있던 X각도"에서 "재장전 절대각(180.6도 등)"으로
                // 자연스럽게 보간되고, 1→0으로 돌아올 때는 그 시점의 조준 X각도로 다시 부드럽게 복귀합니다.
                // (조준값을 별도로 저장했다가 되돌리는 것과 동일한 효과를, 매 프레임 현재값을 기준으로 삼아 얻습니다)
                float reloadFinalX = Mathf.LerpAngle(finalLeftX, reloadShoulderEulerOffset.x, reloadProgress);

                // Y, Z축은 기존처럼 현재 조준값 위에 상대적으로(가산) 더해집니다.
                float reloadFinalY = finalLeftY + reloadShoulderEulerOffset.y * reloadProgress;
                float reloadFinalZ = finalLeftZ + reloadShoulderEulerOffset.z * reloadProgress;

                leftShoulderBone.localRotation = Quaternion.Euler(reloadFinalX, reloadFinalY, reloadFinalZ);
            }
            else
            {
                leftShoulderBone.localRotation = Quaternion.Euler(finalLeftX, finalLeftY, finalLeftZ);
            }
        }

        if (rightShoulderBone != null)
        {
            float finalRightX = NormalizeAngle(defaultRightShoulderEuler.x) - angleX - offsetAngleX;
            float finalRightY = NormalizeAngle(defaultRightShoulderEuler.y) + angleY + offsetAngleY;
            rightShoulderBone.localRotation = Quaternion.Euler(finalRightX, finalRightY, defaultRightShoulderEuler.z);
        }

        // 6. Head/어깨 회전으로 인해 같이 틀어진 카메라의 월드 회전을,
        //    Camera_Rotate가 원래 의도했던 값으로 다시 고정합니다.
        //    → 몸/머리/팔이 아무리 움직여도 실제 1인칭 시야는 항상 정확히 유지됩니다.
        Camera.main.transform.rotation = intendedCameraRotation;
    }

    private float NormalizeAngle(float angle)
    {
        if (angle > 180f) angle -= 360f;
        return angle;
    }
}