using System.Collections.Generic;

// Each bit in closedDoors records one permanently closed door.
public static class PuzzleSolver
{
    public static bool IsDeadEnd(LevelDefinition level, int currentRoom, int closedDoors)
    {
        int allClosed = (1 << level.Doors.Length) - 1;
        if (closedDoors == allClosed) return false;

        for (int i = 0; i < level.Doors.Length; i++)
            if (level.Doors[i].IsExit && (closedDoors & (1 << i)) != 0)
                return true;

        if (currentRoom < 0 || currentRoom >= level.Rooms.Length) return true;

        // Open interior doors form the rooms the player can still reach.
        bool[] reachable = new bool[level.Rooms.Length];
        Queue<int> pending = new Queue<int>();
        reachable[currentRoom] = true;
        pending.Enqueue(currentRoom);

        while (pending.Count > 0)
        {
            int room = pending.Dequeue();
            for (int i = 0; i < level.Doors.Length; i++)
            {
                DoorDefinition door = level.Doors[i];
                if ((closedDoors & (1 << i)) != 0 || door.IsExit || !door.Touches(room))
                    continue;

                int nextRoom = door.OtherRoom(room);
                if (nextRoom < 0 || reachable[nextRoom]) continue;
                reachable[nextRoom] = true;
                pending.Enqueue(nextRoom);
            }
        }

        // A door is lost only if neither of its rooms can be reached anymore.
        for (int i = 0; i < level.Doors.Length; i++)
        {
            if ((closedDoors & (1 << i)) != 0) continue;
            DoorDefinition door = level.Doors[i];
            if (!reachable[door.RoomA] &&
                (door.RoomB < 0 || !reachable[door.RoomB]))
                return true;
        }

        return false;
    }
}
