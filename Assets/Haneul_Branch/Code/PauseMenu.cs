using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// ESC 로 여닫는 일시정지 메뉴.
// 열려 있는 동안 timeScale 을 0 으로 두고, 조작 스크립트를 꺼서 뒤에서 총이 나가지 않게 한다.
public class PauseMenu : MonoBehaviour
{
    [Header("연결")]
    [Tooltip("켜고 끌 메뉴 묶음 (ToggleGroup)")]
    public GameObject menuGroup;
    [Tooltip("일시정지 메뉴 패널 (MenuPannel). 옵션을 열 때 잠깐 감춘다")]
    public GameObject mainPanel;
    public OptionsMenu options;
    public Button resumeButton;
    public Button optionsButton;
    public Button exitButton;

    [Header("설정")]
    public KeyCode toggleKey = KeyCode.Escape;
    [Tooltip("EXIT 를 눌렀을 때 돌아갈 씬")]
    public string titleSceneName = "TITLE";

    [Header("멈춰 있는 동안 끌 스크립트 (비워두면 자동으로 찾음)")]
    public MonoBehaviour[] disableWhilePaused;

    public bool IsPaused { get; private set; }

    void Start()
    {
        if (disableWhilePaused == null || disableWhilePaused.Length == 0)
            disableWhilePaused = CollectControlScripts();

        if (menuGroup != null) menuGroup.SetActive(false);
        IsPaused = false;
        Time.timeScale = 1f;
        ShowCursor(false);
    }

    void Update()
    {
        if (!Input.GetKeyDown(toggleKey)) return;

        // 옵션이 열려 있으면 ESC 는 일시정지 해제가 아니라 뒤로 가기
        if (IsPaused && options != null && options.IsOpen)
        {
            options.Close();
            return;
        }
        Toggle();
    }

    // 씬을 옮기거나 오브젝트가 꺼질 때 멈춘 채로 남지 않도록
    void OnDisable()
    {
        if (IsPaused) Time.timeScale = 1f;
    }

    public void Toggle()
    {
        if (IsPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (IsPaused) return;
        IsPaused = true;

        if (menuGroup != null) menuGroup.SetActive(true);
        if (options != null) options.Close();          // 항상 메뉴 화면부터 보이도록
        if (mainPanel != null) mainPanel.SetActive(true);
        Time.timeScale = 0f;
        SetControlScripts(false);
        ShowCursor(true);
    }

    public void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false;

        if (options != null) options.Close();
        if (menuGroup != null) menuGroup.SetActive(false);
        Time.timeScale = 1f;
        SetControlScripts(true);
        ShowCursor(false);
    }

    public void OpenOptions()
    {
        if (options == null)
        {
            Debug.LogWarning("[PauseMenu] 옵션 화면이 연결되어 있지 않습니다.");
            return;
        }
        if (mainPanel != null) mainPanel.SetActive(false);
        options.Open();
    }

    public void ExitToTitle()
    {
        Time.timeScale = 1f;
        ShowCursor(true);

        if (!SceneLookup.IsInBuild(titleSceneName))
        {
            Debug.LogWarning($"[PauseMenu] '{titleSceneName}' 씬이 Build Settings 에 없습니다.");
            return;
        }
        SceneManager.LoadScene(titleSceneName);
    }

    void SetControlScripts(bool enabled)
    {
        if (disableWhilePaused == null) return;
        foreach (MonoBehaviour mb in disableWhilePaused)
        {
            if (mb != null) mb.enabled = enabled;
        }
    }

    // 플레이어 조작 계열 스크립트를 모아둔다
    MonoBehaviour[] CollectControlScripts()
    {
        List<MonoBehaviour> list = new List<MonoBehaviour>();
        Add(list, FindAnyObjectByType<PlayerFire>());
        Add(list, FindAnyObjectByType<Player>());
        Add(list, FindAnyObjectByType<Camera_Rotate>());
        Add(list, FindAnyObjectByType<PlayerADS>());
        return list.ToArray();
    }

    static void Add(List<MonoBehaviour> list, MonoBehaviour mb)
    {
        if (mb != null) list.Add(mb);
    }

    static void ShowCursor(bool show)
    {
        Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = show;
    }
}
