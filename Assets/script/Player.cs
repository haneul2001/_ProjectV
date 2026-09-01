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

    // Rigidbody가 붙은 오브젝트는 transform을 직접 수정해도 물리 엔진이 매
    // FixedUpdate마다 자기 계산값으로 덮어써버립니다 (non-kinematic Rigidbody의
    // 기본 동작 - test_had가 다루는 카메라/본은 Rigidbody가 없어서 직접 대입이
    // 통했지만, Player는 다릅니다). 그래서 회전은 rb.MoveRotation으로 물리 엔진에
    // "정식으로" 알려줘야 유지됩니다. Update에서 회전량만 누적해뒀다가
    // FixedUpdate에서 이동과 같은 타이밍에 한 번에 적용합니다.
    private float yawAccum = 0f;
    private float rawH = 0f;
    private float rawV = 0f;

    private Vector3 moveDir;
    private bool jumpRequested;

    private bool isGrounded = true;

    public Vector2 MoveInput { get; private set; }
    public float HorizontalSpeed => moveDir.magnitude * moveSpeed;
    public bool IsGrounded => isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.freezeRotation = true; // MoveRotation으로 Y축을 "정식으로" 돌리므로 전체를 얼려도 충돌 없음
    }

    void Update()
    {
        float mouseMoveX = Input.GetAxisRaw("Mouse X");
        yawAccum += mouseMoveX * rotateSpeed * Time.deltaTime;

        // WASD는 원본 입력값만 저장해두고, 실제 방향 변환은 FixedUpdate에서
        // "이번 스텝에 적용될 회전"을 반영한 뒤에 계산합니다.
        rawH = Input.GetAxis("Horizontal");
        rawV = Input.GetAxis("Vertical");
        MoveInput = new Vector2(rawH, rawV);

        if (Input.GetKeyDown(KeyCode.Space) && jumpcount < jumplimit)
        {
            jumpRequested = true;
        }
    }

    void FixedUpdate()
    {
        // 1. 회전을 먼저 적용합니다.
        if (yawAccum != 0f)
        {
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, yawAccum, 0f));
            yawAccum = 0f;
        }

        // 2. [수정] 방금 적용된 "이번 스텝의 최신" 회전(rb.rotation) 기준으로
        //    이동 방향을 계산합니다. 예전엔 Update에서 미리 계산해둔(한 박자 전
        //    회전 기준) 방향을 그대로 썼는데, 그러면 이동이 회전보다 항상 한 물리
        //    스텝만큼 뒤처져서 원을 그리며 움직일 때 살짝 어긋났습니다. 이제는
        //    같은 스텝 안에서 완전히 일치합니다.
        Vector3 localDir = new Vector3(rawH, 0f, rawV);
        localDir.Normalize();
        Vector3 worldDir = rb.rotation * localDir;
        moveDir = worldDir;

        rb.MovePosition(rb.position + worldDir * moveSpeed * Time.fixedDeltaTime);

        if (jumpRequested)
        {
            rb.AddForce(Vector3.up * jumpPower, ForceMode.Impulse);
            jumpcount++;
            jumpRequested = false;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            jumpcount = 0;
            isGrounded = true;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }
}
