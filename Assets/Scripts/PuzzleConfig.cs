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

public enum PuzzleAtmosphere { Light, Dusk }
public enum PuzzleRoomLayout { Compact, WideRooms, TallRooms }

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
    public readonly PuzzleAtmosphere Atmosphere;
    public readonly PuzzleRoomLayout RoomLayout;
    public readonly int LongRoomSpan;
    public readonly int MinElongatedRooms;
    public readonly int MinDistinctBranchingRooms;
    public readonly int OneWayDoorCount; // Internal doors only; 0, 1 or 2.
    public readonly float MaxLayoutAspectRatio; // 0 keeps the existing layout rules.

    public PuzzleConfig(int roomCount, int doorCount, int deadEndCount,
        int branching, int seed, PuzzleTheme visualTheme,
        bool allowLeafRooms = false, int minRoomDegree = 2,
        int preferredStartMinDegree = 3, int minBranchingRooms = 1,
        int minWrongStartSurvivalMoves = 0,
        PuzzleAtmosphere atmosphere = PuzzleAtmosphere.Light,
        PuzzleRoomLayout roomLayout = PuzzleRoomLayout.Compact,
        int longRoomSpan = 2, int minElongatedRooms = 0,
        int minDistinctBranchingRooms = 0, int oneWayDoorCount = 0,
        float maxLayoutAspectRatio = 0f)
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
        Atmosphere = atmosphere;
        RoomLayout = roomLayout;
        LongRoomSpan = longRoomSpan;
        MinElongatedRooms = minElongatedRooms;
        MinDistinctBranchingRooms = minDistinctBranchingRooms;
        OneWayDoorCount = oneWayDoorCount;
        MaxLayoutAspectRatio = maxLayoutAspectRatio;
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
