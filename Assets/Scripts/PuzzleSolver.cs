using System.Collections.Generic;

// Each bit in closedDoors records one permanently closed door.
public static class PuzzleSolver
{
    // Validator used by the generator. EXIT must be the final edge.
    public static bool TryFindWinningStart(LevelDefinition puzzle, out int startRoom)
    {
        HashSet<long> failed = new HashSet<long>();
        for (int room = 0; room < puzzle.Rooms.Length; room++)
        {
            if (!Search(puzzle, room, 0, failed)) continue;
            startRoom = room;
            return true;
        }

        startRoom = -1;
        return false;
    }

    // The generator uses all winning starts to check that a valid start is
    // structurally interesting. Runtime input remains unrestricted.
    public static bool[] FindWinningStarts(LevelDefinition puzzle)
    {
        bool[] winning = new bool[puzzle.Rooms.Length];
        HashSet<long> failed = new HashSet<long>();
        for (int room = 0; room < winning.Length; room++)
            winning[room] = Search(puzzle, room, 0, failed);
        return winning;
    }

    // Longest sequence of interior-door moves from this start before the
    // physical dead-end detector reports a loss. EXIT is excluded because
    // deliberately closing it early is not a plausible route.
    public static int LongestPathBeforeDeadEnd(LevelDefinition puzzle, int startRoom)
    {
        return CountSurvivalMoves(puzzle, startRoom, 0, new Dictionary<long, int>());
    }

    private static int CountSurvivalMoves(LevelDefinition puzzle, int room,
        int closed, Dictionary<long, int> memo)
    {
        if (IsDeadEnd(puzzle, room, closed)) return 0;
        long state = ((long)closed << 16) | (uint)room;
        if (memo.TryGetValue(state, out int cached)) return cached;

        int best = 0;
        for (int i = 0; i < puzzle.Doors.Length; i++)
        {
            DoorDefinition door = puzzle.Doors[i];
            if (door.IsExit || !door.Touches(room) || (closed & (1 << i)) != 0)
                continue;
            int moves = 1 + CountSurvivalMoves(puzzle, door.OtherRoom(room),
                closed | (1 << i), memo);
            if (moves > best) best = moves;
        }

        memo[state] = best;
        return best;
    }

    private static bool Search(LevelDefinition puzzle, int room, int closed,
        HashSet<long> failed)
    {
        long state = ((long)closed << 16) | (uint)room;
        if (failed.Contains(state)) return false;

        int allClosed = (1 << puzzle.Doors.Length) - 1;
        for (int i = 0; i < puzzle.Doors.Length; i++)
        {
            int bit = 1 << i;
            DoorDefinition door = puzzle.Doors[i];
            if ((closed & bit) != 0 || !door.Touches(room)) continue;

            int nextClosed = closed | bit;
            if (door.IsExit)
            {
                if (nextClosed == allClosed) return true;
            }
            else if (Search(puzzle, door.OtherRoom(room), nextClosed, failed))
                return true;
        }

        failed.Add(state);
        return false;
    }

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
