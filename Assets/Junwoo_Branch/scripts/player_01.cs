using Photon.Pun; // Photon 기능 사용
using UnityEngine;

public class player_01 : MonoBehaviourPun
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

        if (!photonView.IsMine)
        {
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // 내 플레이어만 입력 처리
        if (!photonView.IsMine)
        {
            return;
        }

        // 입력값 읽기
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // ★ 입력 확인 로그
        if (h != 0f || v != 0f)
        {
            Debug.Log(
                $"조작된 객체={gameObject.name}, " +
                $"ViewID={photonView.ViewID}, " +
                $"Owner={photonView.OwnerActorNr}, " +
                $"IsMine={photonView.IsMine}, " +
                $"위치={transform.position}"
            );
        }

        // 이동 방향 계산
        Vector3 dir = new Vector3(h, 0f, v);

        if (dir.sqrMagnitude > 1f)
        {
            dir.Normalize();
        }

        dir = transform.TransformDirection(dir);

        // 이동
        transform.position += dir * moveSpeed * Time.deltaTime;

        // 점프
        if (Input.GetKeyDown(KeyCode.Space) &&
            jumpcount < jumplimit)
        {
            rb.AddForce(
                Vector3.up * jumpPower,
                ForceMode.Impulse
            );

            jumpcount++;
        }

        // 회전
        float mouseMoveX = Input.GetAxis("Mouse X");

        transform.Rotate(
            Vector3.up *
            mouseMoveX *
            rotateSpeed *
            Time.deltaTime
        );
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!photonView.IsMine)
        {
            return;
        }

        if (collision.gameObject.CompareTag("Ground"))
        {
            jumpcount = 0;
        }
    }
}