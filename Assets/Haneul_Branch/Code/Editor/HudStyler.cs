#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using K = HaneulUiKit;

// haneul_scene 의 인게임 HUD 를 타이틀과 같은 톤으로 정리하는 에디터 도구.
// 기존 오브젝트를 지우지 않고 이름으로 찾아 배치/색만 다시 잡는다.
// 메뉴: Tools/Haneul/Style HUD
public static class HudStyler
{
    // 화면 가장자리 여백 / 판때기 안쪽 여백
    const float Margin = 48f;
    const float Pad = 16f;

    // 점수판 = [내 점수] [타이머] [상대 점수]
    const float ScoreWidth = 560f;
    const float ScoreX = 190f;   // 점수 숫자의 좌우 위치
    const float TimeHalf = 90f;  // 가운데 타이머 칸의 절반 너비

    const float HandleWidth = 18f;  // 슬라이더 손잡이 폭

    [MenuItem("Tools/Haneul/Style HUD")]
    public static void Style()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        K.EnsureAssets();

        // 지금 열려 있는 씬에 적용한다
        Scene scene = SceneManager.GetActiveScene();
        GameObject canvasGo = GameObject.Find("Canvas");
        if (canvasGo == null)
        {
            Debug.LogError("[HudStyler] 'Canvas' 를 찾지 못했습니다.");
            return;
        }
        Transform canvas = canvasGo.transform;

        StyleCrosshair(Find(canvas, "DefaultGroup/Aim"));
        StyleHealth(Find(canvas, "DefaultGroup/HpBox"));
        StyleScore(Find(canvas, "DefaultGroup/ScoreBox"));
        StyleTime(canvas.Find("DefaultGroup"));
        StyleAmmo(Find(canvas, "DefaultGroup/AmmoBox"));
        StyleMiniMap(Find(canvas, "DefaultGroup/MiniMapBox"));
        StyleToast(Find(canvas, "ToastGroup/MessageBox"));
        StylePauseMenu(Find(canvas, "ToggleGroup"));
        StyleFade(Find(canvas, "ScreenFadeGorup"));

