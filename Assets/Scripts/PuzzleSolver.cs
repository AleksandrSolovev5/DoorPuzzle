using System.Collections.Generic;

// Each bit in closedDoors records one permanently closed door.
public static class PuzzleSolver
{
    public static bool CanFinish(LevelDefinition level, int currentRoom, int closedDoors)
    {
        return Search(level, currentRoom, closedDoors, new HashSet<long>());
    }

    private static bool Search(LevelDefinition level, int room, int closed, HashSet<long> failed)
    {
        long state = ((long)closed << 16) | (uint)room;
        if (failed.Contains(state)) return false;

        int allClosed = (1 << level.Doors.Length) - 1;
        for (int i = 0; i < level.Doors.Length; i++)
        {
            int bit = 1 << i;
            DoorDefinition door = level.Doors[i];
            if ((closed & bit) != 0 || !door.Touches(room)) continue;

            int nextClosed = closed | bit;
            if (door.IsExit)
            {
                if (nextClosed == allClosed) return true;
            }
            else if (Search(level, door.OtherRoom(room), nextClosed, failed))
            {
                return true;
            }
        }

        failed.Add(state);
        return false;
    }
}
