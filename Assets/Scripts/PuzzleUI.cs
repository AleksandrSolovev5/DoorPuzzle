using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;

public sealed class PuzzleUI : MonoBehaviour
{
    private Text levelText;
    private Text doorsText;
    private Text resultText;
    private GameObject restartGameButton;
    private Font font;

    public void Build(UnityAction onRestart, UnityAction onRestartGame)
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 32);

        GameObject eventObject = new GameObject("Event System", typeof(EventSystem),
            typeof(InputSystemUIInputModule));
        eventObject.transform.SetParent(transform, false);
        eventObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

        GameObject canvasObject = new GameObject("Puzzle UI", typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        levelText = MakeText(canvasObject.transform, "Level", 60, TextAnchor.MiddleCenter,
            new Color(0.12f, 0.16f, 0.2f), false);
        Place(levelText.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -85),
            new Vector2(800, 90));

        doorsText = MakeText(canvasObject.transform, "Doors", 42, TextAnchor.MiddleCenter,
            new Color(0.28f, 0.34f, 0.4f), false);
        Place(doorsText.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -165),
            new Vector2(700, 70));

        MakeButton(canvasObject.transform, "Restart", new Vector2(0.5f, 0),
            new Vector2(0, 110), new Vector2(350, 100), onRestart);

        resultText = MakeText(canvasObject.transform, "Result", 88, TextAnchor.MiddleCenter,
            new Color(0.16f, 0.7f, 0.33f), false);
        Place(resultText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 0),
            new Vector2(960, 260));
        resultText.gameObject.SetActive(false);

        restartGameButton = MakeButton(canvasObject.transform, "Restart Game",
            new Vector2(0.5f, 0.5f), new Vector2(0, -210), new Vector2(480, 105),
            onRestartGame);
        restartGameButton.SetActive(false);
    }

    public void SetProgress(LevelDefinition level, int closed)
    {
        levelText.text = level.Name;
        doorsText.text = "Doors " + closed + "/" + level.Doors.Length;
        resultText.gameObject.SetActive(false);
        restartGameButton.SetActive(false);
    }

    public void ShowResult(bool won)
    {
        resultText.text = won ? "CORRECT!" : "YOU LOSE\nPath blocked";
        resultText.fontSize = won ? 88 : 74;
        resultText.color = won ? new Color(0.12f, 0.68f, 0.28f)
            : new Color(0.78f, 0.2f, 0.17f);
        resultText.gameObject.SetActive(true);
    }

    public void ShowGameOver()
    {
        resultText.text = "GAME OVER\nPrototype Complete";
        resultText.fontSize = 68;
        resultText.color = new Color(0.12f, 0.68f, 0.28f);
        resultText.gameObject.SetActive(true);
        restartGameButton.SetActive(true);
    }

    private Text MakeText(Transform parent, string name, int size, TextAnchor alignment,
        Color color, bool raycast)
    {
        GameObject objectWithText = new GameObject(name, typeof(RectTransform), typeof(Text));
        objectWithText.transform.SetParent(parent, false);
        Text label = objectWithText.GetComponent<Text>();
        label.font = font;
        label.fontSize = size;
        label.alignment = alignment;
        label.color = color;
        label.raycastTarget = raycast;
        return label;
    }

    private GameObject MakeButton(Transform parent, string caption, Vector2 anchor,
        Vector2 position, Vector2 size, UnityAction onClick)
    {
        GameObject buttonObject = new GameObject(caption, typeof(RectTransform),
            typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        Place(buttonObject.GetComponent<RectTransform>(), anchor, position, size);
        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.18f, 0.22f, 0.27f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        Text label = MakeText(buttonObject.transform, "Label", 42,
            TextAnchor.MiddleCenter, Color.white, false);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        label.text = caption;
        return buttonObject;
    }

    private static void Place(RectTransform rect, Vector2 anchor, Vector2 position,
        Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
