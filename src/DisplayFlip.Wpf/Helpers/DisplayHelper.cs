using System.Text.RegularExpressions;

namespace DisplayFlip.UI.Helpers;

/// <summary>
/// Display numbering and friendly label helper matching DisplayFlip conventions.
/// </summary>
public static class DisplayHelper
{
    private static readonly Regex DisplayPattern = new(
        @"(?:DISPLAY|Layar|Monitor|Screen)\s*(\d+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex StandaloneNumberPattern = new(
        @"\b([1-9]|[12]\d|3[0-2])\b",
        RegexOptions.Compiled);

    public static string ExtractDisplayNumber(string? stableKey, string? friendlyName, int fallbackIndex)
    {
        string numFromStable = ExtractFromText(stableKey);
        if (!string.IsNullOrEmpty(numFromStable))
            return numFromStable;

        string numFromFriendly = ExtractFromText(friendlyName);
        if (!string.IsNullOrEmpty(numFromFriendly))
            return numFromFriendly;

        return fallbackIndex.ToString();
    }

    public static string ExtractDisplayNumber(string? friendlyName, int fallbackIndex)
    {
        return ExtractDisplayNumber(null, friendlyName, fallbackIndex);
    }

    private static string ExtractFromText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var mDisp = DisplayPattern.Match(text);
        if (mDisp.Success && int.TryParse(mDisp.Groups[1].Value, out int dVal) && dVal > 0 && dVal <= 32)
            return dVal.ToString();

        var mWord = StandaloneNumberPattern.Match(text);
        if (mWord.Success)
            return mWord.Groups[1].Value;

        return string.Empty;
    }

    public static string CleanDeviceName(string? friendlyName)
    {
        if (string.IsNullOrWhiteSpace(friendlyName))
            return "Monitor";

        string name = friendlyName.Trim();
        if (name.StartsWith(@"\\.\", StringComparison.Ordinal))
            name = name[4..];

        return name;
    }

    public static string GetShortDisplayName(string? friendlyName, string? stableKey, int index)
    {
        string num = ExtractDisplayNumber(stableKey, friendlyName, index);
        string clean = CleanDeviceName(friendlyName);
        return $"Layar {num} ({clean})";
    }
}
