using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 체력 숫자와 게이지를 PlayerHealth 에 맞춰 갱신한다.
public class HealthUI : MonoBehaviour
{
    [Header("연결 (비워두면 자동으로 찾음)")]
    public PlayerHealth source;
    public TMP_Text valueText;
    public Image fill;

    [Header("색")]
    public Color normalColor = new Color(0.561f, 0.718f, 0.604f, 1f); // 연한 초록
    public Color lowColor = new Color(0.851f, 0.227f, 0.267f, 1f);    // 빨강
    [Range(0f, 1f)]
    [Tooltip("이 비율 아래로 떨어지면 게이지가 빨갛게 바뀐다")]
    public float lowThreshold = 0.3f;

    private int shownValue = int.MinValue;

    void Start()
    {
        if (source == null) source = FindAnyObjectByType<PlayerHealth>();
        if (source == null)
            Debug.LogWarning("[HealthUI] 씬에서 PlayerHealth 를 찾지 못했습니다.", this);
    }

    void Update()
    {
        if (source == null) return;

        int hp = Mathf.CeilToInt(source.CurrentHp);
        if (hp != shownValue)
        {
            shownValue = hp;
            if (valueText != null) valueText.text = hp.ToString();
        }

        if (fill != null)
        {
            // Image 쪽에서 값이 같으면 무시하므로 매 프레임 넣어도 다시 그리지 않는다
            float normalized = source.Normalized;
            fill.fillAmount = normalized;
            fill.color = normalized <= lowThreshold ? lowColor : normalColor;
        }
    }
}
