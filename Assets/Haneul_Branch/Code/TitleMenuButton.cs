using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 타이틀 버튼의 호버/클릭 연출 (색 채우기 + 좌측 액센트 바)
[RequireComponent(typeof(RectTransform))]
public class TitleMenuButton : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler,
    ISelectHandler, IDeselectHandler
{
    [Header("연결")]
    public Image fill;          // 호버 시 왼쪽에서 채워지는 판
    public Image border;        // 테두리
    public RectTransform tick;  // 좌측 액센트 바
    public TMP_Text label;

    [Header("색상")]
    public Color idleLabel = new Color(0.133f, 0.118f, 0.098f, 1f);
    public Color hoverLabel = new Color(0.969f, 0.949f, 0.910f, 1f);

    [Header("연출")]
    public float speed = 14f;
    public float tickIdleWidth = 6f;
    public float tickHoverWidth = 16f;
    public float labelIdleIndent = 34f;
    public float labelHoverIndent = 48f;

    private RectTransform labelRect;
    private float borderIdleAlpha = 0.34f;
    private bool hovered;
    private bool pressed;
    private float t;

    void Awake()
    {
        if (label != null) labelRect = label.rectTransform;
        if (border != null) borderIdleAlpha = border.color.a;
        t = 0f;
        Apply(0f);
    }

    void OnDisable()
    {
        hovered = pressed = false;
        t = 0f;
        Apply(0f);
    }

    void Update()
    {
        float target = hovered ? 1f : 0f;
        t = Mathf.Lerp(t, target, 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime));
        if (Mathf.Abs(t - target) < 0.001f) t = target;
        Apply(t);
    }

    void Apply(float v)
    {
        // 이징을 살짝 줘서 스윽 채워지는 느낌
        float e = 1f - Mathf.Pow(1f - v, 3f);

        if (fill != null)
        {
            fill.fillAmount = e;
            Color c = fill.color;
            c.a = pressed ? 1f : 0.92f;
            fill.color = c;
        }

        if (border != null)
        {
            Color c = border.color;
            c.a = Mathf.Lerp(borderIdleAlpha, 0f, e);
            border.color = c;
        }

        if (tick != null)
        {
            Vector2 s = tick.sizeDelta;
            s.x = Mathf.Lerp(tickIdleWidth, tickHoverWidth, e);
            tick.sizeDelta = s;
        }

        if (label != null)
        {
            label.color = Color.Lerp(idleLabel, hoverLabel, e);
            if (labelRect != null)
            {
                Vector2 off = labelRect.offsetMin;
                off.x = Mathf.Lerp(labelIdleIndent, labelHoverIndent, e);
                labelRect.offsetMin = off;
            }
        }
    }

    public void OnPointerEnter(PointerEventData e) { hovered = true; }
    public void OnPointerExit(PointerEventData e) { hovered = false; pressed = false; }
    public void OnPointerDown(PointerEventData e) { pressed = true; }
    public void OnPointerUp(PointerEventData e) { pressed = false; }
    public void OnSelect(BaseEventData e) { hovered = true; }
    public void OnDeselect(BaseEventData e) { hovered = false; }
}
