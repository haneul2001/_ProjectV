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

        // --- 멀티플레이 연동 ---------------------------------------------------
    // 내 캐릭터가 아니면 NetPlayer.cs가 useLocalInput을 false로 내리고, 원격에서 받은
    // 상태를 ApplyRemoteState로 밀어넣습니다. 컴포넌트를 통째로 끄지 않는 이유는
    // LegAnimator가 아래 세 프로퍼티를 읽어서 다리를 움직이기 때문입니다.
    // 꺼버리면 상대 다리가 그 자리에 얼어붙습니다.
    [HideInInspector] public bool useLocalInput = true;

    private Vector2 localMoveInput;
    private Vector2 remoteMoveInput;
    private float remoteHorizontalSpeed;
    private bool remoteGrounded;

    public void ApplyRemoteState(Vector2 input, float speed, bool grounded)
    {
        remoteMoveInput = input;
        remoteHorizontalSpeed = speed;
        remoteGrounded = grounded;
    }

    public Vector2 MoveInput => useLocalInput ? localMoveInput : remoteMoveInput;
        public float HorizontalSpeed => useLocalInput ? moveDir.magnitude * moveSpeed : remoteHorizontalSpeed;
    public bool IsGrounded => useLocalInput
        ? (Time.time >= jumpIgnoreUntil && (Time.time - lastGroundedTime) <= GroundGrace)
        : remoteGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.freezeRotation = true; // MoveRotation으로 Y축을 "정식으로" 돌리므로 전체를 얼려도 충돌 없음

        // [주의] maxDepenetrationVelocity는 절대 낮추지 마세요.
        //
        // 이 스크립트의 이동은 rb.MovePosition으로 캡슐을 일단 벽 안쪽으로 밀어 넣고,
        // 물리 엔진의 겹침 해소(depenetration)가 다시 밀어내는 방식으로 성립합니다.
        // 즉 겹침 해소가 "벽을 막아주는 주체"이자 "경사로를 밀어 올려주는 힘"입니다.
        // 이 값을 낮추면 밀어내는 속도가 밀어 넣는 속도를 못 이겨서
        // 벽과 경사면(두께 0.2)을 그대로 통과해버립니다.
        //
        // 경사면 아랫면에 끼어 바닥으로 꺼지던 문제는 이 값이 아니라
        // 바닥 콜라이더(GameScene의 FloorCollider)에 두께를 줘서 해결했습니다.
    }

    void Update()
    {
        // 원격(상대) 캐릭터는 내 키보드/마우스를 읽으면 안 됩니다.
        // 위치와 회전은 NetworkTransform이, 애니메이션용 상태는 ApplyRemoteState가 채웁니다.
        if (!useLocalInput) return;
        float mouseMoveX = Input.GetAxisRaw("Mouse X");
        yawAccum += mouseMoveX * rotateSpeed * Time.deltaTime;

        // WASD는 원본 입력값만 저장해두고, 실제 방향 변환은 FixedUpdate에서
        // "이번 스텝에 적용될 회전"을 반영한 뒤에 계산합니다.
        rawH = Input.GetAxis("Horizontal");
        rawV = Input.GetAxis("Vertical");
        localMoveInput = new Vector2(rawH, rawV);

        // IsGrounded를 같이 봅니다. jumpcount만 보면 발판에서 걸어 내려온 직후
        // (카운터는 0인 채로 공중) 허공에서 한 번 더 뛸 수 있었습니다.
        if (Input.GetKeyDown(KeyCode.Space) && jumpcount < jumplimit && IsGrounded)
        {
            jumpRequested = true;
        }
    }

    void FixedUpdate()
    {
        if (!useLocalInput) return;
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