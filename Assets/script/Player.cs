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
        }
    }
}