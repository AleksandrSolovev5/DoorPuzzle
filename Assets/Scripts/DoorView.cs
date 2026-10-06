using System.Collections;
using UnityEngine;

public sealed class DoorView : MonoBehaviour
{
    // Door leaves share one size. Level 2 configures its touch region separately.
    public const float Width = 0.6f;
    private const float Thickness = 0.09f;
    private const float TapSize = 0.72f;

    public int Index { get; private set; }
    // Animation state only; the authoritative closed-door mask belongs to the game.
    private bool isClosed;

    private SpriteRenderer drawing;
    private Transform hinge;
    private float openAngle;
    private float closedAngle;
    private Color openColor;
    private Color closedColor;
    private SpriteRenderer directionArrow;
    private BoxCollider2D defaultTapArea;
    private PolygonCollider2D expandedTapArea;

    public void Initialize(int index, DoorDefinition door, Vector2 roomCenter, Sprite sprite,
        Vector2? otherRoomCenter = null)
    {
        Index = index;
        closedAngle = door.Vertical ? 90f : 0f;
        openAngle = door.Vertical
            ? (door.Position.x > roomCenter.x ? 15f : 165f)
            : 75f;
        if (door.IsExit && !door.Vertical && door.Position.y > roomCenter.y)
            openAngle = -75f;

        defaultTapArea = gameObject.AddComponent<BoxCollider2D>();
        defaultTapArea.size = new Vector2(TapSize, TapSize);

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

        AddDetail("Hinge Pin", Vector2.zero, 0.045f, PuzzleVisualStyle.Wall);
        AddDetail("Handle", new Vector2(Width * 0.8f, 0), 0.035f,
            PuzzleVisualStyle.Surface);

        openColor = door.IsExit ? PuzzleVisualStyle.ExitOpen :
            door.IsOneWay ? PuzzleVisualStyle.OneWayOpen : PuzzleVisualStyle.DoorOpen;
        closedColor = door.IsExit ? PuzzleVisualStyle.ExitClosed :
            door.IsOneWay ? PuzzleVisualStyle.OneWayClosed : PuzzleVisualStyle.DoorClosed;
        isClosed = false;
        hinge.localRotation = Quaternion.Euler(0, 0, openAngle);
        drawing.color = openColor;
        if (door.IsOneWay)
        {
            if (!otherRoomCenter.HasValue)
                throw new System.ArgumentException("A one-way door needs both room centres.");
            DrawDirectionMarker(door, roomCenter, otherRoomCenter.Value);
        }
    }

    private void DrawDirectionMarker(DoorDefinition door, Vector2 a, Vector2 b)
    {
        Vector2 movement = door.Direction == DoorDirection.AToB ? b - a : a - b;
        // Use the shared-wall normal, including unequal-sized room layouts.
        Vector2 direction = door.Vertical
            ? new Vector2(Mathf.Sign(movement.x), 0)
            : new Vector2(0, Mathf.Sign(movement.y));
        // Attach to the fixed door root, not the animated hinge. The arrow
        // always shows travel direction and stays within the existing tap target.
        GameObject arrow = new GameObject("Allowed Travel Direction");
        arrow.transform.SetParent(transform, false);
        arrow.transform.localScale = Vector3.one * 0.50f;
        arrow.transform.localRotation = Quaternion.Euler(0, 0,
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        directionArrow = arrow.AddComponent<SpriteRenderer>();
        directionArrow.sprite = PuzzleVisualStyle.OneWayArrowSprite;
        directionArrow.color = PuzzleVisualStyle.OneWayArrow;
        directionArrow.sortingOrder = 7;
    }

    private void SetDirectionMarkerProgress(float progress)
    {
        if (directionArrow == null) return;
        Color ink = PuzzleVisualStyle.OneWayArrow;
        ink.a = Mathf.Lerp(1f, 0.45f, progress);
        directionArrow.color = ink;
    }

    public void ConfigureLevel2Interaction(System.Collections.Generic.List<Vector2> worldPolygon,
        float pixelsPerUnit, float minimumArrowPixels)
    {
        defaultTapArea.enabled = false;
        if (expandedTapArea == null)
        {
            expandedTapArea = gameObject.AddComponent<PolygonCollider2D>();
            expandedTapArea.isTrigger = true;
        }
        expandedTapArea.enabled = worldPolygon.Count >= 3;
        if (worldPolygon.Count >= 3)
        {
            Vector2[] local = new Vector2[worldPolygon.Count];
            for (int i = 0; i < local.Length; i++)
                local[i] = transform.InverseTransformPoint(worldPolygon[i]);
            expandedTapArea.pathCount = 1;
            expandedTapArea.SetPath(0, local);
        }
        if (directionArrow != null)
        {
            // The visible stroke spans 35 of the mask's 48 coordinate units.
            float size = Mathf.Max(0.50f, minimumArrowPixels / (pixelsPerUnit * (35f / 48f)));
            directionArrow.transform.localScale = Vector3.one * size;
        }
    }

    public Bounds DirectionBounds => directionArrow != null ? directionArrow.bounds :
        new Bounds(transform.position, Vector3.zero);

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
        if (isClosed) yield break;

        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / duration));
            hinge.localRotation = Quaternion.Euler(0, 0,
                Mathf.LerpAngle(openAngle, closedAngle, progress));
            drawing.color = Color.Lerp(openColor, closedColor, progress);
            SetDirectionMarkerProgress(progress);
            yield return null;
        }

        hinge.localRotation = Quaternion.Euler(0, 0, closedAngle);
        drawing.color = closedColor;
        SetDirectionMarkerProgress(1f);
        isClosed = true;
    }
}
