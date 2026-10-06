using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Presentation for the home screen only; gameplay remains in DoorPuzzleGame.
public sealed class PuzzleMainMenu : MonoBehaviour
{
    private static readonly Color Ink = new Color32(46, 70, 79, 255);
    private static readonly Color Muted = new Color32(126, 152, 163, 255);
    private static Sprite floorSprite;
    private static Sprite buttonSprite;
    private static Sprite shadowSprite;
    private static Sprite gearSprite;
    private static Sprite playSprite;
    private RectTransform composition;
    private CanvasGroup menuInteraction;
    private GameObject settingsPanel;
    private Font font;

    public void Build(Font uiFont, UnityAction onPlay,
        UnityAction<bool> onMusicChanged, UnityAction<bool> onSoundChanged,
        UnityAction onSelectPuzzle)
    {
        font = uiFont;
        EnsureSprites();
        GameObject backdrop = new GameObject("Menu Background", typeof(RectTransform),
            typeof(PuzzleMenuBackdrop));
        backdrop.transform.SetParent(transform.parent, false);
        backdrop.transform.SetSiblingIndex(0);
        RectTransform backdropRect = backdrop.GetComponent<RectTransform>();
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = backdropRect.offsetMax = Vector2.zero;
        backdrop.GetComponent<PuzzleMenuBackdrop>().raycastTarget = false;

        composition = Container(transform, "Menu Composition", Vector2.zero,
            new Vector2(1080, 1920));
        RectTransform menu = Container(composition, "Main Menu Content", Vector2.zero,
            new Vector2(1080, 1920));
        menuInteraction = menu.gameObject.AddComponent<CanvasGroup>();
        BuildBoard(menu, new Vector2(0, 320));
        Label(menu, "Title", "DOOR PUZZLE", 104, new Vector2(0, -118),
            new Vector2(1000, 140), Ink, false);
        Label(menu, "Tagline", "Choose. Cross. Close.", 38, new Vector2(0, -220),
            new Vector2(880, 70), Muted, false);
        BuildPlayButton(menu, onPlay);
        BuildSelectPuzzleButton(menu, onSelectPuzzle);
        BuildSettingsButton(menu);
        BuildSettingsPanel(onMusicChanged, onSoundChanged);
        FitComposition();
    }

    private void LateUpdate()
    {
        FitComposition();
    }

    private void FitComposition()
    {
        if (composition == null) return;
        Rect available = ((RectTransform)transform).rect;
        float scale = Mathf.Min(available.width / 1080f, available.height / 1920f);
        composition.localScale = Vector3.one * Mathf.Max(0.01f, scale);
    }

    private void BuildPlayButton(Transform parent, UnityAction onPlay)
    {
        Vector2 position = new Vector2(0, -406);
        Vector2 size = new Vector2(632, 150);
        Shadow(parent, "Play Soft Shadow", position + new Vector2(0, -24), size, 0.18f);
        Image edge = ImageAt(parent, "Play Lower Edge", position + new Vector2(0, -3),
            size, new Color32(46, 90, 104, 255), PuzzleVisualStyle.RoundedSprite);
        edge.pixelsPerUnitMultiplier = 0.4f;
        Image face = ImageAt(parent, "PLAY", position, size, Color.white, buttonSprite);
        AddButton(face, onPlay);
        ImageAt(face.transform, "Play Symbol", new Vector2(-89, 0), new Vector2(48, 56),
            Color.white, playSprite);
        Label(face.transform, "Label", "PLAY", 50, new Vector2(37, 0),
            new Vector2(190, 90), Color.white, true);
    }

    private void BuildSettingsButton(Transform parent)
    {
        Vector2 position = new Vector2(423, 850);
        Vector2 size = new Vector2(100, 100);
        Shadow(parent, "Settings Shadow", position + new Vector2(0, -5), size, 0.10f);
        Image border = ImageAt(parent, "Settings Border", position, size,
            new Color32(220, 231, 234, 255), PuzzleVisualStyle.CircleSprite);
        Image face = ImageAt(border.transform, "Settings", Vector2.zero,
            size - Vector2.one * 3, new Color32(243, 248, 249, 255),
            PuzzleVisualStyle.CircleSprite);
        face.raycastTarget = true;
        AddButton(face, OpenSettings);
        ImageAt(face.transform, "Gear", Vector2.zero, new Vector2(52, 52),
            new Color32(108, 140, 155, 255), gearSprite);
    }

