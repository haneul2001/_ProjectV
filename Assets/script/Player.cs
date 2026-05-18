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
    void Start()
    {
        rb = GetComponent<Rigidbody>(); 
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

        // x축에는 h, z축에는 v값을 넣어서 벡터로 저장
        transform.position += dir * moveSpeed * Time.deltaTime;

        //rb.MovePosition(transform.position + dir * moveSpeed * Time.deltaTime);

        // Space키를 누르면 점프
        if (Input.GetKeyDown(KeyCode.Space) && jumpcount < jumplimit)
        {
            rb.AddForce(Vector3.up * jumpPower, ForceMode.Impulse);

            jumpcount++;
        }

        // 마우스의 X축 움직임을 받아서 저장
        float mouseMoveX = Input.GetAxis("Mouse X");

        transform.Rotate(Vector3.up * mouseMoveX * rotateSpeed * Time.deltaTime);
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
