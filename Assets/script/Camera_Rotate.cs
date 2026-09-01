using UnityEngine;

public class Camera_Rotate : MonoBehaviour
{
    public float rotateSpeed = 100f;

    //외부 스크립트(test_had)에서 이 값을 참조해서 실시간으로 읽어갈 수 있도록 public으로 선언합니다!
    public float tempX;

    [Header("반동")]
    public float recoilSpeed = 15f;

    private float currentRecoilY = 0f;
    private float targetRecoilY = 0f;
    private float currentRecoilX = 0f;
    private float targetRecoilX = 0f;

    // [로직 설명] 이전 프레임에 좌우 회전값(Y)에 더해진 반동값을 보관해뒀다가,
    // 현재 프레임에 새 반동을 넣기 전에 이전 반동을 빼서 복원하는 로직입니다.
    // 이게 없으면 매 프레임 "현재 Y축 + 반동"을 그대로 대입해보려해서, 반동이 들어간
    // 방향대로 카메라의 원래 축 수치가 계속 깎여나가 마우스 회전이 먹통이 됩니다.
    // 이렇게 하면 반동이 튀더라도 누적 없이 마우스의 원래 방향을 정확히 찾아오며, 좌우 마우스
    // 움직임도 연산에 간섭하지 않고 정상적으로 누적되어 틀어지지 않습니다.
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

        // 상하(X)는 "마우스값(tempX) - 총들림(currentRecoilY)" 수치이며 총들림이 0이되면
        // 자연스럽게 tempX 그대로 복귀합니다.
        float finalX = tempX - currentRecoilY;

        // [수정] transform.eulerAngles.y로 오일러각을 직접 읽으면, Unity가 쿼터니언을
        // 오일러(X,Y,Z)로 "다시 계산"하는 과정에서 여러 조합이 같은 회전을 나타낼 수
        // 있어서(예: X=170,Y=180 ≒ X=-10,Y=0) 특정 각도 구간을 지날 때 표현이 갑자기
        // 다른 조합으로 튈 수 있습니다 (test_had.cs의 rawAngleY에서 고쳤던 것과 동일한
        // 원인). 카메라 자신의 Yaw를 읽을 때도 오일러각 대신, 정면 벡터를 직접 구해서
        // Atan2로 각도를 계산합니다. 이 방식은 표현이 하나로 고정되어(연속적) 튀지 않습니다.
        Vector3 worldForward = transform.forward;
        float currentYaw = Mathf.Atan2(worldForward.x, worldForward.z) * Mathf.Rad2Deg;

        // 좌우(Y)는 현재 방향을 보존합니다: 부모의 Y값에서 "이전 프레임에 들어간 반동"을
        // 빼서 원래의 회전으로 되돌린 값에, 새 프레임의 입력된 반동 값을 더합니다.
        // 이렇게 하면 반동이 튀는 중에도 원래 마우스 방향이 정확히 찾아지며, 돌면서
        // 발생하는 좌우 마우스 값과 간섭없이 부드럽게 정렬되어 틀어지지 않습니다.
        float cleanY = currentYaw - lastAppliedRecoilX;
        float finalY = cleanY + currentRecoilX;
        lastAppliedRecoilX = currentRecoilX;

        transform.eulerAngles = new Vector3(finalX, finalY, 0f);
    }

    // 총 발사 시 호출
    public void AddRecoil(float vertical, float horizontal)
    {
        targetRecoilY += vertical;
        tempX -= vertical;
        targetRecoilX += Random.Range(-horizontal, horizontal);
    }
}