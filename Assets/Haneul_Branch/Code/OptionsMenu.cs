using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 옵션 화면. 지금은 전체 소리 크기만 다룬다.
// 이 스크립트는 옵션 패널이 아니라 Canvas 에 붙인다.
// 패널은 꺼진 채로 시작하는데, 꺼진 오브젝트에서는 Awake 가 돌지 않아
// 저장해둔 음량을 게임 시작 때 못 되돌리기 때문이다.
public class OptionsMenu : MonoBehaviour
{
    public const string MasterVolumeKey = "audio.master";

    [Header("연결")]
    public GameObject panel;
    public Slider masterSlider;
    public TMP_Text masterValue;
    public Button backButton;

    [Tooltip("BACK 을 눌렀을 때 다시 켤 화면 (일시정지 메뉴)")]
    public GameObject returnTo;

    public bool IsOpen { get { return panel != null && panel.activeSelf; } }

    void Awake()
    {
        float saved = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, 1f));
        AudioListener.volume = saved;

        if (masterSlider != null)
        {
            masterSlider.minValue = 0f;
            masterSlider.maxValue = 1f;
            masterSlider.SetValueWithoutNotify(saved);
            masterSlider.onValueChanged.AddListener(SetMasterVolume);
        }

        UpdateLabel(saved);
        if (panel != null) panel.SetActive(false);
    }

    public void Open()
    {
        if (panel != null) panel.SetActive(true);
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        if (returnTo != null) returnTo.SetActive(true);
    }

    // 슬라이더에서 호출된다
    public void SetMasterVolume(float value)
    {
        value = Mathf.Clamp01(value);
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(MasterVolumeKey, value);
        PlayerPrefs.Save();
        UpdateLabel(value);
    }

    void UpdateLabel(float value)
    {
        if (masterValue != null)
            masterValue.text = Mathf.RoundToInt(value * 100f) + "%";
    }
}
