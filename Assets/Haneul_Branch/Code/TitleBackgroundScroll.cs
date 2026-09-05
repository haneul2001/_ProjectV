using UnityEngine;
using UnityEngine.UI;

// 배경 그리드를 아주 천천히 흘려서 정적인 느낌을 없앤다
[RequireComponent(typeof(RawImage))]
public class TitleBackgroundScroll : MonoBehaviour
{
    [Header("초당 이동량 (UV 기준)")]
    public Vector2 speed = new Vector2(0.012f, -0.006f);

    private RawImage image;
    private Rect baseRect;

    void Awake()
    {
        image = GetComponent<RawImage>();
        baseRect = image.uvRect;
    }

    void Update()
    {
        Rect r = image.uvRect;
        r.x = baseRect.x + Mathf.Repeat(Time.unscaledTime * speed.x, 1f);
        r.y = baseRect.y + Mathf.Repeat(Time.unscaledTime * speed.y, 1f);
        image.uvRect = r;
    }
}
