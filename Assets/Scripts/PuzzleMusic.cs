using UnityEngine;

public sealed class PuzzleMusic : MonoBehaviour
{
    private AudioSource source;
    private AudioClip menu;
    private AudioClip level1;
    private AudioClip level2;

    private void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.volume = 0.45f;
    }

    public void Initialize(AudioClip menuClip, AudioClip level1Clip, AudioClip level2Clip)
    {
        menu = menuClip;
        level1 = level1Clip;
        level2 = level2Clip;
        if (menu == null || level1 == null || level2 == null)
            Debug.LogError("Background music references are missing on DoorPuzzleGame in the main scene.");
    }

    public void PlayForState(PuzzleGameState state, int levelIndex)
    {
        AudioClip clip = state == PuzzleGameState.MainMenu || state == PuzzleGameState.PuzzleSelection
            ? menu : levelIndex == 0 ? level1 : level2;
        // Keep the track's position across puzzles, retries and result overlays.
        if (source.clip == clip) return;
        source.Stop();
        source.clip = clip;
        if (clip != null) source.Play();
    }

    public void SetMusicEnabled(bool enabled)
    {
        // Muting preserves playback position and the selected level's track.
        source.mute = !enabled;
    }

}
