using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Presentation for the home screen only; gameplay remains in DoorPuzzleGame.
public sealed class PuzzleMainMenu : MonoBehaviour
{
    private static readonly Color Ink = PuzzleVisualStyle.Text;
    private static readonly Color Muted = PuzzleVisualStyle.MutedText;
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
        Vector2 size = new Vector2(632, 108);
        Image face = ImageAt(parent, "PLAY", position, size,
            PuzzleVisualStyle.Primary, PuzzleVisualStyle.RoundedSprite);
        AddButton(face, onPlay);
        ImageAt(face.transform, "Play Symbol", new Vector2(-89, 0), new Vector2(48, 56),
            PuzzleVisualStyle.Surface, playSprite);
        Label(face.transform, "Label", "PLAY", 50, new Vector2(37, 0),
            new Vector2(190, 90), PuzzleVisualStyle.Surface, true);
    }

    private void BuildSettingsButton(Transform parent)
    {
        Vector2 position = new Vector2(423, 850);
        Vector2 size = new Vector2(100, 100);
        Image border = ImageAt(parent, "Settings Border", position, size,
            PuzzleVisualStyle.Border, PuzzleVisualStyle.CircleSprite);
        Image face = ImageAt(border.transform, "Settings", Vector2.zero,
            size - Vector2.one * 3, PuzzleVisualStyle.Background,
            PuzzleVisualStyle.CircleSprite);
        face.raycastTarget = true;
        AddButton(face, OpenSettings);
        ImageAt(face.transform, "Gear", Vector2.zero, new Vector2(52, 52),
            PuzzleVisualStyle.MutedText, gearSprite);
    }

    private void BuildSelectPuzzleButton(Transform parent, UnityAction onClick)
    {
        Image face = ImageAt(parent, "Select Puzzle", new Vector2(0, -600),
            new Vector2(632, 108), PuzzleVisualStyle.Secondary, PuzzleVisualStyle.RoundedSprite);
        AddButton(face, onClick);
        Label(face.transform, "Label", "SELECT PUZZLE", 34, Vector2.zero,
            new Vector2(580, 90), Ink, false);
    }

    private void BuildSettingsPanel(UnityAction<bool> onMusicChanged,
        UnityAction<bool> onSoundChanged)
    {
        Image overlay = ImageAt(composition, "SettingsPanel", Vector2.zero,
            new Vector2(1080, 1920), PuzzleVisualStyle.Overlay, null);
        overlay.raycastTarget = true;
        settingsPanel = overlay.gameObject;
        Image card = ImageAt(overlay.transform, "Settings Card", Vector2.zero,
            new Vector2(760, 570), PuzzleVisualStyle.Surface, PuzzleVisualStyle.RoundedSprite);
        Label(card.transform, "Title", "SETTINGS", 60, new Vector2(0, 190),
            new Vector2(640, 90), Ink, false);
        BuildAudioToggle(card.transform, "MUSIC", 50, onMusicChanged);
        BuildAudioToggle(card.transform, "SOUND", -76, onSoundChanged);
        Image close = ImageAt(card.transform, "Close Settings", new Vector2(0, -202),
            new Vector2(580, 108), PuzzleVisualStyle.Primary, PuzzleVisualStyle.RoundedSprite);
        AddButton(close, CloseSettings);
        Label(close.transform, "Label", "CLOSE", 36, Vector2.zero,
            new Vector2(500, 80), PuzzleVisualStyle.Surface, true);
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
            new Vector2(52, 52), PuzzleVisualStyle.Surface, PuzzleVisualStyle.CircleSprite);
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
        // Abstract preview uses the same floors, walls and accents as gameplay.
        // The existing menu composition and button actions remain unchanged.
        Vector2 size = new Vector2(568, 540);
        Image board = ImageAt(parent, "Four Room Preview", position, size,
            PuzzleVisualStyle.Wall, PuzzleVisualStyle.RoundedSprite);
        Transform root = board.transform;
        for (int x = -1; x <= 1; x += 2)
        for (int y = -1; y <= 1; y += 2)
            ImageAt(root, "Room Floor", new Vector2(x * 130, y * 126),
                new Vector2(240, 226), PuzzleVisualStyle.Surface, null);

        Door(root, new Vector2(-130, 0), false, false);
        Door(root, new Vector2(0, 126), true, false);
        Door(root, new Vector2(0, -126), true, false);
        Door(root, new Vector2(130, 254), false, true);
        Vector2 pawn = new Vector2(-123, -131);
        ImageAt(root, "Pawn Rim", pawn, Vector2.one * 51,
            PuzzleVisualStyle.Surface, PuzzleVisualStyle.CircleSprite);
        ImageAt(root, "Pawn", pawn, Vector2.one * 47,
            PuzzleVisualStyle.Player, PuzzleVisualStyle.CircleSprite);
    }

    private static void Door(Transform parent, Vector2 position, bool vertical, bool exit)
    {
        Vector2 opening = vertical ? new Vector2(24, 78) : new Vector2(78, exit ? 34 : 30);
        ImageAt(parent, "Door Opening", position, opening, PuzzleVisualStyle.Surface, null);
        Vector2 leaf = vertical ? new Vector2(9, 68) : new Vector2(68, 9);
        ImageAt(parent, exit ? "Exit" : "Door", position, leaf,
            exit ? PuzzleVisualStyle.ExitOpen : PuzzleVisualStyle.DoorOpen, null);
    }

    private static void AddButton(Image face, UnityAction action)
    {
        face.raycastTarget = true;
        Button button = face.gameObject.AddComponent<Button>();
        button.targetGraphic = face;
        ColorBlock colors = button.colors;
        colors.highlightedColor = PuzzleVisualStyle.ButtonHighlight;
        colors.pressedColor = PuzzleVisualStyle.ButtonPressed;
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = PuzzleVisualStyle.ButtonDisabled;
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
        if (sprite == PuzzleVisualStyle.RoundedSprite)
        {
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = PuzzleVisualStyle.ButtonCornerScale;
        }
        return image;
    }

    private static void EnsureSprites()
    {
        if (gearSprite == null) gearSprite = CreateSprite("Menu Gear", true);
        if (playSprite == null) playSprite = CreateSprite("Menu Play Triangle", false);
    }

    private static Sprite CreateSprite(string name, bool gear)
    {
        const int size = 256;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
        texture.name = name;
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Trilinear;
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            Vector2 p = new Vector2((x + 0.5f) / size - 0.5f, (y + 0.5f) / size - 0.5f);
            float distance;
            if (gear)
            {
                float angle = Mathf.Atan2(p.y, p.x);
                float tooth = Mathf.Abs(Mathf.DeltaAngle(angle * Mathf.Rad2Deg,
                    Mathf.Round(angle * Mathf.Rad2Deg / 45f) * 45f));
                float radius = Mathf.Lerp(0.32f, 0.43f,
                    1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(7f, 17f, tooth)));
                distance = Mathf.Max(p.magnitude - radius, 0.16f - p.magnitude);
            }
            else
            {
                Vector2 a = new Vector2(-0.28f, -0.34f);
                Vector2 b = new Vector2(0.32f, 0);
                Vector2 c = new Vector2(-0.28f, 0.34f);
                float edge = Mathf.Min(PointToSegment(p, a, b),
                    Mathf.Min(PointToSegment(p, b, c), PointToSegment(p, c, a)));
                bool inside = Cross(b - a, p - a) >= 0 && Cross(c - b, p - b) >= 0 &&
                    Cross(a - c, p - c) >= 0;
                distance = (inside ? -edge : edge) - 0.02f;
            }
            pixels[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(0.5f - distance * size));
        }
        texture.SetPixels(pixels);
        texture.Apply(true, true);
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect);
        sprite.name = name;
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    private static float PointToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 edge = b - a;
        return Vector2.Distance(p, a + edge * Mathf.Clamp01(Vector2.Dot(p - a, edge) / edge.sqrMagnitude));
    }

    private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
}
