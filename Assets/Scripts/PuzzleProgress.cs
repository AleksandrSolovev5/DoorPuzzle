using System;

// The completed prefix is also the index of the next puzzle to unlock.
// Persistence belongs to the existing game controller, not another manager.
public sealed class PuzzleProgress
{
    public const string SaveKey = "DoorPuzzle.CompletedPuzzleCount.v1";
    private readonly Action<int> save;
    public int TotalCount { get; }
    public int CompletedCount { get; private set; }
    public int PlayIndex => CompletedCount < TotalCount ? CompletedCount : 0;

    public PuzzleProgress(int totalCount, int savedCount, Action<int> persist)
    {
        if (totalCount <= 0) throw new ArgumentOutOfRangeException(nameof(totalCount));
        TotalCount = totalCount;
        CompletedCount = Math.Max(0, Math.Min(savedCount, totalCount));
        save = persist ?? throw new ArgumentNullException(nameof(persist));
    }

    public bool IsUnlocked(int index) => index >= 0 && index < TotalCount && index <= CompletedCount;
    public bool IsCompleted(int index) => index >= 0 && index < CompletedCount;

    public bool Complete(int index)
    {
        // Replaying an old puzzle does not change the frontier. A locked puzzle
        // cannot skip intervening puzzles, even if called outside the UI.
        if (!IsUnlocked(index) || index != CompletedCount) return false;
        int next = CompletedCount + 1;
        save(next);
        CompletedCount = next;
        return true;
    }
}
