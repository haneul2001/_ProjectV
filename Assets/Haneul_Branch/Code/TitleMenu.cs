using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// 타이틀 화면의 버튼 동작 (Matching / Exit)
public class TitleMenu : MonoBehaviour
{
    [Header("매칭 시 이동할 씬 이름")]
    public string matchingSceneName = "SampleScene";

    [Header("페이드 (비워두면 자동으로 찾음)")]
    public CanvasGroup fadeGroup;
    public float fadeInTime = 0.9f;
    public float fadeOutTime = 0.45f;

    private bool busy;

    void Awake()
    {
        if (fadeGroup == null) fadeGroup = GetComponent<CanvasGroup>();
    }

    void Start()
    {
        // 인게임에서 잠갔던 커서를 타이틀에서는 다시 풀어준다
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (fadeGroup != null)
        {
            fadeGroup.alpha = 0f;
            StartCoroutine(Fade(0f, 1f, fadeInTime));
        }
    }

    // MATCHING 버튼
    public void OnMatchingClicked()
    {
        if (busy) return;
        busy = true;
        StartCoroutine(LoadMatching());
    }

    // EXIT 버튼
    public void OnExitClicked()
    {
        if (busy) return;
        busy = true;
        StartCoroutine(QuitGame());
    }

    IEnumerator LoadMatching()
    {
        yield return Fade(1f, 0f, fadeOutTime);

        if (string.IsNullOrEmpty(matchingSceneName))
        {
            Debug.LogWarning("[TitleMenu] 이동할 씬 이름이 비어 있습니다.");
            busy = false;
            yield break;
        }

        if (SceneLookup.IsInBuild(matchingSceneName))
        {
            SceneManager.LoadScene(matchingSceneName);
        }
        else
        {
            Debug.LogWarning($"[TitleMenu] '{matchingSceneName}' 씬이 Build Settings에 없습니다.");
            busy = false;
            yield return Fade(0f, 1f, fadeInTime);
        }
    }

    IEnumerator QuitGame()
    {
        yield return Fade(1f, 0f, fadeOutTime);

        Debug.Log("[TitleMenu] 게임 종료");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    IEnumerator Fade(float from, float to, float time)
    {
        if (fadeGroup == null || time <= 0f)
        {
            if (fadeGroup != null) fadeGroup.alpha = to;
            yield break;
        }

        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            fadeGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / time));
            yield return null;
        }
        fadeGroup.alpha = to;
    }
}
