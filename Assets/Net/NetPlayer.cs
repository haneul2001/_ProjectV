using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 플레이어 프리팹의 네트워크 두뇌.
///
/// 하는 일 3가지:
///  1) 소유권 게이팅 - 내 캐릭터가 아니면 카메라/입력/사격 스크립트를 전부 끕니다.
///     (안 그러면 상대 캐릭터가 내 마우스를 따라 움직이고, 카메라가 2개가 됩니다)
///  2) 원격 표현용 상태 동기화 - 조준 상하각, 이동 입력, 접지 여부를 보내서
///     상대 화면에서도 LegAnimator(다리)와 test_had(상체 조준)가 제대로 움직이게 합니다.
///  3) 라운드 리셋 - 서버가 시키면 스폰 지점으로 순간이동하고 탄약을 채웁니다.
/// </summary>
[RequireComponent(typeof(PlayerHealth))]
public class NetPlayer : NetworkBehaviour
{
    [Header("소유자 전용 (원격 플레이어에선 자동으로 꺼짐)")]
    public GameObject cameraRoot;          // Main Camera 오브젝트. 통째로 꺼서 Camera_Rotate/PlayerADS까지 정지시킵니다.
    public Player playerMove;
    public PlayerFire playerFire;
    public test_had headAim;
    public Camera_Rotate cameraRotate;
    public PlayerADS playerADS;

    [Tooltip("GrenadeThrower 등 소유자만 돌아야 하는 나머지 스크립트")]
    public MonoBehaviour[] extraOwnerOnly;

    [Header("씬에서 이름으로 찾아 연결할 오브젝트")]
    [Tooltip("프리팹은 씬 오브젝트를 직접 참조할 수 없어서, 소유자일 때 이름으로 찾아 PlayerADS에 꽂아줍니다.")]
    public string crosshairObjectName = "Crosshair";
    [Tooltip("플레이어가 스폰되기 전까지 게임 씬을 비춰주는 임시 카메라의 이름. " +
             "내 캐릭터가 스폰되면 자동으로 꺼집니다.")]
    public string standbyCameraName = "StandbyCamera";
    [Header("추락 복구 (안전망)")]
    [Tooltip("이 높이 아래로 떨어지면 마지막으로 땅을 밟았던 자리로 되돌립니다. " +
             "맵 바닥이 y=0이므로 넉넉하게 아래로 잡아둡니다.")]
    public float fallResetY = -8f;



    // --- 원격 표현용 동기화 값 (소유자가 쓰고 모두가 읽음) ---
    private readonly NetworkVariable<float> aimPitch = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private readonly NetworkVariable<Vector2> moveInput = new NetworkVariable<Vector2>(
        Vector2.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private readonly NetworkVariable<float> horizontalSpeed = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private readonly NetworkVariable<bool> grounded = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    // 재장전 자세도 상대 화면에 보이게 동기화합니다.
    // (PlayerFire가 test_had의 isReloading / reloadProgress를 매 프레임 채워주므로 그걸 그대로 실어보냅니다)
    private readonly NetworkVariable<bool> reloading = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private readonly NetworkVariable<float> reloadProgress = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private Rigidbody rb;
    private PlayerHealth health;
    private int lastAmmo;
    private bool inputFrozen;

    // 죽었을 때 몸을 숨기기 위해 미리 모아둡니다.
    // 카메라는 Head 본의 자식이라 렌더러만 꺼도 시야는 그대로 유지됩니다.
    private Renderer[] bodyRenderers;
    private Collider[] bodyColliders;

    // 추락 복구용. 마지막으로 땅을 밟고 있던 안전한 위치를 계속 갱신해둡니다.
    private Vector3 lastSafePosition;
    private bool hasSafePosition;

    // 원래 꺼져 있던 것(예: 루트의 캡슐 MeshRenderer, 디버그용 콜라이더)까지
    // 되살릴 때 같이 켜버리면 안 되므로, 스폰 시점의 상태를 기억해둡니다.
    private bool[] rendererWasEnabled;
    private bool[] colliderWasEnabled;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        health = GetComponent<PlayerHealth>();

        bodyRenderers = GetComponentsInChildren<Renderer>(true);
        bodyColliders = GetComponentsInChildren<Collider>(true);

        // 프리팹에 저장된 "평소 상태"를 그대로 스냅샷해둡니다.
        rendererWasEnabled = new bool[bodyRenderers.Length];
        for (int i = 0; i < bodyRenderers.Length; i++)
            rendererWasEnabled[i] = bodyRenderers[i] != null && bodyRenderers[i].enabled;

        colliderWasEnabled = new bool[bodyColliders.Length];
        for (int i = 0; i < bodyColliders.Length; i++)
            colliderWasEnabled[i] = bodyColliders[i] != null && bodyColliders[i].enabled;
    }

