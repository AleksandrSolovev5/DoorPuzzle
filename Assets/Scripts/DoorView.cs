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

        openColor = door.IsExit ? new Color(0.15f, 0.78f, 0.32f)
            : new Color(1f, 0.48f, 0.14f);
        closedColor = door.IsExit ? new Color(0.07f, 0.38f, 0.19f)
            : new Color(0.31f, 0.34f, 0.37f);
        IsClosed = false;
        hinge.localRotation = Quaternion.Euler(0, 0, openAngle);
        drawing.color = openColor;
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
