#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// 타이틀 화면과 인게임 HUD 가 함께 쓰는 색 / 스프라이트 / UI 생성 헬퍼.
public static class HaneulUiKit
{
    public const string UiFolder = "Assets/Haneul_Branch/Sprite/UI";

    // ── 팔레트 ────────────────────────────────────────────────
    public const string AccentHex = "D93A44";

    public static readonly Color Bg       = Hex("EDE5D8");  // 종이 같은 베이지 바탕
    public static readonly Color BgPanel  = Hex("E1D7C6");  // 한 톤 어두운 면
    public static readonly Color BgPanel2 = Hex("D6CAB5");
    public static readonly Color Fg       = Hex("221E19");  // 글자 / 선
    public static readonly Color Muted    = Hex("8C8171");
    public static readonly Color Accent   = Hex(AccentHex); // 강조 색
    public static readonly Color Sage     = Hex("8FB79A");  // 보조 띄 (연한 초록)
    public static readonly Color OnAccent = Hex("F7F2E8");  // 액센트 위에 얹는 밝은 색
    public static readonly Color Veil     = Hex("4A4238");  // 비네트 / 화면 어둡게

    // ── 공용 애셋 ─────────────────────────────────────────────
    public static Sprite White { get; private set; }
    public static Sprite Border { get; private set; }
    public static Sprite Vignette { get; private set; }
    public static Texture2D Grid { get; private set; }
    public static TMP_FontAsset Font { get; private set; }

    // 스프라이트가 없으면 만들어서 임포트하고, 정적 필드에 물려둔다.
    public static void EnsureAssets()
    {
        if (!AssetDatabase.IsValidFolder(UiFolder))
            Directory.CreateDirectory(UiFolder);

        string white    = UiFolder + "/ui_white.png";
        string border   = UiFolder + "/ui_border.png";
        string vignette = UiFolder + "/ui_vignette.png";
        string grid     = UiFolder + "/ui_grid.png";

        bool created = false;
        if (!File.Exists(white))    { WritePng(white, MakeWhite()); created = true; }
        if (!File.Exists(border))   { WritePng(border, MakeBorder()); created = true; }
        if (!File.Exists(vignette)) { WritePng(vignette, MakeVignette()); created = true; }
        if (!File.Exists(grid))     { WritePng(grid, MakeGrid()); created = true; }

        if (created)
        {
            AssetDatabase.Refresh();
            ImportAsSprite(white, Vector4.zero, TextureWrapMode.Clamp);
            ImportAsSprite(border, new Vector4(3f, 3f, 3f, 3f), TextureWrapMode.Clamp);
            ImportAsSprite(vignette, Vector4.zero, TextureWrapMode.Clamp);
            ImportAsTexture(grid, TextureWrapMode.Repeat);
        }

        White    = AssetDatabase.LoadAssetAtPath<Sprite>(white);
        Border   = AssetDatabase.LoadAssetAtPath<Sprite>(border);
        Vignette = AssetDatabase.LoadAssetAtPath<Sprite>(vignette);
        Grid     = AssetDatabase.LoadAssetAtPath<Texture2D>(grid);
        Font     = LoadFont();
    }