    /// <summary>
    /// 시체를 치웁니다. 죽으면 몸과 콜라이더를 감추고, 라운드가 다시 시작되면 되돌립니다.
    /// 모든 클라이언트에서 실행되어야 하므로 IsDead의 OnValueChanged로 걸어둡니다.
    /// </summary>
    private void SetBodyVisible(bool visible)
    {
        // 되살릴 때는 "원래 켜져 있던 것"만 켭니다.
        // 루트의 캡슐 MeshRenderer처럼 처음부터 꺼져 있던 건 계속 꺼진 채로 둡니다.
        if (bodyRenderers != null)
        {
            for (int i = 0; i < bodyRenderers.Length; i++)
            {
                if (bodyRenderers[i] == null) continue;
                bodyRenderers[i].enabled = visible && rendererWasEnabled[i];
            }
        }

        // 시체에 총알이 막히거나 몸이 걸리는 걸 막습니다.
        if (bodyColliders != null)
        {
            for (int i = 0; i < bodyColliders.Length; i++)
            {
                if (bodyColliders[i] == null) continue;
                bodyColliders[i].enabled = visible && colliderWasEnabled[i];
            }
        }

        // 콜라이더를 끄면 발밑 바닥까지 사라져서 시체가 맵 아래로 떨어집니다.
        // 죽어있는 동안에는 물리를 멈춰서 그 자리에 그대로 두고,
        // 살아날 때 다시 물리를 켭니다. (원격 캐릭터는 원래부터 kinematic이라 건드리지 않습니다)
        if (rb != null && IsOwner)
        {
            if (!visible)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            rb.isKinematic = !visible;
        }
    }

    private void OnDeadChanged(bool wasDead, bool isDead)
    {
        SetBodyVisible(!isDead);
    }

