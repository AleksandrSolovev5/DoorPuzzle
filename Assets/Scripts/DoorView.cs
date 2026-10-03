using System.Collections;
using UnityEngine;

public sealed class DoorView : MonoBehaviour
{
    // Every puzzle uses this same door size and tap target.
    public const float Width = 0.6f;
    private const float Thickness = 0.16f;
    private const float TapSize = 0.72f;

    public int Index { get; private set; }
    public bool IsClosed { get; private set; }

    private SpriteRenderer drawing;
    private Transform hinge;
    private float openAngle;
    private float closedAngle;
    private Color openColor;
    private Color closedColor;

    public void Initialize(int index, DoorDefinition door, Vector2 roomCenter, Sprite sprite)
    {
        Index = index;
        closedAngle = door.Vertical ? 90f : 0f;
        openAngle = door.Vertical
            ? (door.Position.x > roomCenter.x ? 15f : 165f)
            : 75f;
        if (door.IsExit && !door.Vertical && door.Position.y > roomCenter.y)
            openAngle = -75f;

        BoxCollider2D tapArea = gameObject.AddComponent<BoxCollider2D>();
        tapArea.size = new Vector2(TapSize, TapSize);

        GameObject hingeObject = new GameObject("Hinge");
        hingeObject.transform.SetParent(transform, false);
        hinge = hingeObject.transform;
        hinge.localPosition = door.Vertical
            ? new Vector3(0, -Width * 0.5f, 0)
            : new Vector3(-Width * 0.5f, 0, 0);

        GameObject leaf = new GameObject("Leaf");
        leaf.transform.SetParent(hinge, false);
        leaf.transform.localPosition = new Vector3(Width * 0.5f, 0, 0);
        leaf.transform.localScale = new Vector3(Width, Thickness, 1);
        drawing = leaf.AddComponent<SpriteRenderer>();
        drawing.sprite = sprite;
        drawing.sortingOrder = 4;

        AddDetail("Hinge Pin", Vector2.zero, 0.065f, PuzzleVisualStyle.Wall);
        AddDetail("Handle", new Vector2(Width * 0.8f, 0), 0.05f,
            PuzzleVisualStyle.Surface);

        openColor = door.IsExit ? PuzzleVisualStyle.ExitOpen : PuzzleVisualStyle.DoorOpen;
        closedColor = door.IsExit ? PuzzleVisualStyle.ExitClosed : PuzzleVisualStyle.DoorClosed;
        IsClosed = false;
        hinge.localRotation = Quaternion.Euler(0, 0, openAngle);
        drawing.color = openColor;
    }

    private void AddDetail(string name, Vector2 position, float size, Color color)
    {
        GameObject detail = new GameObject(name);
        detail.transform.SetParent(hinge, false);
        detail.transform.localPosition = new Vector3(position.x, position.y, 0);
        detail.transform.localScale = Vector3.one * size;
        SpriteRenderer renderer = detail.AddComponent<SpriteRenderer>();
        renderer.sprite = PuzzleVisualStyle.CircleSprite;
        renderer.color = color;
        renderer.sortingOrder = 5;
    }

    public IEnumerator AnimateClosed(float duration)
    {
        if (IsClosed) yield break;

        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / duration));
            hinge.localRotation = Quaternion.Euler(0, 0,
                Mathf.LerpAngle(openAngle, closedAngle, progress));
            drawing.color = Color.Lerp(openColor, closedColor, progress);
            yield return null;
        }

        hinge.localRotation = Quaternion.Euler(0, 0, closedAngle);
        drawing.color = closedColor;
        IsClosed = true;
    }
}
