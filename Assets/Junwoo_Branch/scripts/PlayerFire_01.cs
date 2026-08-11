using Photon.Pun;
using UnityEngine;

public class PlayerFire_01 : MonoBehaviourPun
{
    [Header("전투")]
    public float damage = 20f;
    public float range = 100f;

    [Header("시각 효과 (VFX)")]
    public GameObject muzzleFlashPrefab; // 총구 화염 프리팹
    public Transform muzzlePoint;        // 총구 위치

    [Header("반동")]
    public float recoilVertical = 5f;
    public float recoilHorizontal = 1f;

    [Header("연사")]
    public bool isAutoFire = true;
    public float fireRate = 0.1f;
    private float nextFireTime = 0f;

    [Header("사운드")]
    public AudioClip[] fireSounds;
    private AudioSource audioSource;

    private Camera_Rotate_01 cameraRotate;
    private Camera playerCamera;

    void Start()
    {
        // 상대방 총소리도 들을 수 있어야 하므로
        // AudioSource는 모든 player_clone에서 준비
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // 내 플레이어가 아니면 입력/카메라 설정은 하지 않음
        if (!photonView.IsMine)
        {
            return;
        }

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // 내 player_clone 내부의 카메라 찾기
        playerCamera = GetComponentInChildren<Camera>(true);

        if (playerCamera != null)
        {
            cameraRotate =
                playerCamera.GetComponent<Camera_Rotate_01>();
        }
    }

    void Update()
    {
        // 내 플레이어만 발사 입력 처리
        if (!photonView.IsMine)
        {
            return;
        }

        // 연사 = 누르는 동안 / 단발 = 클릭할 때
        bool fireInput =
            isAutoFire
            ? Input.GetMouseButton(0)
            : Input.GetMouseButtonDown(0);

        if (fireInput && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            Shoot();
        }
    }

    void Shoot()
    {
        // 1. 모든 플레이어 화면에서
        // 총구 화염 + 총소리 재생
        photonView.RPC(
            nameof(RPC_PlayFireEffect),
            RpcTarget.All
        );

        if (playerCamera == null)
        {
            return;
        }

        // 2. 기존 방식 그대로 화면 중앙에서 Raycast 발사
        Ray ray =
            playerCamera.ViewportPointToRay(
                new Vector2(0.5f, 0.5f)
            );

        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, range))
        {
            // 3. 기존 SurfaceManager 임팩트 처리 유지
            if (SurfaceManager.Instance != null)
            {
                SurfaceManager.Instance.PlayImpact(hit);
            }

            // 4. 맞은 대상의 PhotonView 확인
            PhotonView targetView =
                hit.collider.GetComponentInParent<PhotonView>();

            if (targetView != null)
            {
                // 네트워크 플레이어라면
                // RPC로 모든 클라이언트에 데미지 전달
                targetView.RPC(
                    "RPC_TakeDamage",
                    RpcTarget.All,
                    damage
                );
            }
            else
            {
                // PhotonView가 없는 일반 오브젝트라면
                // 기존 IDamageable 방식 사용
                IDamageable target =
                    hit.collider.GetComponent<IDamageable>();

                if (target != null)
                {
                    target.TakeDamage(damage);
                }
            }
        }

        // 5. 반동은 총을 쏜 사람에게만 적용
        if (cameraRotate != null)
        {
            cameraRotate.AddRecoil(
                recoilVertical,
                recoilHorizontal
            );
        }
    }

    // 모든 클라이언트에서 실행되는 총 발사 효과
    [PunRPC]
    void RPC_PlayFireEffect()
    {
        // --- 1. 총소리 재생 ---
        if (fireSounds != null &&
            fireSounds.Length > 0)
        {
            int randomIndex =
                Random.Range(
                    0,
                    fireSounds.Length
                );

            audioSource.pitch =
                Random.Range(0.9f, 1.1f);

            audioSource.PlayOneShot(
                fireSounds[randomIndex]
            );
        }

        // --- 2. 총구 화염 생성 ---
        if (muzzleFlashPrefab != null &&
            muzzlePoint != null)
        {
            GameObject flash =
                Instantiate(
                    muzzleFlashPrefab,
                    muzzlePoint.position,
                    muzzlePoint.rotation
                );

            // 총을 움직여도 총구 화염이 따라오도록 설정
            flash.transform.SetParent(muzzlePoint);

            // 잠시 후 삭제
            Destroy(flash, 0.2f);
        }
    }
}