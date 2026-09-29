using UnityEngine;

// A level is a small graph with positions for its wireframe drawing.
public sealed class LevelDefinition
{
    public readonly string Name;
    public readonly RoomDefinition[] Rooms;
    public readonly DoorDefinition[] Doors;
    public readonly int StartRoom;
    public readonly float CameraSize;
    public readonly Color FloorColor;

    public LevelDefinition(string name, RoomDefinition[] rooms, DoorDefinition[] doors,
        int startRoom, float cameraSize, Color floorColor)
    {
        Name = name;
        Rooms = rooms;
        Doors = doors;
        StartRoom = startRoom;
        CameraSize = cameraSize;
        FloorColor = floorColor;
    }
}

public sealed class RoomDefinition
{
    public readonly Vector2 Center;
    public readonly Vector2 Size;

    public RoomDefinition(Vector2 center, Vector2 size)
    {
        Center = center;
        Size = size;
    }
}

public sealed class DoorDefinition
{
    public readonly int RoomA;
    public readonly int RoomB; // -1 means outside the house.
    public readonly Vector2 Position;
    public readonly bool Vertical;
    public readonly bool IsExit;

    public DoorDefinition(int roomA, int roomB, Vector2 position,
        bool vertical = false, bool isExit = false)
    {
        RoomA = roomA;
        RoomB = roomB;
        Position = position;
        Vertical = vertical;
        IsExit = isExit;
    }

    public bool Touches(int room) { return RoomA == room || RoomB == room; }
    public int OtherRoom(int room) { return RoomA == room ? RoomB : RoomA; }
}
