using UnityEngine;

// Add another puzzle here without changing the movement, drawing, or solver code.
public static class LevelCatalog
{
    public static readonly LevelDefinition[] Levels =
    {
        new LevelDefinition("LEVEL 1", new[]
        {
            new RoomDefinition(new Vector2(0, -1), new Vector2(4, 2)),
            new RoomDefinition(new Vector2(-1, 1), new Vector2(2, 2)),
            new RoomDefinition(new Vector2(1, 1), new Vector2(2, 2))
        }, new[]
        {
            new DoorDefinition(0, 1, new Vector2(-1, 0)),
            new DoorDefinition(1, 2, new Vector2(0, 1), true),
            new DoorDefinition(2, 0, new Vector2(1, 0)),
            new DoorDefinition(0, -1, new Vector2(0, -2), isExit: true)
        }, 4.5f, new Color(0.92f, 0.96f, 0.96f)),

        new LevelDefinition("LEVEL 2", new[]
        {
            // Three equal columns; the player chooses a starting room.
            new RoomDefinition(new Vector2(0, -2.2f), new Vector2(1.6f, 2)),
            new RoomDefinition(new Vector2(1.6f, -1.2f), new Vector2(1.6f, 4)),
            new RoomDefinition(new Vector2(1.6f, 2), new Vector2(1.6f, 2.4f)),
            new RoomDefinition(new Vector2(0, 1), new Vector2(1.6f, 4.4f)),
            new RoomDefinition(new Vector2(-1.6f, -2.2f), new Vector2(1.6f, 2)),
            new RoomDefinition(new Vector2(-1.6f, -0.2f), new Vector2(1.6f, 2)),
            new RoomDefinition(new Vector2(-1.6f, 2), new Vector2(1.6f, 2.4f))
        }, new[]
        {
            // The intended route follows these nine doors, then the exit.
            new DoorDefinition(0, 1, new Vector2(0.8f, -2.2f), true),
            new DoorDefinition(1, 2, new Vector2(2, 0.8f)),
            new DoorDefinition(2, 1, new Vector2(1.2f, 0.8f)),
            new DoorDefinition(1, 3, new Vector2(0.8f, -0.2f), true),
            new DoorDefinition(3, 0, new Vector2(0, -1.2f)),
            new DoorDefinition(0, 4, new Vector2(-0.8f, -2.2f), true),
            new DoorDefinition(4, 5, new Vector2(-1.6f, -1.2f)),
            new DoorDefinition(5, 6, new Vector2(-1.6f, 0.8f)),
            new DoorDefinition(6, 3, new Vector2(-0.8f, 2), true),
            new DoorDefinition(3, -1, new Vector2(0, 3.2f), isExit: true)
        }, 4.7f, new Color(0.96f, 0.93f, 0.98f))
    };
}
