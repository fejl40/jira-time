namespace JiraTime.Components;

/// <summary>
/// One tile of a number on a flip board: the character it shows now, the one it showed before (a space when it
/// was empty) and its place, counted from the decimal point so digits keep their tile when the number grows.
/// </summary>
public record FlipTile(char Now, char Before, int Place)
{
    /// <summary>The place of the decimal point; whole digits are 0 (ones), 1 (tens) and up, decimals -2 and down.</summary>
    public const int PointPlace = -1;

    /// <summary>The tile turns over: its character changed.</summary>
    public bool Flips => Now != Before;

    public bool IsPoint => Place == PointPlace;
}

/// <summary>
/// Which tiles a number shows on a flip board and which of them turn over when it changes, like a station
/// departure board: numbers are lined up on the decimal point, so 7.5 → 15.5 only turns the ones and adds a tens.
/// </summary>
public static class FlipBoard
{
    /// <summary>The tiles of <paramref name="now"/> (as <see cref="Format.Hours"/> writes it), left to right, each
    /// with what the same place showed in <paramref name="before"/>; null before means nothing turns.</summary>
    public static IReadOnlyList<FlipTile> Between(string? before, string now)
    {
        var (wholeNow, decimalsNow) = Split(now);
        var (wholeBefore, decimalsBefore) = Split(before ?? now);
        var tiles = new List<FlipTile>();
        for (var i = 0; i < wholeNow.Length; i++)
        {
            var place = wholeNow.Length - 1 - i;
            tiles.Add(new FlipTile(wholeNow[i], At(wholeBefore, wholeBefore.Length - 1 - place), place));
        }
        if (decimalsNow is null) return tiles;
        tiles.Add(new FlipTile('.', decimalsBefore is null ? ' ' : '.', FlipTile.PointPlace));
        for (var i = 0; i < decimalsNow.Length; i++)
            tiles.Add(new FlipTile(decimalsNow[i], At(decimalsBefore ?? "", i), FlipTile.PointPlace - 1 - i));
        return tiles;
    }

    private static (string Whole, string? Decimals) Split(string number)
    {
        var point = number.IndexOf('.');
        return point < 0 ? (number, null) : (number[..point], number[(point + 1)..]);
    }

    private static char At(string text, int index) => index >= 0 && index < text.Length ? text[index] : ' ';
}
