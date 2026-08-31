using UnityEngine;

public class Camera_Rotate : MonoBehaviour
{
    public float rotateSpeed = 100f;

    //외부 스크립트(test_had)에서 이 상하 조준값을 실시간으로 읽어갈 수 있도록 public으로 열었습니다!
    public float tempX;

    [Header("반동")]
    public float recoilSpeed = 15f;

    private float currentRecoilY = 0f;
    private float targetRecoilY = 0f;
    private float currentRecoilX = 0f;
    private float targetRecoilX = 0f;

    // [버그 수정] 지난 프레임에 좌우 회전값(Y)에 얹었던 반동분을 기억해뒀다가,
    // 다음 프레임에 새 반동을 얹기 전에 먼저 빼내기 위한 값입니다.
    // 이게 없으면 매 프레임 "현재 Y값 + 반동"을 그대로 저장해버려서, 반동이 줄어드는
    // 과정에서 더해졌던 값들이 원래 방향으로 돌아오지 못하고 영구적으로 쌓입니다.
    private float lastAppliedRecoilX = 0f;

    void Update()
    {
        float mouseMoveY = Input.GetAxis("Mouse Y");
        tempX -= mouseMoveY * rotateSpeed * Time.deltaTime;
        tempX = Mathf.Clamp(tempX, -45f + currentRecoilY, 45f + currentRecoilY);

        currentRecoilY = Mathf.Lerp(currentRecoilY, targetRecoilY, Time.deltaTime * recoilSpeed);
        currentRecoilX = Mathf.Lerp(currentRecoilX, targetRecoilX, Time.deltaTime * recoilSpeed);
        targetRecoilY = 0f;
        targetRecoilX = 0f;

        // 상하(X)는 "조준값(tempX) - 흔들림(currentRecoilY)" 구조라서 흔들림이 0으로
        // 줄어들면 자동으로 tempX 그대로 복귀합니다.
        float finalX = tempX - currentRecoilY;

        // 좌우(Y)도 같은 원리로 맞춥니다: 저장된 Y값에서 "지난 프레임에 얹었던 반동분"을
        // 먼저 제거해 순수한 방향값을 구한 뒤, 그 위에 이번 프레임 반동만 새로 얹습니다.
        // 이렇게 하면 반동이 잦아들 때 원래 보던 방향으로 정확히 돌아오고, 총을 쏘면서
        // 움직여도 좌우 방향이 랜덤하게 누적되어 틀어지지 않습니다.
        float cleanY = transform.eulerAngles.y - lastAppliedRecoilX;
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