    private void BuildSelectPuzzleButton(Transform parent, UnityAction onClick)
    {
        Image face = ImageAt(parent, "Select Puzzle", new Vector2(0, -600),
            new Vector2(632, 104), PuzzleVisualStyle.Secondary, PuzzleVisualStyle.RoundedSprite);
        face.pixelsPerUnitMultiplier = 0.4f;
        AddButton(face, onClick);
        Label(face.transform, "Label", "SELECT PUZZLE", 34, Vector2.zero,
            new Vector2(580, 90), Ink, true);
    }

    private void BuildSettingsPanel(UnityAction<bool> onMusicChanged,
        UnityAction<bool> onSoundChanged)
    {
        Image overlay = ImageAt(composition, "SettingsPanel", Vector2.zero,
            new Vector2(1080, 1920), PuzzleVisualStyle.Overlay, null);
        overlay.raycastTarget = true;
        settingsPanel = overlay.gameObject;
        Shadow(overlay.transform, "Settings Card Shadow", new Vector2(0, -14),
            new Vector2(760, 570), 0.18f);
        Image card = ImageAt(overlay.transform, "Settings Card", Vector2.zero,
            new Vector2(760, 570), PuzzleVisualStyle.Surface, PuzzleVisualStyle.RoundedSprite);
        card.pixelsPerUnitMultiplier = 0.4f;
        Label(card.transform, "Title", "SETTINGS", 60, new Vector2(0, 190),
            new Vector2(640, 90), Ink, false);
        BuildAudioToggle(card.transform, "MUSIC", 50, onMusicChanged);
        BuildAudioToggle(card.transform, "SOUND", -76, onSoundChanged);
        Image close = ImageAt(card.transform, "Close Settings", new Vector2(0, -202),
            new Vector2(580, 96), PuzzleVisualStyle.Primary, PuzzleVisualStyle.RoundedSprite);
        AddButton(close, CloseSettings);
        Label(close.transform, "Label", "CLOSE", 36, Vector2.zero,
            new Vector2(500, 80), Color.white, true);
        settingsPanel.SetActive(false);
    }

    private void BuildAudioToggle(Transform parent, string caption, float y,
        UnityAction<bool> onChanged)
    {
        Image row = ImageAt(parent, caption + " Toggle", new Vector2(0, y),
            new Vector2(580, 104), PuzzleVisualStyle.Secondary, PuzzleVisualStyle.RoundedSprite);
        row.raycastTarget = true;
        Label(row.transform, "Label", caption, 34, new Vector2(-150, 0),
            new Vector2(230, 80), Ink, true);
        Image track = ImageAt(row.transform, "Switch Track", new Vector2(180, 0),
            new Vector2(148, 68), PuzzleVisualStyle.Primary, PuzzleVisualStyle.RoundedSprite);
        Image knob = ImageAt(track.transform, "Switch Knob", new Vector2(38, 0),
            new Vector2(52, 52), Color.white, PuzzleVisualStyle.CircleSprite);
        Text status = Label(row.transform, "Status", "ON", 28, new Vector2(46, 0),
            new Vector2(90, 72), PuzzleVisualStyle.Primary, true);
        Toggle toggle = row.gameObject.AddComponent<Toggle>();
        toggle.targetGraphic = row;
        toggle.transition = Selectable.Transition.None;
        toggle.toggleTransition = Toggle.ToggleTransition.None;
        toggle.SetIsOnWithoutNotify(true);
        toggle.onValueChanged.AddListener(enabled =>
        {
            track.color = enabled ? PuzzleVisualStyle.Primary : PuzzleVisualStyle.Border;
            knob.rectTransform.anchoredPosition = new Vector2(enabled ? 38 : -38, 0);
            status.text = enabled ? "ON" : "OFF";
            status.color = enabled ? PuzzleVisualStyle.Primary : Muted;
            if (onChanged != null) onChanged(enabled);
        });
    }

