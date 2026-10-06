using System.Collections.Generic;

// Each bit in closedDoors records one permanently closed door.
public static class PuzzleSolver
{
    // Custom configs must not turn an exhaustive validation into unbounded
    // memory growth. Catalog puzzles visit far fewer states than this limit.
    private const int MaxSearchStates = 100000;

    internal sealed class SearchLimitException : System.InvalidOperationException
    {
        public SearchLimitException() : base("Puzzle validation exceeded " + MaxSearchStates + " states.") { }
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

    // A witness route lets the generator orient doors without destroying all solutions.
    public static bool TryFindWinningRoute(LevelDefinition puzzle, out int startRoom,
        out int[] doorOrder)
    {
        HashSet<long> failed = new HashSet<long>();
        List<int> route = new List<int>();
        for (int room = 0; room < puzzle.Rooms.Length; room++)
        {
            if (!Search(puzzle, room, 0, failed, -1, route)) continue;
            startRoom = room;
            doorOrder = route.ToArray();
            return true;
        }
        startRoom = -1;
        doorOrder = null;
        return false;
    }

    // Every arrow must remove an otherwise winning alternative from a valid start.
    // Reverse just this door; keep every other arrow and EXIT-last constraint.
    public static bool HasMeaningfulOneWayDoors(LevelDefinition puzzle, bool[] winningStarts)
    {
        for (int i = 0; i < puzzle.Doors.Length; i++)
        {
            if (!puzzle.Doors[i].IsOneWay) continue;
            bool reverseWasUseful = false;
            HashSet<long> failed = new HashSet<long>();
            for (int room = 0; room < winningStarts.Length; room++)
            {
                if (!winningStarts[room] || !Search(puzzle, room, 0, failed, i)) continue;
                reverseWasUseful = true;
                break;
            }
            if (!reverseWasUseful) return false;
        }
        return true;
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
        if (memo.Count >= MaxSearchStates) throw new SearchLimitException();

        int best = 0;
        for (int i = 0; i < puzzle.Doors.Length; i++)
        {
            DoorDefinition door = puzzle.Doors[i];
            if (door.IsExit || !door.CanTraverseFrom(room) || (closed & (1 << i)) != 0)
                continue;
            int moves = 1 + CountSurvivalMoves(puzzle, door.OtherRoom(room),
                closed | (1 << i), memo);
            if (moves > best) best = moves;
        }

        memo[state] = best;
        return best;
    }

    private static bool Search(LevelDefinition puzzle, int room, int closed,
        HashSet<long> failed, int reverseDoor = -1, List<int> route = null)
    {
        long state = ((long)closed << 16) | (uint)room;
        if (failed.Contains(state)) return false;
        if (failed.Count >= MaxSearchStates) throw new SearchLimitException();

        int allClosed = (1 << puzzle.Doors.Length) - 1;
        for (int i = 0; i < puzzle.Doors.Length; i++)
        {
            int bit = 1 << i;
            DoorDefinition door = puzzle.Doors[i];
            bool canTraverse = i == reverseDoor
                ? room == door.AllowedToRoom : door.CanTraverseFrom(room);
            if ((closed & bit) != 0 || !canTraverse) continue;

            int nextClosed = closed | bit;
            if (door.IsExit)
            {
                if (nextClosed == allClosed)
                {
                    route?.Add(i);
                    return true;
                }
            }
            else
            {
                route?.Add(i);
                if (Search(puzzle, door.OtherRoom(room), nextClosed, failed, reverseDoor, route))
                    return true;
                if (route != null) route.RemoveAt(route.Count - 1);
            }
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
                if ((closedDoors & (1 << i)) != 0 || door.IsExit || !door.CanTraverseFrom(room))
                    continue;

                int nextRoom = door.OtherRoom(room);
                if (nextRoom < 0 || reachable[nextRoom]) continue;
                reachable[nextRoom] = true;
                pending.Enqueue(nextRoom);
            }
        }

        // A one-way door is lost if its permitted entrance is unreachable,
        // even when its destination room can still be reached.
        for (int i = 0; i < level.Doors.Length; i++)
        {
            if ((closedDoors & (1 << i)) != 0) continue;
            DoorDefinition door = level.Doors[i];
            if (door.IsOneWay ? !reachable[door.AllowedFromRoom] :
                !reachable[door.RoomA] &&
                (door.RoomB < 0 || !reachable[door.RoomB]))
                return true;
        }

        return false;
    }
}