    public override void OnNetworkSpawn()
    {
        bool owner = IsOwner;

        // 1. 카메라 - 내 것만 켭니다. 카메라 오브젝트에 Camera_Rotate / PlayerADS /
        //    AudioListener가 다 붙어있어서 통째로 끄면 한 번에 정리됩니다.
        if (cameraRoot != null) cameraRoot.SetActive(owner);

        // 2. 이동 - 컴포넌트를 끄지 않고 "입력만" 막습니다.
        //    LegAnimator가 Player.cs에서 속도/접지 값을 읽어가기 때문에,
        //    컴포넌트 자체가 죽으면 상대 다리가 얼어붙습니다.
        if (playerMove != null) playerMove.useLocalInput = owner;

        // 3. 상체 조준 - test_had는 Camera.main을 참조하는데, 원격 플레이어 입장에서
        //    Camera.main은 "내" 카메라라서 상대 몸이 내 시선을 따라옵니다.
        //    원격일 땐 네트워크로 받은 각도를 쓰도록 전환합니다.
        if (headAim != null) headAim.useNetworkAim = !owner;

        // 4. 사격 / 수류탄 - 원격에선 완전히 정지
        if (playerFire != null) playerFire.enabled = owner;
        if (extraOwnerOnly != null)
        {
            foreach (var mb in extraOwnerOnly)
                if (mb != null) mb.enabled = owner;
        }

        // 5. 물리 - 원격 캐릭터는 NetworkTransform이 위치를 끌고 오므로
        //    물리 시뮬레이션이 끼어들면 서로 싸웁니다.
        if (rb != null)
        {
            rb.isKinematic = !owner;
            rb.interpolation = owner ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
        }

        if (owner)
        {
            HookUpSceneReferences();
            if (playerFire != null) lastAmmo = playerFire.currentAmmo;

            // 저장해둔 마우스 감도를 내 캐릭터에만 적용합니다.
            GameSettings.ApplyTo(playerMove, cameraRotate);

            lastSafePosition = transform.position;
            hasSafePosition = true;

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        // 6. 사망 시 몸 숨김 / 라운드 재시작 시 다시 표시
        if (health != null)
        {
            health.IsDead.OnValueChanged += OnDeadChanged;
            SetBodyVisible(!health.IsDead.Value);
        }

        gameObject.name = owner ? "player (LOCAL)" : "player (REMOTE)";
    }

    public override void OnNetworkDespawn()
    {
        if (health != null) health.IsDead.OnValueChanged -= OnDeadChanged;
    }

    /// <summary>
    /// 프리팹에는 씬 오브젝트 참조를 저장할 수 없으므로, 소유자일 때 런타임에 찾아 연결합니다.
    /// </summary>
    private void HookUpSceneReferences()
    {
        if (playerADS != null && playerADS.crosshair == null && !string.IsNullOrEmpty(crosshairObjectName))
        {
            var found = GameObject.Find(crosshairObjectName);
            if (found != null) playerADS.crosshair = found;
        }

        // 내 캐릭터가 스폰되기 전까지 게임 씬을 비춰주던 대기용 카메라를 끕니다.
        // (이게 없으면 스폰 전에 화면이 새까맣게 나옵니다)
        if (!string.IsNullOrEmpty(standbyCameraName))
        {
            var standby = GameObject.Find(standbyCameraName);
            if (standby != null) standby.SetActive(false);
        }
    }

    void Update()
    {
        if (IsOwner) OwnerUpdate();
        else RemoteUpdate();
    }

    private void OwnerUpdate()
    {
        // 죽었거나, 라운드 사이거나, ESC 설정 창이 열려있으면 조작을 막습니다.
        bool shouldFreeze = health != null && health.IsDead.Value;
        var round = RoundManager.Instance;
        if (round != null && !round.RoundActive.Value) shouldFreeze = true;
        if (PauseMenu.IsOpen) shouldFreeze = true;

        if (shouldFreeze != inputFrozen)
        {
            inputFrozen = shouldFreeze;
            if (playerMove != null) playerMove.useLocalInput = !shouldFreeze;
            if (playerFire != null) playerFire.enabled = !shouldFreeze;

            // 시야 회전도 같이 멈춥니다. 안 그러면 설정 창에서 마우스를 움직일 때
            // 뒤에서 화면이 같이 돌아갑니다.
            if (cameraRotate != null) cameraRotate.enabled = !shouldFreeze;

            if (extraOwnerOnly != null)
            {
                foreach (var mb in extraOwnerOnly)
                    if (mb != null) mb.enabled = !shouldFreeze;
            }
        }

        CheckFall();

        // 원격 표현용 값 송출
        if (cameraRotate != null) aimPitch.Value = cameraRotate.tempX;

        // 재장전 상태. PlayerFire가 headAim에 써넣은 값을 그대로 중계합니다.
        if (headAim != null)
        {
            if (headAim.isReloading && !reloading.Value) ReloadSoundRpc();
            reloading.Value = headAim.isReloading;
            reloadProgress.Value = headAim.reloadProgress;
        }

        if (playerMove != null)
        {
            moveInput.Value = playerMove.MoveInput;
            horizontalSpeed.Value = playerMove.HorizontalSpeed;
            grounded.Value = playerMove.IsGrounded;
        }

        // 총알이 줄어든 프레임 = 방금 쐈다. 상대에게 총구 화염/소리를 재생시킵니다.
        // (PlayerFire.cs를 건드리지 않고 발사를 감지하기 위한 방법)
        if (playerFire != null)
        {
            if (playerFire.currentAmmo < lastAmmo) FireEffectRpc();
            lastAmmo = playerFire.currentAmmo;
        }
    }

    /// <summary>
    /// 맵 밖으로 떨어졌을 때 마지막 안전 지점으로 되돌립니다.
    ///
    /// 경사면 아랫면에 끼어서 바닥을 뚫는 문제는 바닥 콜라이더에 두께를 주고
    /// maxDepenetrationVelocity를 낮춰서 막았지만, 물리는 언제든 예상 밖으로
    /// 동작할 수 있어서 마지막 방어선을 하나 둡니다.
    /// 이게 없으면 한 번 뚫렸을 때 라운드가 끝날 때까지 무한히 떨어집니다.
    /// </summary>
    private void CheckFall()
    {
        // 땅을 밟고 있고 정상 상태일 때의 위치를 계속 기억해둡니다.
        if (!inputFrozen && playerMove != null && playerMove.IsGrounded &&
            transform.position.y > fallResetY)
        {
            lastSafePosition = transform.position;
            hasSafePosition = true;
            return;
        }

        if (transform.position.y >= fallResetY) return;

        Vector3 target = hasSafePosition
            ? lastSafePosition + Vector3.up * 0.5f
            : transform.position + Vector3.up * 10f;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = target;
        }
        transform.position = target;

        Debug.LogWarning("[NetPlayer] 맵 밖으로 떨어져 마지막 안전 지점으로 복구했습니다.");
    }

