using UnityEngine;

public class PlayerFire : MonoBehaviour
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

    private Camera_Rotate cameraRotate;

    void Start()
    {
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
        bool fireInput = isAutoFire ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);

        if (fireInput && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            Shoot();
        }
    }

    void Shoot()
    {
        // --- 1. 사운드 재생 ---
        if (fireSounds.Length > 0)
        {
            int randomIndex = Random.Range(0, fireSounds.Length);
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(fireSounds[randomIndex]);
        }

        // --- 2. 총구 화염 생성 ---
        if (muzzleFlashPrefab != null && muzzlePoint != null)
        {
            // 총구 위치에 화염 생성
            GameObject flash = Instantiate(muzzleFlashPrefab, muzzlePoint.position, muzzlePoint.rotation);

            // 총을 움직여도 화염이 따라오게 하려면 부모 설정 (선택사항)
            flash.transform.SetParent(muzzlePoint);

            // 화염은 아주 잠깐 보이고 사라져야 함 (0.05초 추천)
            Destroy(flash, 0.2f);
        }

        // --- 3. 레이캐스트 발사 ---
        Ray ray = Camera.main.ViewportPointToRay(new Vector2(0.5f, 0.5f));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, range))
        {
            // --- 4. 데미지 처리 ---
            IDamageable target = hit.collider.GetComponent<IDamageable>();
            if (target != null)
            {
                target.TakeDamage(damage);
            }

            // --- 5. 임팩트 이펙트 처리 (SurfaceManager) ---
            if (SurfaceManager.Instance != null)
            {
                SurfaceManager.Instance.PlayImpact(hit);
            }
        }

        // --- 6. 반동 적용 ---
        if (cameraRotate != null)
        {
            cameraRotate.AddRecoil(recoilVertical, recoilHorizontal);
        }
    }
}