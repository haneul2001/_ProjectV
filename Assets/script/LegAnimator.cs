using UnityEngine;

/// <summary>
/// 애니메이션 클립 없이, 스크립트만으로 다리(허벅지/무릎)를 걷는 것처럼 흔드는 컴포넌트.
/// test_had.cs와 같은 패턴: Model 오브젝트는 Player와 다른 오브젝트이므로,
/// Player의 이동 속도/접지 상태를 GetComponentInParent로 찾아서 참조합니다.
///
/// 전제: 이 스크립트는 Model(뼈대, Animator가 있는) 오브젝트에 붙이고,
///       Player(원기둥 물리 콜라이더 + Player.cs)는 이 오브젝트의 부모(또는 조상)여야 합니다.
/// </summary>
public class LegAnimator : MonoBehaviour
{
    private Animator anim;
    private Player player; // 부모 쪽에 있는 Player.cs (속도/접지 상태 소스)

    [Header("본 이름 (하이어라키에 보이는 실제 이름)")]
    [Tooltip("Avatar Humanoid 매핑을 거치지 않고, 하이어라키에서 이 이름과 정확히 일치하는 본을 직접 찾습니다.")]
    public string leftUpperLegBoneName = "LeftUpLeg";
    public string rightUpperLegBoneName = "RightUpLeg";

    [Tooltip("무릎(정강이) 본 이름. 점프 포즈에서만 사용되고, 걷기 스윙에는 영향 없음")]
    public string leftLowerLegBoneName = "LeftLeg";
    public string rightLowerLegBoneName = "RightLeg";

    private Transform leftUpperLeg;
    private Transform rightUpperLeg;
    private Transform leftLowerLeg;
    private Transform rightLowerLeg;

    private Quaternion defaultLeftLowerLeg;
    private Quaternion defaultRightLowerLeg;

    private Quaternion defaultLeftUpperLeg;
    private Quaternion defaultRightUpperLeg;

    [Header("보행 사이클")]
    [Tooltip("속도 1(m/s)당 다리가 왕복하는 빠르기. 값이 클수록 다리가 빨리 움직임")]
    public float strideFrequency = 2.2f;

    [Tooltip("이 속도에서 스윙 폭이 최대치(Max Swing Angle)에 도달함. 그 이상은 더 안 커짐")]
    public float speedForFullSwing = 5f;

    [Header("스윙 각도")]
    [Range(0f, 60f)] public float maxSwingAngle = 20f;

    [Header("사이드 스텝 (A/D, 대각선용)")]
    [Tooltip("좌우로 이동할 때 섞이는 사이드 스윙 폭. 0이면 항상 앞/뒤 스윙만 함")]
    [Range(0f, 60f)] public float maxSideSwingAngle = 14f;
    [Tooltip("사이드 스윙 방향이 반대로 보이면 체크")]
    public bool invertSideSwing = false;

    [Header("공중(점프/추락) 처리")]
    [Tooltip("땅에서 떨어지면 스윙 폭을 이 속도로 부드럽게 0으로 줄여서 다리를 중립 자세로 되돌림")]
    public float airborneBlendSpeed = 8f;
    [Tooltip("공중에 떴을 때 두 허벅지를 이 각도만큼 앞으로 살짝 구부림 (점프 포즈)")]
    [Range(0f, 60f)] public float jumpLegBendAngle = 22f;
    [Tooltip("허벅지 굽는 방향이 반대로(이상하게) 보이면 체크")]
    public bool invertJumpThighBend = false;
    [Tooltip("공중에 떴을 때 두 무릎을 이 각도만큼 뒤로 살짝 접음 (점프 포즈)")]
    [Range(0f, 90f)] public float jumpKneeBendAngle = 35f;
    [Tooltip("무릎 접히는 방향이 반대로(이상하게) 보이면 체크")]
    public bool invertJumpKneeBend = false;

