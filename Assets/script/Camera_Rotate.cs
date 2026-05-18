using UnityEngine;

public class Camera_Rotate : MonoBehaviour
{
    public float rotateSpeed = 100f;
    float tempX;
     void Update()
    {
        // 마우스의 X축 움직임을 받아서 저장
        float mouseMoveY = Input.GetAxis("Mouse Y");
        transform.Rotate(-mouseMoveY * rotateSpeed * Time.deltaTime, 0, 0);

        if (transform.eulerAngles.x > 180)
        {
            tempX = transform.eulerAngles.x - 360;
        }
        else
        {
            tempX = transform.eulerAngles.x;
        }

        tempX = Mathf.Clamp(tempX, -30f, 30f);

        transform.eulerAngles = new Vector3(tempX, transform.eulerAngles.y, transform.eulerAngles.z);
    }
}