        WireAmmo(canvas);
        WireHealth(canvas);
        WirePauseMenu(canvasGo, canvas.Find("ToggleGroup"));
        WireToast(canvas.Find("ToastGroup"));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[HudStyler] HUD 정리 완료 — " + scene.name);
    }

    // 베이지 판때기 + 테두리. HUD 글자가 3D 배경 위에서도 읽히도록 깔아준다.
    static void Plate(Transform t)
    {
        K.Img(t.gameObject, K.WithA(K.Bg, 0.96f), K.White);
        K.Frame(t, "Frame", K.WithA(K.Fg, 0.22f));
    }

    // ── 조준선 ────────────────────────────────────────────────
    // 가운데 점 + 사방 네 개의 선.
    // 여기만 밝은 색을 쓴다. 배경이 UI 가 아니라 3D 화면이라 어두운 바닥에서도 보여야 한다.
    static void StyleCrosshair(Transform aim)
    {
        if (aim == null) return;

        K.Img(aim.gameObject, Color.clear, K.White);
        K.Rect(aim, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(44f, 44f));

        const float gap = 5f, len = 11f, thick = 3f;
        float off = gap + len * 0.5f;

        CrosshairPart(aim, "Up", new Vector2(0f, off), new Vector2(thick, len));
        CrosshairPart(aim, "Down", new Vector2(0f, -off), new Vector2(thick, len));
        CrosshairPart(aim, "Left", new Vector2(-off, 0f), new Vector2(len, thick));
        CrosshairPart(aim, "Right", new Vector2(off, 0f), new Vector2(len, thick));
        CrosshairPart(aim, "Dot", Vector2.zero, new Vector2(2f, 2f));
    }

    static void CrosshairPart(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        Image img = K.Img(K.Child(parent, name), K.OnAccent, K.White);
        K.Rect(img, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);

        Outline outline = img.GetComponent<Outline>();
        if (outline == null) outline = img.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.6f);
        outline.effectDistance = new Vector2(1f, -1f);
    }

    // ── 체력 (좌하단) ─────────────────────────────────────────
    static void StyleHealth(Transform hp)
    {
        if (hp == null) return;

        Plate(hp);
        K.Rect(hp, new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(Margin, Margin), new Vector2(400f, 104f));

        // 기존 'BG' 를 체력 바의 홈으로 재사용
        Image track = K.Img(K.Child(hp, "BG"), K.WithA(K.Fg, 0.14f), K.White);
        RectTransform trackRt = track.rectTransform;
        trackRt.anchorMin = new Vector2(0f, 0f);
        trackRt.anchorMax = new Vector2(1f, 0f);
        trackRt.pivot = new Vector2(0.5f, 0f);
        trackRt.offsetMin = new Vector2(Pad, Pad);
        trackRt.offsetMax = new Vector2(-Pad, Pad + 12f);

        // 체력 띠 — 연한 초록
        Image fill = K.Img(K.Child(track.transform, "Fill"), K.Sage, K.White);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 1f;
        K.Stretch(fill);

        TMP_Text value = K.Text(hp, "HpValue", "100", 46f, K.Fg);
        value.fontStyle = FontStyles.Bold;
        value.alignment = TextAlignmentOptions.BottomLeft;
        K.Rect(value, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(Pad, 34f), new Vector2(220f, 54f));

        TMP_Text label = K.Text(hp, "HpLabel", "HP", 18f, K.Muted);
        label.characterSpacing = 10f;
        label.alignment = TextAlignmentOptions.BottomLeft;
        K.Rect(label, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(110f, 40f), new Vector2(120f, 24f));
    }

    // ── 점수 (상단 중앙) ──────────────────────────────────────
    static void StyleScore(Transform score)
    {
        if (score == null) return;

        K.Img(score.gameObject, Color.clear, K.White);
        K.Rect(score, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -40f), new Vector2(ScoreWidth, 100f));

        Image bg = K.Img(K.Child(score, "bg"), K.WithA(K.Bg, 0.96f), K.White);
        K.Stretch(bg);
        K.Frame(score, "Frame", K.WithA(K.Fg, 0.22f));

        // 아래쪽 연한 초록 띠
        K.Bar(score, "BottomBand", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
            Vector2.zero, new Vector2(0f, 4f), K.Sage);

        // 타이머 양옆 구분선 (기존 'Image' 를 왼쪽 선으로 재사용)
        Divider(score, "Image", -TimeHalf);
        Divider(score, "DividerRight", TimeHalf);

        ScoreSide(score, "Text (TMP)", "LabelYou", "YOU", -ScoreX, K.Fg);
        ScoreSide(score, "Text (TMP) (1)", "LabelEnemy", "ENEMY", ScoreX, K.Accent);
    }

    static void Divider(Transform score, string name, float x)
    {
        Image divider = K.Img(K.Child(score, name), K.WithA(K.Fg, 0.22f), K.White);
        K.Rect(divider, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(x, 0f), new Vector2(2f, 44f));
    }

    // ── 라운드 타이머 (점수판 가운데) ─────────────────────────
    // SampleScene 에만 있던 TimeBox 를 살려 점수판 가운데 칸에 얹는다.
    // ScoreBox 와 겹쳐 그리므로 형제 순서를 마지막으로 옮겨 위에 오게 한다.
    static void StyleTime(Transform group)
    {
        if (group == null) return;

        GameObject go = K.Child(group, "TimeBox");
        K.Img(go, Color.clear, K.White);
        K.Rect(go.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -40f), new Vector2(TimeHalf * 2f, 100f));
        go.transform.SetAsLastSibling();

        TMP_Text label = K.Text(go.transform, "TimeLabel", "TIME", 16f, K.Muted);
        label.characterSpacing = 12f;
        label.alignment = TextAlignmentOptions.Center;
        K.Rect(label, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 32f), new Vector2(150f, 24f));

        TMP_Text value = K.Text(go.transform, "TimeValue", "1:30", 40f, K.Fg);
        value.fontStyle = FontStyles.Bold;
        value.alignment = TextAlignmentOptions.Center;
        K.Rect(value, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -10f), new Vector2(150f, 56f));
    }

    static void ScoreSide(Transform score, string valueName, string labelName, string labelText, float x, Color color)
    {
        Transform valueTr = score.Find(valueName);
        if (valueTr != null)
        {
            TMP_Text value = valueTr.GetComponent<TMP_Text>();
            if (value != null)
            {
                K.Style(value, 54f, color);
                value.fontStyle = FontStyles.Bold;
                value.alignment = TextAlignmentOptions.Center;
                K.Rect(value, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(x, -10f), new Vector2(140f, 68f));
            }
        }

        TMP_Text label = K.Text(score, labelName, labelText, 16f, K.Muted);
        label.characterSpacing = 12f;
        label.alignment = TextAlignmentOptions.Center;
        K.Rect(label, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(x, 32f), new Vector2(140f, 24f));
    }

    // ── 탄약 (우하단) ─────────────────────────────────────────
    static void StyleAmmo(Transform ammo)
    {
        if (ammo == null) return;

        Plate(ammo);
        K.Rect(ammo, new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-Margin, Margin), new Vector2(400f, 104f));

        // 기존 'BG' 는 판때기 안쪽 밑줄로
        Image rule = K.Img(K.Child(ammo, "BG"), K.Sage, K.White);
        RectTransform ruleRt = rule.rectTransform;
        ruleRt.anchorMin = new Vector2(0f, 0f);
        ruleRt.anchorMax = new Vector2(1f, 0f);
        ruleRt.pivot = new Vector2(0.5f, 0f);
        ruleRt.offsetMin = new Vector2(Pad, Pad);
        ruleRt.offsetMax = new Vector2(-Pad, Pad + 4f);

        Transform icon = ammo.Find("AmmoImage");
        if (icon != null)
        {
            Image img = icon.GetComponent<Image>();
            if (img != null) img.color = K.WithA(K.Fg, 0.55f);
            K.Rect(icon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(Pad + 4f, 8f), new Vector2(48f, 48f));
        }

        Transform box = ammo.Find("TextBox");
        if (box == null) return;
        K.Stretch(box, 80f, Pad, 0f, Pad + 8f);

        // "30 / 30" — 현재 탄약만 크게, 나머지는 죽인다
        AmmoText(box, "CurrentAmmo", 58f, K.Fg, true,
            new Vector2(-108f, 0f), new Vector2(220f, 68f), TextAlignmentOptions.MidlineRight);
        AmmoText(box, "StringText", 28f, K.Muted, false,
            new Vector2(-54f, -2f), new Vector2(32f, 40f), TextAlignmentOptions.Center);
        AmmoText(box, "TotalAmmo", 28f, K.Muted, false,
            new Vector2(0f, -4f), new Vector2(88f, 40f), TextAlignmentOptions.MidlineRight);
    }

    static void AmmoText(Transform parent, string name, float size, Color color, bool bold,
                         Vector2 pos, Vector2 rectSize, TextAlignmentOptions align)
    {
        Transform t = parent.Find(name);
        if (t == null) return;
        TMP_Text text = t.GetComponent<TMP_Text>();
        if (text == null) return;

        K.Style(text, size, color);
        text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        text.alignment = align;
        K.Rect(text, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), pos, rectSize);
    }

    // ── 미니맵 (좌상단) ───────────────────────────────────────
    static void StyleMiniMap(Transform map)
    {
        if (map == null) return;

        // 나중에 렌더 텍스처를 넣을 수 있도록 본체 Image 는 배경판으로 남겨둔다
        Plate(map);
        K.Rect(map, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(Margin, -Margin), new Vector2(240f, 240f));

        // 네 모서리 브래킷
        Corner(map, "TL", new Vector2(0f, 1f), new Vector2(1f, -1f));
        Corner(map, "TR", new Vector2(1f, 1f), new Vector2(-1f, -1f));
        Corner(map, "BL", new Vector2(0f, 0f), new Vector2(1f, 1f));
        Corner(map, "BR", new Vector2(1f, 0f), new Vector2(-1f, 1f));

        // 위쪽 연한 초록 띠 + 라벨
        K.Bar(map, "TopBand", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            Vector2.zero, new Vector2(0f, 4f), K.Sage);

        TMP_Text label = K.Text(map, "MapLabel", "MAP", 14f, K.Muted);
        label.characterSpacing = 12f;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        K.Rect(label, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(10f, -12f), new Vector2(120f, 20f));
    }

    static void Corner(Transform parent, string name, Vector2 anchor, Vector2 dir)
    {
        const float len = 18f, thick = 2f;

        Image h = K.Img(K.Child(parent, "Tick_" + name + "_H"), K.Accent, K.White);
        K.Rect(h, anchor, anchor, Vector2.zero, new Vector2(len, thick));

        Image v = K.Img(K.Child(parent, "Tick_" + name + "_V"), K.Accent, K.White);
        K.Rect(v, anchor, anchor, new Vector2(0f, dir.y * thick), new Vector2(thick, len));
    }

    // ── 안내 메시지 ───────────────────────────────────────────
    static void StyleToast(Transform toast)
    {
        if (toast == null) return;

        K.Img(toast.gameObject, K.WithA(K.Bg, 0.97f), K.White);
        K.Rect(toast, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -176f), new Vector2(560f, 64f));
        K.Frame(toast, "Frame", K.WithA(K.Fg, 0.22f));

        K.Bar(toast, "BarL", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
            Vector2.zero, new Vector2(4f, 0f), K.Accent);
        K.Bar(toast, "BarR", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f),
            Vector2.zero, new Vector2(4f, 0f), K.Accent);

        Transform t = toast.Find("Text (TMP)");
        if (t == null) return;
        TMP_Text text = t.GetComponent<TMP_Text>();
        if (text == null) return;

        K.Style(text, 30f, K.Fg);
        text.fontStyle = FontStyles.Bold;
        text.characterSpacing = 6f;
        text.alignment = TextAlignmentOptions.Center;
        K.Stretch(text, 20f, 20f);
    }

    // ── 일시정지 메뉴 ─────────────────────────────────────────
    static void StylePauseMenu(Transform group)
    {
        if (group == null) return;

        Transform dim = group.Find("BGPannel");
        if (dim != null)
        {
            K.Img(dim.gameObject, K.WithA(K.Veil, 0.62f), K.White);
            K.Stretch(dim);
        }

        Transform panel = group.Find("MenuPannel");
        if (panel == null) return;

        K.Img(panel.gameObject, K.WithA(K.Bg, 0.98f), K.White);
        K.Rect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 480f));

        K.Frame(panel, "Frame", K.WithA(K.Fg, 0.22f));
        K.Bar(panel, "TopBar", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            Vector2.zero, new Vector2(0f, 4f), K.Accent);
        K.Bar(panel, "BottomBand", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
            Vector2.zero, new Vector2(0f, 4f), K.Sage);

        // 제목 — 기존 Image 는 투명하게 두고 자식으로 글자를 넣는다
        Transform head = panel.Find("MenuText");
        if (head != null)
        {
            K.Img(head.gameObject, Color.clear, K.White);
            K.Rect(head, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(420f, 52f));

            TMP_Text label = K.Text(head, "Label", "PAUSED", 40f, K.Fg);
            label.fontStyle = FontStyles.Bold;
            label.characterSpacing = 14f;
            label.alignment = TextAlignmentOptions.Center;
            K.Stretch(label);
        }

        PauseButton(panel, "ReSume", "RESUME", 60f);
        PauseButton(panel, "Option", "OPTIONS", -36f);
        PauseButton(panel, "Exit", "EXIT", -132f);

        StyleOptionPanel(group);
    }

    // ── 옵션 화면 ─────────────────────────────────────────────
    static void StyleOptionPanel(Transform group)
    {
        GameObject go = K.Child(group, "OptionPannel");
        Transform panel = go.transform;

        K.Img(go, K.WithA(K.Bg, 0.98f), K.White);
        K.Rect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 340f));

        K.Frame(panel, "Frame", K.WithA(K.Fg, 0.22f));
        K.Bar(panel, "TopBar", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            Vector2.zero, new Vector2(0f, 4f), K.Accent);
        K.Bar(panel, "BottomBand", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
            Vector2.zero, new Vector2(0f, 3f), K.Sage);

        TMP_Text title = K.Text(panel, "Title", "OPTIONS", 34f, K.Fg);
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 14f;
        title.alignment = TextAlignmentOptions.Center;
        K.Rect(title, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(420f, 46f));

        TMP_Text label = K.Text(panel, "MasterLabel", "MASTER VOLUME", 20f, K.Muted);
        label.characterSpacing = 12f;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        K.Rect(label, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 54f), new Vector2(420f, 24f));

        BuildSlider(panel, "MasterSlider", new Vector2(-36f, 4f), new Vector2(348f, 24f));

        TMP_Text value = K.Text(panel, "MasterValue", "100%", 22f, K.Fg);
        value.fontStyle = FontStyles.Bold;
        value.alignment = TextAlignmentOptions.MidlineRight;
        K.Rect(value, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(174f, 4f), new Vector2(80f, 26f));

        GameObject back = K.Child(panel, "Back");
        K.Rect(back.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -96f), new Vector2(420f, 64f));
        K.MenuButton(back, "BACK", 24f);

        go.SetActive(false);
    }

    // uGUI Slider 는 Fill / Handle 을 정해진 구조로 만들어줘야 동작한다.
    static Slider BuildSlider(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        GameObject go = K.Child(parent, name);
        K.Rect(go.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);

        // 줄 전체에서 클릭/드래그를 받도록 투명한 판을 깐다.
        // 이게 없으면 손잡이(18px) 위를 정확히 집었을 때만 움직인다.
        Image hitArea = K.Img(go, new Color(0f, 0f, 0f, 0f), K.White);
        hitArea.raycastTarget = true;

        Image track = K.Img(K.Child(go.transform, "Background"), K.WithA(K.Fg, 0.14f), K.White);
        track.raycastTarget = true;
        RectTransform trackRt = track.rectTransform;
        trackRt.anchorMin = new Vector2(0f, 0.5f);
        trackRt.anchorMax = new Vector2(1f, 0.5f);
        trackRt.pivot = new Vector2(0.5f, 0.5f);
        trackRt.anchoredPosition = Vector2.zero;
        trackRt.sizeDelta = new Vector2(0f, 8f);

        GameObject fillArea = K.Child(go.transform, "Fill Area");
        RectTransform fillAreaRt = (RectTransform)fillArea.transform;
        fillAreaRt.anchorMin = new Vector2(0f, 0.5f);
        fillAreaRt.anchorMax = new Vector2(1f, 0.5f);
        fillAreaRt.pivot = new Vector2(0.5f, 0.5f);
        fillAreaRt.anchoredPosition = Vector2.zero;
        fillAreaRt.sizeDelta = new Vector2(-HandleWidth, 8f);

        Image fill = K.Img(K.Child(fillArea.transform, "Fill"), K.Sage, K.White);
        RectTransform fillRt = fill.rectTransform;
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;

        GameObject slideArea = K.Child(go.transform, "Handle Slide Area");
        RectTransform slideRt = (RectTransform)slideArea.transform;
        slideRt.anchorMin = Vector2.zero;
        slideRt.anchorMax = Vector2.one;
        slideRt.offsetMin = Vector2.zero;
        slideRt.offsetMax = Vector2.zero;
        slideRt.sizeDelta = new Vector2(-HandleWidth, 0f);

        Image handle = K.Img(K.Child(slideArea.transform, "Handle"), K.Fg, K.White);
        handle.raycastTarget = true;
        RectTransform handleRt = handle.rectTransform;
        handleRt.anchorMin = new Vector2(0f, 0f);
        handleRt.anchorMax = new Vector2(0f, 1f);
        handleRt.pivot = new Vector2(0.5f, 0.5f);
        handleRt.anchoredPosition = Vector2.zero;
        handleRt.sizeDelta = new Vector2(HandleWidth, -2f);

        Slider slider = go.GetComponent<Slider>();
        if (slider == null) slider = go.AddComponent<Slider>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.fillRect = fillRt;
        slider.handleRect = handleRt;
        slider.targetGraphic = handle;
        slider.transition = Selectable.Transition.ColorTint;

        ColorBlock colors = slider.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        colors.selectedColor = Color.white;
        slider.colors = colors;

        return slider;
    }

    static void PauseButton(Transform panel, string name, string labelText, float y)
    {
        Transform t = panel.Find(name);
        if (t == null) return;
        K.Rect(t, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(420f, 72f));
        K.MenuButton(t.gameObject, labelText, 26f);
    }

    // ── 화면 페이드 ───────────────────────────────────────────
    static void StyleFade(Transform fade)
    {
        if (fade == null) return;
        K.Img(fade.gameObject, K.Veil, K.White);
        K.Stretch(fade);
    }

    // ── 스크립트 연동 ─────────────────────────────────────────

    // 탄창 숫자를 PlayerFire 에 연결한다 (씬에 없으면 런타임에 알아서 찾는다)
    static void WireAmmo(Transform canvas)
    {
        PlayerFire fire = Object.FindAnyObjectByType<PlayerFire>();
        if (fire == null)
        {
            Debug.LogWarning("[HudStyler] 씬에 PlayerFire 가 없어 탄창 UI 는 런타임에 연결됩니다.");
            return;
        }

        AmmoBind(canvas, "DefaultGroup/AmmoBox/TextBox/CurrentAmmo", AmmoUI.DisplayType.Current, fire);
        AmmoBind(canvas, "DefaultGroup/AmmoBox/TextBox/TotalAmmo", AmmoUI.DisplayType.Reserve, fire);
    }

    static void AmmoBind(Transform canvas, string path, AmmoUI.DisplayType type, PlayerFire fire)
    {
        Transform t = canvas.Find(path);
        if (t == null) return;

        AmmoUI ui = t.GetComponent<AmmoUI>();
        if (ui == null) ui = t.gameObject.AddComponent<AmmoUI>();
        ui.displayType = type;
        ui.source = fire;
        EditorUtility.SetDirty(ui);
    }

    // 체력 숫자 / 게이지를 PlayerHealth 에 연결한다
    static void WireHealth(Transform canvas)
    {
        Transform hp = canvas.Find("DefaultGroup/HpBox");
        if (hp == null) return;

        HealthUI ui = hp.GetComponent<HealthUI>();
        if (ui == null) ui = hp.gameObject.AddComponent<HealthUI>();

        Transform value = hp.Find("HpValue");
        Transform fill = hp.Find("BG/Fill");
        ui.valueText = value != null ? value.GetComponent<TMP_Text>() : null;
        ui.fill = fill != null ? fill.GetComponent<Image>() : null;
        ui.normalColor = K.Sage;
        ui.lowColor = K.Accent;

        PlayerHealth health = Object.FindAnyObjectByType<PlayerHealth>();
        if (health == null)
        {
            // 총알은 IDamageable 을 찾으므로 콜라이더가 있는 플레이어 본체에 붙여야 한다
            PlayerFire fire = Object.FindAnyObjectByType<PlayerFire>();
            if (fire != null)
            {
                health = fire.gameObject.AddComponent<PlayerHealth>();
                Debug.Log("[HudStyler] " + fire.gameObject.name + " 에 PlayerHealth 를 새로 붙였습니다.");
            }
        }
        ui.source = health;
        if (health == null)
            Debug.LogWarning("[HudStyler] 플레이어를 찾지 못해 체력 UI 는 런타임에 연결됩니다.");

        EditorUtility.SetDirty(ui);
    }

    // ESC 토글 + 메뉴 버튼 연결
    static void WirePauseMenu(GameObject canvasGo, Transform group)
    {
        if (group == null) return;

        PauseMenu menu = canvasGo.GetComponent<PauseMenu>();
        if (menu == null) menu = canvasGo.AddComponent<PauseMenu>();

        menu.menuGroup = group.gameObject;
        menu.titleSceneName = "TITLE";

        Transform panel = group.Find("MenuPannel");
        if (panel == null) return;

        menu.mainPanel = panel.gameObject;
        menu.resumeButton = Bind(panel, "ReSume", new UnityEngine.Events.UnityAction(menu.Resume));
        menu.optionsButton = Bind(panel, "Option", new UnityEngine.Events.UnityAction(menu.OpenOptions));
        menu.exitButton = Bind(panel, "Exit", new UnityEngine.Events.UnityAction(menu.ExitToTitle));

        menu.options = WireOptions(canvasGo, group, panel.gameObject);

        EditorUtility.SetDirty(menu);
    }

    // 옵션 화면. 컨트롤러는 꺼져 있는 패널이 아니라 Canvas 에 둔다.
    static OptionsMenu WireOptions(GameObject canvasGo, Transform group, GameObject returnTo)
    {
        Transform panel = group.Find("OptionPannel");
        if (panel == null) return null;

        OptionsMenu options = canvasGo.GetComponent<OptionsMenu>();
        if (options == null) options = canvasGo.AddComponent<OptionsMenu>();

        options.panel = panel.gameObject;
        options.returnTo = returnTo;

        Transform slider = panel.Find("MasterSlider");
        Transform value = panel.Find("MasterValue");
        options.masterSlider = slider != null ? slider.GetComponent<Slider>() : null;
        options.masterValue = value != null ? value.GetComponent<TMP_Text>() : null;
        options.backButton = Bind(panel, "Back", new UnityEngine.Events.UnityAction(options.Close));

        EditorUtility.SetDirty(options);
        return options;
    }

    static Button Bind(Transform panel, string name, UnityEngine.Events.UnityAction call)
    {
        Transform t = panel.Find(name);
        if (t == null) return null;

        Button button = t.GetComponent<Button>();
        if (button == null) return null;

        // 도구를 다시 돌려도 같은 항목이 쌓이지 않도록 비우고 다시 건다
        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(button.onClick, i);
        UnityEventTools.AddPersistentListener(button.onClick, call);

        EditorUtility.SetDirty(button);
        return button;
    }

    // 위에서 내려오는 토스트.
    // 컨트롤러는 ToastGroup 에 두고 MessageBox 만 움직인다.
    static void WireToast(Transform group)
    {
        if (group == null) return;

        Transform box = group.Find("MessageBox");
        if (box == null) return;

        // 예전 버전에서 상자에 직접 붙였던 컴포넌트는 걷어낸다
        ToastMessage stale = box.GetComponent<ToastMessage>();
        if (stale != null) Object.DestroyImmediate(stale);

        CanvasGroup fade = box.GetComponent<CanvasGroup>();
        if (fade == null) fade = box.gameObject.AddComponent<CanvasGroup>();

        ToastMessage message = group.GetComponent<ToastMessage>();
        if (message == null) message = group.gameObject.AddComponent<ToastMessage>();

        message.box = box as RectTransform;
        message.label = box.GetComponentInChildren<TMP_Text>(true);
        message.group = fade;

        EditorUtility.SetDirty(message);
    }

    static Transform Find(Transform root, string path)
    {
        Transform t = root.Find(path);
        if (t == null) Debug.LogWarning("[HudStyler] 찾지 못함: " + path);
        return t;
    }
}
#endif
