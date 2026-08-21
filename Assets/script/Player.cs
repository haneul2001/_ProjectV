using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class Player : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotateSpeed = 100f;
    public float jumpPower = 6f;

    int jumpcount = 0;
    int jumplimit = 1;

    Rigidbody rb;

    // Update에서 입력만 받아두고, 실제 이동/회전은 FixedUpdate에서 물리적으로 적용합니다.
    private Vector3 moveDir;
    private float mouseMoveX;
    private bool jumpRequested;

    // [다리 스크립트 애니메이션용] Model 오브젝트(test_had.cs 등)는 이 Player와 다른
    // 오브젝트에 붙어있으므로, 다리 스크립트가 여기서 값을 읽어갈 수 있게 공개해둡니다.
    private bool isGrounded = true;

    // 수평(XZ) 이동 속도. 다리 스윙 폭/속도를 계산할 때 사용.
    // ※ rb.velocity 대신 실제 이동 입력(moveDir * moveSpeed)을 직접 사용합니다.
    //    MovePosition으로 움직이는 Non-kinematic Rigidbody는 rb.velocity에
    //    이동량이 정확히 반영되지 않는 경우가 있기 때문입니다.
    // [다리 스크립트 방향 반응용] 정규화 전 원본 입력값 (전후/좌우 구분).
    // 대각선(예: W+D)으로 움직일 때 다리가 방향에 맞게 자연스럽게 섞이도록
    // LegAnimator가 이 값을 읽어갑니다.
    public Vector2 MoveInput { get; private set; }

    public float HorizontalSpeed => moveDir.magnitude * moveSpeed;

    // 현재 땅에 붙어있는지 여부. 공중에 뜨면(점프 등) 다리를 중립 자세로 되돌리는 데 사용.
    public bool IsGrounded => isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // [벽 뚫림 방지] 기존에는 transform.position을 직접 바꿔서 이동했기 때문에
        // 물리 충돌 판정을 전혀 거치지 않았고, 벽에 붙어서 이동하면 몸(콜라이더)이
        // 그대로 벽 속으로 살짝 파고들 수 있었습니다 (벽 안에 있던 카메라도 같이 파고든 이유).
        // Rigidbody를 물리적으로 움직이게(MovePosition) 바꾸고, 아래 옵션들로
        // 충돌이 안정적으로 판정되도록 보강합니다.
        rb.interpolation = RigidbodyInterpolation.Interpolate;         // 물리 스텝 사이 움직임을 부드럽게 보간 (화면 끊김 방지)
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // 빠르게 움직여도 얇은 벽을 순간적으로 뚫지 않도록
        rb.freezeRotation = true;                                      // 충돌 시 캐릭터가 넘어지거나 제멋대로 회전하지 않도록 고정
    }

    void Update()
    {
        // WASD 입력을 숫자로 받아서 저장
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 dir = new Vector3(h, 0, v);
        dir.Normalize();

        // 다리 애니메이션이 전후/좌우 비율을 알 수 있도록 정규화 전 원본 입력을 저장
        MoveInput = new Vector2(h, v);

        // dir 벡터는 월드 좌표계 기준이므로, 플레이어의 방향에 맞게 변환
        dir = transform.TransformDirection(dir);

        // 실제 이동은 물리 타이밍에 맞춰 FixedUpdate에서 처리하기 위해 저장만 해둡니다.
        moveDir = dir;

        // Space키를 누르면 점프 요청 (키 입력 감지는 프레임 단위인 Update에서 해야 안 놓칩니다)
        if (Input.GetKeyDown(KeyCode.Space) && jumpcount < jumplimit)
        {
            jumpRequested = true;
        }

        // 마우스의 X축 움직임을 받아서 저장
        mouseMoveX = Input.GetAxis("Mouse X");
    }

    void FixedUpdate()
    {
        // [벽 뚫림 방지 핵심] transform.position을 직접 바꾸는 대신 rb.MovePosition을 사용합니다.
        // MovePosition은 물리 엔진이 충돌을 판정해서 실제로 벽을 통과하지 못하게 막아줍니다.
        rb.MovePosition(rb.position + moveDir * moveSpeed * Time.fixedDeltaTime);

        if (jumpRequested)
        {
            rb.AddForce(Vector3.up * jumpPower, ForceMode.Impulse);
            jumpcount++;
            jumpRequested = false;
        }

        // 회전도 물리 스텝에 맞춰 MoveRotation으로 처리해서 충돌 판정과 엇갈리지 않게 합니다.
        Quaternion turn = Quaternion.Euler(0f, mouseMoveX * rotateSpeed * Time.fixedDeltaTime, 0f);
        rb.MoveRotation(rb.rotation * turn);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 땅에 닿으면 점프 횟수 초기화
        if (collision.gameObject.CompareTag("Ground"))
        {
            jumpcount = 0;
            isGrounded = true;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        // 땅에서 떨어지면(점프/추락) 다리 애니메이션 스크립트가 알 수 있도록 false로 전환
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }
}