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
    GameComplete,
    PuzzleSelection
}

public sealed class DoorPuzzleGame : MonoBehaviour
{
    [Header("Background music")]
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip level1Music;
    [SerializeField] private AudioClip level2Music;

    [Header("Sound effects")]
    [SerializeField] private AudioClip doorCloseSound;
    [SerializeField] private AudioClip puzzleCompleteSound;
    [SerializeField, Range(0f, 1f)] private float soundEffectsVolume = 0.8f;

    [Header("Input")]
    [SerializeField, Min(1f)] private float minSwipeDistance = 60f;

    private const float PlayerMoveSpeed = 4.5f;
    private static Sprite squareSprite;
    private static Sprite circleSprite;

    private Camera mainCamera;
    private PuzzleUI ui;
    private PuzzleMusic music;
    private AudioSource soundEffects;
    private bool soundEnabled = true;
    private PuzzleProgress progress;
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
    private Rect lastCameraArea;
    private Vector2Int lastCameraScreenSize;
    private Bounds level2VisualBounds;
    private bool gestureActive;
    private bool gestureIsTouch;
    private int gestureTouchIndex;
    private int gestureTouchId;
    private Vector2 gestureStartScreen;
    private Vector2 gestureStartWorld;
    private float gestureMaxDistanceSquared;
    private int gestureRoom;
    private PuzzleGameState gestureState;

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
        if (progress == null)
            progress = new PuzzleProgress(LevelCatalog.PuzzleCount,
                PlayerPrefs.GetInt(PuzzleProgress.SaveKey, 0), completed =>
                {
                    PlayerPrefs.SetInt(PuzzleProgress.SaveKey, completed);
                    // Persist at victory, before NEXT or an application shutdown.
                    PlayerPrefs.Save();
                });
        ui = gameObject.AddComponent<PuzzleUI>();
        ui.Build(StartGame, RestartPuzzle, NextPuzzle, NextLevel, ReturnHome,
            SetMusicEnabled, SetSoundEnabled, OpenPuzzleSelection, SelectPuzzle, progress);
        generatedPuzzles = new LevelDefinition[LevelCatalog.Levels.Length][];
        music = gameObject.AddComponent<PuzzleMusic>();
        music.Initialize(menuMusic, level1Music, level2Music);
        soundEffects = gameObject.AddComponent<AudioSource>();
        soundEffects.playOnAwake = false;
        soundEffects.loop = false;
        soundEffects.spatialBlend = 0f;
        if (doorCloseSound == null || puzzleCompleteSound == null)
            Debug.LogWarning("Sound effect references are missing on DoorPuzzleGame in the main scene.");
        ShowMainMenu();
    }

    private void Update()
    {
        if (!CanAcceptGameplayInput())
        {
            CancelGesture();
            return;
        }

        if (gestureActive)
        {
            UpdateGesture();
            return;
        }

        // Track one finger. Ignore secondary fingers and touch-emulated mouse
        // events so a single gesture can never produce two moves.
        if (Touchscreen.current != null)
        {
            bool touchInUse = false;
            for (int i = 0; i < Touchscreen.current.touches.Count; i++)
            {
                var touch = Touchscreen.current.touches[i];
                touchInUse |= touch.press.isPressed || touch.press.wasReleasedThisFrame;
                if (!touch.press.wasPressedThisFrame) continue;
                BeginGesture(touch.startPosition.ReadValue());
                gestureIsTouch = true;
                gestureTouchIndex = i;
                gestureTouchId = touch.touchId.ReadValue();
                if (gestureActive) UpdateGesture();
                return;
            }
            if (touchInUse) return;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            BeginGesture(Mouse.current.position.ReadValue());
            gestureIsTouch = false;
            if (gestureActive) UpdateGesture();
        }
    }

    private void BeginGesture(Vector2 screenPosition)
    {
        if (ui.IsPointerOverUI(screenPosition)) return;
        if (currentLevelIndex == 1 && !ui.GameplayScreenArea(true).Contains(screenPosition)) return;
        gestureActive = true;
        gestureStartScreen = screenPosition;
        gestureStartWorld = mainCamera.ScreenToWorldPoint(screenPosition);
        gestureMaxDistanceSquared = 0f;
        gestureState = state;
        gestureRoom = state == PuzzleGameState.Playing &&
            PuzzleSwipeResolver.ContainsRoom(level, currentRoom, gestureStartWorld) ? currentRoom : -1;
    }

    private void UpdateGesture()
    {
        Vector2 position;
        bool released;
        if (gestureIsTouch)
        {
            Touchscreen screen = Touchscreen.current;
            if (screen == null || gestureTouchIndex >= screen.touches.Count)
            {
                CancelGesture();
                return;
            }
            var touch = screen.touches[gestureTouchIndex];
            if (touch.touchId.ReadValue() != gestureTouchId ||
                touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Canceled)
            {
                CancelGesture();
                return;
            }
            position = touch.position.ReadValue();
            released = touch.press.wasReleasedThisFrame;
            if (!touch.press.isPressed && !released)
            {
                CancelGesture();
                return;
            }
        }
        else
        {
            if (Mouse.current == null)
            {
                CancelGesture();
                return;
            }
            position = Mouse.current.position.ReadValue();
            released = Mouse.current.leftButton.wasReleasedThisFrame;
            if (!Mouse.current.leftButton.isPressed && !released)
            {
                CancelGesture();
                return;
            }
        }

        gestureMaxDistanceSquared = Mathf.Max(gestureMaxDistanceSquared,
            (position - gestureStartScreen).sqrMagnitude);
        if (released) EndGesture(position);
    }

    private void EndGesture(Vector2 screenPosition)
    {
        // Clear first: state changes and movement also cancel any pending gesture.
        gestureActive = false;
        if (!CanAcceptGameplayInput() || state != gestureState || ui.IsPointerOverUI(screenPosition)) return;
        float threshold = Mathf.Max(1f, minSwipeDistance);
        if (gestureMaxDistanceSquared < threshold * threshold)
        {
            TryTap(gestureStartScreen);
            return;
        }
        if (state != PuzzleGameState.Playing || gestureRoom != currentRoom) return;
        Vector2 end = mainCamera.ScreenToWorldPoint(screenPosition);
        int door = PuzzleSwipeResolver.FindDoor(level, currentRoom,
            gestureStartWorld, end, CanUseDoor);
        if (door >= 0) TryMoveThroughDoor(door);
    }

    private void CancelGesture()
    {
        gestureActive = false;
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused) CancelGesture();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) CancelGesture();
    }

    private void OnDisable() => CancelGesture();

    private bool CanUseDoor(int index)
    {
        return state == PuzzleGameState.Playing && !busy && level != null &&
            index >= 0 && index < level.Doors.Length &&
            (closedDoors & (1 << index)) == 0 && level.Doors[index].CanTraverseFrom(currentRoom);
    }

    private bool TryMoveThroughDoor(int index)
    {
        if (!CanUseDoor(index)) return false;
        CancelGesture();
        StartCoroutine(CrossDoor(index));
        return true;
    }

    private void LateUpdate()
    {
        if (level == null) return;
        Rect area = ui.GameplayScreenArea(currentLevelIndex == 1);
        if (area != lastCameraArea || lastCameraScreenSize != new Vector2Int(Screen.width, Screen.height))
        {
            CancelGesture();
            FitPuzzleCamera(area);
        }
    }

    private void TryTap(Vector2 screenPosition)
    {
        if (!CanAcceptGameplayInput() || ui.IsPointerOverUI(screenPosition)) return;
        if (currentLevelIndex == 1 && !ui.GameplayScreenArea(true).Contains(screenPosition)) return;

        Vector3 world = mainCamera.ScreenToWorldPoint(screenPosition);
        if (state == PuzzleGameState.ChooseStartRoom)
        {
            TryChooseStartRoom(new Vector2(world.x, world.y));
            return;
        }

        Collider2D[] hits = Physics2D.OverlapPointAll(new Vector2(world.x, world.y));
        if (currentLevelIndex == 1)
        {
            // Stable nearest ownership also handles a boundary hit. Do not skip
            // a blocked arrow in favour of a neighbouring usable door.
            DoorView nearest = null;
            float distance = float.PositiveInfinity;
            foreach (Collider2D hit in hits)
            {
                DoorView view = hit.GetComponent<DoorView>();
                if (view == null) continue;
                float candidate = ((Vector2)world - level.Doors[view.Index].Position).sqrMagnitude;
                if (candidate < distance || (candidate == distance && nearest != null && view.Index < nearest.Index))
                {
                    nearest = view;
                    distance = candidate;
                }
            }
            if (nearest == null) return;
            TryMoveThroughDoor(nearest.Index);
            return;
        }

        foreach (Collider2D hit in hits)
        {
            DoorView view = hit.GetComponent<DoorView>();
            if (view == null) continue;

            if (TryMoveThroughDoor(view.Index)) return;
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
        if (!CanUseDoor(index)) yield break;
        DoorDefinition door = level.Doors[index];
        busy = true;
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
        PlaySoundEffect(doorCloseSound);
        yield return doorViews[index].AnimateClosed(0.35f);
        closedDoors |= 1 << index;
        closedCount++;
        RefreshProgress();
        busy = false;

        if (door.IsExit && closedCount == level.Doors.Length)
        {
            CompletePuzzle();
        }
        else if (PuzzleSolver.IsDeadEnd(level, currentRoom, closedDoors))
        {
            SetState(PuzzleGameState.Lose);
        }
    }

    private void CompletePuzzle()
    {
        progress.Complete(LevelCatalog.ToPuzzleIndex(currentLevelIndex, currentPuzzleIndex));
        PlaySoundEffect(puzzleCompleteSound);
        if (currentPuzzleIndex + 1 < LevelCatalog.Levels[currentLevelIndex].Puzzles.Length)
            SetState(PuzzleGameState.PuzzleComplete);
        else if (currentLevelIndex + 1 < LevelCatalog.Levels.Length)
            SetState(PuzzleGameState.LevelComplete);
        else
            SetState(PuzzleGameState.GameComplete);
    }

    private void PlaySoundEffect(AudioClip clip)
    {
        if (soundEnabled && clip != null) soundEffects.PlayOneShot(clip, soundEffectsVolume);
    }

    private void SetMusicEnabled(bool enabled)
    {
        music.SetMusicEnabled(enabled);
    }

    private void SetSoundEnabled(bool enabled)
    {
        soundEnabled = enabled;
        soundEffects.mute = !enabled;
        if (!enabled) soundEffects.Stop();
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
        // PLAY AGAIN retains its existing behavior; PLAY resumes the frontier.
        int index = state == PuzzleGameState.GameComplete ? 0 : progress.PlayIndex;
        if (LevelCatalog.TryGetPuzzle(index, out int levelIndex, out int puzzleIndex))
            LoadPuzzle(levelIndex, puzzleIndex);
    }

    private void OpenPuzzleSelection()
    {
        if (state == PuzzleGameState.MainMenu) SetState(PuzzleGameState.PuzzleSelection);
    }

    private void SelectPuzzle(int index)
    {
        if (state != PuzzleGameState.PuzzleSelection || !progress.IsUnlocked(index)) return;
        if (!LevelCatalog.TryGetPuzzle(index, out int levelIndex, out int puzzleIndex)) return;
        StopAllCoroutines();
        LoadPuzzle(levelIndex, puzzleIndex);
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
        CancelGesture();
        state = nextState;
        music.PlayForState(nextState, currentLevelIndex);
        ui.ShowState(nextState);
        if (startRoomHints != null)
            startRoomHints.SetActive(nextState == PuzzleGameState.ChooseStartRoom);
    }

    private void ClearPuzzle()
    {
        CancelGesture();
        if (soundEffects != null) soundEffects.Stop();
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
        if (levelIndex == 1)
            level = Level2Presentation.Prepare(level, ui.GameplayScreenArea(true), ui.PixelScale);
        currentRoom = -1;
        closedDoors = 0;
        closedCount = 0;
        player = null;
        busy = false;
        mainCamera.orthographic = true;
        if (levelIndex != 1) FitPuzzleCamera(ui.GameplayScreenArea());
        mainCamera.backgroundColor = level.Palette.Background;
        ui.SetGameplayPalette(level.Palette);

        levelRoot = new GameObject(level.Name);
        levelRoot.transform.SetParent(transform, false);
        DrawRooms();
        DrawDoors();
        if (levelIndex == 1)
        {
            level2VisualBounds = Level2Presentation.Measure(level);
            // Actual drawings include wall thickness, shadows, open leaves,
            // hinges, EXIT and arrow strokes; touch colliders are not visual bounds.
            foreach (SpriteRenderer renderer in levelRoot.GetComponentsInChildren<SpriteRenderer>(true))
                level2VisualBounds.Encapsulate(renderer.bounds);
            FitPuzzleCamera(ui.GameplayScreenArea(true));
        }
        RefreshProgress();
        SetState(PuzzleGameState.ChooseStartRoom);
    }

    private void FitPuzzleCamera(Rect area)
    {
        if (Screen.height <= 0 || Screen.width <= 0) return;
        if (currentLevelIndex == 1)
        {
            FitLevel2Camera(area);
            return;
        }
        Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        foreach (RoomDefinition room in level.Rooms)
        {
            min = Vector2.Min(min, room.Center - room.Size * 0.5f);
            max = Vector2.Max(max, room.Center + room.Size * 0.5f);
        }
        foreach (DoorDefinition door in level.Doors)
        {
            if (!door.IsExit) continue;
            // Include the final step outside, even when the EXIT is on a long room.
            min = Vector2.Min(min, door.Position - Vector2.one * 0.8f);
            max = Vector2.Max(max, door.Position + Vector2.one * 0.8f);
        }
        min -= Vector2.one * 0.16f;
        max += Vector2.one * 0.16f;
        Vector2 size = max - min;
        float pixelsPerUnit = Mathf.Min(area.width / size.x, area.height / size.y);
        mainCamera.orthographicSize = Mathf.Max(level.CameraSize, Screen.height / (2f * pixelsPerUnit));
        pixelsPerUnit = Screen.height / (2f * mainCamera.orthographicSize);
        Vector2 center = (min + max) * 0.5f -
            (area.center - new Vector2(Screen.width, Screen.height) * 0.5f) / pixelsPerUnit;
        mainCamera.transform.position = new Vector3(center.x, center.y, -10);
        lastCameraArea = area;
        lastCameraScreenSize = new Vector2Int(Screen.width, Screen.height);
    }

    private void FitLevel2Camera(Rect area)
    {
        Bounds bounds = level2VisualBounds;
        for (int pass = 0; pass < 3; pass++)
        {
            float pixels = Level2Presentation.FitPixels(bounds, area);
            // No fixed CameraSize floor: small houses can really fill the viewport.
            mainCamera.orthographicSize = Screen.height / (2f * pixels);
            Vector2 center = (Vector2)bounds.center -
                (area.center - new Vector2(Screen.width, Screen.height) * 0.5f) / pixels;
            mainCamera.transform.position = new Vector3(center.x, center.y, -10);
            Rect worldArea = Level2Presentation.WorldArea(bounds.center, area, pixels);
            foreach (DoorView view in doorViews)
            {
                view.ConfigureLevel2Interaction(
                    Level2Presentation.TouchPolygon(level, view.Index, pixels, ui.PixelScale, worldArea),
                    pixels, Level2Presentation.MinimumArrowPixels(ui.PixelScale));
                bounds.Encapsulate(view.DirectionBounds);
            }
        }
        Physics2D.SyncTransforms();
        lastCameraArea = area;
        lastCameraScreenSize = new Vector2Int(Screen.width, Screen.height);
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
                room.Size + Vector2.one * 0.22f, level.Palette.Shadow, -1,
                PuzzleVisualStyle.RoomShadowSprite);
            SpriteRenderer floor = Shape("Room " + (i + 1) + " floor",
                room.Center, room.Size, level.Palette.RoomColor(level.FloorColor), 0);
            BoxCollider2D roomArea = floor.gameObject.AddComponent<BoxCollider2D>();
            roomArea.size = Vector2.one;
            roomArea.isTrigger = true;
            float left = room.Center.x - room.Size.x * 0.5f;
            float right = room.Center.x + room.Size.x * 0.5f;
            float bottom = room.Center.y - room.Size.y * 0.5f;
            float top = room.Center.y + room.Size.y * 0.5f;
            Color wall = level.Palette.Wall;
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
        Color color = level.Palette.SelectionMarker;
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
            GameObject doorObject = new GameObject(door.IsExit ? "Exit Door" :
                door.IsOneWay ? "One-way Door" : "Door");
            doorObject.transform.SetParent(levelRoot.transform, false);
            doorObject.transform.position = new Vector3(door.Position.x, door.Position.y, 0);
            DoorView view = doorObject.AddComponent<DoorView>();
            Vector2? otherCenter = door.RoomB >= 0 ? level.Rooms[door.RoomB].Center : (Vector2?)null;
            view.Initialize(i, door, level.Rooms[door.RoomA].Center, squareSprite, otherCenter);
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
        int order, Sprite sprite = null)
    {
        GameObject objectWithSprite = new GameObject(name);
        objectWithSprite.transform.SetParent(levelRoot.transform, false);
        objectWithSprite.transform.position = new Vector3(center.x, center.y, 0);
        objectWithSprite.transform.localScale = new Vector3(size.x, size.y, 1);
        SpriteRenderer renderer = objectWithSprite.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite != null ? sprite : squareSprite;
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
