using UnityEngine;

// Environment and HUD ink vary by level. Buttons, doors, EXIT and pawn retain
// the shared PuzzleVisualStyle palette in both atmospheres.
public sealed class PuzzlePalette
{
    private static readonly PuzzlePalette Light = new PuzzlePalette(false);
    private static readonly PuzzlePalette Dusk = new PuzzlePalette(true);
    public readonly bool IsDusk;

    private PuzzlePalette(bool dusk) { IsDusk = dusk; }

    public static PuzzlePalette For(PuzzleAtmosphere atmosphere)
    {
        return atmosphere == PuzzleAtmosphere.Dusk ? Dusk : Light;
    }

    public Color Background => IsDusk ? Rgb(70, 86, 107) : Rgb(238, 243, 247);
    public Color RoomSurface => IsDusk ? Rgb(116, 132, 153) : Rgb(247, 250, 252);
    public Color Wall => IsDusk ? Rgb(51, 62, 77) : PuzzleVisualStyle.Wall;
    public Color HeaderText => IsDusk ? Rgb(237, 242, 249) : PuzzleVisualStyle.Text;
    public Color MutedText => IsDusk ? Rgb(195, 206, 221) : PuzzleVisualStyle.MutedText;
    public Color HeaderRule => IsDusk ? Rgb(111, 128, 151) : PuzzleVisualStyle.Border;
    public Color HudSurface => IsDusk ? Rgb(83, 100, 121) : PuzzleVisualStyle.Surface;
    public Color HintSurface => IsDusk ? Rgb(82, 98, 120) : PuzzleVisualStyle.PrimarySoft;
    public Color HintText => IsDusk ? Rgb(224, 231, 244) : PuzzleVisualStyle.Primary;
    public Color SelectionMarker => IsDusk ? Rgb(175, 187, 209) :
        Color.Lerp(RoomSurface, PuzzleVisualStyle.Primary, 0.34f);
    public Color Shadow => IsDusk ? new Color(0.08f, 0.12f, 0.19f, 0.22f) : PuzzleVisualStyle.Shadow;

    public Color RoomColor(Color puzzleTint)
    {
        return Color.Lerp(RoomSurface, puzzleTint, IsDusk ? 0.055f : 0.10f);
    }

    private static Color Rgb(byte r, byte g, byte b) => new Color32(r, g, b, 255);
}
