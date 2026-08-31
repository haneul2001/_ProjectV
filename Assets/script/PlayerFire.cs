using UnityEngine;
using System.Collections;

public class PlayerFire : MonoBehaviour
{
    [Header("전투")]
    public float damage = 20f;
    public float range = 100f;

    [Header("탄약 / 재장전")]
    [Tooltip("탄창 하나에 들어가는 최대 총알 수이자, 게임 시작 시 기본으로 장전되는 총알 수")]
    public int magazineSize = 25;
    [Tooltip("현재 탄창에 장전되어 있는 총알 수 (Start에서 magazineSize로 채워짐)")]
    public int currentAmmo;
    [Tooltip("탄창 밖에 남아있는 예비 총알 수. 인스펙터에서 자유롭게 설정 가능")]
    public int totalAmmo = 250;
    public float reloadTime = 2f;         // 재장전 전체 시간 (탄약이 채워지고 다시 발사 가능해지기까지)
    public bool isReloading = false;      // 재장전 중인지 여부 (외부에서 읽기용으로 public)

    [Tooltip("왼쪽 어깨 스윙 모션 자체가 끝나는 데 걸리는 시간. reloadTime보다 짧게 잡으면 " +
             "팔은 빠르게 움직이고 남은 시간은 그냥 대기(재장전 마무리)하는 것처럼 보입니다.")]
    public float reloadMotionDuration = 0.5f;

    [Header("재장전 - 왼쪽 어깨 연동")]
    [Tooltip("왼쪽 어깨(LeftShoulder) 본을 갖고 있는 test_had 컴포넌트를 연결하세요. " +
             "재장전 중에는 test_had가 평소 하던 조준 연동 대신, 이 스크립트가 넘겨주는 " +
             "진행도(0~1)에 맞춰 왼쪽 어깨만 별도로 움직입니다.")]
    public test_had headAnim;

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

    [Header("재장전 사운드")]
    [Tooltip("재장전 시작할 때 (탄창 빼는 소리)")]
    public AudioClip reloadStartSound;
    [Tooltip("재장전 끝날 때 (새 탄창 끼우는 소리, 선택)")]
    public AudioClip reloadEndSound;

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

        currentAmmo = magazineSize;

        if (headAnim == null)
        {
            Debug.LogWarning("[PlayerFire] Head Anim이 연결되지 않았습니다! " +
                              "인스펙터에서 test_had가 붙은 캐릭터 모델 오브젝트를 Head Anim 필드에 드래그하세요. " +
                              "연결 안 하면 재장전 시 왼쪽 어깨 애니메이션이 작동하지 않습니다.");
        }
    }

    void Update()
    {
        // R키로 재장전 요청. 이미 재장전 중이거나 탄창이 꽉 차있거나 예비 탄약이 없으면 무시.
        if (Input.GetKeyDown(KeyCode.R) && !isReloading && currentAmmo < magazineSize && totalAmmo > 0)
        {
            StartCoroutine(Reload());
        }

        // 재장전 중에는 발사 입력을 아예 받지 않음
        if (isReloading) return;

        bool fireInput = isAutoFire ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);

        if (fireInput && Time.time >= nextFireTime)
        {
            // 탄약이 없으면 발사 대신 자동으로 재장전 (예비 탄약이 있을 때만)
            if (currentAmmo <= 0)
            {
                if (totalAmmo > 0)
                {
                    StartCoroutine(Reload());
                }
                else
                {
                    Debug.Log("[Ammo] 예비 탄약도 없습니다. 재장전 불가.");
                }
                return;
            }

            nextFireTime = Time.time + fireRate;
            Shoot();
        }
    }

    private IEnumerator Reload()
    {
        isReloading = true;

        Debug.Log($"[Reload] 재장전 시작 - 장전탄: {currentAmmo}/{magazineSize}, 예비탄: {totalAmmo}");

        // test_had에게 "재장전 중이니 왼쪽 어깨는 평소 조준 연동 대신 내가 주는 진행도로 움직여라" 알림
        if (headAnim != null)
        {
            headAnim.isReloading = true;
        }

        // 재장전 시작 사운드 (탄창 빼는 소리)
        if (reloadStartSound != null && audioSource != null)
        {
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(reloadStartSound);
        }

        // 매 프레임 진행도(0~1)를 계산해서 넘겨줌. Sin 곡선이라 0에서 시작해서
        // reloadMotionDuration의 절반 지점에서 1(최대로 움직인 상태)이 됐다가 다시 0으로 돌아옵니다.
        // reloadMotionDuration이 reloadTime보다 짧으면, 모션이 끝난 뒤 남은 시간은
        // reloadProgress가 0에 고정된 채로 그냥 재장전 대기 시간처럼 흘러갑니다.
        float elapsed = 0f;
        bool endSoundPlayed = false;
        while (elapsed < reloadTime)
        {
            elapsed += Time.deltaTime;
            float motionT = Mathf.Clamp01(elapsed / reloadMotionDuration);
            float progress = Mathf.Sin(motionT * Mathf.PI); // 0 -> 1 -> 0, reloadMotionDuration 안에서 완료됨

            if (headAnim != null)
            {
                headAnim.reloadProgress = progress;
            }

            // 모션이 절반 지점(팔이 가장 많이 움직인 순간)을 지나면 새 탄창 끼우는 소리 재생
            if (!endSoundPlayed && motionT >= 0.5f)
            {
                endSoundPlayed = true;
                if (reloadEndSound != null && audioSource != null)
                {
                    audioSource.pitch = 1f;
                    audioSource.PlayOneShot(reloadEndSound);
                }
            }

            yield return null;
        }

        if (headAnim != null)
        {
            headAnim.reloadProgress = 0f;
            headAnim.isReloading = false;
        }

        // 탄창 빈 부분만큼 예비 탄약에서 채워옴 (예비 탄약이 모자라면 있는 만큼만)
        int amountNeeded = magazineSize - currentAmmo;
        int amountToLoad = Mathf.Min(amountNeeded, totalAmmo);

        currentAmmo += amountToLoad;
        totalAmmo -= amountToLoad;

        isReloading = false;

        Debug.Log($"[Reload] 재장전 완료 - 장전탄: {currentAmmo}/{magazineSize}, 예비탄: {totalAmmo}");
    }

    void Shoot()
    {
        // --- 0. 탄약 소모 ---
        currentAmmo--;

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