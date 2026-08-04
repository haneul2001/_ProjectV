using Photon.Pun;          // [추가] Photon 네트워크 기능 사용
using UnityEngine;

public class Player : MonoBehaviourPun   // [변경] MonoBehaviour → MonoBehaviourPun
{
    public float moveSpeed = 5f;
    public float rotateSpeed = 100f;
    public float jumpPower = 6f;

    int jumpcount = 0;
    int jumplimit = 1;

    Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // [추가 ①]
        // 자신의 플레이어가 아니면 커서 설정을 하지 않는다.
        // 상대 플레이어는 Photon이 움직여 주기 때문에 입력을 받지 않는다.
        if (!photonView.IsMine)
        {
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // [추가 ②]
        // Photon에서 가장 중요한 코드.
        // 자신의 플레이어만 키보드와 마우스 입력을 처리한다.
        // 상대 플레이어는 네트워크 동기화만 수행한다.
        if (!photonView.IsMine)
        {
            return;
        }

        // WASD 입력을 숫자로 받아서 저장
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 dir = new Vector3(h, 0, v);

        dir.Normalize();

        // dir 벡터는 월드 좌표계 기준이므로,
        // 플레이어의 방향에 맞게 변환
        dir = transform.TransformDirection(dir);

        // x축에는 h, z축에는 v값을 넣어서 벡터로 저장
        transform.position += dir * moveSpeed * Time.deltaTime;

        // Rigidbody를 사용할 경우 아래 코드로 이동 가능
        // rb.MovePosition(transform.position + dir * moveSpeed * Time.deltaTime);

        // Space키를 누르면 점프
        if (Input.GetKeyDown(KeyCode.Space) &&
            jumpcount < jumplimit)
        {
            rb.AddForce(Vector3.up * jumpPower,
                        ForceMode.Impulse);

            jumpcount++;
        }

        // 마우스의 X축 움직임을 받아서 저장
        float mouseMoveX = Input.GetAxis("Mouse X");

        transform.Rotate(
            Vector3.up *
            mouseMoveX *
            rotateSpeed *
            Time.deltaTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // [추가 ③]
        // 자신의 플레이어만 점프 횟수를 초기화한다.
        if (!photonView.IsMine)
        {
            return;
        }

        // 땅에 닿으면 점프 횟수 초기화
        if (collision.gameObject.CompareTag("Ground"))
        {
            jumpcount = 0;
        }
    }
}