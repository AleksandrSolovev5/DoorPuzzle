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

    public Color Background => IsDusk ? Rgb(69, 73, 80) : PuzzleVisualStyle.Background;
    public Color RoomSurface => IsDusk ? Rgb(91, 103, 118) : PuzzleVisualStyle.Surface;
    public Color Wall => IsDusk ? Rgb(39, 41, 46) : PuzzleVisualStyle.Wall;
    public Color HeaderText => IsDusk ? Rgb(243, 238, 230) : PuzzleVisualStyle.Text;
    public Color MutedText => IsDusk ? Rgb(201, 202, 207) : PuzzleVisualStyle.MutedText;
    public Color HeaderRule => IsDusk ? Rgb(112, 115, 124) : PuzzleVisualStyle.Border;
    public Color HintText => HeaderText;
    public Color SelectionMarker => IsDusk ? Rgb(155, 163, 180) :
        Color.Lerp(RoomSurface, PuzzleVisualStyle.MutedText, 0.24f);
    public Color Shadow => IsDusk ? new Color(0.06f, 0.07f, 0.09f, 0.12f) : PuzzleVisualStyle.Shadow;

    public Color RoomColor(Color puzzleTint)
    {
        return Color.Lerp(RoomSurface, puzzleTint, 0.025f);
    }

    private static Color Rgb(byte r, byte g, byte b) => new Color32(r, g, b, 255);
}