    [Header("방향 보정")]
    [Tooltip("모델 리깅에 따라 다리가 반대로 움직이면 체크해서 뒤집으세요")]
    public bool invertSwingDirection = false;

    private float phase = 0f;
    private float currentAmplitudeMultiplier = 1f; // 접지/공중 상태에 따른 블렌드 값 (0~1)

    void Start()
    {
        anim = GetComponent<Animator>();

        // Model 오브젝트 자신에게는 Player.cs가 없고, 부모(물리용 원기둥)에 붙어있으므로
        // GetComponentInParent로 찾아 올라갑니다. (test_had.cs가 Camera_Rotate를 찾는 것과 동일한 패턴)
        player = GetComponentInParent<Player>();
        if (player == null)
        {
            Debug.LogWarning("[LegAnimator] 부모 계층에서 Player.cs를 찾지 못했습니다. " +
                             "이 스크립트는 Player 오브젝트의 자식(Model)에 붙어있어야 합니다.");
        }

        // [핵심] Avatar Humanoid 매핑을 거치지 않고, 하이어라키에서 이름이 정확히
        // 일치하는 본을 직접 재귀 탐색으로 찾습니다. Avatar 매핑이 잘못돼 있어서
        // 엉뚱한 본(Hips 등)이 움직이는 문제를 원천적으로 방지합니다.
        leftUpperLeg = FindDeepChild(transform, leftUpperLegBoneName);
        rightUpperLeg = FindDeepChild(transform, rightUpperLegBoneName);
        leftLowerLeg = FindDeepChild(transform, leftLowerLegBoneName);
        rightLowerLeg = FindDeepChild(transform, rightLowerLegBoneName);

        if (leftUpperLeg == null)
            Debug.LogWarning($"[LegAnimator] '{leftUpperLegBoneName}' 이름의 본을 하이어라키에서 못 찾았습니다. Bone Name 필드를 확인하세요.");
        if (rightUpperLeg == null)
            Debug.LogWarning($"[LegAnimator] '{rightUpperLegBoneName}' 이름의 본을 하이어라키에서 못 찾았습니다. Bone Name 필드를 확인하세요.");
        if (leftLowerLeg == null)
            Debug.LogWarning($"[LegAnimator] '{leftLowerLegBoneName}' 이름의 본을 하이어라키에서 못 찾았습니다. Bone Name 필드를 확인하세요.");
        if (rightLowerLeg == null)
            Debug.LogWarning($"[LegAnimator] '{rightLowerLegBoneName}' 이름의 본을 하이어라키에서 못 찾았습니다. Bone Name 필드를 확인하세요.");

        if (leftUpperLeg != null) defaultLeftUpperLeg = leftUpperLeg.localRotation;
        if (rightUpperLeg != null) defaultRightUpperLeg = rightUpperLeg.localRotation;
        if (leftLowerLeg != null) defaultLeftLowerLeg = leftLowerLeg.localRotation;
        if (rightLowerLeg != null) defaultRightLowerLeg = rightLowerLeg.localRotation;
    }

    // 자신을 포함해 모든 자식을 재귀적으로 뒤져서 이름이 정확히 일치하는 Transform을 찾음
    private Transform FindDeepChild(Transform parent, string name)
    {
        if (parent.name == name) return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result = FindDeepChild(parent.GetChild(i), name);
            if (result != null) return result;
        }

