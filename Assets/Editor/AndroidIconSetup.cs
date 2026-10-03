#if UNITY_ANDROID
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

[InitializeOnLoad]
public sealed class AndroidIconSetup : IPreprocessBuildWithReport
{
    private const string ArtworkPath = "Assets/Icons/DoorPuzzleIcon.png";
    private const string TransparentPath = "Assets/Icons/TransparentForeground.png";

    static AndroidIconSetup()
    {
        EditorApplication.delayCall += Apply;
    }

    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform == BuildTarget.Android)
            Apply();
    }

    private static void Apply()
    {
        Texture2D artwork = AssetDatabase.LoadAssetAtPath<Texture2D>(ArtworkPath);
        Texture2D transparent = AssetDatabase.LoadAssetAtPath<Texture2D>(TransparentPath);
        if (artwork == null || transparent == null)
        {
            Debug.LogError("Door Puzzle Android icon textures are missing.");
            return;
        }

        NamedBuildTarget target = NamedBuildTarget.Android;
        SetAdaptiveIcons(target, artwork, transparent);
    }

    private static void SetAdaptiveIcons(NamedBuildTarget target,
        Texture2D artwork, Texture2D transparent)
    {
        // Android 8+ adaptive icons only. The launcher applies its own shape.
        PlatformIconKind kind = AndroidPlatformIconKind.Adaptive;
        PlatformIcon[] slots = PlayerSettings.GetPlatformIcons(target, kind);
        foreach (PlatformIcon slot in slots)
            slot.SetTextures(artwork, transparent);
        PlayerSettings.SetPlatformIcons(target, kind, slots);
    }
}
#endif
