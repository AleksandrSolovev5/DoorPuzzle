using UnityEngine;

// One generated puzzle: a room graph plus positions for its 2D drawing.
public sealed class LevelDefinition
{
    public readonly string Name;
    public readonly RoomDefinition[] Rooms;
    public readonly DoorDefinition[] Doors;
    public readonly float CameraSize;
    public readonly Color FloorColor;
    public readonly PuzzlePalette Palette;

    public LevelDefinition(string name, RoomDefinition[] rooms, DoorDefinition[] doors,
        float cameraSize, Color floorColor, PuzzleAtmosphere atmosphere = PuzzleAtmosphere.Light)
    {
        Name = name;
        Rooms = rooms;
        Doors = doors;
        CameraSize = cameraSize;
        FloorColor = floorColor;
        Palette = PuzzlePalette.For(atmosphere);
    }
}

public sealed class RoomDefinition
{
    public readonly Vector2 Center;
    public readonly Vector2 Size;
    public readonly Vector2 SpawnPoint;
    public readonly Vector2Int GridFootprint;

    public RoomDefinition(Vector2 center, Vector2 size)
        : this(center, size, Vector2Int.one) { }

    public RoomDefinition(Vector2 center, Vector2 size, Vector2Int gridFootprint)
    {
        Center = center;
        Size = size;
        SpawnPoint = center;
        GridFootprint = gridFootprint;
    }
}

public enum DoorDirection { Bidirectional, AToB, BToA }

public sealed class DoorDefinition
{
    public readonly int RoomA;
    public readonly int RoomB; // -1 means outside the house.
    public readonly Vector2 Position;
    public readonly bool Vertical;
    public readonly bool IsExit;
    public readonly DoorDirection Direction;
    public bool IsOneWay => Direction != DoorDirection.Bidirectional;
    public int AllowedFromRoom => !IsOneWay ? -1 :
        Direction == DoorDirection.AToB ? RoomA : RoomB;
    public int AllowedToRoom => !IsOneWay ? -1 :
        Direction == DoorDirection.AToB ? RoomB : RoomA;

    public DoorDefinition(int roomA, int roomB, Vector2 position,
        bool vertical = false, bool isExit = false,
        DoorDirection direction = DoorDirection.Bidirectional)
    {
        if (direction != DoorDirection.Bidirectional &&
            direction != DoorDirection.AToB && direction != DoorDirection.BToA)
            throw new System.ArgumentOutOfRangeException(nameof(direction));
        if (direction != DoorDirection.Bidirectional && (isExit || roomA < 0 || roomB < 0))
            throw new System.ArgumentException("Only interior doors can be one-way.");
        RoomA = roomA;
        RoomB = roomB;
        Position = position;
        Vertical = vertical;
        IsExit = isExit;
        Direction = direction;
    }

    public bool Touches(int room) { return RoomA == room || RoomB == room; }
    public int OtherRoom(int room) { return RoomA == room ? RoomB : RoomA; }

    public bool CanTraverseFrom(int room)
    {
        return room >= 0 && Touches(room) && (!IsOneWay || room == AllowedFromRoom);
    }

    public DoorDefinition WithDirection(DoorDirection direction)
    {
        return new DoorDefinition(RoomA, RoomB, Position, Vertical, IsExit, direction);
    }
}
