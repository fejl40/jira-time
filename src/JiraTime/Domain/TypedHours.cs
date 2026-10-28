using System.Globalization;

namespace JiraTime.Domain;

/// <summary>Hours as the user types them.</summary>
public static class TypedHours
{
    /// <summary>
    /// Reads hours typed by the user: decimals with a dot or comma (1.5, 1,5) or hours and minutes (1:30).
    /// Rounded to whole minutes. Null when it can't be read or is not positive.
    /// </summary>
    public static TimeSpan? Parse(string? text)
    {
        var s = text?.Trim().TrimEnd('h', 'H').Trim() ?? "";
        TimeSpan value;
        if (s.Split(':') is [var h, var m])
        {
            if (!int.TryParse(h, NumberStyles.None, CultureInfo.InvariantCulture, out var hours)
                || !int.TryParse(m, NumberStyles.None, CultureInfo.InvariantCulture, out var minutes) || minutes >= 60) return null;
            value = new TimeSpan(hours, minutes, 0);
        }
        else if (decimal.TryParse(s.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var d))
            value = TimeSpan.FromMinutes((double)Math.Round(d * 60));
        else return null;
        return value > TimeSpan.Zero ? value : null;
    }
}
