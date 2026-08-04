using UnityEngine;

public class PlayerFire : MonoBehaviour
{
    public GameObject shootEffectPref;

    [Header("�ݵ�")]
    public float recoilVertical = 5f;
    public float recoilHorizontal = 1f;

    [Header("����")]
    public bool isAutoFire = true;       // true = ����, false = �ܹ�
    public float fireRate = 0.1f;        // �߻� ���� (��) �������� ����
    private float nextFireTime = 0f;

    [Header("����")]
    public AudioClip[] fireSounds;       // ���� ���� �ѼҸ��� ���� �迭 [�����]
    private AudioSource audioSource;

    private Camera_Rotate cameraRotate;
    private Ammo ammo;

    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        cameraRotate = Camera.main.GetComponent<Camera_Rotate>();

        ammo = GetComponent<Ammo>(); // ź�� ��ũ��Ʈ

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void Update()
    {
        // ���� = ������ ���� / �ܹ� = Ŭ���� ��
        bool fireInput = isAutoFire ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);

        if (fireInput && Time.time >= nextFireTime)
        {
            bool canFire = (ammo == null)|| ammo.Use();

            if(canFire){
                nextFireTime = Time.time + fireRate;
                Shoot(); 
            }
            else{
                ammo.TryReload();
            }
        }
    }

    void Shoot()
    {
        // 1. ��ϵ� �ѼҸ��� 1�� �̻��� ���� ����
        if (fireSounds.Length > 0)
        {
            // 2. 0������ �迭�� ������ ��ȣ �� �ϳ��� �������� ����
            int randomIndex = Random.Range(0, fireSounds.Length);
            AudioClip selectedSound = fireSounds[randomIndex];

            // 3. (����) �Ҹ��� ������(Pitch)�� �� ������ ���� �̼��ϰ� �������� ����!
            // �̷��� �ϸ� ���� �Ҹ��� ���͵� �ٸ� �Ҹ�ó�� ����� �� �����մϴ�.
            audioSource.pitch = Random.Range(0.9f, 1.1f);

            // 4. ���� ���� ���
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