using System.Collections;
using TMPro;
using UnityEngine;

// 화면 위쪽에서 아래로 내려오는 안내 문구.
// 한 번 부르면 [내려옴 → 잠깐 머무름 → 다시 올라감] 순서로 재생된다.
// 이 스크립트는 상자(MessageBox)가 아니라 그 부모(ToastGroup)에 붙인다.
// 상자를 껐다 켜면 자기 자신이 꺼져서 코루틴이 멈추기 때문에,
// 숨길 때는 SetActive 대신 CanvasGroup 의 알파를 쓴다.
public class ToastMessage : MonoBehaviour
{
    [Header("연결")]
    public RectTransform box;   // MessageBox
    public TMP_Text label;
    public CanvasGroup group;

    [Header("연출")]
    [Tooltip("0 이면 화면 밖으로 완전히 빠지는 거리를 자동으로 계산한다")]
    public float slideDistance = 0f;
    public float slideInTime = 0.32f;
    public float slideOutTime = 0.22f;

    [Header("라운드 시작 카운트다운")]
    public bool playOnStart = true;
    public string[] countdownSteps = { "3", "2", "1", "Game Start !" };
    [Tooltip("숫자 하나가 머무는 시간")]
    public float countHold = 0.55f;
    [Tooltip("마지막 문구가 머무는 시간")]
    public float finalHold = 1.2f;

    [Header("문구")]
    public string killMessage = "Kill !";
    public string victoryMessage = "Victory !";
    public float messageHold = 1.2f;

    private Vector2 restPosition;
    private Coroutine playing;

    void Awake()
    {
        if (box == null) box = transform as RectTransform;
        if (label == null && box != null) label = box.GetComponentInChildren<TMP_Text>(true);
        if (group == null && box != null) group = box.GetComponent<CanvasGroup>();

        if (box != null)
        {
            restPosition = box.anchoredPosition;
            Hide();
        }
    }

    // 화면 위로 치워두고 투명하게
    void Hide()
    {
        box.anchoredPosition = restPosition + new Vector2(0f, HideOffset());
        if (group != null)
        {
            group.alpha = 0f;
            group.blocksRaycasts = false;
        }
    }

    void Start()
    {
        if (playOnStart) PlayCountdown();
    }

    // 3 → 2 → 1 → Game Start !
    public void PlayCountdown()
    {
        if (countdownSteps == null || countdownSteps.Length == 0) return;

        float[] holds = new float[countdownSteps.Length];
        for (int i = 0; i < holds.Length; i++)
            holds[i] = (i == holds.Length - 1) ? finalHold : countHold;

        Play(countdownSteps, holds);
    }

    public void ShowKill() { Show(killMessage); }

    public void ShowVictory() { Show(victoryMessage); }

    public void Show(string message) { Show(message, messageHold); }

    public void Show(string message, float hold)
    {
        Play(new string[] { message }, new float[] { hold });
    }

    void Play(string[] messages, float[] holds)
    {
        if (box == null || label == null) return;
        if (playing != null) StopCoroutine(playing);
        playing = StartCoroutine(Sequence(messages, holds));
    }

    IEnumerator Sequence(string[] messages, float[] holds)
    {
        Vector2 hidden = restPosition + new Vector2(0f, HideOffset());

        for (int i = 0; i < messages.Length; i++)
        {
            label.text = messages[i];
            yield return Slide(hidden, restPosition, slideInTime, true);
            yield return new WaitForSeconds(holds[i]);
            yield return Slide(restPosition, hidden, slideOutTime, false);
        }

        Hide();
        playing = null;
    }

    IEnumerator Slide(Vector2 from, Vector2 to, float time, bool fadeIn)
    {
        if (time <= 0f)
        {
            box.anchoredPosition = to;
            if (group != null) group.alpha = fadeIn ? 1f : 0f;
            yield break;
        }

        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / time);
            // 내려올 땐 부드럽게 감속, 올라갈 땐 가속
            float eased = fadeIn ? 1f - Mathf.Pow(1f - k, 3f) : k * k;

            box.anchoredPosition = Vector2.LerpUnclamped(from, to, eased);
            if (group != null) group.alpha = fadeIn ? eased : 1f - eased;
            yield return null;
        }

        box.anchoredPosition = to;
        if (group != null) group.alpha = fadeIn ? 1f : 0f;
    }

    // 화면 위로 완전히 사라지는 데 필요한 거리
    float HideOffset()
    {
        if (slideDistance > 0f) return slideDistance;
        return Mathf.Abs(restPosition.y) + box.rect.height + 40f;
    }
}
