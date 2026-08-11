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
        if (leftShoulderBone != null) defaultLeftShoulderEuler = leftShoulderBone.localEulerAngles;
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
            float finalLeftX = NormalizeAngle(defaultLeftShoulderEuler.x) - angleX - offsetAngleX;
            float finalLeftY = NormalizeAngle(defaultLeftShoulderEuler.y) + angleY + offsetAngleY;
            leftShoulderBone.localRotation = Quaternion.Euler(finalLeftX, finalLeftY, defaultLeftShoulderEuler.z);
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