// Edit a row to change a puzzle. The layout is generated from these values.
public static class LevelCatalog
{
    public static int PuzzleCount
    {
        get
        {
            int count = 0;
            foreach (GameLevelConfig level in Levels) count += level.Puzzles.Length;
            return count;
        }
    }

    public static int ToPuzzleIndex(int levelIndex, int puzzleIndex)
    {
        int index = puzzleIndex;
        for (int i = 0; i < levelIndex; i++) index += Levels[i].Puzzles.Length;
        return index;
    }

    public static bool TryGetPuzzle(int index, out int levelIndex, out int puzzleIndex)
    {
        levelIndex = puzzleIndex = -1;
        if (index < 0) return false;
        for (int i = 0; i < Levels.Length; i++)
        {
            if (index < Levels[i].Puzzles.Length)
            {
                levelIndex = i;
                puzzleIndex = index;
                return true;
            }
            index -= Levels[i].Puzzles.Length;
        }
        return false;
    }

    public static readonly GameLevelConfig[] Levels =
    {
        new GameLevelConfig(
            // Tutorial: one leaf is intentional; all later puzzles forbid it.
            new PuzzleConfig(3, 4, 0, 1, 101, PuzzleTheme.Mint,
                allowLeafRooms: true, minRoomDegree: 1, minBranchingRooms: 1),
            new PuzzleConfig(4, 6, 0, 1, 102, PuzzleTheme.Sky,
                minBranchingRooms: 2),
            new PuzzleConfig(5, 7, 0, 1, 103, PuzzleTheme.Sand,
                minBranchingRooms: 1),
            new PuzzleConfig(5, 8, 0, 1, 104, PuzzleTheme.Lilac,
                minBranchingRooms: 2)),

        new GameLevelConfig(
            new PuzzleConfig(6, 10, 0, 2, 201, PuzzleTheme.Sky,
                minBranchingRooms: 3, minWrongStartSurvivalMoves: 3,
                atmosphere: PuzzleAtmosphere.Dusk, roomLayout: PuzzleRoomLayout.WideRooms,
                minElongatedRooms: 2, minDistinctBranchingRooms: 1, oneWayDoorCount: 1,
                maxLayoutAspectRatio: 1.5f),
            new PuzzleConfig(7, 11, 0, 2, 202, PuzzleTheme.Teal,
                minBranchingRooms: 4, minWrongStartSurvivalMoves: 3,
                atmosphere: PuzzleAtmosphere.Dusk, roomLayout: PuzzleRoomLayout.TallRooms,
                minElongatedRooms: 2, minDistinctBranchingRooms: 2, oneWayDoorCount: 1,
                maxLayoutAspectRatio: 1.5f),
            new PuzzleConfig(7, 12, 0, 2, 203, PuzzleTheme.Violet,
                minBranchingRooms: 4, minWrongStartSurvivalMoves: 4,
                atmosphere: PuzzleAtmosphere.Dusk, roomLayout: PuzzleRoomLayout.WideRooms,
                minElongatedRooms: 2, minDistinctBranchingRooms: 2, oneWayDoorCount: 2,
                maxLayoutAspectRatio: 1.5f),
            new PuzzleConfig(8, 13, 0, 2, 204, PuzzleTheme.Lilac,
                minBranchingRooms: 5, minWrongStartSurvivalMoves: 4,
                atmosphere: PuzzleAtmosphere.Dusk, roomLayout: PuzzleRoomLayout.TallRooms,
                minElongatedRooms: 2, minDistinctBranchingRooms: 3, oneWayDoorCount: 2,
                maxLayoutAspectRatio: 1.5f))
    };
}
