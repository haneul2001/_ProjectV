using TMPro;
using UnityEngine;

// 탄창 UI. 실제 탄약을 들고 있는 PlayerFire 의 값을 그대로 비춘다.
public class AmmoUI : MonoBehaviour
{
    public enum DisplayType
    {
        Current,   // 지금 탄창에 남은 탄
        Reserve,   // 탄창 밖 여분 탄
        Magazine   // 탄창 최대치
    }

    [Header("무엇을 표시할 지")]
    public DisplayType displayType = DisplayType.Current;

    [Header("연결 (비워두면 자동으로 찾음)")]
    public PlayerFire source;

    private TMP_Text text;
    private int shown = int.MinValue;

    void Awake()
    {
        text = GetComponent<TMP_Text>();
    }

    void Start()
    {
        if (source == null) source = FindAnyObjectByType<PlayerFire>();
        if (source == null)
            Debug.LogWarning("[AmmoUI] 씬에서 PlayerFire 를 찾지 못했습니다.", this);
    }

    void Update()
    {
        if (source == null || text == null) return;

        int value = CurrentValue();
        if (value == shown) return; // 값이 바뀔 때만 갱신 (매 프레임 문자열 만들지 않도록)

        shown = value;
        text.text = value.ToString();
    }

    int CurrentValue()
    {
        switch (displayType)
        {
            case DisplayType.Reserve:  return source.totalAmmo;
            case DisplayType.Magazine: return source.magazineSize;
            default:                   return source.currentAmmo;
        }
    }
}
