// Edit a row to change a puzzle. The layout is generated from these values.
public static class LevelCatalog
{
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
            new PuzzleConfig(5, 8, 0, 2, 201, PuzzleTheme.Coral,
                minBranchingRooms: 2, minWrongStartSurvivalMoves: 2),
            new PuzzleConfig(5, 9, 0, 2, 202, PuzzleTheme.Teal,
                minBranchingRooms: 3, minWrongStartSurvivalMoves: 2),
            new PuzzleConfig(6, 8, 0, 2, 203, PuzzleTheme.Violet,
                minBranchingRooms: 2, minWrongStartSurvivalMoves: 2),
            new PuzzleConfig(7, 9, 0, 2, 204, PuzzleTheme.Rose,
                minBranchingRooms: 2, minWrongStartSurvivalMoves: 3))
    };
}
