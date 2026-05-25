using UnityEngine;

public class Camera_Rotate : MonoBehaviour
{
    public float rotateSpeed = 100f;
    float tempX;

    [Header("¹Ýµ¿")]
    public float recoilSpeed = 15f;

    private float currentRecoilY = 0f;
    private float targetRecoilY = 0f;
    private float currentRecoilX = 0f;
    private float targetRecoilX = 0f;

    void Update()
    {
        float mouseMoveY = Input.GetAxis("Mouse Y");
        tempX -= mouseMoveY * rotateSpeed * Time.deltaTime;
        tempX = Mathf.Clamp(tempX, -90f + currentRecoilY, 90f + currentRecoilY);

        currentRecoilY = Mathf.Lerp(currentRecoilY, targetRecoilY, Time.deltaTime * recoilSpeed);
        currentRecoilX = Mathf.Lerp(currentRecoilX, targetRecoilX, Time.deltaTime * recoilSpeed);
        targetRecoilY = 0f;
        targetRecoilX = 0f;

        float finalX = tempX - currentRecoilY;
        transform.eulerAngles = new Vector3(finalX, transform.eulerAngles.y + currentRecoilX, 0f);
    }

    public void AddRecoil(float vertical, float horizontal)
    {
        targetRecoilY += vertical;
        tempX -= vertical;
        targetRecoilX += Random.Range(-horizontal, horizontal); // ÁÂ¿ì ·£´ý
    }
}