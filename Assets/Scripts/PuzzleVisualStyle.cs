using UnityEngine;

// Shared presentation only: no puzzle rules or generation settings live here.
public static class PuzzleVisualStyle
{
    public static readonly Color Background = Rgb(238, 243, 243);
    public static readonly Color Surface = Rgb(252, 253, 251);
    public static readonly Color Text = Rgb(38, 60, 69);
    public static readonly Color MutedText = Rgb(104, 124, 132);
    public static readonly Color Border = Rgb(213, 223, 223);
    public static readonly Color Primary = Rgb(53, 95, 109);
    public static readonly Color PrimarySoft = Rgb(225, 236, 237);
    public static readonly Color Secondary = Rgb(241, 246, 245);
    public static readonly Color Success = Rgb(77, 137, 124);
    public static readonly Color SuccessSoft = Rgb(228, 241, 235);
    public static readonly Color Danger = Rgb(163, 97, 89);
    public static readonly Color DangerSoft = Rgb(247, 233, 229);
    public static readonly Color Wall = Rgb(96, 119, 124);
    public static readonly Color DoorOpen = Rgb(214, 154, 80);
    public static readonly Color DoorClosed = Rgb(160, 175, 178);
    public static readonly Color OneWayOpen = Rgb(154, 143, 186);
    public static readonly Color OneWayClosed = Rgb(127, 130, 151);
    public static readonly Color OneWayArrow = Rgb(244, 240, 250);
    public static readonly Color ExitOpen = Rgb(85, 139, 112);
    public static readonly Color ExitClosed = Rgb(139, 168, 151);
    public static readonly Color Player = Rgb(72, 123, 178);
    public static readonly Color Shadow = new Color(0.12f, 0.23f, 0.27f, 0.07f);
    public static readonly Color Overlay = new Color(0.12f, 0.20f, 0.24f, 0.26f);

    private static Sprite roundedSprite;
    private static Sprite circleSprite;
    private static Sprite restartSprite;
    private static Sprite roomShadowSprite;
    private static Sprite oneWayArrowSprite;

    public static Sprite RoundedSprite
    {
        get
        {
            if (roundedSprite == null) roundedSprite = CreateShape(false);
            return roundedSprite;
        }
    }

    public static Sprite CircleSprite
    {
        get
        {
            if (circleSprite == null) circleSprite = CreateShape(true);
            return circleSprite;
        }
    }

    public static Sprite RestartSprite
    {
        get
        {
            if (restartSprite == null) restartSprite = CreateRestartArrow();
            return restartSprite;
        }
    }

    public static Sprite RoomShadowSprite
    {
        get
        {
            if (roomShadowSprite == null) roomShadowSprite = CreateRoomShadow();
            return roomShadowSprite;
        }
    }

    public static Sprite OneWayArrowSprite
    {
        get
        {
            if (oneWayArrowSprite == null) oneWayArrowSprite = CreateOneWayArrow();
            return oneWayArrowSprite;
        }
    }

