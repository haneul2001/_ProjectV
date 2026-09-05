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

    // 접지 판정. 태그도 레이어도 안 봅니다. 닿은 면이 나를 위로 떠받치고
    // 있는지(법선이 위쪽인지)만 보므로 경사로든 조각난 바닥이든 그냥 됩니다.
    private const float GroundNormalThreshold = 0.4f; // 이보다 눕은 면은 벽으로 취급
    private const float GroundGrace = 0.15f;          // 접촉이 잠깐 끊겨도 버티는 시간
    private const float JumpIgnoreTime = 0.1f;        // 점프 직후 강제 공중 시간
    private float lastGroundedTime = -999f;
    private float jumpIgnoreUntil;

    public Vector2 MoveInput { get; private set; }
    public float HorizontalSpeed => moveDir.magnitude * moveSpeed;
    public bool IsGrounded =>
        Time.time >= jumpIgnoreUntil && (Time.time - lastGroundedTime) <= GroundGrace;

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

        // IsGrounded를 같이 봅니다. jumpcount만 보면 발판에서 걸어 내려온 직후
        // (카운터는 0인 채로 공중) 허공에서 한 번 더 뛸 수 있었습니다.
        if (Input.GetKeyDown(KeyCode.Space) && jumpcount < jumplimit && IsGrounded)
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

            // 점프한 순간 바로 공중 상태로. 안 그러면 아직 발이 바닥에 걸쳐 있어서
            // 점프 애니메이션이 늦게 나옵니다.
            lastGroundedTime = -999f;
            jumpIgnoreUntil = Time.time + JumpIgnoreTime;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        EvaluateGroundContacts(collision);
    }

    private void OnCollisionExit(Collision collision)
    {
        // 원본에선 여기서 isGrounded = false를 했지만, 바닥이 여러 조각이면
        // 다음 조각의 Enter보다 이전 조각의 Exit이 먼저 와서 한 프레임씩
        // 공중으로 튀었습니다. 지금은 GroundGrace 시간이 알아서 처리하므로
        // 여기서 할 일이 없습니다. 순서 유지를 위해 자리만 남겨둡니다.
    }

    // 매 물리 스텝마다 접촉을 갱신합니다. Enter만으로는 가만히 서 있을 때
    // 갱신이 안 됩니다.
    private void OnCollisionStay(Collision collision)
    {
        EvaluateGroundContacts(collision);
    }

    // 닿은 면 중 하나라도 나를 위로 떠받치면 땅. 벽(법선이 수평)은 제외.
    private void EvaluateGroundContacts(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y > GroundNormalThreshold)
            {
                lastGroundedTime = Time.time;
                jumpcount = 0;
                return;
            }
        }
    }
}