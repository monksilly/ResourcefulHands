using System;
using System.Text.RegularExpressions;

namespace ResourcefulHands;

public struct WKVersion : IComparable<WKVersion>
{
    public int Major;
    public int Minor;
    public string Suffix;

    public WKVersion(string version)
    {
        // Regex match version, eg. "0.55m"
        var match = Regex.Match(version, @"v?(\d+)\.(\d+)([a-z]?)");
        if (match.Success)
        {
            Major = int.Parse(match.Groups[1].Value);
            Minor = int.Parse(match.Groups[2].Value);
            // Default to "a" if not version "0.55" is the same as "0.55a"
            Suffix = string.IsNullOrEmpty(match.Groups[3].Value) ? "a" : match.Groups[3].Value;
        }
        else
        {
            Major = Minor = 0;
            Suffix = "a";
        }
    }

    public int CompareTo(WKVersion other)
    {
        if (Major != other.Major) return Major.CompareTo(other.Major);
        if (Minor != other.Minor) return Minor.CompareTo(other.Minor);
        return String.Compare(Suffix, other.Suffix, StringComparison.Ordinal);
    }
    
    public static bool operator >=(WKVersion a, WKVersion b) => a.CompareTo(b) >= 0;
    public static bool operator <=(WKVersion a, WKVersion b) => a.CompareTo(b) <= 0;
    public static bool operator >(WKVersion a, WKVersion b) => a.CompareTo(b) > 0;
    public static bool operator <(WKVersion a, WKVersion b) => a.CompareTo(b) < 0;
}