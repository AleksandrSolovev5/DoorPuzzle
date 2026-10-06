using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// A code-generated panel using the same typography, shapes and safe area as the menu.
public sealed class PuzzleSelectionView : MonoBehaviour
{
    private readonly List<Button> puzzleButtons = new List<Button>();
    private readonly List<Text> titles = new List<Text>();
    private readonly List<Text> statuses = new List<Text>();
    private PuzzleProgress progress;
    private RectTransform composition;
    private Font font;

    public void Build(Font uiFont, PuzzleProgress savedProgress,
        UnityAction<int> onSelect, UnityAction onHome)
    {
        font = uiFont;
        progress = savedProgress;
        composition = RectAt(transform, "Puzzle Selection", Vector2.zero, new Vector2(1080, 1920));
        Label(composition, "Title", "SELECT PUZZLE", 62, new Vector2(0, 650),
            new Vector2(960, 100), PuzzleVisualStyle.Text);
        Label(composition, "Hint", "Choose an unlocked puzzle", 30, new Vector2(0, 548),
            new Vector2(960, 70), PuzzleVisualStyle.MutedText);

        // The existing game has two levels. Wrap later levels into another pair.
        for (int level = 0; level < LevelCatalog.Levels.Length; level++)
        {
            float x = level % 2 == 0 ? -226 : 226;
            float top = 410 - (level / 2) * 750;
            Label(composition, "Level " + (level + 1), "LEVEL " + (level + 1), 36,
                new Vector2(x, top), new Vector2(392, 72), PuzzleVisualStyle.Text);
            for (int puzzle = 0; puzzle < LevelCatalog.Levels[level].Puzzles.Length; puzzle++)
            {
                int index = LevelCatalog.ToPuzzleIndex(level, puzzle);
                Button button = MakeButton(composition, "Puzzle " + index,
                    new Vector2(x, top - 110 - puzzle * 146), new Vector2(392, 120),
                    () => onSelect(index));
                Text title = Label(button.transform, "Title", "PUZZLE " + (puzzle + 1), 34,
                    new Vector2(0, 21), new Vector2(350, 56), PuzzleVisualStyle.Text);
                Text status = Label(button.transform, "Status", "", 22,
                    new Vector2(0, -30), new Vector2(350, 38), PuzzleVisualStyle.MutedText);
                puzzleButtons.Add(button);
                titles.Add(title);
                statuses.Add(status);
            }
        }
        Button back = MakeButton(composition, "Back", new Vector2(0, -540),
            new Vector2(580, 108), onHome);
        back.GetComponent<Image>().color = PuzzleVisualStyle.Primary;
        Label(back.transform, "Label", "BACK", 34, Vector2.zero,
            new Vector2(540, 90), PuzzleVisualStyle.Surface);
        Refresh();
        Fit();
    }

    public void Refresh()
    {
        for (int i = 0; i < puzzleButtons.Count; i++)
        {
            bool unlocked = progress.IsUnlocked(i);
            bool completed = progress.IsCompleted(i);
            puzzleButtons[i].interactable = unlocked;
            puzzleButtons[i].GetComponent<Image>().color = unlocked
                ? PuzzleVisualStyle.Surface : PuzzleVisualStyle.Secondary;
            titles[i].color = unlocked ? PuzzleVisualStyle.Text : PuzzleVisualStyle.MutedText;
            statuses[i].text = completed ? "COMPLETED" : unlocked ? "OPEN" : "LOCKED";
            statuses[i].color = completed ? PuzzleVisualStyle.Success : PuzzleVisualStyle.MutedText;
        }
    }

    private void LateUpdate() => Fit();

    private void Fit()
    {
        if (composition == null) return;
        Rect available = ((RectTransform)transform).rect;
        composition.localScale = Vector3.one * Mathf.Max(0.01f,
            Mathf.Min(available.width / 1080f, available.height / 1920f));
    }

    private Button MakeButton(Transform parent, string name, Vector2 position,
        Vector2 size, UnityAction onClick)
    {
        Image image = RectAt(parent, name, position, size).gameObject.AddComponent<Image>();
        image.sprite = PuzzleVisualStyle.RoundedSprite;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = PuzzleVisualStyle.ButtonCornerScale;
        image.color = PuzzleVisualStyle.Surface;
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.highlightedColor = PuzzleVisualStyle.ButtonHighlight;
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = PuzzleVisualStyle.ButtonPressed;
        colors.disabledColor = PuzzleVisualStyle.ButtonDisabled;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.onClick.AddListener(onClick);
        return button;
    }

    private Text Label(Transform parent, string name, string caption, int size,
        Vector2 position, Vector2 bounds, Color color)
    {
        Text text = RectAt(parent, name, position, bounds).gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.fontStyle = name == "Hint" || name == "Status" ? FontStyle.Normal : FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.text = caption;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform RectAt(Transform parent, string name, Vector2 position, Vector2 size)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }
}