    private void RemoteUpdate()
    {
        if (playerMove != null)
            playerMove.ApplyRemoteState(moveInput.Value, horizontalSpeed.Value, grounded.Value);

        if (headAim != null)
        {
            headAim.netAngleX = aimPitch.Value;
            headAim.netAngleY = 0f;

            // 재장전 자세 재생. test_had가 이 두 값만 보고 왼쪽 어깨를 움직이므로,
            // 값만 넣어주면 상대 화면에서도 똑같은 동작이 나옵니다.
            headAim.isReloading = reloading.Value;
            headAim.reloadProgress = reloadProgress.Value;
        }
    }

    // ------------------------------------------------------------------
    // 발사 이펙트 복제
    // ------------------------------------------------------------------
    [Rpc(SendTo.NotOwner)]
    private void FireEffectRpc()
    {
        if (playerFire == null) return;

        if (playerFire.muzzleFlashPrefab != null && playerFire.muzzlePoint != null)
        {
            var flash = Instantiate(playerFire.muzzleFlashPrefab,
                playerFire.muzzlePoint.position, playerFire.muzzlePoint.rotation);
            flash.transform.SetParent(playerFire.muzzlePoint);
            Destroy(flash, 0.2f);
        }

        var src = playerFire.GetComponent<AudioSource>();
        if (src != null && playerFire.fireSounds != null && playerFire.fireSounds.Length > 0)
        {
            var clip = playerFire.fireSounds[Random.Range(0, playerFire.fireSounds.Length)];
            if (clip != null)
            {
                src.pitch = Random.Range(0.9f, 1.1f);
                src.PlayOneShot(clip);
            }
        }
    }

    /// <summary>
    /// 상대 화면에서도 재장전 소리가 들리게 합니다.
    /// PlayerFire의 reloadStartSound / reloadEndSound를 그대로 빌려 씁니다.
    /// </summary>
    [Rpc(SendTo.NotOwner)]
    private void ReloadSoundRpc()
    {
        if (playerFire == null) return;

        var src = playerFire.GetComponent<AudioSource>();
        if (src == null) return;

        if (playerFire.reloadStartSound != null)
        {
            src.pitch = 1f;
            src.PlayOneShot(playerFire.reloadStartSound);
        }

        if (playerFire.reloadEndSound != null)
            StartCoroutine(PlayReloadEndSoundDelayed(src));
    }

    private System.Collections.IEnumerator PlayReloadEndSoundDelayed(AudioSource src)
    {
        // 소유자 쪽은 재장전 모션이 절반쯤 진행됐을 때 탄창 삽입음을 냅니다. 타이밍을 맞춥니다.
        yield return new WaitForSeconds(playerFire.reloadMotionDuration * 0.5f);

        if (src == null || playerFire == null || playerFire.reloadEndSound == null) yield break;
        src.pitch = 1f;
        src.PlayOneShot(playerFire.reloadEndSound);
    }


    // ------------------------------------------------------------------
    // 라운드 리셋 (서버 -> 소유자)
    // ------------------------------------------------------------------
    /// <summary>
    /// 클라이언트 권위 NetworkTransform이라 서버가 위치를 직접 못 바꿉니다.
    /// 그래서 소유자에게 "여기로 가라"고 시킵니다.
    /// </summary>
    [Rpc(SendTo.Owner)]
    public void RespawnRpc(Vector3 position, float yaw)
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));

        if (rb != null)
        {
            rb.position = position;
            rb.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        // 리스폰 지점을 새로운 안전 지점으로 삼습니다.
        lastSafePosition = position;
        hasSafePosition = true;

        // 카메라 상하각도 정면으로 초기화
        if (cameraRotate != null) cameraRotate.tempX = 0f;

        // 탄약 완충
        if (playerFire != null)
        {
            playerFire.currentAmmo = playerFire.magazineSize;
            lastAmmo = playerFire.currentAmmo;
        }
    }
}
