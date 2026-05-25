using UnityEngine;

public class PlayerFire : MonoBehaviour
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
    public AudioClip[] fireSounds;       // 여러 개의 총소리를 담을 배열 [변경됨]
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
        // 연사 = 누르는 동안 / 단발 = 클릭할 때
        bool fireInput = isAutoFire ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);

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

            // 3. (꿀팁) 소리의 높낮이(Pitch)를 쏠 때마다 아주 미세하게 랜덤으로 변경!
            // 이렇게 하면 같은 소리가 나와도 다른 소리처럼 들려서 덜 지루합니다.
            audioSource.pitch = Random.Range(0.9f, 1.1f);

            // 4. 뽑힌 사운드 재생
            audioSource.PlayOneShot(selectedSound);
        }

        Ray ray = Camera.main.ViewportPointToRay(new Vector2(0.5f, 0.5f));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            GameObject shootEffect = Instantiate(shootEffectPref, hit.point + hit.normal * 0.01f, Quaternion.LookRotation(hit.normal));
            shootEffect.transform.SetParent(hit.transform);
        }

        cameraRotate.AddRecoil(recoilVertical, recoilHorizontal);
    }
}