using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;

public sealed class PuzzleUI : MonoBehaviour
{
    private enum ButtonStyle { Primary, Secondary, Retry }
    private enum Icon { Play, Restart, Next, Home, Check, Close }

    private readonly List<RaycastResult> pointerHits = new List<RaycastResult>();
    private readonly List<RectTransform> safeAreas = new List<RectTransform>();
    private readonly List<Text> contextLabels = new List<Text>();
    private readonly List<Graphic> gameplayButtonInk = new List<Graphic>();
    private Text levelText;
    private Text puzzleText;
    private Text doorsText;
    private Text levelCompleteText;
    private Text chooseStartText;
    private Image headerRule;
    private Canvas uiCanvas;
    private GameObject chooseStartHint;
    private GameObject mainMenuPanel;
    private GameObject gameplayPanel;
    private GameObject puzzleCompletePanel;
    private GameObject losePanel;
    private GameObject levelCompletePanel;
    private GameObject gameCompletePanel;
    private GameObject puzzleSelectionPanel;
    private PuzzleSelectionView puzzleSelection;
    private GameObject[] panels;
    private Font font;
    private Rect lastSafeArea;
    private int lastWidth = -1;
    private int lastHeight = -1;

    public void Build(UnityAction onPlay, UnityAction onRestart,
        UnityAction onNextPuzzle, UnityAction onNextLevel, UnityAction onHome,
        UnityAction<bool> onMusicChanged, UnityAction<bool> onSoundChanged,
        UnityAction onOpenSelection, UnityAction<int> onSelectPuzzle, PuzzleProgress progress)
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 32);

        if (EventSystem.current == null)
        {
            GameObject eventObject = new GameObject("Event System", typeof(EventSystem),
                typeof(InputSystemUIInputModule));
            eventObject.transform.SetParent(transform, false);
            eventObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        GameObject canvasObject = new GameObject("Puzzle UI", typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        uiCanvas = canvas;
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        Transform root = canvasObject.transform;
        mainMenuPanel = MakePanel(root, "MainMenuPanel", true, false);
        gameplayPanel = MakePanel(root, "GameplayPanel", false, false);
        puzzleCompletePanel = MakePanel(root, "PuzzleCompletePanel", true, true);
        losePanel = MakePanel(root, "LosePanel", true, true);
        levelCompletePanel = MakePanel(root, "LevelCompletePanel", true, true);
        gameCompletePanel = MakePanel(root, "GameCompletePanel", true, true);
        puzzleSelectionPanel = MakePanel(root, "PuzzleSelectionPanel", true, false);
        panels = new[] { mainMenuPanel, gameplayPanel, puzzleCompletePanel,
            losePanel, levelCompletePanel, gameCompletePanel, puzzleSelectionPanel };

        BuildMainMenu(onPlay, onMusicChanged, onSoundChanged, onOpenSelection);
        BuildGameplayPanel(onRestart, onHome);
        Transform selectionRoot = MakeSafeContent(puzzleSelectionPanel, "Content");
        puzzleSelection = selectionRoot.gameObject.AddComponent<PuzzleSelectionView>();
        puzzleSelection.Build(font, progress, onSelectPuzzle, onHome);

        Transform correct = MakeResultCard(puzzleCompletePanel, 592,
            PuzzleVisualStyle.Success, Icon.Check);
        MakeCenteredText(correct, "Title", "CORRECT!", 72, 18, PuzzleVisualStyle.Text);
        MakeCenteredButton(correct, "NEXT", -168, onNextPuzzle, ButtonStyle.Primary, Icon.Next);

        Transform lose = MakeResultCard(losePanel, 640,
            PuzzleVisualStyle.Danger, Icon.Close);
        MakeCenteredText(lose, "Title", "YOU LOSE", 72, 18, PuzzleVisualStyle.Text);
        MakeCenteredText(lose, "Message", "Path blocked", 34, -66, PuzzleVisualStyle.MutedText);
        MakeCenteredButton(lose, "RETRY", -204, onRestart, ButtonStyle.Retry, Icon.Restart);

        Transform complete = MakeResultCard(levelCompletePanel, 592,
            PuzzleVisualStyle.Success, Icon.Check);
        levelCompleteText = MakeCenteredText(complete, "Title", "LEVEL 1 COMPLETE",
            60, 18, PuzzleVisualStyle.Text);
        MakeCenteredButton(complete, "NEXT LEVEL", -168, onNextLevel, ButtonStyle.Primary, Icon.Next);

        Transform gameComplete = MakeResultCard(gameCompletePanel, 720,
            PuzzleVisualStyle.Success, Icon.Check);
        MakeCenteredText(gameComplete, "Title", "GAME COMPLETE", 64, 76, PuzzleVisualStyle.Text);
        MakeCenteredButton(gameComplete, "PLAY AGAIN", -84, onPlay, ButtonStyle.Primary, Icon.Play);
        MakeCenteredButton(gameComplete, "HOME", -220, onHome, ButtonStyle.Secondary, Icon.Home);

        // Result overlays also offer a way home without having to retry or advance.
        BuildHomeFooter(puzzleCompletePanel, onHome);
        BuildHomeFooter(losePanel, onHome);
        BuildHomeFooter(levelCompletePanel, onHome);

        UpdateSafeAreas();
        ShowState(PuzzleGameState.MainMenu);
    }

    private void BuildMainMenu(UnityAction onPlay,
        UnityAction<bool> onMusicChanged, UnityAction<bool> onSoundChanged,
        UnityAction onSelectPuzzle)
    {
        Transform root = MakeSafeContent(mainMenuPanel, "Content");
        root.gameObject.AddComponent<PuzzleMainMenu>().Build(font, onPlay,
            onMusicChanged, onSoundChanged, onSelectPuzzle);
    }

    private void BuildGameplayPanel(UnityAction onRestart, UnityAction onHome)
    {
        Transform root = MakeSafeContent(gameplayPanel, "GameplayHUD");
        levelText = MakeText(root, "Level", 28, PuzzleVisualStyle.MutedText);
        levelText.alignment = TextAnchor.MiddleLeft;
        Place(levelText.rectTransform, new Vector2(0.5f, 1), new Vector2(-220, -60),
            new Vector2(440, 54));

        puzzleText = MakeText(root, "Puzzle", 50, PuzzleVisualStyle.Text);
        puzzleText.fontStyle = FontStyle.Bold;
        puzzleText.alignment = TextAnchor.MiddleLeft;
        Place(puzzleText.rectTransform, new Vector2(0.5f, 1), new Vector2(-190, -118),
            new Vector2(500, 72));

        doorsText = MakeText(root, "Doors", 30, PuzzleVisualStyle.MutedText);
        doorsText.alignment = TextAnchor.MiddleRight;
        Place(doorsText.rectTransform, new Vector2(0.5f, 1), new Vector2(316, -118),
            new Vector2(248, 72));

        headerRule = MakeImage(root, "Header Rule", new Vector2(0.5f, 1), new Vector2(0, -180),
            new Vector2(880, 2), PuzzleVisualStyle.Border);

        chooseStartHint = new GameObject("Choose Start Room", typeof(RectTransform));
        chooseStartHint.transform.SetParent(root, false);
        Place(chooseStartHint.GetComponent<RectTransform>(), new Vector2(0.5f, 1),
            new Vector2(0, -246), new Vector2(620, 76));
        Text hint = MakeText(chooseStartHint.transform, "Label", 30, PuzzleVisualStyle.Primary);
        chooseStartText = hint;
        hint.text = "CHOOSE START ROOM";
        Stretch(hint.rectTransform);

        MakeButton(root, "RESTART", new Vector2(0.5f, 0), new Vector2(-220, 92),
            new Vector2(360, 108), onRestart, ButtonStyle.Secondary, Icon.Restart, true);
        MakeButton(root, "HOME", new Vector2(0.5f, 0), new Vector2(220, 92),
            new Vector2(360, 108), onHome, ButtonStyle.Secondary, Icon.Home, true);
    }

    private void BuildHomeFooter(GameObject panel, UnityAction onHome)
    {
        Transform root = MakeSafeContent(panel, "Home Footer");
        MakeButton(root, "HOME", new Vector2(0.5f, 0), new Vector2(0, 92),
            new Vector2(360, 108), onHome, ButtonStyle.Secondary, Icon.Home);
    }

    private Transform MakeResultCard(GameObject panel, float height, Color accent,
        Icon icon)
    {
        Transform root = MakeSafeContent(panel, "Content");
        GameObject card = MakeImage(root, "Result Surface", new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(864, height), PuzzleVisualStyle.Surface,
            PuzzleVisualStyle.RoundedSprite, PuzzleVisualStyle.ButtonCornerScale).gameObject;
        Text context = MakeCenteredText(card.transform, "Context", "", 28,
            height * 0.5f - 54, PuzzleVisualStyle.MutedText);
        contextLabels.Add(context);
        Image badge = MakeImage(card.transform, "Status Badge", new Vector2(0.5f, 0.5f),
            new Vector2(0, height * 0.5f - 158), new Vector2(80, 80), Color.clear);
        MakeIcon(badge.transform, icon, Vector2.zero, 40, accent);
        return card.transform;
    }

    public void SetProgress(int levelNumber, int puzzleNumber, int puzzleCount,
        int closed, int doorCount)
    {
        levelText.text = "LEVEL " + levelNumber;
        puzzleText.text = "PUZZLE " + puzzleNumber + "/" + puzzleCount;
        doorsText.text = "Doors " + closed + "/" + doorCount;
        levelCompleteText.text = "LEVEL " + levelNumber + " COMPLETE";
        foreach (Text label in contextLabels)
            label.text = "LEVEL " + levelNumber + "  /  PUZZLE " + puzzleNumber + "/" + puzzleCount;
    }

    public void SetGameplayPalette(PuzzlePalette palette)
    {
        levelText.color = palette.MutedText;
        puzzleText.color = palette.HeaderText;
        doorsText.color = palette.MutedText;
        headerRule.color = palette.HeaderRule;
        chooseStartText.color = palette.HintText;
        foreach (Graphic ink in gameplayButtonInk) ink.color = palette.MutedText;
    }

    public float PixelScale => uiCanvas.scaleFactor;

    public Rect GameplayScreenArea(bool spacious = false)
    {
        // Reserve the same HUD and footer spacing for both level atmospheres.
        // ScreenSpaceOverlay canvas units become pixels through its scale factor.
        float scale = uiCanvas.scaleFactor;
        Rect safe = Screen.safeArea;
        float side = Mathf.Min((spacious ? 20f : 60f) * scale, safe.width * 0.12f);
        float top = spacious ? 300f * scale : Mathf.Min(310f * scale, safe.height * 0.28f);
        float bottom = spacious ? 158f * scale : Mathf.Min(174f * scale, safe.height * 0.18f);
        return new Rect(safe.xMin + side, safe.yMin + bottom,
            Mathf.Max(1f, safe.width - side * 2f), Mathf.Max(1f, safe.height - top - bottom));
    }

    public void ShowState(PuzzleGameState state)
    {
        GameObject activePanel;
        switch (state)
        {
            case PuzzleGameState.ChooseStartRoom:
            case PuzzleGameState.Playing:
                activePanel = gameplayPanel;
                break;
            case PuzzleGameState.PuzzleComplete:
                activePanel = puzzleCompletePanel;
                break;
            case PuzzleGameState.Lose:
                activePanel = losePanel;
                break;
            case PuzzleGameState.LevelComplete:
                activePanel = levelCompletePanel;
                break;
            case PuzzleGameState.GameComplete:
                activePanel = gameCompletePanel;
                break;
            case PuzzleGameState.PuzzleSelection:
                puzzleSelection.Refresh();
                activePanel = puzzleSelectionPanel;
                break;
            default:
                activePanel = mainMenuPanel;
                break;
        }

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
        foreach (GameObject panel in panels)
            panel.SetActive(panel == activePanel);
        chooseStartHint.SetActive(state == PuzzleGameState.ChooseStartRoom);
    }

    // A direct UI raycast works for both mouse and touch and does not depend
    // on whether EventSystem.Update has already processed this frame's input.
    public bool IsPointerOverUI(Vector2 screenPosition)
    {
        if (EventSystem.current == null) return false;
        PointerEventData pointer = new PointerEventData(EventSystem.current);
        pointer.position = screenPosition;
        pointerHits.Clear();
        EventSystem.current.RaycastAll(pointer, pointerHits);
        return pointerHits.Count > 0;
    }

    private void LateUpdate()
    {
        UpdateSafeAreas();
    }

    private void UpdateSafeAreas()
    {
        if (Screen.width <= 0 || Screen.height <= 0) return;
        Rect area = Screen.safeArea;
        if (Screen.width == lastWidth && Screen.height == lastHeight && area == lastSafeArea)
            return;
        lastWidth = Screen.width;
        lastHeight = Screen.height;
        lastSafeArea = area;
        Vector2 min = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
        Vector2 max = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
        foreach (RectTransform rect in safeAreas)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }

    private GameObject MakePanel(Transform parent, string name, bool background, bool overlay)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform));
        panel.transform.SetParent(parent, false);
        Stretch(panel.GetComponent<RectTransform>());
        if (background)
        {
            Image image = panel.AddComponent<Image>();
            image.color = overlay ? PuzzleVisualStyle.Overlay : PuzzleVisualStyle.Background;
            image.raycastTarget = true;
        }
        panel.SetActive(false);
        return panel;
    }

    private Transform MakeSafeContent(GameObject panel, string name)
    {
        GameObject content = new GameObject(name, typeof(RectTransform));
        content.transform.SetParent(panel.transform, false);
        RectTransform rect = content.GetComponent<RectTransform>();
        Stretch(rect);
        safeAreas.Add(rect);
        return content.transform;
    }

    private Image MakeImage(Transform parent, string name, Vector2 anchor,
        Vector2 position, Vector2 size, Color color, Sprite sprite = null,
        float cornerScale = 0.75f)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        if (sprite != null)
        {
            image.sprite = sprite;
            if (sprite == PuzzleVisualStyle.RoundedSprite)
            {
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = cornerScale;
            }
        }
        Place(image.rectTransform, anchor, position, size);
        return image;
    }

    private Text MakeCenteredText(Transform parent, string name, string caption,
        int size, float y, Color color, float width = 768)
    {
        Text text = MakeText(parent, name, size, color);
        Place(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, y),
            new Vector2(width, 110));
        text.text = caption;
        return text;
    }

    private Text MakeText(Transform parent, string name, int size, Color color)
    {
        GameObject objectWithText = new GameObject(name, typeof(RectTransform), typeof(Text));
        objectWithText.transform.SetParent(parent, false);
        Text label = objectWithText.GetComponent<Text>();
        label.font = font;
        label.fontSize = size;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = color;
        label.raycastTarget = false;
        return label;
    }

    private void MakeCenteredButton(Transform parent, string caption, float y,
        UnityAction onClick, ButtonStyle style, Icon icon)
    {
        MakeButton(parent, caption, new Vector2(0.5f, 0.5f), new Vector2(0, y),
            new Vector2(580, 108), onClick, style, icon);
    }

    private void MakeButton(Transform parent, string caption, Vector2 anchor,
        Vector2 position, Vector2 size, UnityAction onClick, ButtonStyle style, Icon icon,
        bool gameplay = false)
    {
        Color fill = style == ButtonStyle.Secondary ? PuzzleVisualStyle.Secondary : PuzzleVisualStyle.Primary;
        Color ink = style == ButtonStyle.Secondary ? PuzzleVisualStyle.Text : PuzzleVisualStyle.Surface;
        Image image = MakeImage(parent, caption, anchor, position, size,
            gameplay ? Color.clear : fill,
            PuzzleVisualStyle.RoundedSprite, PuzzleVisualStyle.ButtonCornerScale);
        image.raycastTarget = true;
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = PuzzleVisualStyle.ButtonHighlight;
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = PuzzleVisualStyle.ButtonPressed;
        colors.disabledColor = PuzzleVisualStyle.ButtonDisabled;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.onClick.AddListener(onClick);

        int fontSize = size.x < 400 ? 30 : 34;
        Text label = MakeText(image.transform, "Label", fontSize, ink);
        label.fontStyle = style == ButtonStyle.Secondary ? FontStyle.Normal : FontStyle.Bold;
        label.text = caption;
        float captionWidth = Mathf.Min(label.preferredWidth, size.x - 130);
        Place(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(25, 0),
            new Vector2(captionWidth + 8, size.y));
        MakeIcon(image.transform, icon, new Vector2(-(captionWidth + 20) * 0.5f, 0), 30, ink);
        if (gameplay)
        {
            // Transparent button retains the full touch region; ink changes with the level.
            // Tint the label itself so pressed feedback also works without a card fill.
            button.targetGraphic = label;
            foreach (Graphic graphic in image.GetComponentsInChildren<Graphic>())
                if (graphic != image) gameplayButtonInk.Add(graphic);
        }
    }

    private void MakeIcon(Transform parent, Icon icon, Vector2 position, float size, Color color)
    {
        GameObject iconObject = new GameObject(icon + " Icon", typeof(RectTransform));
        iconObject.transform.SetParent(parent, false);
        Place(iconObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f),
            position, new Vector2(size, size));
        Transform root = iconObject.transform;
        float unit = size / 48f;
        switch (icon)
        {
            case Icon.Play:
                IconLine(root, new Vector2(-9, -15), new Vector2(15, 0), unit, color);
                IconLine(root, new Vector2(15, 0), new Vector2(-9, 15), unit, color);
                IconLine(root, new Vector2(-9, 15), new Vector2(-9, -15), unit, color);
                break;
            case Icon.Next:
                IconLine(root, new Vector2(-15, 0), new Vector2(15, 0), unit, color);
                IconLine(root, new Vector2(5, 10), new Vector2(15, 0), unit, color);
                IconLine(root, new Vector2(15, 0), new Vector2(5, -10), unit, color);
                break;
            case Icon.Check:
                IconLine(root, new Vector2(-14, 0), new Vector2(-4, -10), unit, color);
                IconLine(root, new Vector2(-4, -10), new Vector2(15, 12), unit, color);
                break;
            case Icon.Close:
                IconLine(root, new Vector2(-12, -12), new Vector2(12, 12), unit, color);
                IconLine(root, new Vector2(-12, 12), new Vector2(12, -12), unit, color);
                break;
            case Icon.Restart:
                MakeImage(root, "Smooth Restart Arrow", new Vector2(0.5f, 0.5f),
                    Vector2.zero, new Vector2(size, size), color,
                    PuzzleVisualStyle.RestartSprite);
                break;
            case Icon.Home:
                IconLine(root, new Vector2(-17, 0), new Vector2(0, 16), unit, color);
                IconLine(root, new Vector2(0, 16), new Vector2(17, 0), unit, color);
                IconLine(root, new Vector2(-12, 0), new Vector2(-12, -16), unit, color);
                IconLine(root, new Vector2(-12, -16), new Vector2(12, -16), unit, color);
                IconLine(root, new Vector2(12, -16), new Vector2(12, 0), unit, color);
                IconLine(root, new Vector2(-4, -16), new Vector2(-4, -5), unit, color);
                IconLine(root, new Vector2(-4, -5), new Vector2(4, -5), unit, color);
                IconLine(root, new Vector2(4, -5), new Vector2(4, -16), unit, color);
                break;
        }
    }

    private void IconLine(Transform parent, Vector2 from, Vector2 to, float unit, Color color)
    {
        Vector2 delta = to - from;
        Image line = MakeImage(parent, "Stroke", new Vector2(0.5f, 0.5f),
            (from + to) * (0.5f * unit), new Vector2(delta.magnitude * unit, 3 * unit), color);
        line.rectTransform.localRotation = Quaternion.Euler(0, 0,
            Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
    }

    private static void Stretch(RectTransform rect, float inset = 0)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.one * inset;
        rect.offsetMax = -Vector2.one * inset;
    }

    private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
