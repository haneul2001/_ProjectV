using UnityEngine;

/// <summary>
/// 사운드 / 마우스 감도 설정. PlayerPrefs에 저장해서 게임을 껐다 켜도 유지됩니다.
///
/// 감도는 원래 두 군데에 나뉘어 있습니다:
///  - 좌우(Yaw)  : Player.rotateSpeed        (몸통을 돌립니다)
///  - 상하(Pitch): Camera_Rotate.rotateSpeed (카메라만 위아래로 움직입니다)
/// 그래서 설정 창에서도 두 값을 따로 조절합니다.
///
/// 감도는 "내 캐릭터"에만 적용합니다. 상대 캐릭터의 회전은 네트워크로 받아오는 것이라
/// 여기서 건드리면 안 됩니다.
/// </summary>
public static class GameSettings
{
    // --- 기본값. 프리팹에 원래 들어있던 값과 같습니다. ---
    public const float DefaultVolume = 1f;
    public const float DefaultSensitivityX = 100f;
    public const float DefaultSensitivityY = 100f;

    // --- 슬라이더 범위 ---
    public const float MinSensitivity = 20f;
    public const float MaxSensitivity = 400f;

    private const string KeyVolume = "opt_volume";
    private const string KeySensX = "opt_sens_x";
    private const string KeySensY = "opt_sens_y";

    private static bool loaded;

    private static float volume = DefaultVolume;
    private static float sensitivityX = DefaultSensitivityX;
    private static float sensitivityY = DefaultSensitivityY;

    /// <summary>전체 볼륨 (0~1)</summary>
    public static float Volume
    {
        get { Load(); return volume; }
        set
        {
            Load();
            volume = Mathf.Clamp01(value);
            AudioListener.volume = volume;
            PlayerPrefs.SetFloat(KeyVolume, volume);
        }
    }

    /// <summary>좌우 감도</summary>
    public static float SensitivityX
    {
        get { Load(); return sensitivityX; }
        set
        {
            Load();
            sensitivityX = Mathf.Clamp(value, MinSensitivity, MaxSensitivity);
            PlayerPrefs.SetFloat(KeySensX, sensitivityX);
            ApplyToLocalPlayer();
        }
    }

    /// <summary>상하 감도</summary>
    public static float SensitivityY
    {
        get { Load(); return sensitivityY; }
        set
        {
            Load();
            sensitivityY = Mathf.Clamp(value, MinSensitivity, MaxSensitivity);
            PlayerPrefs.SetFloat(KeySensY, sensitivityY);
            ApplyToLocalPlayer();
        }
    }

    private static void Load()
    {
        if (loaded) return;
        loaded = true;

        volume = PlayerPrefs.GetFloat(KeyVolume, DefaultVolume);
        sensitivityX = PlayerPrefs.GetFloat(KeySensX, DefaultSensitivityX);
        sensitivityY = PlayerPrefs.GetFloat(KeySensY, DefaultSensitivityY);

        AudioListener.volume = volume;
    }

    /// <summary>
    /// 내 캐릭터에 감도를 적용합니다.
    /// 스폰 직후(NetPlayer)와, 설정 창에서 슬라이더를 움직일 때마다 호출됩니다.
    /// </summary>
    public static void ApplyTo(Player move, Camera_Rotate look)
    {
        Load();
        if (move != null) move.rotateSpeed = sensitivityX;
        if (look != null) look.rotateSpeed = sensitivityY;
    }

    /// <summary>
    /// 지금 살아있는 내 캐릭터를 찾아 감도를 바로 반영합니다.
    /// 참조를 따로 들고 있지 않아도 되도록 씬에서 찾습니다.
    /// (설정 변경은 자주 일어나지 않으므로 비용은 문제되지 않습니다)
    /// </summary>
    private static void ApplyToLocalPlayer()
    {
        var nm = Unity.Netcode.NetworkManager.Singleton;
        if (nm == null || !nm.IsClient) return;

        var playerObject = nm.LocalClient != null ? nm.LocalClient.PlayerObject : null;
        if (playerObject == null) return;

        ApplyTo(playerObject.GetComponent<Player>(),
                playerObject.GetComponentInChildren<Camera_Rotate>(true));
    }

    /// <summary>게임이 시작될 때 저장된 볼륨을 즉시 반영합니다.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        Load();
    }
}
