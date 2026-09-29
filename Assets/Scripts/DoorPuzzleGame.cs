using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class DoorPuzzleGame : MonoBehaviour
{
    private enum GameState { ChooseStartRoom, Playing }

    private const float PlayerMoveSpeed = 4.5f;
    private static Sprite squareSprite;
    private static Sprite circleSprite;

    private Camera mainCamera;
    private PuzzleUI ui;
    private LevelDefinition level;
    private GameObject levelRoot;
    private DoorView[] doorViews;
    private Transform player;
    private int levelIndex;
    private int currentRoom;
    private int closedDoors;
    private int closedCount;
    private GameState state;
    private bool busy;

    private void Awake()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Screen.orientation = ScreenOrientation.Portrait;
        Application.targetFrameRate = 60;
#endif

        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main scene needs a camera tagged MainCamera.");
            enabled = false;
            return;
        }

        CreateSprites();
        ui = gameObject.AddComponent<PuzzleUI>();
        ui.Build(RestartLevel, RestartGame);
        LoadLevel(0);
    }

    private void Update()
    {
        if (busy) return;

        // Input System is the project's active input backend. Both paths work
        // without changing the Android settings or adding an input asset.
        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (!touch.press.wasPressedThisFrame) continue;
                TryTap(touch.position.ReadValue());
                return;
            }
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            TryTap(Mouse.current.position.ReadValue());
    }

    private void TryTap(Vector2 screenPosition)
    {
        Vector3 world = mainCamera.ScreenToWorldPoint(screenPosition);
        if (state == GameState.ChooseStartRoom)
        {
            TryChooseStartRoom(new Vector2(world.x, world.y));
            return;
        }

        Collider2D hit = Physics2D.OverlapPoint(new Vector2(world.x, world.y));
        if (hit == null) return;
        DoorView view = hit.GetComponent<DoorView>();
        if (view == null) return;

        int index = view.Index;
        DoorDefinition door = level.Doors[index];
        if ((closedDoors & (1 << index)) != 0 || !door.Touches(currentRoom)) return;

        StartCoroutine(CrossDoor(index));
    }

    private void TryChooseStartRoom(Vector2 point)
    {
        for (int i = 0; i < level.Rooms.Length; i++)
        {
            RoomDefinition room = level.Rooms[i];
            Vector2 offset = point - room.Center;
            if (Mathf.Abs(offset.x) >= room.Size.x * 0.5f ||
                Mathf.Abs(offset.y) >= room.Size.y * 0.5f)
                continue;

            currentRoom = i;
            DrawPlayer();
            state = GameState.Playing;
            ui.SetChooseStartRoom(false);
            return;
        }
    }

    private IEnumerator CrossDoor(int index)
    {
        busy = true;
        DoorDefinition door = level.Doors[index];
        Vector3 crossing = new Vector3(door.Position.x, door.Position.y, 0);
        int nextRoom = door.OtherRoom(currentRoom);
        Vector3 destination;
        if (door.IsExit)
        {
            Vector2 outward = (door.Position - level.Rooms[currentRoom].Center).normalized;
            destination = crossing + new Vector3(outward.x, outward.y, 0) * 0.6f;
        }
        else
        {
            Vector2 center = level.Rooms[nextRoom].Center;
            destination = new Vector3(center.x, center.y, 0);
        }

        yield return MovePlayerThroughDoor(crossing, destination);

        currentRoom = nextRoom;
        yield return doorViews[index].AnimateClosed(0.35f);
        closedDoors |= 1 << index;
        closedCount++;
        ui.SetProgress(level, closedCount);

        if (door.IsExit && closedCount == level.Doors.Length)
        {
            ui.ShowResult(true);
            yield return new WaitForSeconds(1.5f);
            if (levelIndex + 1 < LevelCatalog.Levels.Length)
                LoadLevel(levelIndex + 1);
            else
                ui.ShowGameOver();
        }
        else if (PuzzleSolver.IsDeadEnd(level, currentRoom, closedDoors))
        {
            ui.ShowResult(false);
            yield return new WaitForSeconds(1.5f);
            LoadLevel(levelIndex);
        }
        else
        {
            busy = false;
        }
    }

    private IEnumerator MovePlayerThroughDoor(Vector3 crossing, Vector3 destination)
    {
        Vector3 start = player.position;
        float beforeDoor = Vector3.Distance(start, crossing);
        float totalDistance = beforeDoor + Vector3.Distance(crossing, destination);
        float traveled = 0f;

        while (traveled < totalDistance)
        {
            traveled = Mathf.Min(traveled + PlayerMoveSpeed * Time.deltaTime, totalDistance);
            player.position = traveled < beforeDoor
                ? Vector3.MoveTowards(start, crossing, traveled)
                : Vector3.MoveTowards(crossing, destination, traveled - beforeDoor);
            yield return null;
        }
        player.position = destination;
    }

    private void RestartLevel()
    {
        StopAllCoroutines();
        LoadLevel(levelIndex);
    }

    private void RestartGame()
    {
        StopAllCoroutines();
        LoadLevel(0);
    }

    private void LoadLevel(int index)
    {
        if (levelRoot != null)
        {
            levelRoot.SetActive(false);
            Destroy(levelRoot);
        }

        levelIndex = index;
        level = LevelCatalog.Levels[index];
        currentRoom = -1;
        closedDoors = 0;
        closedCount = 0;
        player = null;
        state = GameState.ChooseStartRoom;
        busy = false;
        mainCamera.orthographic = true;
        float halfWidth = 0;
        foreach (RoomDefinition room in level.Rooms)
            halfWidth = Mathf.Max(halfWidth, Mathf.Abs(room.Center.x) + room.Size.x * 0.5f);
        foreach (DoorDefinition door in level.Doors)
            if (door.IsExit)
                halfWidth = Mathf.Max(halfWidth, Mathf.Abs(door.Position.x) + 0.75f);
        mainCamera.orthographicSize = Mathf.Max(level.CameraSize,
            (halfWidth + 0.2f) / mainCamera.aspect);
        mainCamera.transform.position = new Vector3(0, 0, -10);
        mainCamera.backgroundColor = new Color(0.98f, 0.98f, 0.96f);

        levelRoot = new GameObject(level.Name);
        levelRoot.transform.SetParent(transform, false);
        DrawRooms();
        DrawDoors();
        ui.SetProgress(level, closedCount);
        ui.SetChooseStartRoom(true);
    }

    private void DrawRooms()
    {
        for (int i = 0; i < level.Rooms.Length; i++)
        {
            RoomDefinition room = level.Rooms[i];
            Shape("Room " + (i + 1) + " floor", room.Center, room.Size, level.FloorColor, 0);
            float left = room.Center.x - room.Size.x * 0.5f;
            float right = room.Center.x + room.Size.x * 0.5f;
            float bottom = room.Center.y - room.Size.y * 0.5f;
            float top = room.Center.y + room.Size.y * 0.5f;
            Color wall = new Color(0.16f, 0.20f, 0.23f);
            DrawWall(i, false, top, left, right, wall);
            DrawWall(i, false, bottom, left, right, wall);
            DrawWall(i, true, left, bottom, top, wall);
            DrawWall(i, true, right, bottom, top, wall);
        }
    }

    private void DrawWall(int roomIndex, bool vertical, float coordinate,
        float start, float end, Color color)
    {
        List<DoorDefinition> openings = new List<DoorDefinition>();
        foreach (DoorDefinition door in level.Doors)
        {
            if (!door.Touches(roomIndex) || door.Vertical != vertical) continue;
            float doorCoordinate = vertical ? door.Position.x : door.Position.y;
            float along = vertical ? door.Position.y : door.Position.x;
            if (Mathf.Abs(doorCoordinate - coordinate) < 0.01f &&
                along > start && along < end)
                openings.Add(door);
        }
        openings.Sort((a, b) => (vertical ? a.Position.y : a.Position.x)
            .CompareTo(vertical ? b.Position.y : b.Position.x));

        float cursor = start;
        foreach (DoorDefinition door in openings)
        {
            float along = vertical ? door.Position.y : door.Position.x;
            float gapStart = Mathf.Max(start, along - DoorView.Width * 0.5f);
            float gapEnd = Mathf.Min(end, along + DoorView.Width * 0.5f);
            if (gapStart > cursor + 0.01f)
                DrawWallSegment(vertical, coordinate, cursor, gapStart, color);
            cursor = Mathf.Max(cursor, gapEnd);
        }
        if (end > cursor + 0.01f)
            DrawWallSegment(vertical, coordinate, cursor, end, color);
    }

    private void DrawWallSegment(bool vertical, float coordinate, float start,
        float end, Color color)
    {
        Vector2 center = vertical
            ? new Vector2(coordinate, (start + end) * 0.5f)
            : new Vector2((start + end) * 0.5f, coordinate);
        Vector2 size = vertical
            ? new Vector2(0.09f, end - start + 0.01f)
            : new Vector2(end - start + 0.01f, 0.09f);
        Shape("Wall", center, size, color, 2);
    }

    private void DrawDoors()
    {
        doorViews = new DoorView[level.Doors.Length];
        for (int i = 0; i < level.Doors.Length; i++)
        {
            DoorDefinition door = level.Doors[i];
            GameObject doorObject = new GameObject(door.IsExit ? "Exit Door" : "Door");
            doorObject.transform.SetParent(levelRoot.transform, false);
            doorObject.transform.position = new Vector3(door.Position.x, door.Position.y, 0);
            DoorView view = doorObject.AddComponent<DoorView>();
            view.Initialize(i, door, level.Rooms[door.RoomA].Center, squareSprite);
            doorViews[i] = view;
        }
    }

    private void DrawPlayer()
    {
        GameObject body = new GameObject("Player");
        body.transform.SetParent(levelRoot.transform, false);
        Vector2 start = level.Rooms[currentRoom].Center;
        body.transform.position = new Vector3(start.x, start.y, 0);
        body.transform.localScale = new Vector3(0.6f, 0.6f, 1);
        SpriteRenderer renderer = body.AddComponent<SpriteRenderer>();
        renderer.sprite = circleSprite;
        renderer.color = levelIndex == 1 ? new Color(0.91f, 0.18f, 0.22f)
            : new Color(0.13f, 0.57f, 0.85f);
        renderer.sortingOrder = 10;
        player = body.transform;
    }

    private SpriteRenderer Shape(string name, Vector2 center, Vector2 size, Color color,
        int order)
    {
        GameObject objectWithSprite = new GameObject(name);
        objectWithSprite.transform.SetParent(levelRoot.transform, false);
        objectWithSprite.transform.position = new Vector3(center.x, center.y, 0);
        objectWithSprite.transform.localScale = new Vector3(size.x, size.y, 1);
        SpriteRenderer renderer = objectWithSprite.AddComponent<SpriteRenderer>();
        renderer.sprite = squareSprite;
        renderer.color = color;
        renderer.sortingOrder = order;
        return renderer;
    }

    private static void CreateSprites()
    {
        if (squareSprite != null) return;

        Texture2D square = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        square.SetPixel(0, 0, Color.white);
        square.Apply();
        squareSprite = Sprite.Create(square, new Rect(0, 0, 1, 1),
            new Vector2(0.5f, 0.5f), 1);

        const int pixels = 64;
        Texture2D circle = new Texture2D(pixels, pixels, TextureFormat.RGBA32, false);
        for (int y = 0; y < pixels; y++)
        for (int x = 0; x < pixels; x++)
        {
            float dx = x - (pixels - 1) * 0.5f;
            float dy = y - (pixels - 1) * 0.5f;
            circle.SetPixel(x, y, dx * dx + dy * dy < 29 * 29
                ? Color.white : Color.clear);
        }
        circle.Apply();
        circleSprite = Sprite.Create(circle, new Rect(0, 0, pixels, pixels),
            new Vector2(0.5f, 0.5f), pixels);
    }
}
