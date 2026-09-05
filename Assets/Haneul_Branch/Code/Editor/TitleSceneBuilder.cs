#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using K = HaneulUiKit;

// TITLE.unity 를 통째로 다시 구성하는 에디터 도구.
// 메뉴: Tools/Haneul/Build Title Scene
public static class TitleSceneBuilder
{
    const string ScenePath  = "Assets/Haneul_Branch/TITLE.unity";
    const string CanvasName = "TitleCanvas";

    // 배경 대각선 기울기 (음수 = 우상향)
    const float Tilt = -24f;

    [MenuItem("Tools/Haneul/Build Title Scene")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        K.EnsureAssets();

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // 다시 실행해도 중복되지 않도록 기존 UI 루트를 정리
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == CanvasName || root.name == "EventSystem")
                Object.DestroyImmediate(root);
        }

        SetupCamera();

        // ── 캔버스 ────────────────────────────────────────────
        GameObject canvasGo = new GameObject(CanvasName,
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));

        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        TitleMenu menu = canvasGo.AddComponent<TitleMenu>();
        menu.fadeGroup = canvasGo.GetComponent<CanvasGroup>();
        menu.matchingSceneName = "Scene1";

        Transform uiRoot = canvasGo.transform;

        BuildBackground(uiRoot);
        BuildFrame(uiRoot);
        BuildTitleBlock(uiRoot);
        BuildMenu(uiRoot, menu);

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        RegisterInBuildSettings();

        Debug.Log("[TitleSceneBuilder] TITLE.unity 구성 완료");
    }

    // ── 배경 ──────────────────────────────────────────────────
    static void BuildBackground(Transform parent)
    {
        Transform bg = Panel("Background", parent).transform;

        Image basePlate = K.Img(K.NewUI("Base", bg), K.Bg, K.White);
        K.Stretch(basePlate);

        // 화면을 가르는 대각 면 (오른쪽이 밝고 왼쪽이 어둡다)
        MakeDiagonal(bg, "Shape_Large", new Vector2(1700f, 0f), new Vector2(2400f, 2800f), K.BgPanel);
        MakeDiagonal(bg, "Shape_Edge", new Vector2(1690f, 0f), new Vector2(2400f, 2800f), K.BgPanel2);
        MakeDiagonal(bg, "Shape_Left", new Vector2(-1620f, 0f), new Vector2(1600f, 2800f), K.BgPanel2);

        // 대각선 라인들
        MakeDiagonal(bg, "Line_Accent", new Vector2(470f, 0f), new Vector2(5f, 2800f), K.Accent);
        MakeDiagonal(bg, "Line_Soft", new Vector2(438f, 0f), new Vector2(1f, 2800f), K.WithA(K.Fg, 0.16f));
        MakeDiagonal(bg, "Line_Soft2", new Vector2(-760f, 0f), new Vector2(1f, 2800f), K.WithA(K.Fg, 0.10f));

        // 미세한 그리드
        GameObject gridGo = K.NewUI("Grid", bg);
        RawImage grid = gridGo.AddComponent<RawImage>();
        grid.texture = K.Grid;
        grid.color = K.WithA(K.Fg, 0.06f);
        grid.uvRect = new Rect(0f, 0f, 30f, 17f);
        grid.raycastTarget = false;
        K.Stretch(grid);
        gridGo.AddComponent<TitleBackgroundScroll>();

        // 비네트
        Image vig = K.Img(K.NewUI("Vignette", bg), K.WithA(K.Veil, 0.26f), K.Vignette);
        K.Stretch(vig);
    }

    static void MakeDiagonal(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        Image img = K.Img(K.NewUI(name, parent), color, K.White);
        K.Rect(img, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
        img.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Tilt);
    }

    // ── 상/하단 프레임 ────────────────────────────────────────
    static void BuildFrame(Transform parent)
    {
        Transform frame = Panel("Frame", parent).transform;

        // 상단 / 하단 가로 라인
        Rule(frame, "TopRule", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), -104f);
        Rule(frame, "BottomRule", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), 104f);

        // 좌측 상단 브랜드
        Image mark = K.Img(K.NewUI("Mark", frame), K.Accent, K.White);
        K.Rect(mark, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(150f, -70f), new Vector2(12f, 12f));

        TMP_Text brand = K.Text(frame, "Brand", "MAIN MENU", 20f, K.Muted);
        brand.characterSpacing = 16f;
        brand.alignment = TextAlignmentOptions.MidlineLeft;
        K.Rect(brand, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(178f, -70f), new Vector2(900f, 30f));

        // 우측 상단 보조 텍스트
        TMP_Text server = K.Text(frame, "Server", "SEOUL  ·  PING 12ms", 18f, K.Muted);
        server.characterSpacing = 10f;
        server.alignment = TextAlignmentOptions.MidlineRight;
        K.Rect(server, new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-150f, -70f), new Vector2(700f, 30f));

        // 우측 하단 버전
        TMP_Text ver = K.Text(frame, "Version", "v0.1.0", 18f, K.WithA(K.Muted, 0.8f));
        ver.characterSpacing = 10f;
        ver.alignment = TextAlignmentOptions.MidlineRight;
        K.Rect(ver, new Vector2(1f, 0f), new Vector2(1f, 0.5f), new Vector2(-150f, 68f), new Vector2(500f, 30f));
    }

    // 좌우 여백을 둔 가로 라인
    static void Rule(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, float y)
    {
        Image img = K.Img(K.NewUI(name, parent), K.WithA(K.Sage, 0.9f), K.White);
        RectTransform rt = img.rectTransform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.offsetMin = new Vector2(150f, 0f);
        rt.offsetMax = new Vector2(-150f, 0f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, 2f);
    }

    // ── 타이틀 ────────────────────────────────────────────────
    static void BuildTitleBlock(Transform parent)
    {
        GameObject block = K.NewUI("TitleBlock", parent);
        K.Rect(block.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(150f, 168f), new Vector2(1200f, 360f));

        Image bar = K.Img(K.NewUI("AccentBar", block.transform), K.Accent, K.White);
        K.Rect(bar, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(92f, 6f));

        TMP_Text title = K.Text(block.transform, "Title", "PROJECT <color=#" + K.AccentHex + ">V</color>", 158f, K.Fg);
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 4f;
        title.alignment = TextAlignmentOptions.TopLeft;
        K.Rect(title, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(-9f, -30f), new Vector2(1200f, 200f));
        title.rectTransform.localScale = new Vector3(0.94f, 1f, 1f);

        TMP_Text sub = K.Text(block.transform, "Subtitle", "KONKUK UNIV.  ·  1v1 TACTICAL SHOOTER", 24f, K.Muted);
        sub.characterSpacing = 20f;
        sub.alignment = TextAlignmentOptions.TopLeft;
        K.Rect(sub, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -238f), new Vector2(1200f, 40f));
    }

    // ── 메뉴 버튼 ─────────────────────────────────────────────
    static void BuildMenu(Transform parent, TitleMenu menu)
    {
        GameObject block = K.NewUI("Menu", parent);
        K.Rect(block.transform, new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(150f, 196f), new Vector2(440f, 172f));

        TMP_Text tag = K.Text(block.transform, "MenuTag", "SELECT", 18f, K.Muted);
        tag.characterSpacing = 20f;
        tag.alignment = TextAlignmentOptions.MidlineLeft;
        K.Rect(tag, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(2f, 64f), new Vector2(440f, 24f));

        K.Bar(block.transform, "MenuRule", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 26f), new Vector2(440f, 2f), K.WithA(K.Sage, 0.9f));

        Button matching = MakeButton(block.transform, "Btn_Matching", "MATCHING", 0f);
        Button exit = MakeButton(block.transform, "Btn_Exit", "EXIT", -98f);

        UnityEventTools.AddPersistentListener(matching.onClick,
            new UnityEngine.Events.UnityAction(menu.OnMatchingClicked));
        UnityEventTools.AddPersistentListener(exit.onClick,
            new UnityEngine.Events.UnityAction(menu.OnExitClicked));
    }

    static Button MakeButton(Transform parent, string name, string labelText, float y)
    {
        GameObject go = K.NewUI(name, parent);
        K.Rect(go.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, y), new Vector2(440f, 74f));
        return K.MenuButton(go, labelText);
    }

    static GameObject Panel(string name, Transform parent)
    {
        GameObject go = K.NewUI(name, parent);
        K.Stretch(go.transform);
        return go;
    }

    // ── 카메라 / 빌드 세팅 ────────────────────────────────────
    static void SetupCamera()
    {
        Camera cam = Object.FindAnyObjectByType<Camera>();
        if (cam == null) return;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = K.Bg;
    }

    static void RegisterInBuildSettings()
    {
        List<EditorBuildSettingsScene> list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        int index = list.FindIndex(s => s.path == ScenePath);
        if (index >= 0)
        {
            EditorBuildSettingsScene entry = list[index];
            entry.enabled = true;
            list.RemoveAt(index);
            list.Insert(0, entry);
        }
        else
        {
            list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        }
        EditorBuildSettings.scenes = list.ToArray();
    }
}
#endif
