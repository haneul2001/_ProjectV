using UnityEngine;

public class Camera_Rotate : MonoBehaviour
{
    public float rotateSpeed = 100f;

    //�ܺ� ��ũ��Ʈ(test_had)���� �� ���� ���ذ��� �ǽð����� �о �� �ֵ��� public���� �������ϴ�!
    public float tempX;

    [Header("�ݵ�")]
    public float recoilSpeed = 15f;

    private float currentRecoilY = 0f;
    private float targetRecoilY = 0f;
    private float currentRecoilX = 0f;
    private float targetRecoilX = 0f;

    // [���� ����] ���� �����ӿ� �¿� ȸ����(Y)�� ����� �ݵ����� ����ص״ٰ�,
    // ���� �����ӿ� �� �ݵ��� ��� ���� ���� ������ ���� ���Դϴ�.
    // �̰� ������ �� ������ "���� Y�� + �ݵ�"�� �״�� �����ع�����, �ݵ��� �پ���
    // �������� �������� ������ ���� �������� ���ƿ��� ���ϰ� ���������� ���Դϴ�.
    private float lastAppliedRecoilX = 0f;

    void Update()
    {
        float mouseMoveY = Input.GetAxisRaw("Mouse Y");
        tempX -= mouseMoveY * rotateSpeed * Time.deltaTime;
        tempX = Mathf.Clamp(tempX, -45f + currentRecoilY, 45f + currentRecoilY);

        currentRecoilY = Mathf.Lerp(currentRecoilY, targetRecoilY, Time.deltaTime * recoilSpeed);
        currentRecoilX = Mathf.Lerp(currentRecoilX, targetRecoilX, Time.deltaTime * recoilSpeed);
        targetRecoilY = 0f;
        targetRecoilX = 0f;

        // ����(X)�� "���ذ�(tempX) - ��鸲(currentRecoilY)" ������ ��鸲�� 0����
        // �پ��� �ڵ����� tempX �״�� �����մϴ�.
        float finalX = tempX - currentRecoilY;

        // �¿�(Y)�� ���� ������ ����ϴ�: ����� Y������ "���� �����ӿ� ����� �ݵ���"��
        // ���� ������ ������ ���Ⱚ�� ���� ��, �� ���� �̹� ������ �ݵ��� ���� ����ϴ�.
        // �̷��� �ϸ� �ݵ��� ��Ƶ� �� ���� ���� �������� ��Ȯ�� ���ƿ���, ���� ��鼭
        // �������� �¿� ������ �����ϰ� �����Ǿ� Ʋ������ �ʽ��ϴ�.
        float cleanY = transform.eulerAngles.y - lastAppliedRecoilX;
        float finalY = cleanY + currentRecoilX;
        lastAppliedRecoilX = currentRecoilX;

        transform.eulerAngles = new Vector3(finalX, finalY, 0f);
    }

    // �� �߻� �� ȣ��
    public void AddRecoil(float vertical, float horizontal)
    {
        targetRecoilY += vertical;
        tempX -= vertical;
        targetRecoilX += Random.Range(-horizontal, horizontal);
    }
}