using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum PuzzleGameState
{
    MainMenu,
    ChooseStartRoom,
    Playing,
    PuzzleComplete,
    Lose,
    LevelComplete,
    GameComplete
}

public sealed class DoorPuzzleGame : MonoBehaviour
{
    private const float PlayerMoveSpeed = 4.5f;
    private static Sprite squareSprite;
    private static Sprite circleSprite;

    private Camera mainCamera;
    private PuzzleUI ui;
    private LevelDefinition level;
    private GameObject levelRoot;
    private GameObject startRoomHints;
    private DoorView[] doorViews;
    private LevelDefinition[][] generatedPuzzles;
    private Transform player;
    private int currentLevelIndex;
    private int currentPuzzleIndex;
    private int currentRoom;
    private int closedDoors;
    private int closedCount;
    private PuzzleGameState state;
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

        mainCamera.backgroundColor = PuzzleVisualStyle.Background;
        CreateSprites();
        ui = gameObject.AddComponent<PuzzleUI>();
        ui.Build(StartGame, RestartPuzzle, NextPuzzle, NextLevel, ReturnHome);
        generatedPuzzles = new LevelDefinition[LevelCatalog.Levels.Length][];
        ShowMainMenu();
    }

    private void Update()
    {
        if (!CanAcceptGameplayInput()) return;

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
        if (!CanAcceptGameplayInput() || ui.IsPointerOverUI(screenPosition)) return;

        Vector3 world = mainCamera.ScreenToWorldPoint(screenPosition);
        if (state == PuzzleGameState.ChooseStartRoom)
        {
            TryChooseStartRoom(new Vector2(world.x, world.y));
            return;
        }

        foreach (Collider2D hit in Physics2D.OverlapPointAll(new Vector2(world.x, world.y)))
        {
            DoorView view = hit.GetComponent<DoorView>();
            if (view == null) continue;

            int index = view.Index;
            DoorDefinition door = level.Doors[index];
            if ((closedDoors & (1 << index)) != 0 || !door.Touches(currentRoom))
                continue;

            StartCoroutine(CrossDoor(index));
            return;
        }
    }

    private void TryChooseStartRoom(Vector2 point)
    {
        if (state != PuzzleGameState.ChooseStartRoom || busy) return;

        for (int i = 0; i < level.Rooms.Length; i++)
        {
            RoomDefinition room = level.Rooms[i];
            Vector2 offset = point - room.Center;
            if (Mathf.Abs(offset.x) >= room.Size.x * 0.5f ||
                Mathf.Abs(offset.y) >= room.Size.y * 0.5f)
                continue;

            currentRoom = i;
            DrawPlayer();
            SetState(PuzzleGameState.Playing);
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
        RefreshProgress();
        busy = false;

        if (door.IsExit && closedCount == level.Doors.Length)
        {
            if (currentPuzzleIndex + 1 < LevelCatalog.Levels[currentLevelIndex].Puzzles.Length)
                SetState(PuzzleGameState.PuzzleComplete);
            else if (currentLevelIndex + 1 < LevelCatalog.Levels.Length)
                SetState(PuzzleGameState.LevelComplete);
            else
                SetState(PuzzleGameState.GameComplete);
        }
        else if (PuzzleSolver.IsDeadEnd(level, currentRoom, closedDoors))
        {
            SetState(PuzzleGameState.Lose);
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

    private void RestartPuzzle()
    {
        if (state != PuzzleGameState.ChooseStartRoom &&
            state != PuzzleGameState.Playing && state != PuzzleGameState.Lose)
            return;

        StopAllCoroutines();
        LoadPuzzle(currentLevelIndex, currentPuzzleIndex);
    }

    private void StartGame()
    {
        if (state != PuzzleGameState.MainMenu && state != PuzzleGameState.GameComplete)
            return;

        StopAllCoroutines();
        LoadPuzzle(0, 0);
    }

    private void NextPuzzle()
    {
        if (state != PuzzleGameState.PuzzleComplete) return;
        LoadPuzzle(currentLevelIndex, currentPuzzleIndex + 1);
    }

    private void NextLevel()
    {
        if (state != PuzzleGameState.LevelComplete) return;
        LoadPuzzle(currentLevelIndex + 1, 0);
    }

    private void ReturnHome()
    {
        if (state != PuzzleGameState.GameComplete) return;
        ShowMainMenu();
    }

    private void ShowMainMenu()
    {
        StopAllCoroutines();
        ClearPuzzle();
        currentLevelIndex = 0;
        currentPuzzleIndex = 0;
        currentRoom = -1;
        closedDoors = 0;
        closedCount = 0;
        busy = false;
        SetState(PuzzleGameState.MainMenu);
    }

    private bool CanAcceptGameplayInput()
    {
        return !busy && (state == PuzzleGameState.ChooseStartRoom ||
            state == PuzzleGameState.Playing);
    }

    private void SetState(PuzzleGameState nextState)
    {
        state = nextState;
        ui.ShowState(nextState);
        if (startRoomHints != null)
            startRoomHints.SetActive(nextState == PuzzleGameState.ChooseStartRoom);
    }

    private void ClearPuzzle()
    {
        if (levelRoot != null)
        {
            levelRoot.SetActive(false);
            Destroy(levelRoot);
        }
        levelRoot = null;
        startRoomHints = null;
        level = null;
        doorViews = null;
        player = null;
    }

    private void LoadPuzzle(int levelIndex, int puzzleIndex)
    {
        ClearPuzzle();

        currentLevelIndex = levelIndex;
        currentPuzzleIndex = puzzleIndex;
        if (generatedPuzzles[levelIndex] == null)
            generatedPuzzles[levelIndex] =
                new LevelDefinition[LevelCatalog.Levels[levelIndex].Puzzles.Length];
        if (generatedPuzzles[levelIndex][puzzleIndex] == null)
        {
            PuzzleConfig config = LevelCatalog.Levels[levelIndex].Puzzles[puzzleIndex];
            string name = "Level " + (levelIndex + 1) + " Puzzle " + (puzzleIndex + 1);
            generatedPuzzles[levelIndex][puzzleIndex] = PuzzleGenerator.Generate(config, name);
        }
        level = generatedPuzzles[levelIndex][puzzleIndex];
        currentRoom = -1;
        closedDoors = 0;
        closedCount = 0;
        player = null;
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
        mainCamera.backgroundColor = PuzzleVisualStyle.Background;

        levelRoot = new GameObject(level.Name);
        levelRoot.transform.SetParent(transform, false);
        DrawRooms();
        DrawDoors();
        RefreshProgress();
        SetState(PuzzleGameState.ChooseStartRoom);
    }

    private void RefreshProgress()
    {
        ui.SetProgress(currentLevelIndex + 1, currentPuzzleIndex + 1,
            LevelCatalog.Levels[currentLevelIndex].Puzzles.Length,
            closedCount, level.Doors.Length);
    }

    private void DrawRooms()
    {
        startRoomHints = new GameObject("Start Room Hints");
        startRoomHints.transform.SetParent(levelRoot.transform, false);
        for (int i = 0; i < level.Rooms.Length; i++)
        {
            RoomDefinition room = level.Rooms[i];
            Shape("Room shadow", room.Center + new Vector2(0.04f, -0.07f),
                room.Size, PuzzleVisualStyle.Shadow, -1);
            SpriteRenderer floor = Shape("Room " + (i + 1) + " floor",
                room.Center, room.Size, PuzzleVisualStyle.RoomColor(level.FloorColor), 0);
            BoxCollider2D roomArea = floor.gameObject.AddComponent<BoxCollider2D>();
            roomArea.size = Vector2.one;
            roomArea.isTrigger = true;
            float left = room.Center.x - room.Size.x * 0.5f;
            float right = room.Center.x + room.Size.x * 0.5f;
            float bottom = room.Center.y - room.Size.y * 0.5f;
            float top = room.Center.y + room.Size.y * 0.5f;
            Color wall = PuzzleVisualStyle.Wall;
            DrawWall(i, false, top, left, right, wall);
            DrawWall(i, false, bottom, left, right, wall);
            DrawWall(i, true, left, bottom, top, wall);
            DrawWall(i, true, right, bottom, top, wall);
            DrawStartRoomHint(room);
        }
    }

    private void DrawStartRoomHint(RoomDefinition room)
    {
        // Quiet corner marks show that every room can be selected. They have
        // no colliders and disappear after the player chooses a room.
        Color color = Color.Lerp(PuzzleVisualStyle.RoomSurface,
            PuzzleVisualStyle.Primary, 0.34f);
        for (int x = -1; x <= 1; x += 2)
        for (int y = -1; y <= 1; y += 2)
        {
            Vector2 corner = room.Center + new Vector2(
                x * (room.Size.x * 0.5f - 0.17f),
                y * (room.Size.y * 0.5f - 0.17f));
            Shape("Selection corner", corner - new Vector2(x * 0.07f, 0),
                new Vector2(0.14f, 0.022f), color, 1)
                .transform.SetParent(startRoomHints.transform, true);
            Shape("Selection corner", corner - new Vector2(0, y * 0.07f),
                new Vector2(0.022f, 0.14f), color, 1)
                .transform.SetParent(startRoomHints.transform, true);
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
            ? new Vector2(0.07f, end - start + 0.01f)
            : new Vector2(end - start + 0.01f, 0.07f);
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
        Vector2 start = level.Rooms[currentRoom].SpawnPoint;
        body.transform.position = new Vector3(start.x, start.y, 0);
        body.transform.localScale = new Vector3(0.6f, 0.6f, 1);
        SpriteRenderer renderer = body.AddComponent<SpriteRenderer>();
        renderer.sprite = circleSprite;
        renderer.color = PuzzleVisualStyle.Player;
        renderer.sortingOrder = 10;
        GameObject rim = new GameObject("Player Rim");
        rim.transform.SetParent(body.transform, false);
        rim.transform.localScale = Vector3.one * 1.14f;
        SpriteRenderer rimRenderer = rim.AddComponent<SpriteRenderer>();
        rimRenderer.sprite = circleSprite;
        rimRenderer.color = PuzzleVisualStyle.Surface;
        rimRenderer.sortingOrder = 9;
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

        circleSprite = PuzzleVisualStyle.CircleSprite;
    }
}
