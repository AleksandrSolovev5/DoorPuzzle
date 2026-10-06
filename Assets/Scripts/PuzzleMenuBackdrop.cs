using UnityEngine;
using UnityEngine.UI;

// A quiet warm wash behind the existing menu composition.
public sealed class PuzzleMenuBackdrop : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect rect = rectTransform.rect;
        if (rect.width <= 0 || rect.height <= 0) return;

        Color bottom = PuzzleVisualStyle.Background;
        Color top = Color.Lerp(bottom, PuzzleVisualStyle.Surface, 0.35f);
        mesh.AddVert(new Vector2(rect.xMin, rect.yMin), bottom, Vector2.zero);
        mesh.AddVert(new Vector2(rect.xMax, rect.yMin), bottom, Vector2.zero);
        mesh.AddVert(new Vector2(rect.xMax, rect.yMax), top, Vector2.zero);
        mesh.AddVert(new Vector2(rect.xMin, rect.yMax), top, Vector2.zero);
        mesh.AddTriangle(0, 1, 2);
        mesh.AddTriangle(0, 2, 3);
    }
}
