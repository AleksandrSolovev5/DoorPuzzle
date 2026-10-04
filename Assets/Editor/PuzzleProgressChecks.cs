using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Opt-in batch checks. The production PlayerPrefs key is never written or deleted.
[InitializeOnLoad]
public static class PuzzleProgressChecks
{
    private const string TestKey = "DoorPuzzle.ProgressChecks.Isolated.v1";
    private const string PhaseKey = "DoorPuzzle.ProgressChecks.Phase";
    private const string ReportPath = "Temp/ProgressChecks/scenarios.txt";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static DoorPuzzleGame game;
    private static PuzzleProgress progress;
    private static IEnumerator checks;
    private static int phase;
    private static int lastFrame;
    private static double started;
    private static bool sceneReloadRequested;
    private static int sceneReloadFrame;

    static PuzzleProgressChecks()
    {
        EditorApplication.playModeStateChanged += OnPlayMode;
        EditorApplication.update += Bootstrap;
    }

    public static void BeginFirstRun() => Begin(1);
    public static void BeginSecondRun() => Begin(2);

    private static void Begin(int run)
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Run these checks in batch mode only.");
        Directory.CreateDirectory("Temp/ProgressChecks");
        if (run == 1)
        {
            File.WriteAllText(ReportPath, "");
            PlayerPrefs.DeleteKey(TestKey);
            PlayerPrefs.Save();
        }
        EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
        SessionState.SetInt(PhaseKey, run);
    }

    private static void Bootstrap()
    {
        if (SessionState.GetInt(PhaseKey, 0) == 0 || checks != null ||
            EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (!EditorApplication.isPlaying)
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.EnterPlaymode();
            return;
        }
        if (EditorApplication.isPaused) EditorApplication.isPaused = false;
        // Entering Play Mode may trigger a forced script reload in this Unity
        // version. Recreate the runtime UI after it, including dynamic listeners.
        if (!sceneReloadRequested)
        {
            sceneReloadRequested = true;
            sceneReloadFrame = Time.frameCount;
            UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
            return;
        }
        if (Time.frameCount <= sceneReloadFrame + 1) return;
        DoorPuzzleGame ready = UnityEngine.Object.FindAnyObjectByType<DoorPuzzleGame>();
        if (ready == null || Get<PuzzleUI>(ready, "ui") == null ||
            Get<PuzzleSelectionView>(Get<PuzzleUI>(ready, "ui"), "puzzleSelection") == null ||
            Get<PuzzleMusic>(ready, "music") == null) return;
        phase = SessionState.GetInt(PhaseKey, 0);
        InitializeChecks();
    }

    private static void OnPlayMode(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        phase = SessionState.GetInt(PhaseKey, 0);
        if (phase == 0) return;
        // Bootstrap also resumes this setup after Unity recompiles assemblies.
    }

    private static void InitializeChecks()
    {
        try
        {
            game = UnityEngine.Object.FindAnyObjectByType<DoorPuzzleGame>();
            Require(game != null, "Main scene contains the existing game controller");
            Record("Initializing isolated progress storage");
            progress = new PuzzleProgress(LevelCatalog.PuzzleCount, PlayerPrefs.GetInt(TestKey, 0), completed =>
            {
                PlayerPrefs.SetInt(TestKey, completed);
                PlayerPrefs.Save();
            });
            Record("Isolated progress storage loaded");
            // Replace only this runtime instance's storage and its view reference.
            // Awake has read the real save, but has not written it.
            Set(game, "progress", progress);
            Set(Get<PuzzleSelectionView>(Get<PuzzleUI>(game, "ui"), "puzzleSelection"), "progress", progress);
            Record("Runtime storage references replaced");
            Time.timeScale = 12f;
            Application.targetFrameRate = 60;
            started = EditorApplication.timeSinceStartup;
            lastFrame = -1;
            checks = phase == 1 ? FirstRun() : SecondRun();
            EditorApplication.update += Step;
        }
        catch (Exception error) { Fail(error); }
    }

    private static void Step()
    {
        if (Time.frameCount == lastFrame) return;
        lastFrame = Time.frameCount;
        try
        {
            if (EditorApplication.timeSinceStartup - started > 180)
                throw new TimeoutException("Progress checks exceeded 180 seconds.");
            if (checks.MoveNext()) return;
            EditorApplication.update -= Step;
            SessionState.SetInt(PhaseKey, 0);
            Time.timeScale = 1f;
            if (phase == 2)
            {
                PlayerPrefs.DeleteKey(TestKey);
                PlayerPrefs.Save();
            }
            Record("PASS: batch phase " + phase);
            EditorApplication.Exit(0);
        }
        catch (Exception error) { Fail(error); }
    }

    private static IEnumerator FirstRun()
    {
        Require(State == PuzzleGameState.MainMenu, "Application starts on the main menu");
        foreach (string field in new[] { "menuMusic", "level1Music", "level2Music", "doorCloseSound", "puzzleCompleteSound" })
            Require(Get<AudioClip>(game, field) != null, "Audio reference: " + field);
        Click("Settings");
        Toggle musicToggle = Find<Toggle>("MUSIC Toggle");
        Toggle soundToggle = Find<Toggle>("SOUND Toggle");
        musicToggle.isOn = false;
        soundToggle.isOn = false;
        Require(Get<AudioSource>(Get<PuzzleMusic>(game, "music"), "source").mute &&
            !Get<bool>(game, "soundEnabled"), "Settings still mute music and effects independently");
        musicToggle.isOn = true;
        soundToggle.isOn = true;
        Click("Close Settings");
        Click("Select Puzzle");
        CheckSelection(1);
        Require(State == PuzzleGameState.PuzzleSelection, "1. Only Puzzle 1 is initially unlocked");
        Invoke("SelectPuzzle", 1);
        Require(State == PuzzleGameState.PuzzleSelection, "Locked puzzles cannot be started through the controller");
        Click("Back");
        Click("PLAY");
        Require(Index == 0, "2. PLAY starts Puzzle 1");
        IEnumerator solve = SolveCurrent();
        while (solve.MoveNext()) yield return null;
        Require(progress.CompletedCount == 1 && PlayerPrefs.GetInt(TestKey, -1) == 1,
            "3. Completing Puzzle 1 immediately persists and unlocks only Puzzle 2");
        Click("HOME");
        Click("Select Puzzle");
        CheckSelection(2);
        Require(progress.CompletedCount == 1, "7. Puzzle 3 is still locked");
        // Leave the isolated save intact for a second, entirely new Unity process.
    }

    private static IEnumerator SecondRun()
    {
        Require(progress.CompletedCount == 1 && progress.IsUnlocked(1) && !progress.IsUnlocked(2),
            "4. Puzzle 2 remains unlocked after a complete process restart");
        Click("PLAY");
        Require(Index == 1, "5. PLAY resumes at Puzzle 2 after restart");
        Click("HOME");
        Click("Select Puzzle");
        Click("Puzzle 0");
        Require(Index == 0, "6. Selection starts previously completed Puzzle 1");
        IEnumerator replay = SolveCurrent();
        while (replay.MoveNext()) yield return null;
        Require(progress.CompletedCount == 1 && PlayerPrefs.GetInt(TestKey, -1) == 1,
            "10. Replaying an old puzzle does not advance or reduce progress");
        Click("NEXT");

        for (int index = 1; index < LevelCatalog.PuzzleCount; index++)
        {
            Require(Index == index, "Sequential transition to puzzle " + (index + 1));
            IEnumerator solve = SolveCurrent();
            while (solve.MoveNext()) yield return null;
            Require(progress.CompletedCount == index + 1 && PlayerPrefs.GetInt(TestKey, -1) == index + 1,
                "Victory persists exactly one next puzzle: " + (index + 1));
            if (State == PuzzleGameState.PuzzleComplete) Click("NEXT");
            else if (State == PuzzleGameState.LevelComplete) Click("NEXT LEVEL");
        }
        Require(State == PuzzleGameState.GameComplete, "Final victory retains the game complete screen");
        Click("PLAY AGAIN");
        Require(Index == 0 && progress.CompletedCount == LevelCatalog.PuzzleCount,
            "PLAY AGAIN starts Puzzle 1 without clearing completion");
        Click("HOME");
        Click("PLAY");
        Require(Index == 0, "8. PLAY starts Puzzle 1 when all puzzles have been completed");
        Click("HOME");

        for (int index = 0; index < LevelCatalog.PuzzleCount; index++)
        {
            Click("Select Puzzle");
            CheckSelection(LevelCatalog.PuzzleCount);
            Click("Puzzle " + index);
            LevelDefinition puzzle = Get<LevelDefinition>(game, "level");
            Invoke("TryChooseStartRoom", puzzle.Rooms[0].Center);
            int door = Array.FindIndex(puzzle.Doors, value => value.CanTraverseFrom(0));
            game.StartCoroutine((IEnumerator)Invoke("CrossDoor", door));
            Click("HOME");
            yield return null;
            yield return null;
            Require(State == PuzzleGameState.MainMenu && Get<GameObject>(game, "levelRoot") == null &&
                !Get<bool>(game, "busy") && progress.CompletedCount == LevelCatalog.PuzzleCount,
                "9/10. HOME exits puzzle " + (index + 1) + " during movement and preserves progress");
        }
        // Exercise the lose overlay's HOME path with an early EXIT.
        Click("PLAY");
        LevelDefinition lastPuzzle = Get<LevelDefinition>(game, "level");
        int exit = Array.FindIndex(lastPuzzle.Doors, value => value.IsExit);
        Invoke("TryChooseStartRoom", lastPuzzle.Rooms[lastPuzzle.Doors[exit].RoomA].Center);
        game.StartCoroutine((IEnumerator)Invoke("CrossDoor", exit));
        while (Get<bool>(game, "busy")) yield return null;
        Require(State == PuzzleGameState.Lose, "Early EXIT still loses and grants no progress");
        Click("HOME");
        Require(State == PuzzleGameState.MainMenu && PlayerPrefs.GetInt(TestKey, -1) == LevelCatalog.PuzzleCount,
            "HOME works on the lose screen without resetting the save");
    }

    private static IEnumerator SolveCurrent()
    {
        LevelDefinition puzzle = Get<LevelDefinition>(game, "level");
        Require(PuzzleSolver.TryFindWinningRoute(puzzle, out int start, out int[] route), "Puzzle has an EXIT-last route");
        Invoke("TryChooseStartRoom", puzzle.Rooms[start].Center);
        foreach (int door in route)
        {
            int before = Get<int>(game, "closedCount");
            game.StartCoroutine((IEnumerator)Invoke("CrossDoor", door));
            while (Get<bool>(game, "busy")) yield return null;
            Require(Get<int>(game, "closedCount") == before + 1, "Door closes permanently");
        }
    }

    private static void CheckSelection(int count)
    {
        var view = Get<PuzzleSelectionView>(Get<PuzzleUI>(game, "ui"), "puzzleSelection");
        List<Button> buttons = Get<List<Button>>(view, "puzzleButtons");
        Require(buttons.Count == LevelCatalog.PuzzleCount, "Selection displays all existing puzzles");
        for (int i = 0; i < buttons.Count; i++)
            Require(buttons[i].interactable == (i < count), "Puzzle " + (i + 1) + " lock state");
    }

    private static T Find<T>(string name) where T : Component
    {
        Canvas canvas = Get<Canvas>(Get<PuzzleUI>(game, "ui"), "uiCanvas");
        foreach (T component in canvas.GetComponentsInChildren<T>(true))
            if (component.name == name && component.gameObject.activeInHierarchy) return component;
        throw new InvalidOperationException("Active UI control not found: " + name);
    }

    private static void Click(string name)
    {
        Button button = Find<Button>(name);
        Require(button.IsInteractable(), "Button is usable: " + name);
        button.onClick.Invoke();
    }

    private static PuzzleGameState State => Get<PuzzleGameState>(game, "state");
    private static int Index => LevelCatalog.ToPuzzleIndex(Get<int>(game, "currentLevelIndex"), Get<int>(game, "currentPuzzleIndex"));
    private static T Get<T>(object target, string name) => (T)target.GetType().GetField(name, Private).GetValue(target);
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);
    private static object Invoke(string name, params object[] args) => typeof(DoorPuzzleGame).GetMethod(name, Private).Invoke(game, args);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Record("PASS: " + message);
    }

    private static void Record(string message)
    {
        Debug.Log("[ProgressChecks] " + message);
        File.AppendAllText(ReportPath, message + Environment.NewLine);
    }

    private static void Fail(Exception error)
    {
        EditorApplication.update -= Step;
        SessionState.SetInt(PhaseKey, 0);
        PlayerPrefs.DeleteKey(TestKey);
        PlayerPrefs.Save();
        Debug.LogException(error);
        File.AppendAllText(ReportPath, "FAIL: " + error + Environment.NewLine);
        EditorApplication.Exit(1);
    }
}
