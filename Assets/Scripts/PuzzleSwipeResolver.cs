using System;
using UnityEngine;

// Resolves gesture geometry only. Availability and movement remain in DoorPuzzleGame.
public static class PuzzleSwipeResolver
{
    public static bool ContainsRoom(LevelDefinition puzzle, int room, Vector2 point)
    {
        if (room < 0 || room >= puzzle.Rooms.Length) return false;
        RoomDefinition definition = puzzle.Rooms[room];
        Vector2 offset = point - definition.Center;
        return Mathf.Abs(offset.x) < definition.Size.x * 0.5f &&
            Mathf.Abs(offset.y) < definition.Size.y * 0.5f;
    }

    public static int RoomAtPoint(LevelDefinition puzzle, Vector2 point)
    {
        for (int i = 0; i < puzzle.Rooms.Length; i++)
            if (ContainsRoom(puzzle, i, point)) return i;
        return -1;
    }

    public static int FindDoor(LevelDefinition puzzle, int currentRoom,
        Vector2 start, Vector2 end, Func<int, bool> canUseDoor)
    {
        if (!ContainsRoom(puzzle, currentRoom, start)) return -1;
        int target = RoomAtPoint(puzzle, end);
        if (target == currentRoom) return -1;

        int best = -1;
        float bestDistance = float.PositiveInfinity;
        for (int i = 0; i < puzzle.Doors.Length; i++)
        {
            DoorDefinition door = puzzle.Doors[i];
            if (!door.Touches(currentRoom)) continue;
            if (target >= 0)
            {
                if (door.IsExit || door.OtherRoom(currentRoom) != target) continue;
            }
            else if (!door.IsExit || !CrossesExit(puzzle.Rooms[currentRoom], door, start, end))
                continue;

            float distance = DistanceToSegment(door.Position, start, end);
            if (distance >= bestDistance) continue;
            bestDistance = distance;
            best = i;
        }
        // Resolve intention before availability. Otherwise aiming at a closed
        // or reverse one-way door silently selects another parallel door.
        return best >= 0 && canUseDoor(best) ? best : -1;
    }

    private static bool CrossesExit(RoomDefinition room, DoorDefinition door,
        Vector2 start, Vector2 end)
    {
        // Cross this particular outer wall near its EXIT, not any arbitrary wall.
        Vector2 normal = door.Vertical
            ? new Vector2(Mathf.Sign(door.Position.x - room.Center.x), 0)
            : new Vector2(0, Mathf.Sign(door.Position.y - room.Center.y));
        float before = Vector2.Dot(start - door.Position, normal);
        float after = Vector2.Dot(end - door.Position, normal);
        if (before >= 0f || after <= 0f) return false;
        Vector2 crossing = Vector2.Lerp(start, end, -before / (after - before));
        float offset = door.Vertical ? crossing.y - door.Position.y : crossing.x - door.Position.x;
        // A comfortable corridor twice the visual width of a door.
        return Mathf.Abs(offset) <= DoorView.Width;
    }

    private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 edge = end - start;
        if (edge.sqrMagnitude < 0.000001f) return (point - start).sqrMagnitude;
        Vector2 nearest = start + edge * Mathf.Clamp01(Vector2.Dot(point - start, edge) / edge.sqrMagnitude);
        return (point - nearest).sqrMagnitude;
    }
}
