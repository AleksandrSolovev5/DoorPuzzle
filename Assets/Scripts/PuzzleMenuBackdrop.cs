using UnityEngine;
using UnityEngine.UI;

// Lightweight decorative geometry. No textures, input, or menu behaviour.
public sealed class PuzzleMenuBackdrop : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect rect = rectTransform.rect;
        if (rect.width <= 0 || rect.height <= 0) return;

        const int columns = 12;
        const int rows = 20;
        for (int y = 0; y <= rows; y++)
        for (int x = 0; x <= columns; x++)
        {
            Vector2 p = new Vector2(x / (float)columns, y / (float)rows);
            float light = Mathf.Exp(-((p - new Vector2(0.5f, 0.51f)).sqrMagnitude) * 6f);
            Color tint = Color.Lerp(new Color32(237, 245, 247, 255),
                new Color32(249, 251, 249, 255), light);
            mesh.AddVert(Point(rect, p), tint, Vector2.zero);
        }
        for (int y = 0; y < rows; y++)
        for (int x = 0; x < columns; x++)
        {
            int i = y * (columns + 1) + x;
            mesh.AddTriangle(i, i + 1, i + columns + 2);
            mesh.AddTriangle(i, i + columns + 2, i + columns + 1);
        }

        Color wash = new Color(0.75f, 0.85f, 0.89f, 0.15f);
        Shape(mesh, Point(rect, new Vector2(-0.09f, 1.10f)), rect.width * 0.57f, wash, false);
        Shape(mesh, Point(rect, new Vector2(1.12f, -0.13f)), rect.width * 0.59f, wash, false);
        Color line = new Color(0.69f, 0.79f, 0.84f, 0.30f);
        for (int mirror = 0; mirror < 2; mirror++)
        {
            Curve(mesh, rect, new Vector2(-0.04f, 0.75f), new Vector2(0.14f, 0.78f),
                new Vector2(0.08f, 0.87f), new Vector2(0.30f, 0.89f), line, mirror == 1);
            Curve(mesh, rect, new Vector2(0.30f, 0.89f), new Vector2(0.53f, 0.91f),
                new Vector2(0.50f, 0.94f), new Vector2(0.57f, 1.06f), line, mirror == 1);
            for (int x = 0; x < 3; x++)
            for (int y = 0; y < 3; y++)
            {
                Vector2 p = new Vector2(0.084f + x * 0.029f, 0.927f - y * 0.017f);
                if (mirror == 1) p = Vector2.one - p;
                Shape(mesh, Point(rect, p), rect.width * 0.0038f,
                    new Color(0.57f, 0.70f, 0.77f, 0.27f), false);
            }
        }
        Color diamond = new Color(0.73f, 0.83f, 0.88f, 0.15f);
        Shape(mesh, Point(rect, new Vector2(0.848f, 0.84f)), rect.width * 0.031f, diamond, true);
        Shape(mesh, Point(rect, new Vector2(0.91f, 0.787f)), rect.width * 0.05f, diamond * new Color(1, 1, 1, 0.5f), true);
        Shape(mesh, Point(rect, new Vector2(0.086f, 0.178f)), rect.width * 0.045f, diamond * new Color(1, 1, 1, 0.5f), true);
        Shape(mesh, Point(rect, new Vector2(0.144f, 0.148f)), rect.width * 0.048f, diamond, true);
    }

    private static Vector2 Point(Rect rect, Vector2 normalized)
    {
        return rect.min + Vector2.Scale(normalized, rect.size);
    }

    private static void Shape(VertexHelper mesh, Vector2 center, float radius, Color tint, bool diamond)
    {
        const int segments = 64;
        int start = mesh.currentVertCount;
        mesh.AddVert(center, tint, Vector2.zero);
        Color clear = new Color(tint.r, tint.g, tint.b, 0);
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * 2f * Mathf.PI / segments;
            Vector2 p = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            if (diamond)
            {
                p = new Vector2(Mathf.Sign(p.x) * Mathf.Pow(Mathf.Abs(p.x), 0.5f),
                    Mathf.Sign(p.y) * Mathf.Pow(Mathf.Abs(p.y), 0.5f));
                p = new Vector2(p.x - p.y, p.x + p.y) * 0.707107f;
            }
            mesh.AddVert(center + p * radius, tint, Vector2.zero);
            mesh.AddVert(center + p * (radius + 1.5f), clear, Vector2.zero);
            if (i == 0) continue;
            int v = start + 1 + i * 2;
            mesh.AddTriangle(start, v - 2, v);
            mesh.AddTriangle(v - 2, v - 1, v);
            mesh.AddTriangle(v - 1, v + 1, v);
        }
    }

    private static void Curve(VertexHelper mesh, Rect rect, Vector2 a, Vector2 b,
        Vector2 c, Vector2 d, Color tint, bool mirror)
    {
        const int segments = 64;
        int start = mesh.currentVertCount;
        Color clear = new Color(tint.r, tint.g, tint.b, 0);
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float s = 1f - t;
            Vector2 p = a * (s * s * s) + b * (3 * s * s * t) +
                c * (3 * s * t * t) + d * (t * t * t);
            Vector2 tangent = (b - a) * (3 * s * s) + (c - b) * (6 * s * t) +
                (d - c) * (3 * t * t);
            if (mirror) { p = Vector2.one - p; tangent = -tangent; }
            tangent = Vector2.Scale(tangent, rect.size).normalized;
            Vector2 normal = new Vector2(-tangent.y, tangent.x);
            Vector2 center = Point(rect, p);
            mesh.AddVert(center - normal * 2f, clear, Vector2.zero);
            mesh.AddVert(center - normal * 0.7f, tint, Vector2.zero);
            mesh.AddVert(center + normal * 0.7f, tint, Vector2.zero);
            mesh.AddVert(center + normal * 2f, clear, Vector2.zero);
            if (i == 0) continue;
            int v = start + i * 4;
            for (int strip = 0; strip < 3; strip++)
            {
                mesh.AddTriangle(v - 4 + strip, v - 3 + strip, v + strip);
                mesh.AddTriangle(v - 3 + strip, v + strip + 1, v + strip);
            }
        }
    }
}
