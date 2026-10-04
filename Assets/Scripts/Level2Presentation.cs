using System.Collections.Generic;
using UnityEngine;

// Visual relayout only: room IDs, door IDs, directions and EXIT are preserved.
public static class Level2Presentation
{
    public const float ScreenFill = 0.90f;
    private const float MinimumRoomSide = 1.6f;
    private const float TapMultiplier = 2.2f;
    private const float Separation = 0.012f;

    private static float Density(float uiScale)
    {
        if (!Application.isMobilePlatform) return Mathf.Max(1f, uiScale);
        float reported = Screen.dpi > 0 ? Screen.dpi / 160f : 1f;
        float resolution = Mathf.Min(Screen.width, Screen.height) / 400f;
        return Mathf.Clamp(Mathf.Max(reported, resolution), 1f, 4f);
    }

    public static float MinimumTouchPixels(float uiScale) =>
        Mathf.Max(44f * Density(uiScale), 88f * uiScale);

    public static float MinimumArrowPixels(float uiScale) =>
        Mathf.Max(20f * Density(uiScale), 28f * uiScale);

    public static LevelDefinition Prepare(LevelDefinition source, Rect screenArea, float uiScale)
    {
        float shortest = float.PositiveInfinity;
        foreach (RoomDefinition room in source.Rooms)
            shortest = Mathf.Min(shortest, Mathf.Min(room.Size.x, room.Size.y));
        float compact = Mathf.Min(1f, MinimumRoomSide / shortest);
        LevelDefinition best = source;
        bool bestComfortable = false;
        float bestScore = -1f, bestPixels = -1f;
        // Six bounded alternatives: orientation and mild compression of rooms.
        // Door leaves retain their original size; the logical graph never changes.
        foreach (float scale in new[] { 1f, Mathf.Lerp(1f, compact, 0.5f), compact })
        for (int rotation = 0; rotation < 2; rotation++)
        {
            LevelDefinition candidate = CopyLayout(source, scale, rotation == 1);
            Bounds bounds = Measure(candidate);
            float pixels = FitPixels(bounds, screenArea);
            candidate = SpaceParallelDoors(candidate, MinimumTouchPixels(uiScale) / pixels);
            bounds = Measure(candidate);
            pixels = FitPixels(bounds, screenArea);
            Rect worldArea = WorldArea(bounds.center, screenArea, pixels);
            float touch = float.PositiveInfinity;
            for (int i = 0; i < candidate.Doors.Length; i++)
            {
                List<Vector2> polygon = TouchPolygon(candidate, i, pixels, uiScale, worldArea);
                Vector2 size = PolygonSize(polygon);
                touch = Mathf.Min(touch, Mathf.Min(size.x, size.y) * pixels);
            }
            float visible = DoorView.Width * pixels;
            float score = Mathf.Min(touch / MinimumTouchPixels(uiScale),
                visible / Mathf.Max(26f * uiScale, 12f * Density(uiScale)));
            bool comfortable = score >= 0.99f;
            if ((comfortable && !bestComfortable) ||
                (comfortable == bestComfortable &&
                    (comfortable ? pixels > bestPixels : score > bestScore)))
            {
                best = candidate;
                bestComfortable = comfortable;
                bestPixels = pixels;
                bestScore = score;
            }
        }
        if (!bestComfortable)
            Debug.LogWarning(source.Name + ": small viewport limits separated touch targets; " +
                "using the most readable of six compact layouts. No extra camera zoom-out is applied.");
        return best;
    }

    private static Vector2 Rotate(Vector2 point, bool turn) =>
        turn ? new Vector2(-point.y, point.x) : point;

    private static LevelDefinition CopyLayout(LevelDefinition source, float scale, bool turn)
    {
        RoomDefinition[] rooms = new RoomDefinition[source.Rooms.Length];
        for (int i = 0; i < rooms.Length; i++)
        {
            RoomDefinition room = source.Rooms[i];
            Vector2 size = turn ? new Vector2(room.Size.y, room.Size.x) : room.Size;
            Vector2Int footprint = turn
                ? new Vector2Int(room.GridFootprint.y, room.GridFootprint.x) : room.GridFootprint;
            rooms[i] = new RoomDefinition(Rotate(room.Center, turn) * scale, size * scale, footprint);
        }
        DoorDefinition[] doors = new DoorDefinition[source.Doors.Length];
        for (int i = 0; i < doors.Length; i++)
        {
            DoorDefinition door = source.Doors[i];
            doors[i] = new DoorDefinition(door.RoomA, door.RoomB, Rotate(door.Position, turn) * scale,
                turn ? !door.Vertical : door.Vertical, door.IsExit, door.Direction);
        }
        return Copy(source, rooms, doors);
    }

    private static LevelDefinition Copy(LevelDefinition source, RoomDefinition[] rooms, DoorDefinition[] doors)
    {
        return new LevelDefinition(source.Name, rooms, doors, source.CameraSize, source.FloorColor,
            source.Palette.IsDusk ? PuzzleAtmosphere.Dusk : PuzzleAtmosphere.Light);
    }