    // ── 계층 헬퍼 ─────────────────────────────────────────────
    public static GameObject NewUI(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    // 같은 이름의 자식이 있으면 재사용한다 (도구를 여러 번 실행해도 안전하도록)
    public static GameObject Child(Transform parent, string name)
    {
        Transform t = parent.Find(name);
        return t != null ? t.gameObject : NewUI(name, parent);
    }

    public static Image Img(GameObject go, Color color, Sprite sprite)
    {
        Image img = go.GetComponent<Image>();
        if (img == null) img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.type = Image.Type.Simple;
        img.fillCenter = true;
        img.raycastTarget = false;
        return img;
    }

    public static TextMeshProUGUI Text(Transform parent, string name, string content, float size, Color color)
    {
        GameObject go = Child(parent, name);
        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        if (t == null) t = go.AddComponent<TextMeshProUGUI>();
        t.text = content;
        Style(t, size, color);
        return t;
    }

    // 내용은 건드리지 않고 서식만 맞춘다 (런타임 스크립트가 채우는 텍스트용)
    public static TMP_Text Style(TMP_Text t, float size, Color color)
    {
        if (t == null) return null;
        if (Font != null) t.font = Font;
        t.fontSize = size;
        t.color = color;
        t.richText = true;
        t.raycastTarget = false;
        t.overflowMode = TextOverflowModes.Overflow;
        return t;
    }

    public static void Rect(Component c, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        RectTransform rt = c.transform as RectTransform;
        if (rt == null) return;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    public static void Stretch(Component c, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f)
    {
        RectTransform rt = c.transform as RectTransform;
        if (rt == null) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    // 한쪽 변에 붙는 얇은 띠 (구분선, 액센트 바)
    public static Image Bar(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
                            Vector2 pivot, Vector2 pos, Vector2 size, Color color)
    {
        Image img = Img(Child(parent, name), color, White);
        RectTransform rt = img.rectTransform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return img;
    }

    // 사각 테두리 (9-slice, 가운데는 비어 있음)
    public static Image Frame(Transform parent, string name, Color color)
    {
        Image img = Img(Child(parent, name), color, Border);
        img.type = Image.Type.Sliced;
        img.fillCenter = false;
        Stretch(img);
        return img;
    }

    // ── 메뉴 버튼 (타이틀 / 일시정지 공용) ────────────────────
    // 호버하면 왼쪽부터 액센트가 차오르고 글자가 반전된다.
    public static Button MenuButton(GameObject go, string labelText, float fontSize = 30f)
    {
        Image bg = Img(go, WithA(Fg, 0.05f), White);
        bg.raycastTarget = true;

        Image fill = Img(Child(go.transform, "Fill"), WithA(Accent, 0.92f), White);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 0f;
        Stretch(fill);

        Image border = Frame(go.transform, "Border", WithA(Fg, 0.32f));

        Image tick = Img(Child(go.transform, "Tick"), Accent, White);
        RectTransform tickRt = tick.rectTransform;
        tickRt.anchorMin = new Vector2(0f, 0f);
        tickRt.anchorMax = new Vector2(0f, 1f);
        tickRt.pivot = new Vector2(0f, 0.5f);
        tickRt.anchoredPosition = Vector2.zero;
        tickRt.sizeDelta = new Vector2(6f, 0f);

        TextMeshProUGUI label = Text(go.transform, "Label", labelText, fontSize, Fg);
        label.fontStyle = FontStyles.Bold;
        label.characterSpacing = 12f;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        Stretch(label, 34f, 20f);

        Button button = go.GetComponent<Button>();
        if (button == null) button = go.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = bg;

        TitleMenuButton fx = go.GetComponent<TitleMenuButton>();
        if (fx == null) fx = go.AddComponent<TitleMenuButton>();
        fx.fill = fill;
        fx.border = border;
        fx.tick = tickRt;
        fx.label = label;
        fx.idleLabel = Fg;
        fx.hoverLabel = OnAccent;

        return button;
    }

    // ── 색 유틸 ───────────────────────────────────────────────
    public static Color Hex(string hex)
    {
        Color c;
        ColorUtility.TryParseHtmlString("#" + hex, out c);
        return c;
    }

    public static Color WithA(Color c, float a)
    {
        c.a = a;
        return c;
    }

    // ── 스프라이트 생성 ───────────────────────────────────────
    static Texture2D MakeWhite()
    {
        Texture2D t = new Texture2D(8, 8, TextureFormat.RGBA32, false);
        Color[] px = new Color[64];
        for (int i = 0; i < px.Length; i++) px[i] = Color.white;
        t.SetPixels(px);
        t.Apply();
        return t;
    }

    static Texture2D MakeBorder()
    {
        const int n = 32, b = 3;
        Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                bool edge = x < b || y < b || x >= n - b || y >= n - b;
                t.SetPixel(x, y, edge ? Color.white : new Color(1f, 1f, 1f, 0f));
            }
        t.Apply();
        return t;
    }

    static Texture2D MakeVignette()
    {
        const int w = 512, h = 288;
        Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float nx = (x / (w - 1f)) * 2f - 1f;
                float ny = (y / (h - 1f)) * 2f - 1f;
                float d = Mathf.Sqrt(nx * nx * 0.82f + ny * ny);
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f, 1.28f, d)) * 0.88f;
                t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        t.Apply();
        return t;
    }

    static Texture2D MakeGrid()
    {
        const int n = 64;
        Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                bool line = x == 0 || y == 0;
                t.SetPixel(x, y, line ? Color.white : new Color(1f, 1f, 1f, 0f));
            }
        t.Apply();
        return t;
    }

    static void WritePng(string path, Texture2D tex)
    {
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    static void ImportAsSprite(string path, Vector4 border, TextureWrapMode wrap)
    {
        TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return;

        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.filterMode = FilterMode.Bilinear;
        ti.wrapMode = wrap;
        ti.textureCompression = TextureImporterCompression.Uncompressed;

        TextureImporterSettings settings = new TextureImporterSettings();
        ti.ReadTextureSettings(settings);
        settings.spriteBorder = border;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        ti.SetTextureSettings(settings);

        ti.SaveAndReimport();
    }

    static void ImportAsTexture(string path, TextureWrapMode wrap)
    {
        TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return;

        ti.textureType = TextureImporterType.Default;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.wrapMode = wrap;
        ti.filterMode = FilterMode.Bilinear;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.SaveAndReimport();
    }

    static TMP_FontAsset LoadFont()
    {
        TMP_FontAsset f = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if (f == null && TMP_Settings.instance != null) f = TMP_Settings.defaultFontAsset;
        return f;
    }
}
#endif
