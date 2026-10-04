using System;
using System.Collections.Generic;
using UnityEngine;

public static class PuzzleGenerator
{
    private const int GridSize = 3;
    private const int MaxAttempts = 256;
    private const float RoomSize = 1.8f;
    private const float PairedDoorOffset = 0.42f;

    private struct Edge
    {
        public readonly int A;
        public readonly int B;

        public Edge(int a, int b)
        {
            A = a;
            B = b;
        }
    }

    private struct ExitOption
    {
        public readonly int Room;
        public readonly Vector2Int Direction;

        public ExitOption(int room, Vector2Int direction)
        {
            Room = room;
            Direction = direction;
        }
    }

    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left
    };

    public static LevelDefinition Generate(PuzzleConfig config, string name)
    {
        if (config.RoomCount < 3 || config.RoomCount > GridSize * GridSize ||
            config.DoorCount < config.RoomCount + 1 || config.DoorCount > 30 ||
            config.DeadEndCount < 0 || config.Branching < 0 || config.Branching > 2 ||
            config.MinRoomDegree < 1 || config.MinRoomDegree > 4 ||
            config.PreferredStartMinDegree < 1 || config.MinBranchingRooms < 0 ||
            config.MinWrongStartSurvivalMoves < 0 || config.MinElongatedRooms < 0 ||
            config.MinDistinctBranchingRooms < 0 ||
            config.OneWayDoorCount < 0 || config.OneWayDoorCount > 2 ||
            float.IsNaN(config.MaxLayoutAspectRatio) || float.IsInfinity(config.MaxLayoutAspectRatio) ||
            (config.MaxLayoutAspectRatio != 0f && config.MaxLayoutAspectRatio < 1f) ||
            !Enum.IsDefined(typeof(PuzzleRoomLayout), config.RoomLayout) ||
            !Enum.IsDefined(typeof(PuzzleAtmosphere), config.Atmosphere))
            throw new ArgumentException("Invalid puzzle configuration for " + name);

        config = NormalizeConfig(config, name);
        if (TryGenerate(config, name, 0, out LevelDefinition puzzle)) return puzzle;

        // Quality preferences are softer than the no-leaf rule. Keep the
        // degree floor and EXIT-last solver even if a custom config is tight.
        Debug.LogWarning(name + ": quality settings could not be met after " +
            MaxAttempts + " attempts. Retrying with relaxed branching/start preferences.");
        PuzzleConfig fallback = NormalizeConfig(new PuzzleConfig(config.RoomCount,
            config.DoorCount,
            config.DeadEndCount, 0, config.Seed, config.VisualTheme,
            config.AllowLeafRooms, Math.Min(config.MinRoomDegree, 2), 2, 0, 0,
            config.Atmosphere, config.RoomLayout, config.LongRoomSpan,
            config.MinElongatedRooms, oneWayDoorCount: config.OneWayDoorCount,
            maxLayoutAspectRatio: config.MaxLayoutAspectRatio), name);
        if (TryGenerate(fallback, name, MaxAttempts, out puzzle)) return puzzle;

        Debug.LogWarning(name + ": no variant met the requested configuration " +
            "after " + (MaxAttempts * 2) + " attempts. Using a solvable " +
            "no-leaf fallback layout with " + config.RoomCount + " rooms.");
        return CreateSafeFallback(config, name);
    }

    private static PuzzleConfig NormalizeConfig(PuzzleConfig config, string name)
    {
        int maxDoors = config.RoomCount + MaxGridAdjacencies(config.RoomCount);
        int minDegree = config.AllowLeafRooms ? config.MinRoomDegree :
            Math.Max(2, config.MinRoomDegree);
        int minInteriorDoors = MinimumInteriorDoors(config.RoomCount,
            minDegree, config.AllowLeafRooms);
        while (minInteriorDoors + 1 > maxDoors && minDegree > 2)
        {
            minDegree--;
            minInteriorDoors = MinimumInteriorDoors(config.RoomCount,
                minDegree, config.AllowLeafRooms);
        }
        int doorCount = Math.Min(Math.Max(config.DoorCount, minInteriorDoors + 1),
            maxDoors);
        int deadEnds = config.AllowLeafRooms ? config.DeadEndCount : 0;
        int span = Math.Max(2, Math.Min(config.MaxLayoutAspectRatio > 0f ? 2 : 3, config.LongRoomSpan));
        int elongated = config.RoomLayout == PuzzleRoomLayout.Compact ? 0 :
            Math.Min(config.MinElongatedRooms, config.RoomCount <= 4 ? 2 : GridSize);
        if (span != config.LongRoomSpan || elongated != config.MinElongatedRooms)
            Debug.LogWarning(name + ": incompatible room-size preferences; using span " +
                span + " and at least " + elongated + " elongated rooms.");
        if (doorCount != config.DoorCount || deadEnds != config.DeadEndCount ||
            minDegree != config.MinRoomDegree)
            Debug.LogWarning(name + ": incompatible room/door/degree settings; " +
                "using " + doorCount + " total doors, minimum interior degree " +
                minDegree + " and " + deadEnds + " non-exit leaf rooms.");

        return new PuzzleConfig(config.RoomCount, doorCount, deadEnds,
            config.Branching, config.Seed, config.VisualTheme,
            config.AllowLeafRooms, minDegree,
            config.PreferredStartMinDegree, config.MinBranchingRooms,
            config.MinWrongStartSurvivalMoves, config.Atmosphere,
            config.RoomLayout, span, elongated, config.MinDistinctBranchingRooms,
            config.OneWayDoorCount, config.MaxLayoutAspectRatio);
    }

    private static int MinimumInteriorDoors(int roomCount, int minDegree,
        bool allowLeafRooms)
    {
        int result = Math.Max(roomCount - 1,
            (roomCount * minDegree + 1) / 2);
        // A connected degree-two graph is a cycle. The orthogonal grid is
        // bipartite, so an odd number of rooms needs at least one extra edge.
        if (!allowLeafRooms && minDegree == 2 && roomCount % 2 == 1)
            result = Math.Max(result, roomCount + 1);
        return result;
    }

    private static int MaxGridAdjacencies(int roomCount)
    {
        int best = 0;
        for (int mask = 0; mask < 1 << (GridSize * GridSize); mask++)
        {
            int count = 0, links = 0;
            for (int cell = 0; cell < GridSize * GridSize; cell++)
            {
                if ((mask & (1 << cell)) == 0) continue;
                count++;
                int x = cell % GridSize, y = cell / GridSize;
                if (x + 1 < GridSize && (mask & (1 << (cell + 1))) != 0)
                    links++;
                if (y + 1 < GridSize && (mask & (1 << (cell + GridSize))) != 0)
                    links++;
            }
            if (count == roomCount && links > best) best = links;
        }
        return best;
    }

    private static LevelDefinition CreateSafeFallback(PuzzleConfig original,
        string name)
    {
        // A four-room cycle with a doubled tail has even interior degree at
        // every room. The three-room case uses a doubled two-edge path.
        // Both admit an Euler tour ending at an exposed EXIT wall.
        Vector2Int[] orderedCells =
        {
            new Vector2Int(0, 0), new Vector2Int(1, 0),
            new Vector2Int(1, 1), new Vector2Int(0, 1),
            new Vector2Int(2, 0), new Vector2Int(2, 1),
            new Vector2Int(2, 2), new Vector2Int(1, 2),
            new Vector2Int(0, 2)
        };
        List<Vector2Int> cells = new List<Vector2Int>();
        for (int i = 0; i < original.RoomCount; i++) cells.Add(orderedCells[i]);

        List<Edge> edges = new List<Edge>();
        if (cells.Count == 3)
        {
            edges.Add(new Edge(0, 1)); edges.Add(new Edge(0, 1));
            edges.Add(new Edge(1, 2)); edges.Add(new Edge(1, 2));
        }
        else
        {
            edges.Add(new Edge(0, 1)); edges.Add(new Edge(1, 2));
            edges.Add(new Edge(2, 3)); edges.Add(new Edge(0, 3));
            for (int i = 4; i < cells.Count; i++)
            {
                int previous = i == 4 ? 1 : i - 1;
                edges.Add(new Edge(previous, i));
                edges.Add(new Edge(previous, i));
            }
        }

        PuzzleConfig fallback = new PuzzleConfig(cells.Count, edges.Count + 1,
            0, 0, original.Seed, original.VisualTheme,
            false, 2, 2, 0, 0, original.Atmosphere, original.RoomLayout,
            original.LongRoomSpan, Math.Min(original.MinElongatedRooms, 2),
            oneWayDoorCount: original.OneWayDoorCount,
            maxLayoutAspectRatio: original.MaxLayoutAspectRatio);
        if (TryFinishGraph(fallback, name, cells, edges,
            new System.Random(original.Seed), out LevelDefinition result))
            return result;
        throw new InvalidOperationException(name + ": safe fallback validation failed.");
    }

    private static bool TryGenerate(PuzzleConfig config, string name,
        int attemptOffset, out LevelDefinition result)
    {
        for (int attempt = 0; attempt < MaxAttempts; attempt++)
        {
            // Each attempt is reproducible from the config seed.
            System.Random random = new System.Random(unchecked(config.Seed * 1009 +
                (attempt + attemptOffset) * 7919));
            List<Vector2Int> cells = CreateRoomPath(config.RoomCount, random);
            if (cells == null || !HasLayoutFootprint(config, cells)) continue;

            // Graph first: a path visits every room, then extra adjacent edges
            // create cycles, parallel doors, and choices.
            List<Edge> edges = new List<Edge>();
            for (int i = 1; i < cells.Count; i++)
                edges.Add(new Edge(i - 1, i));

            List<Edge> candidates = CreateExtraEdges(cells);
            Shuffle(candidates, random);
            if (TryAddEdges(config, name, cells, candidates, edges, 0,
                config.DoorCount - config.RoomCount, random, out LevelDefinition puzzle))
            {
                result = puzzle;
                return true;
            }
        }
        result = null;
        return false;
    }

    private static List<Vector2Int> CreateRoomPath(int count, System.Random random)
    {
        List<Vector2Int> path = new List<Vector2Int>
        {
            new Vector2Int(random.Next(GridSize), random.Next(GridSize))
        };

        while (path.Count < count)
        {
            Vector2Int last = path[path.Count - 1];
            List<Vector2Int> options = new List<Vector2Int>();
            foreach (Vector2Int direction in Directions)
            {
                Vector2Int next = last + direction;
                if (next.x >= 0 && next.x < GridSize &&
                    next.y >= 0 && next.y < GridSize && !path.Contains(next))
                    options.Add(next);
            }

            if (options.Count == 0) return null;
            path.Add(options[random.Next(options.Count)]);
        }

        return path;
    }

    private static List<Edge> CreateExtraEdges(List<Vector2Int> cells)
    {
        List<Edge> candidates = new List<Edge>();
        for (int a = 0; a < cells.Count; a++)
        for (int b = a + 1; b < cells.Count; b++)
            if (Mathf.Abs(cells[a].x - cells[b].x) +
                Mathf.Abs(cells[a].y - cells[b].y) == 1)
                candidates.Add(new Edge(a, b));
        return candidates;
    }

    private static bool HasLayoutFootprint(PuzzleConfig config, List<Vector2Int> cells)
    {
        GetCellBounds(cells, out Vector2Int min, out Vector2Int max);
        int width = max.x - min.x + 1;
        int height = max.y - min.y + 1;
        if (config.RoomLayout == PuzzleRoomLayout.Compact)
        {
            // Small lessons use a 2x2 block; five rooms use a tidy 2x3 footprint.
            if (cells.Count <= 4) return width == 2 && height == 2;
            if (cells.Count == 5) return width * height == 6;
            return true;
        }
        // Later puzzles avoid a completely filled rectangle when possible.
        return cells.Count == GridSize * GridSize || width * height > cells.Count;
    }

    private static bool TryAddEdges(PuzzleConfig config, string name,
        List<Vector2Int> cells, List<Edge> candidates, List<Edge> edges,
        int nextCandidate, int remaining, System.Random random,
        out LevelDefinition puzzle)
    {
        if (remaining == 0)
            return TryFinishGraph(config, name, cells, edges, random, out puzzle);

        for (int i = nextCandidate; i <= candidates.Count - remaining; i++)
        {
            edges.Add(candidates[i]);
            if (TryAddEdges(config, name, cells, candidates, edges, i + 1,
                remaining - 1, random, out puzzle))
                return true;
            edges.RemoveAt(edges.Count - 1);
        }

        puzzle = null;
        return false;
    }

    private static bool TryFinishGraph(PuzzleConfig config, string name,
        List<Vector2Int> cells, List<Edge> edges, System.Random random,
        out LevelDefinition puzzle)
    {
        puzzle = null;
        int[] degree = CountDegrees(edges, cells.Count);
        for (int room = 0; room < degree.Length; room++)
            if (degree[room] < config.MinRoomDegree ||
                (!config.AllowLeafRooms && degree[room] == 1))
                return false;
        // Parallel doors do not turn a chain of distinct rooms into a
        // meaningful route choice. A connected graph needs at least N
        // distinct links to contain a cycle.
        if (!config.AllowLeafRooms && cells.Count >= 4 &&
            CountDistinctEdges(edges) < cells.Count)
            return false;
        if (!HasRequiredBranching(config, edges, degree)) return false;

        List<ExitOption> exits = new List<ExitOption>();
        for (int room = 0; room < cells.Count; room++)
            foreach (Vector2Int direction in Directions)
                if (!cells.Contains(cells[room] + direction))
                    exits.Add(new ExitOption(room, direction));
        Shuffle(exits, random);

        // Size changes transform the drawing only. They never add graph vertices.
        RoomDefinition[] rooms = CreateRooms(cells, config);
        if (rooms == null) return false;

        foreach (ExitOption exit in exits)
        {
            if (CountDeadEnds(degree, exit.Room) != config.DeadEndCount)
                continue;

            // Only after choosing the logical graph do we lay out rectangles
            // and locate doors on their shared walls.
            DoorDefinition[] doors = CreateDoors(edges, cells, rooms, exit);
            LevelDefinition candidate = new LevelDefinition(name, rooms, doors,
                5f, ThemeColor(config.VisualTheme), config.Atmosphere);
            if (TryFinishDirections(config, candidate, degree, out puzzle)) return true;
        }

        return false;
    }

    private static bool HasStartQuality(PuzzleConfig config, LevelDefinition candidate,
        int[] degree, out bool[] winningStarts, bool requireMeaningfulArrows = false)
    {
        winningStarts = PuzzleSolver.FindWinningStarts(candidate);
        if (requireMeaningfulArrows &&
            !PuzzleSolver.HasMeaningfulOneWayDoors(candidate, winningStarts))
            return false;
        bool hasPreferredStart = false;
        int bestWrongSurvival = 0;
        for (int room = 0; room < candidate.Rooms.Length; room++)
        {
            if (winningStarts[room])
            {
                if (degree[room] >= config.PreferredStartMinDegree && degree[room] > 1)
                    hasPreferredStart = true;
            }
            else if (config.MinWrongStartSurvivalMoves > 0 &&
                bestWrongSurvival < config.MinWrongStartSurvivalMoves)
                bestWrongSurvival = Math.Max(bestWrongSurvival,
                    PuzzleSolver.LongestPathBeforeDeadEnd(candidate, room));
        }
        return hasPreferredStart && bestWrongSurvival >= config.MinWrongStartSurvivalMoves;
    }

    private static bool TryFinishDirections(PuzzleConfig config, LevelDefinition undirected,
        int[] degree, out LevelDefinition puzzle)
    {
        puzzle = null;
        if (!HasStartQuality(config, undirected, degree, out _)) return false;
        if (config.OneWayDoorCount == 0)
        {
            // Level 1 keeps its original graph, seed stream, and validation.
            puzzle = undirected;
            return true;
        }
        if (!PuzzleSolver.TryFindWinningRoute(undirected, out int startRoom, out int[] route))
            return false;

        int[] fromRoom = new int[undirected.Doors.Length];
        List<int> interiorDoors = new List<int>();
        int current = startRoom;
        foreach (int index in route)
        {
            DoorDefinition door = undirected.Doors[index];
            fromRoom[index] = current;
            if (!door.IsExit) interiorDoors.Add(index);
            current = door.OtherRoom(current);
        }

        // At most C(29,2)=406 variants with the supported 0..2 arrows and
        // 30-door limit. All loops are finite; EXIT is never a candidate.
        List<int[]> selections = new List<int[]>();
        for (int a = 0; a < interiorDoors.Count; a++)
        {
            if (config.OneWayDoorCount == 1)
                selections.Add(new[] { interiorDoors[a] });
            else
                for (int b = a + 1; b < interiorDoors.Count; b++)
                    selections.Add(new[] { interiorDoors[a], interiorDoors[b] });
        }
        System.Random directionRandom = new System.Random(unchecked(config.Seed * 104729 +
            startRoom * 7919 + undirected.Doors.Length * 271));
        Shuffle(selections, directionRandom);
        foreach (int[] selection in selections)
        {
            DoorDefinition[] doors = (DoorDefinition[])undirected.Doors.Clone();
            foreach (int index in selection)
            {
                DoorDefinition door = doors[index];
                DoorDirection direction = fromRoom[index] == door.RoomA
                    ? DoorDirection.AToB : DoorDirection.BToA;
                doors[index] = door.WithDirection(direction);
            }
            LevelDefinition directed = new LevelDefinition(undirected.Name, undirected.Rooms,
                doors, undirected.CameraSize, undirected.FloorColor, config.Atmosphere);
            // The witness still works, but perform the actual directed solver
            // and quality checks, including survival with directed reachability.
            if (!HasStartQuality(config, directed, degree, out _, true))
                continue;
            puzzle = directed;
            return true;
        }
        return false;
    }

    private static int[] CountDegrees(List<Edge> edges, int roomCount)
    {
        int[] degree = new int[roomCount];
        foreach (Edge edge in edges)
        {
            degree[edge.A]++;
            degree[edge.B]++;
        }
        return degree;
    }

    private static int CountDistinctEdges(List<Edge> edges)
    {
        HashSet<long> pairs = new HashSet<long>();
        foreach (Edge edge in edges)
            pairs.Add(((long)edge.A << 16) | (uint)edge.B);
        return pairs.Count;
    }

    private static bool HasRequiredBranching(PuzzleConfig config, List<Edge> edges,
        int[] degree)
    {
        int branchingRooms = 0;
        foreach (int roomDegree in degree)
            if (roomDegree >= 3) branchingRooms++;
        if (branchingRooms < config.MinBranchingRooms) return false;
        if (config.Branching == 0 && config.MinDistinctBranchingRooms == 0) return true;

        bool[,] neighbours = new bool[config.RoomCount, config.RoomCount];
        foreach (Edge edge in edges)
        {
            neighbours[edge.A, edge.B] = true;
            neighbours[edge.B, edge.A] = true;
        }

        int distinctBranchingRooms = 0;
        bool hasBranching = config.Branching == 0;
        for (int room = 0; room < degree.Length; room++)
        {
            int distinct = 0;
            for (int other = 0; other < degree.Length; other++)
                if (neighbours[room, other]) distinct++;
            if (distinct >= 3) distinctBranchingRooms++;
            if (config.Branching == 1 && degree[room] >= 3) hasBranching = true;
            if (config.Branching == 2 && distinct >= 3) hasBranching = true;
        }
        return hasBranching && distinctBranchingRooms >= config.MinDistinctBranchingRooms;
    }

    private static int CountDeadEnds(int[] degree, int exitRoom)
    {
        int count = 0;
        for (int i = 0; i < degree.Length; i++)
            if (i != exitRoom && degree[i] == 1) count++;
        return count;
    }

    private static void GetCellBounds(List<Vector2Int> cells,
        out Vector2Int min, out Vector2Int max)
    {
        min = new Vector2Int(GridSize, GridSize);
        max = Vector2Int.zero;
        foreach (Vector2Int cell in cells)
        {
            min = new Vector2Int(Mathf.Min(min.x, cell.x), Mathf.Min(min.y, cell.y));
            max = new Vector2Int(Mathf.Max(max.x, cell.x), Mathf.Max(max.y, cell.y));
        }
    }

    private static RoomDefinition[] CreateRooms(List<Vector2Int> cells, PuzzleConfig config)
    {
        // Level 1 uses precisely the original band selection and random stream.
        if (config.MaxLayoutAspectRatio <= 0f)
            return CreateRoomBands(cells, config, config.RoomLayout, -1);

        PuzzleRoomLayout alternate = config.RoomLayout == PuzzleRoomLayout.WideRooms
            ? PuzzleRoomLayout.TallRooms : PuzzleRoomLayout.WideRooms;
        List<int> order = new List<int> { 0, 1, 2 };
        Shuffle(order, new System.Random(unchecked(config.Seed * 31337 + (int)config.RoomLayout * 271)));
        foreach (PuzzleRoomLayout layout in new[] { config.RoomLayout, alternate })
        foreach (int band in order)
        {
            RoomDefinition[] rooms = CreateRoomBands(cells, config, layout, band);
            if (rooms != null && HasRoomAspect(rooms, config.MaxLayoutAspectRatio)) return rooms;
        }
        return null; // Try another graph placement; attempts remain bounded.
    }

    private static bool HasRoomAspect(RoomDefinition[] rooms, float maximum)
    {
        Vector2 min = rooms[0].Center - rooms[0].Size * 0.5f;
        Vector2 max = rooms[0].Center + rooms[0].Size * 0.5f;
        foreach (RoomDefinition room in rooms)
        {
            min = Vector2.Min(min, room.Center - room.Size * 0.5f);
            max = Vector2.Max(max, room.Center + room.Size * 0.5f);
        }
        Vector2 size = max - min;
        return Mathf.Max(size.x / size.y, size.y / size.x) <= maximum + 0.001f;
    }

    private static RoomDefinition[] CreateRoomBands(List<Vector2Int> cells, PuzzleConfig config,
        PuzzleRoomLayout layout, int preferredBand)
    {
        int[] widths = { 1, 1, 1 };
        int[] heights = { 1, 1, 1 };
        if (layout != PuzzleRoomLayout.Compact)
        {
            bool wide = layout == PuzzleRoomLayout.WideRooms;
            List<int> bands = new List<int>();
            for (int band = 0; band < GridSize; band++)
            {
                int occupants = 0;
                foreach (Vector2Int cell in cells)
                    if ((wide ? cell.x : cell.y) == band) occupants++;
                if (occupants >= Math.Max(1, config.MinElongatedRooms)) bands.Add(band);
            }
            if (bands.Count == 0) return null;
            // A separate seeded stream keeps visual choices out of graph search.
            System.Random layoutRandom = new System.Random(unchecked(config.Seed * 31337 +
                (int)config.RoomLayout * 271));
            int selected = preferredBand >= 0 ? preferredBand : bands[layoutRandom.Next(bands.Count)];
            if (!bands.Contains(selected)) return null;
            if (wide) widths[selected] = config.LongRoomSpan;
            else heights[selected] = config.LongRoomSpan;
        }

        // Weighted grid bands guarantee shared walls, no overlaps, and only
        // 1x1 / 2x1 / 1x2 / 3x1 / 1x3 rooms (never an accidental 2x2 room).
        float[] xBounds = BandBounds(widths);
        float[] yBounds = BandBounds(heights);
        GetCellBounds(cells, out Vector2Int min, out Vector2Int max);
        Vector2 origin = new Vector2((xBounds[min.x] + xBounds[max.x + 1]) * 0.5f,
            (yBounds[min.y] + yBounds[max.y + 1]) * 0.5f);

        RoomDefinition[] rooms = new RoomDefinition[cells.Count];
        for (int i = 0; i < cells.Count; i++)
        {
            Vector2Int cell = cells[i];
            Vector2 center = new Vector2((xBounds[cell.x] + xBounds[cell.x + 1]) * 0.5f,
                (yBounds[cell.y] + yBounds[cell.y + 1]) * 0.5f) - origin;
            Vector2Int footprint = new Vector2Int(widths[cell.x], heights[cell.y]);
            rooms[i] = new RoomDefinition(center,
                new Vector2(footprint.x, footprint.y) * RoomSize, footprint);
        }
        return rooms;
    }

    private static float[] BandBounds(int[] lengths)
    {
        float[] result = new float[lengths.Length + 1];
        for (int i = 0; i < lengths.Length; i++)
            result[i + 1] = result[i] + lengths[i] * RoomSize;
        return result;
    }

    private static DoorDefinition[] CreateDoors(List<Edge> edges,
        List<Vector2Int> cells, RoomDefinition[] rooms, ExitOption exit)
    {
        int[,] totals = new int[rooms.Length, rooms.Length];
        int[,] used = new int[rooms.Length, rooms.Length];
        foreach (Edge edge in edges)
            totals[edge.A, edge.B]++;

        DoorDefinition[] doors = new DoorDefinition[edges.Count + 1];
        for (int i = 0; i < edges.Count; i++)
        {
            Edge edge = edges[i];
            int ordinal = used[edge.A, edge.B]++;
            float offset = totals[edge.A, edge.B] == 2
                ? (ordinal == 0 ? -PairedDoorOffset : PairedDoorOffset) : 0f;
            bool vertical = cells[edge.A].x != cells[edge.B].x;
            RoomDefinition a = rooms[edge.A];
            RoomDefinition b = rooms[edge.B];
            // Midpoint between unequal room centres is NOT the shared wall.
            Vector2 position = vertical
                ? new Vector2(a.Center.x + Math.Sign(cells[edge.B].x - cells[edge.A].x) * a.Size.x * 0.5f,
                    (Mathf.Max(a.Center.y - a.Size.y * 0.5f, b.Center.y - b.Size.y * 0.5f) +
                     Mathf.Min(a.Center.y + a.Size.y * 0.5f, b.Center.y + b.Size.y * 0.5f)) * 0.5f)
                : new Vector2(
                    (Mathf.Max(a.Center.x - a.Size.x * 0.5f, b.Center.x - b.Size.x * 0.5f) +
                     Mathf.Min(a.Center.x + a.Size.x * 0.5f, b.Center.x + b.Size.x * 0.5f)) * 0.5f,
                    a.Center.y + Math.Sign(cells[edge.B].y - cells[edge.A].y) * a.Size.y * 0.5f);
            position += vertical ? Vector2.up * offset : Vector2.right * offset;
            doors[i] = new DoorDefinition(edge.A, edge.B, position, vertical);
        }

        Vector2 direction = new Vector2(exit.Direction.x, exit.Direction.y);
        Vector2 exitPosition = rooms[exit.Room].Center +
            Vector2.Scale(direction, rooms[exit.Room].Size) * 0.5f;
        doors[doors.Length - 1] = new DoorDefinition(exit.Room, -1, exitPosition,
            exit.Direction.x != 0, true);
        return doors;
    }

    private static Color ThemeColor(PuzzleTheme theme)
    {
        switch (theme)
        {
            case PuzzleTheme.Sky: return new Color(0.89f, 0.94f, 1f);
            case PuzzleTheme.Sand: return new Color(0.98f, 0.95f, 0.86f);
            case PuzzleTheme.Lilac: return new Color(0.94f, 0.91f, 0.99f);
            case PuzzleTheme.Coral: return new Color(1f, 0.91f, 0.88f);
            case PuzzleTheme.Teal: return new Color(0.85f, 0.96f, 0.94f);
            case PuzzleTheme.Violet: return new Color(0.91f, 0.88f, 0.98f);
            case PuzzleTheme.Rose: return new Color(0.99f, 0.89f, 0.93f);
            default: return new Color(0.90f, 0.97f, 0.92f);
        }
    }

    private static void Shuffle<T>(List<T> values, System.Random random)
    {
        for (int i = values.Count - 1; i > 0; i--)
        {
            int other = random.Next(i + 1);
            T value = values[i];
            values[i] = values[other];
            values[other] = value;
        }
    }
}