    private static LevelDefinition SpaceParallelDoors(LevelDefinition source, float minimumSpacing)
    {
        DoorDefinition[] doors = (DoorDefinition[])source.Doors.Clone();
        for (int i = 0; i < doors.Length; i++)
        for (int j = i + 1; j < doors.Length; j++)
        {
            DoorDefinition a = doors[i], b = doors[j];
            if (a.IsExit || b.IsExit || a.RoomA != b.RoomA || a.RoomB != b.RoomB) continue;
            RoomDefinition first = source.Rooms[a.RoomA], second = source.Rooms[a.RoomB];
            float low = a.Vertical
                ? Mathf.Max(first.Center.y - first.Size.y * 0.5f, second.Center.y - second.Size.y * 0.5f)
                : Mathf.Max(first.Center.x - first.Size.x * 0.5f, second.Center.x - second.Size.x * 0.5f);
            float high = a.Vertical
                ? Mathf.Min(first.Center.y + first.Size.y * 0.5f, second.Center.y + second.Size.y * 0.5f)
                : Mathf.Min(first.Center.x + first.Size.x * 0.5f, second.Center.x + second.Size.x * 0.5f);
            float span = Mathf.Min(Mathf.Max(0.84f, minimumSpacing + 0.04f), high - low - DoorView.Width - 0.12f);
            float middle = (high + low) * 0.5f;
            float sign = (a.Vertical ? a.Position.y < b.Position.y : a.Position.x < b.Position.x) ? -1f : 1f;
            Vector2 pa = a.Position, pb = b.Position;
            if (a.Vertical) { pa.y = middle + sign * span * 0.5f; pb.y = middle - sign * span * 0.5f; }
            else { pa.x = middle + sign * span * 0.5f; pb.x = middle - sign * span * 0.5f; }
            doors[i] = new DoorDefinition(a.RoomA, a.RoomB, pa, a.Vertical, a.IsExit, a.Direction);
            doors[j] = new DoorDefinition(b.RoomA, b.RoomB, pb, b.Vertical, b.IsExit, b.Direction);
        }
        return Copy(source, source.Rooms, doors);
    }

    public static Bounds Measure(LevelDefinition puzzle)
    {
        RoomDefinition first = puzzle.Rooms[0];
        Bounds result = new Bounds(first.Center, first.Size + Vector2.one * 0.36f);
        foreach (RoomDefinition room in puzzle.Rooms)
            result.Encapsulate(new Bounds(room.Center, room.Size + Vector2.one * 0.36f));
        foreach (DoorDefinition door in puzzle.Doors)
        {
            result.Encapsulate(new Bounds(door.Position, door.Vertical
                ? new Vector3(0.22f, 0.72f, 0) : new Vector3(0.72f, 0.22f, 0)));
            if (!door.IsExit) continue;
            Vector2 outward = (door.Position - puzzle.Rooms[door.RoomA].Center).normalized;
            result.Encapsulate(new Bounds(door.Position + outward * 0.6f, Vector2.one * 0.76f));
        }
        return result;
    }

    public static float FitPixels(Bounds bounds, Rect area) =>
        Mathf.Max(0.01f, ScreenFill * Mathf.Min(area.width / bounds.size.x, area.height / bounds.size.y));

    public static Rect WorldArea(Vector2 center, Rect screenArea, float pixelsPerUnit)
    {
        Vector2 size = screenArea.size / pixelsPerUnit;
        return new Rect(center - size * 0.5f, size);
    }

    public static List<Vector2> TouchPolygon(LevelDefinition puzzle, int index,
        float pixelsPerUnit, float uiScale, Rect worldArea)
    {
        Vector2 center = puzzle.Doors[index].Position;
        float half = Mathf.Max(DoorView.Width * TapMultiplier,
            MinimumTouchPixels(uiScale) / pixelsPerUnit) * 0.5f;
        List<Vector2> points = new List<Vector2>
        {
            center + new Vector2(-half, -half), center + new Vector2(half, -half),
            center + new Vector2(half, half), center + new Vector2(-half, half)
        };
        for (int i = 0; i < puzzle.Doors.Length; i++)
        {
            if (i == index) continue;
            Vector2 other = puzzle.Doors[i].Position;
            Vector2 normal = (other - center).normalized;
            if (normal.sqrMagnitude < 0.5f) continue;
            points = Clip(points, normal, Vector2.Dot(normal, (center + other) * 0.5f) - Separation);
        }
        points = Clip(points, Vector2.right, worldArea.xMax);
        points = Clip(points, Vector2.left, -worldArea.xMin);
        points = Clip(points, Vector2.up, worldArea.yMax);
        return Clip(points, Vector2.down, -worldArea.yMin);
    }

    private static List<Vector2> Clip(List<Vector2> points, Vector2 normal, float limit)
    {
        List<Vector2> result = new List<Vector2>();
        if (points.Count == 0) return result;
        Vector2 previous = points[points.Count - 1];
        float before = Vector2.Dot(normal, previous) - limit;
        foreach (Vector2 current in points)
        {
            float after = Vector2.Dot(normal, current) - limit;
            if ((before <= 0) != (after <= 0))
                result.Add(Vector2.Lerp(previous, current, before / (before - after)));
            if (after <= 0) result.Add(current);
            previous = current;
            before = after;
        }
        return result;
    }

    private static Vector2 PolygonSize(List<Vector2> points)
    {
        if (points.Count == 0) return Vector2.zero;
        Vector2 min = points[0], max = points[0];
        foreach (Vector2 point in points) { min = Vector2.Min(min, point); max = Vector2.Max(max, point); }
        return max - min;
    }
}
