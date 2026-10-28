namespace Flowstate.Core.Updates;

public sealed record SemanticVersion(int Major, int Minor, int Patch, string? PreRelease) : IComparable<SemanticVersion>
{
    public bool IsPreRelease => !string.IsNullOrEmpty(PreRelease);

    public static bool TryParse(string? text, out SemanticVersion? version)
    {
        version = null;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var s = text.Trim();
        if (s.StartsWith('v') || s.StartsWith('V')) s = s[1..];
        var plus = s.IndexOf('+');
        if (plus >= 0) s = s[..plus];
        string? pre = null;
        var dash = s.IndexOf('-');
        if (dash >= 0) { pre = s[(dash + 1)..]; s = s[..dash]; if (pre.Length == 0) return false; }
        var parts = s.Split('.');
        if (parts.Length != 3) return false;
        if (!int.TryParse(parts[0], out var ma) || !int.TryParse(parts[1], out var mi) || !int.TryParse(parts[2], out var pa)) return false;
        if (ma < 0 || mi < 0 || pa < 0) return false;
        version = new SemanticVersion(ma, mi, pa, pre);
        return true;
    }

    public int CompareTo(SemanticVersion? other)
    {
        if (other is null) return 1;
        var c = Major.CompareTo(other.Major); if (c != 0) return c;
        c = Minor.CompareTo(other.Minor); if (c != 0) return c;
        c = Patch.CompareTo(other.Patch); if (c != 0) return c;
        if (!IsPreRelease && !other.IsPreRelease) return 0;
        if (!IsPreRelease) return 1;
        if (!other.IsPreRelease) return -1;
        var a = PreRelease!.Split('.');
        var b = other.PreRelease!.Split('.');
        for (int i = 0; i < Math.Min(a.Length, b.Length); i++)
        {
            var an = int.TryParse(a[i], out var ai);
            var bn = int.TryParse(b[i], out var bi);
            if (an && bn) c = ai.CompareTo(bi);
            else if (an) c = -1;
            else if (bn) c = 1;
            else c = string.CompareOrdinal(a[i], b[i]);
            if (c != 0) return c;
        }
        return a.Length.CompareTo(b.Length);
    }

    public static bool IsUpdateAvailable(string current, string latest, bool includePreReleases)
    {
        if (!TryParse(current, out var cur) || !TryParse(latest, out var lat)) return false;
        if (lat!.IsPreRelease && !includePreReleases) return false;
        return lat.CompareTo(cur) > 0;
    }

    public override string ToString() => IsPreRelease ? $"{Major}.{Minor}.{Patch}-{PreRelease}" : $"{Major}.{Minor}.{Patch}";
}
