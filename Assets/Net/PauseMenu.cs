using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ESC를 눌러 여는 인게임 설정 창.
///  - 사운드 볼륨
///  - 좌우 / 상하 마우스 감도 (따로 조절)
///  - 매치 중도 포기 후 로비로 나가기
///
/// 창이 열려 있는 동안에는 IsOpen이 true가 되고, NetPlayer가 이 값을 보고
/// 이동 / 사격 / 시야 회전을 멈춥니다. 안 그러면 마우스로 슬라이더를 잡는 동안
/// 화면이 같이 돌아갑니다.
///
/// 게임 자체는 멈추지 않습니다(Time.timeScale 그대로). 멀티플레이라서
/// 내 쪽만 시간을 멈추면 상대와 어긋나기 때문입니다. 설정 창을 여는 건
/// 안전지대가 아니므로, 여는 동안에도 상대는 계속 움직이고 총알도 날아옵니다.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    /// <summary>NetPlayer가 입력을 막을지 판단할 때 읽습니다.</summary>
    public static bool IsOpen { get; private set; }

    [Header("창")]
    public GameObject panel;

    [Header("슬라이더")]
    public Slider volumeSlider;
    public Slider sensXSlider;
    public Slider sensYSlider;

    [Header("슬라이더 값 표시")]
    public TMP_Text volumeValueText;
    public TMP_Text sensXValueText;
    public TMP_Text sensYValueText;

    [Header("버튼")]
    public Button resumeButton;
    public Button leaveButton;

    void Awake()
    {
        // 씬을 다시 들어왔을 때 이전 상태가 남지 않도록 초기화합니다.
        IsOpen = false;
        if (panel != null) panel.SetActive(false);
    }

    void Start()
    {
        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.SetValueWithoutNotify(GameSettings.Volume);
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        if (sensXSlider != null)
        {
            sensXSlider.minValue = GameSettings.MinSensitivity;
            sensXSlider.maxValue = GameSettings.MaxSensitivity;
            sensXSlider.SetValueWithoutNotify(GameSettings.SensitivityX);
            sensXSlider.onValueChanged.AddListener(OnSensXChanged);
        }

        if (sensYSlider != null)
        {
            sensYSlider.minValue = GameSettings.MinSensitivity;
            sensYSlider.maxValue = GameSettings.MaxSensitivity;
            sensYSlider.SetValueWithoutNotify(GameSettings.SensitivityY);
            sensYSlider.onValueChanged.AddListener(OnSensYChanged);
        }

        if (resumeButton != null) resumeButton.onClick.AddListener(Close);
        if (leaveButton != null) leaveButton.onClick.AddListener(LeaveMatch);

        RefreshLabels();
    }

    void OnDestroy()
    {
        // 다음 씬으로 넘어갈 때 열린 상태가 남아있으면 조작이 계속 막힙니다.
        IsOpen = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsOpen) Close();
            else Open();
        }
    }

    public void Open()
    {
        IsOpen = true;
        if (panel != null) panel.SetActive(true);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // 창을 열 때마다 현재 저장값으로 맞춰줍니다.
        if (volumeSlider != null) volumeSlider.SetValueWithoutNotify(GameSettings.Volume);
        if (sensXSlider != null) sensXSlider.SetValueWithoutNotify(GameSettings.SensitivityX);
        if (sensYSlider != null) sensYSlider.SetValueWithoutNotify(GameSettings.SensitivityY);
        RefreshLabels();
    }

    public void Close()
    {
        IsOpen = false;
        if (panel != null) panel.SetActive(false);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    /// <summary>매치를 포기하고 로비로 돌아갑니다.</summary>
    public void LeaveMatch()
    {
        IsOpen = false;
        if (panel != null) panel.SetActive(false);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // MatchSession.Leave()가 세션 정리 + 매치메이킹 씬 로드까지 처리합니다.
        // 내가 호스트였다면 상대는 연결이 끊기고, 상대 쪽 MatchSession이
        // 알아서 로비로 돌려보냅니다.
        if (MatchSession.Instance != null)
            MatchSession.Instance.Leave();
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(MatchSession.LobbySceneName);
    }

    // ------------------------------------------------------------------
    private void OnVolumeChanged(float value)
    {
        GameSettings.Volume = value;
        RefreshLabels();
    }

    private void OnSensXChanged(float value)
    {
        GameSettings.SensitivityX = value;
        RefreshLabels();
    }

    private void OnSensYChanged(float value)
    {
        GameSettings.SensitivityY = value;
        RefreshLabels();
    }

    private void RefreshLabels()
    {
        if (volumeValueText != null)
            volumeValueText.text = Mathf.RoundToInt(GameSettings.Volume * 100f) + "%";

        if (sensXValueText != null)
            sensXValueText.text = Mathf.RoundToInt(GameSettings.SensitivityX).ToString();

        if (sensYValueText != null)
            sensYValueText.text = Mathf.RoundToInt(GameSettings.SensitivityY).ToString();
    }
}