    private static Sprite CreateOneWayArrow()
    {
        // Font-independent, antialiased arrow pointing right; the view rotates it.
        const int size = 256;
        const float coordinates = 48f;
        float pixelSize = coordinates / size;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
        texture.name = "One-way Door Arrow";
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.filterMode = FilterMode.Trilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            Vector2 p = new Vector2(x + 0.5f - size * 0.5f, y + 0.5f - size * 0.5f) * pixelSize;
            float distance = Mathf.Min(SegmentDistance(p, new Vector2(-15, 0), new Vector2(15, 0)),
                Mathf.Min(SegmentDistance(p, new Vector2(4, 10), new Vector2(15, 0)),
                    SegmentDistance(p, new Vector2(4, -10), new Vector2(15, 0)))) - 2.5f;
            pixels[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(0.5f - distance / pixelSize));
        }
        texture.SetPixels(pixels);
        texture.Apply(true, true);
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect);
        sprite.name = texture.name;
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    private static Sprite CreateRoomShadow()
    {
        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
        texture.name = "Soft Room Shadow";
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.filterMode = FilterMode.Trilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            Vector2 p = new Vector2((x + 0.5f) / size - 0.5f, (y + 0.5f) / size - 0.5f);
            Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - Vector2.one * 0.40f;
            float distance = new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude;
            float alpha = Mathf.Exp(-distance * distance / 0.002f);
            pixels[y * size + x] = new Color(1, 1, 1, alpha);
        }
        texture.SetPixels(pixels);
        texture.Apply(true, true);
        roomShadowSprite = Sprite.Create(texture, new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect);
        roomShadowSprite.name = texture.name;
        roomShadowSprite.hideFlags = HideFlags.HideAndDontSave;
        return roomShadowSprite;
    }

    private static Color Rgb(byte r, byte g, byte b)
    {
        return new Color(r / 255f, g / 255f, b / 255f);
    }

    private static Sprite CreateRestartArrow()
    {
        // One continuous stroke, with rounded ends and a filtered alpha edge.
        // Mipmaps keep the high-resolution mask smooth at small button sizes.
        const int size = 256;
        const float coordinateSize = 48f;
        const float radius = 15f;
        const float halfStroke = 1.75f;
        const float startAngle = -35f;
        const float endAngle = 235f;
        float pixelSize = coordinateSize / size;
        Vector2 start = new Vector2(Mathf.Cos(startAngle * Mathf.Deg2Rad),
            Mathf.Sin(startAngle * Mathf.Deg2Rad)) * radius;
        Vector2 end = new Vector2(Mathf.Cos(endAngle * Mathf.Deg2Rad),
            Mathf.Sin(endAngle * Mathf.Deg2Rad)) * radius;
        Vector2 tangent = new Vector2(-Mathf.Sin(endAngle * Mathf.Deg2Rad),
            Mathf.Cos(endAngle * Mathf.Deg2Rad));
        Vector2 normal = new Vector2(-tangent.y, tangent.x);
        Vector2 headA = end - tangent * 8f + normal * 5f;
        Vector2 headB = end - tangent * 8f - normal * 5f;

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
        texture.name = "UI Smooth Restart Arrow";
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Trilinear;
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            Vector2 p = new Vector2(x + 0.5f - size * 0.5f,
                y + 0.5f - size * 0.5f) * pixelSize;
            float angle = Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg;
            if (angle < startAngle) angle += 360f;
            float arcDistance = angle <= endAngle
                ? Mathf.Abs(p.magnitude - radius)
                : Mathf.Min(Vector2.Distance(p, start), Vector2.Distance(p, end));
            float distance = Mathf.Min(arcDistance,
                Mathf.Min(SegmentDistance(p, headA, end),
                    SegmentDistance(p, headB, end))) - halfStroke;
            float alpha = Mathf.Clamp01(0.5f - distance / pixelSize);
            pixels[y * size + x] = new Color(1, 1, 1, alpha);
        }
        texture.SetPixels(pixels);
        texture.Apply(true, true);
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect);
        sprite.name = texture.name;
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    private static float SegmentDistance(Vector2 point, Vector2 from, Vector2 to)
    {
        Vector2 segment = to - from;
        float t = Mathf.Clamp01(Vector2.Dot(point - from, segment) / segment.sqrMagnitude);
        return Vector2.Distance(point, from + segment * t);
    }

    private static Sprite CreateShape(bool circle)
    {
        // Two tiny reusable masks replace imported UI textures/icon packs.
        const int size = 64;
        const float radius = 12f;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = circle ? "UI Circle" : "UI Rounded Rectangle";
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            Vector2 p = new Vector2(x + 0.5f - size * 0.5f,
                y + 0.5f - size * 0.5f);
            float distance;
            if (circle)
                distance = p.magnitude - (size * 0.5f - 1f);
            else
            {
                Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) -
                    Vector2.one * (size * 0.5f - radius);
                distance = new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude +
                    Mathf.Min(Mathf.Max(q.x, q.y), 0) - radius;
            }
            pixels[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(0.5f - distance));
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f), circle ? size : 100f, 0,
            SpriteMeshType.FullRect, circle ? Vector4.zero : Vector4.one * 14f);
        sprite.name = texture.name;
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }
}
