using Photon.Pun;
using UnityEngine;

public class Camera_Rotate_01 : MonoBehaviour
{
    public float rotateSpeed = 100f;
    float tempX;

    [Header("반동")]
    public float recoilSpeed = 15f;

    private float currentRecoilY = 0f;
    private float targetRecoilY = 0f;
    private float currentRecoilX = 0f;
    private float targetRecoilX = 0f;

    private PhotonView ownerPhotonView;

    void Start()
    {
        // Main Camera는 player_clone의 자식이므로
        // 부모에서 PhotonView를 찾는다.
        ownerPhotonView = GetComponentInParent<PhotonView>();

        if (ownerPhotonView == null)
        {
            Debug.LogError(
                "Camera_Rotate: 부모 player_clone에서 PhotonView를 찾지 못했습니다."
            );
        }
    }

    void Update()
    {
        // 내 플레이어의 카메라가 아니면 마우스 입력을 처리하지 않는다.
        if (ownerPhotonView == null || !ownerPhotonView.IsMine)
        {
            return;
        }

        float mouseMoveY = Input.GetAxis("Mouse Y");

        // 마우스를 위로 움직이면 화면도 위로 올라가게 함
        tempX -= mouseMoveY * rotateSpeed * Time.deltaTime;
        tempX = Mathf.Clamp(
            tempX,
            -90f + currentRecoilY,
            90f + currentRecoilY
        );

        currentRecoilY = Mathf.Lerp(
            currentRecoilY,
            targetRecoilY,
            Time.deltaTime * recoilSpeed
        );

        currentRecoilX = Mathf.Lerp(
            currentRecoilX,
            targetRecoilX,
            Time.deltaTime * recoilSpeed
        );

        targetRecoilY = 0f;
        targetRecoilX = 0f;

        float finalX = tempX - currentRecoilY;

        transform.localRotation = Quaternion.Euler(
            finalX,
            currentRecoilX,
            0f
        );
    }

    public void AddRecoil(float vertical, float horizontal)
    {
        // 상대 플레이어 카메라에는 반동을 적용하지 않는다.
        if (ownerPhotonView == null || !ownerPhotonView.IsMine)
        {
            return;
        }

        targetRecoilY += vertical;
        tempX -= vertical;

        targetRecoilX += Random.Range(
            -horizontal,
            horizontal
        );
    }
}