        return null;
    }

    void LateUpdate()
    {
        if (player == null) return;

        float speed = player.HorizontalSpeed;
        bool grounded = player.IsGrounded;

        // 접지/공중 상태에 따라 스윙 폭을 부드럽게 블렌드 (점프 중엔 서서히 중립 자세로)
        float targetMultiplier = grounded ? 1f : 0f;
        currentAmplitudeMultiplier = Mathf.Lerp(
            currentAmplitudeMultiplier, targetMultiplier, Time.deltaTime * airborneBlendSpeed);

        // 속도가 빠를수록 위상(phase)이 빨리 진행 -> 다리가 빨리 왕복
        phase += speed * Time.deltaTime * strideFrequency;

        // 속도가 빠를수록 스윙 폭도 커짐 (speedForFullSwing에서 최대치 도달)
        float speedFactor = Mathf.Clamp01(speed / speedForFullSwing);

        // 원본 입력(전후/좌우 비율)을 정규화해서, 지금 이동이 "얼마나 앞/뒤 성분인지"
        // "얼마나 좌/우 성분인지"를 0~1 비율로 얻습니다.
        // 예) W만 누르면 forwardRatio=1, sideRatio=0 / D만 누르면 forwardRatio=0, sideRatio=1
        //     W+D 대각선이면 둘 다 약 0.7 정도씩 섞여서 자연스럽게 블렌드됨
        Vector2 input = player.MoveInput;
        Vector2 normalizedInput = input.sqrMagnitude > 0.0001f ? input.normalized : Vector2.zero;
        float forwardRatio = Mathf.Abs(normalizedInput.y);
        float sideRatio = Mathf.Abs(normalizedInput.x);

        float forwardAmplitude = maxSwingAngle * speedFactor * currentAmplitudeMultiplier * forwardRatio;
        float sideAmplitude = maxSideSwingAngle * speedFactor * currentAmplitudeMultiplier * sideRatio;

        float dir = invertSwingDirection ? -1f : 1f;
        float sideDir = (invertSideSwing ? -1f : 1f) * Mathf.Sign(normalizedInput.x == 0f ? 1f : normalizedInput.x);

        // 앞/뒤 스윙 (X축)
        float leftSwingX = Mathf.Sin(phase) * forwardAmplitude * dir;
        float rightSwingX = Mathf.Sin(phase + Mathf.PI) * forwardAmplitude * dir;

        // 좌/우 스텝 스윙 (Z축) - 같은 위상으로 옆으로도 다리를 교차시켜 사이드/대각선 이동을 표현
        float leftSwingZ = Mathf.Sin(phase) * sideAmplitude * sideDir;
        float rightSwingZ = Mathf.Sin(phase + Mathf.PI) * sideAmplitude * sideDir;

        // 공중(점프/추락)일 때 두 다리를 살짝 앞으로 구부리는 포즈.
        // currentAmplitudeMultiplier가 접지=1, 공중=0으로 블렌드되므로,
        // 그 반대값(1 - multiplier)을 써서 공중일 때만 서서히 부드럽게 나타나게 함
        float airborneBendBlend = 1f - currentAmplitudeMultiplier;
        float jumpThighBend = jumpLegBendAngle * airborneBendBlend * (invertJumpThighBend ? -1f : 1f);
        float jumpKneeBend = jumpKneeBendAngle * airborneBendBlend * (invertJumpKneeBend ? -1f : 1f);

        leftSwingX += jumpThighBend;
        rightSwingX += jumpThighBend;

        // 허벅지(UpperLeg)만 통짜로 회전. 정강이는 허벅지의 자식 본이라
        // 별도로 안 건드려도 자연스럽게 같이 딸려서 움직입니다.
        if (leftUpperLeg != null)
            leftUpperLeg.localRotation = defaultLeftUpperLeg * Quaternion.Euler(leftSwingX, 0f, leftSwingZ);

        if (rightUpperLeg != null)
            rightUpperLeg.localRotation = defaultRightUpperLeg * Quaternion.Euler(rightSwingX, 0f, rightSwingZ);

        // 무릎(LowerLeg)은 걷기 스윙에는 관여하지 않고, 점프 포즈에서만 뒤로 살짝 접힘
        if (leftLowerLeg != null)
            leftLowerLeg.localRotation = defaultLeftLowerLeg * Quaternion.Euler(-jumpKneeBend, 0f, 0f);

        if (rightLowerLeg != null)
            rightLowerLeg.localRotation = defaultRightLowerLeg * Quaternion.Euler(-jumpKneeBend, 0f, 0f);
    }
}