    private void OpenSettings()
    {
        menuInteraction.interactable = false;
        menuInteraction.blocksRaycasts = false;
        settingsPanel.SetActive(true);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    private void CloseSettings()
    {
        settingsPanel.SetActive(false);
        menuInteraction.interactable = true;
        menuInteraction.blocksRaycasts = true;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    private void OnDisable()
    {
        if (settingsPanel != null) CloseSettings();
    }

    private void BuildBoard(Transform parent, Vector2 position)
    {
        Vector2 size = new Vector2(568, 540);
        Shadow(parent, "Board Soft Shadow", position + new Vector2(0, -28), size, 0.19f);
        Image lower = ImageAt(parent, "Raised Board Base", position + new Vector2(0, -18),
            size, new Color32(222, 229, 228, 255), PuzzleVisualStyle.RoundedSprite);
        lower.pixelsPerUnitMultiplier = 0.4f;
        Image board = ImageAt(parent, "Four Room Preview", position, size,
            new Color32(253, 253, 251, 255), PuzzleVisualStyle.RoundedSprite);
        board.pixelsPerUnitMultiplier = 0.4f;
        Transform root = board.transform;

        for (int x = -1; x <= 1; x += 2)
        for (int y = -1; y <= 1; y += 2)
        {
            Vector2 center = new Vector2(x * 130, y * 126);
            Image rim = ImageAt(root, "Recessed Room Rim", center + new Vector2(0, 6),
                new Vector2(236, 226), new Color32(217, 220, 215, 255),
                PuzzleVisualStyle.RoundedSprite);
            rim.pixelsPerUnitMultiplier = 0.45f;
            ImageAt(root, "Room Floor", center + new Vector2(0, -3),
                new Vector2(236, 216), Color.white, floorSprite);
            Image patch = ImageAt(root, "Floor Inlay", center + new Vector2(17, -8),
                new Vector2(53, 53), new Color(0.68f, 0.66f, 0.61f, 0.07f),
                PuzzleVisualStyle.RoundedSprite);
            patch.pixelsPerUnitMultiplier = 1.5f;
        }

        Door(root, new Vector2(-130, 0), false, false);
        Door(root, new Vector2(0, 126), true, false);
        Door(root, new Vector2(0, -126), true, false);
        // Light is behind the green door, spilling into the upper right room.
        ImageAt(root, "Exit Light", new Vector2(130, 172), new Vector2(144, 146),
            new Color(0.60f, 1f, 0.72f, 0.13f), shadowSprite);
        Door(root, new Vector2(130, 244), false, true);
        Plant(root, new Vector2(-190, 172), 1f);
        Plant(root, new Vector2(184, -193), 1.1f);

        for (int i = 0; i < 3; i++)
            ImageAt(root, "Player Trail", new Vector2(-194 + i * 21, -205 + i * 23),
                Vector2.one * 14, new Color(0.33f, 0.64f, 0.79f, 0.20f),
                PuzzleVisualStyle.CircleSprite);
        Vector2 pawn = new Vector2(-123, -131);
        Shadow(root, "Pawn Shadow", pawn + new Vector2(2, -7), Vector2.one * 42, 0.20f);
        ImageAt(root, "Blue Pawn Base", pawn + new Vector2(0, -3), Vector2.one * 47,
            new Color32(32, 125, 166, 255), PuzzleVisualStyle.CircleSprite);
        ImageAt(root, "Blue Pawn", pawn, Vector2.one * 47,
            new Color32(49, 159, 207, 255), PuzzleVisualStyle.CircleSprite);
        ImageAt(root, "Pawn Highlight", pawn + new Vector2(-8, 9), Vector2.one * 17,
            new Color(0.65f, 0.90f, 1f, 0.16f), PuzzleVisualStyle.CircleSprite);
    }

    private static void Door(Transform parent, Vector2 position, bool vertical, bool exit)
    {
        Vector2 size = vertical ? new Vector2(22, 78) : new Vector2(78, 28);
        Color baseColor = exit ? new Color32(72, 142, 97, 255) : new Color32(204, 126, 48, 255);
        Color faceColor = exit ? new Color32(107, 179, 129, 255) : new Color32(242, 173, 83, 255);
        Image body = ImageAt(parent, exit ? "Green Exit" : "Amber Door", position,
            size, baseColor, PuzzleVisualStyle.RoundedSprite);
        body.pixelsPerUnitMultiplier = 2.5f;
        Image face = ImageAt(body.transform, "Door Face", new Vector2(0, 3),
            size - new Vector2(4, 6), faceColor, PuzzleVisualStyle.RoundedSprite);
        face.pixelsPerUnitMultiplier = 2.5f;
    }

    private static void Plant(Transform parent, Vector2 position, float scale)
    {
        RectTransform root = Container(parent, "Preview Plant", position, Vector2.one * 56);
        root.localScale = Vector3.one * scale;
        ImageAt(root, "Pot Shadow", new Vector2(2, -6), new Vector2(28, 20),
            new Color(0.32f, 0.36f, 0.29f, 0.12f), PuzzleVisualStyle.CircleSprite);
        ImageAt(root, "Pot", new Vector2(0, -10), new Vector2(23, 26),
            new Color32(203, 192, 169, 255), PuzzleVisualStyle.CircleSprite);
        ImageAt(root, "Soil", new Vector2(0, -2), new Vector2(21, 10),
            new Color32(158, 153, 129, 255), PuzzleVisualStyle.CircleSprite);
        for (int i = 0; i < 5; i++)
        {
            float angle = 16 + i * 72;
            Vector2 offset = new Vector2(Mathf.Sin(angle * Mathf.Deg2Rad),
                Mathf.Cos(angle * Mathf.Deg2Rad)) * 13;
            Image leaf = ImageAt(root, "Leaf", offset + new Vector2(0, 10),
                new Vector2(17, 30), Color.Lerp(new Color32(132, 158, 127, 255),
                    new Color32(174, 192, 157, 255), i / 4f), PuzzleVisualStyle.CircleSprite);
            leaf.rectTransform.localRotation = Quaternion.Euler(0, 0, -angle);
        }
        ImageAt(root, "Plant Center", new Vector2(0, 10), Vector2.one * 12,
            new Color32(169, 186, 150, 255), PuzzleVisualStyle.CircleSprite);
    }

    private static void AddButton(Image face, UnityAction action)
    {
        face.raycastTarget = true;
        Button button = face.gameObject.AddComponent<Button>();
        button.targetGraphic = face;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.96f, 0.98f, 1f);
        colors.pressedColor = new Color(0.83f, 0.90f, 0.93f);
        colors.selectedColor = Color.white;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        if (action != null) button.onClick.AddListener(action);
    }

    private Text Label(Transform parent, string name, string caption, int size,
        Vector2 position, Vector2 bounds, Color color, bool bold)
    {
        RectTransform root = Container(parent, name, position, bounds);
        Text label = root.gameObject.AddComponent<Text>();
        label.font = font;
        label.fontSize = size;
        label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        label.alignment = TextAnchor.MiddleCenter;
        label.text = caption;
        label.color = color;
        label.raycastTarget = false;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        return label;
    }

    private static RectTransform Container(Transform parent, string name,
        Vector2 position, Vector2 size)
    {
        GameObject child = new GameObject(name, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static Image ImageAt(Transform parent, string name, Vector2 position,
        Vector2 size, Color color, Sprite sprite)
    {
        Image image = Container(parent, name, position, size).gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        if (sprite == PuzzleVisualStyle.RoundedSprite || sprite == shadowSprite)
            image.type = Image.Type.Sliced;
        return image;
    }

    private static void Shadow(Transform parent, string name, Vector2 position,
        Vector2 size, float opacity)
    {
        ImageAt(parent, name, position, size + Vector2.one * 80,
            new Color(0.19f, 0.32f, 0.36f, opacity), shadowSprite);
    }

    private static void EnsureSprites()
    {
        if (floorSprite == null) floorSprite = CreateSprite("Menu Floor", 0);
        if (buttonSprite == null) buttonSprite = CreateSprite("Menu Play Button", 1);
        if (shadowSprite == null) shadowSprite = CreateSprite("Menu Soft Shadow", 2);
        if (gearSprite == null) gearSprite = CreateSprite("Menu Gear", 3);
        if (playSprite == null) playSprite = CreateSprite("Menu Play Triangle", 4);
    }

    private static Sprite CreateSprite(string name, int kind)
    {
        const int size = 256;
        int width = kind == 1 ? 1024 : size;
        Texture2D texture = new Texture2D(width, size, TextureFormat.RGBA32, true);
        texture.name = name;
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Trilinear;
        Color[] pixels = new Color[width * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < width; x++)
        {
            Vector2 p = new Vector2((x + 0.5f) / width - 0.5f, (y + 0.5f) / size - 0.5f);
            Color pixel = Color.white;
            float distance;
            if (kind == 2)
            {
                distance = RoundedDistance(p, new Vector2(0.32f, 0.32f), 0.08f);
                float outside = Mathf.Max(0, distance);
                pixel.a = Mathf.Exp(-outside * outside / 0.008f);
            }
            else if (kind == 3)
            {
                float angle = Mathf.Atan2(p.y, p.x);
                float tooth = Mathf.Abs(Mathf.DeltaAngle(angle * Mathf.Rad2Deg, 
                    Mathf.Round(angle * Mathf.Rad2Deg / 45f) * 45f));
                float radius = Mathf.Lerp(0.32f, 0.43f,
                    1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(7f, 17f, tooth)));
                distance = Mathf.Max(p.magnitude - radius, 0.16f - p.magnitude);
                pixel.a = Mathf.Clamp01(0.5f - distance * size);
            }
            else if (kind == 4)
            {
                Vector2 a = new Vector2(-0.28f, -0.34f);
                Vector2 b = new Vector2(0.32f, 0);
                Vector2 c = new Vector2(-0.28f, 0.34f);
                float edge = Mathf.Min(PointToSegment(p, a, b),
                    Mathf.Min(PointToSegment(p, b, c), PointToSegment(p, c, a)));
                bool inside = Cross(b - a, p - a) >= 0 && Cross(c - b, p - b) >= 0 &&
                    Cross(a - c, p - c) >= 0;
                distance = (inside ? -edge : edge) - 0.02f;
                pixel.a = Mathf.Clamp01(0.5f - distance * size);
            }
            else
            {
                Vector2 shapePoint = kind == 1 ? new Vector2(p.x * 4.2f, p.y) : p;
                Vector2 halfSize = kind == 1 ? new Vector2(2.095f, 0.495f) : Vector2.one * 0.495f;
                distance = RoundedDistance(shapePoint, halfSize, kind == 0 ? 0.085f : 0.21f);
                pixel = kind == 0
                    ? Color.Lerp(new Color32(239, 237, 229, 255), new Color32(215, 214, 205, 255),
                        Mathf.Pow(Mathf.Clamp01(p.y + 0.5f), 5f))
                    : Color.Lerp(new Color32(53, 99, 113, 255), new Color32(81, 137, 155, 255),
                        p.y + 0.5f);
                pixel.a = Mathf.Clamp01(0.5f - distance * size);
            }
            pixels[y * width + x] = pixel;
        }
        texture.SetPixels(pixels);
        texture.Apply(true, true);
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, width, size),
            new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect,
            kind == 2 ? Vector4.one * 96 : Vector4.zero);
        sprite.name = name;
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    private static float RoundedDistance(Vector2 p, Vector2 halfSize, float radius)
    {
        Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - halfSize + Vector2.one * radius;
        return new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude +
            Mathf.Min(Mathf.Max(q.x, q.y), 0) - radius;
    }

    private static float PointToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 edge = b - a;
        return Vector2.Distance(p, a + edge * Mathf.Clamp01(Vector2.Dot(p - a, edge) / edge.sqrMagnitude));
    }

    private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
}
