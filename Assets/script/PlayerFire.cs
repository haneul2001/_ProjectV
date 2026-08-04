using Photon.Pun;          // [추가] Photon 네트워크 기능 사용
using UnityEngine;

public class PlayerFire : MonoBehaviourPun   // [변경] MonoBehaviour → MonoBehaviourPun
{
    public GameObject shootEffectPref;

    [Header("반동")]
    public float recoilVertical = 5f;
    public float recoilHorizontal = 1f;

    [Header("연사")]
    public bool isAutoFire = true;       // true = 연사, false = 단발
    public float fireRate = 0.1f;        // 발사 간격 (초) 낮을수록 빠름
    private float nextFireTime = 0f;

    [Header("사운드")]
    public AudioClip[] fireSounds;       // 여러 개의 총소리를 담을 배열
    private AudioSource audioSource;

    private Camera_Rotate cameraRotate;

    void Start()
    {
        // [추가 ①]
        // 자신의 플레이어가 아니면 초기 설정을 하지 않는다.
        if (!photonView.IsMine)
        {
            return;
        }

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        cameraRotate = Camera.main.GetComponent<Camera_Rotate>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void Update()
    {
        // [추가 ②]
        // 자신의 플레이어만 발사 입력을 처리한다.
        if (!photonView.IsMine)
        {
            return;
        }

        // 연사 = 누르는 동안 / 단발 = 클릭할 때
        bool fireInput = isAutoFire
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
        // 1. 등록된 총소리가 1개 이상일 때만 실행
        if (fireSounds.Length > 0)
        {
            // 2. 0번부터 배열의 마지막 번호 중 하나를 랜덤으로 뽑음
            int randomIndex = Random.Range(0, fireSounds.Length);
            AudioClip selectedSound = fireSounds[randomIndex];

            // 3. 소리의 Pitch를 약간 랜덤하게 변경
            audioSource.pitch = Random.Range(0.9f, 1.1f);

            // 4. 선택된 총소리 재생
            audioSource.PlayOneShot(selectedSound);
        }

        Ray ray = Camera.main.ViewportPointToRay(new Vector2(0.5f, 0.5f));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            // 총알 자국 이펙트 생성
            GameObject shootEffect = Instantiate(
                shootEffectPref,
                hit.point + hit.normal * 0.01f,
                Quaternion.LookRotation(hit.normal));

            shootEffect.transform.SetParent(hit.transform);
        }

        // 반동 적용
        cameraRotate.AddRecoil(recoilVertical, recoilHorizontal);
    }
}