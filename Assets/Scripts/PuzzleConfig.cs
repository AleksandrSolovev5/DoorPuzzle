using System;

public enum PuzzleTheme
{
    Mint,
    Sky,
    Sand,
    Lilac,
    Coral,
    Teal,
    Violet,
    Rose
}

// All degree settings count interior doors only; EXIT is never a neighbour.
// DoorCount includes EXIT. DeadEndCount counts non-exit rooms with one
// interior door. Branching: 0 = any, 1 = three doors in one room,
// 2 = three distinct neighbouring rooms.
[Serializable]
public sealed class PuzzleConfig
{
    public readonly int RoomCount;
    public readonly int DoorCount;
    public readonly int DeadEndCount;
    public readonly int Branching;
    public readonly int Seed;
    public readonly PuzzleTheme VisualTheme;
    public readonly bool AllowLeafRooms;
    public readonly int MinRoomDegree;
    public readonly int PreferredStartMinDegree;
    public readonly int MinBranchingRooms;
    public readonly int MinWrongStartSurvivalMoves;

    public PuzzleConfig(int roomCount, int doorCount, int deadEndCount,
        int branching, int seed, PuzzleTheme visualTheme,
        bool allowLeafRooms = false, int minRoomDegree = 2,
        int preferredStartMinDegree = 3, int minBranchingRooms = 1,
        int minWrongStartSurvivalMoves = 0)
    {
        RoomCount = roomCount;
        DoorCount = doorCount;
        DeadEndCount = deadEndCount;
        Branching = branching;
        Seed = seed;
        VisualTheme = visualTheme;
        AllowLeafRooms = allowLeafRooms;
        MinRoomDegree = minRoomDegree;
        PreferredStartMinDegree = preferredStartMinDegree;
        MinBranchingRooms = minBranchingRooms;
        MinWrongStartSurvivalMoves = minWrongStartSurvivalMoves;
    }
}

public sealed class GameLevelConfig
{
    public readonly PuzzleConfig[] Puzzles;

    public GameLevelConfig(params PuzzleConfig[] puzzles)
    {
        Puzzles = puzzles;
    }
}
