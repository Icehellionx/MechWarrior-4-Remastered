namespace MW4Remastered.Core.Media;

public enum MediaRecognitionStatus
{
    Recognized,
    RecognizedWithExcludedContent,
    Unknown,
    Ambiguous,
    UnsafeInventory
}

public sealed record MediaRecognitionResult(
    MediaRecognitionStatus Status,
    MediaLayoutDefinition? Layout,
    IReadOnlyList<string> ExcludedPaths,
    string Message);

public sealed class MediaRecognizer
{
    public MediaRecognitionResult Recognize(IEnumerable<string> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        var normalized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in paths)
        {
            if (!TryNormalizeRelativePath(path, out var safePath))
            {
                return new MediaRecognitionResult(
                    MediaRecognitionStatus.UnsafeInventory,
                    null,
                    Array.Empty<string>(),
                    $"Media inventory contains an unsafe path: {path}");
            }

            if (safePath.Length > 0) normalized.Add(safePath);
        }

        var matches = MediaCatalog.Layouts
            .Where(layout => layout.RequiredPaths.All(normalized.Contains))
            .ToArray();

        if (matches.Length == 0)
        {
            var nearest = MediaCatalog.Layouts
                .Select(layout => new
                {
                    Layout = layout,
                    Present = layout.RequiredPaths.Count(normalized.Contains),
                    Missing = layout.RequiredPaths.Where(path => !normalized.Contains(path)).ToArray(),
                })
                .Where(item => item.Present >= 2 && item.Missing.Length <= 2)
                .OrderByDescending(item => item.Present)
                .ThenBy(item => item.Missing.Length)
                .ToArray();
            var unknownMessage = nearest.Length > 0 &&
                (nearest.Length == 1 || nearest[0].Present > nearest[1].Present || nearest[0].Missing.Length < nearest[1].Missing.Length)
                ? $"Media does not match a supported layout. Closest known layout: {nearest[0].Layout.DisplayName}; missing: {string.Join(", ", nearest[0].Missing)}."
                : "Media does not match a supported layout.";
            return new MediaRecognitionResult(MediaRecognitionStatus.Unknown, null, Array.Empty<string>(), unknownMessage);
        }

        if (matches.Length > 1)
        {
            return new MediaRecognitionResult(MediaRecognitionStatus.Ambiguous, null, Array.Empty<string>(), "Media matches more than one supported layout.");
        }

        var excluded = normalized.Where(IsForbidden).Order(StringComparer.OrdinalIgnoreCase).ToArray();
        var status = excluded.Length == 0 ? MediaRecognitionStatus.Recognized : MediaRecognitionStatus.RecognizedWithExcludedContent;
        var message = excluded.Length == 0
            ? $"Recognized {matches[0].DisplayName}."
            : $"Recognized {matches[0].DisplayName}; {excluded.Length} prohibited path(s) must be excluded from extraction.";
        return new MediaRecognitionResult(status, matches[0], excluded, message);
    }

    internal static bool TryNormalizeRelativePath(string? value, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(value)) return true;

        var candidate = value.Replace('\\', '/').Trim();
        while (candidate.StartsWith("./", StringComparison.Ordinal)) candidate = candidate[2..];
        candidate = candidate.TrimEnd('/');
        if (candidate.Length == 0) return true;
        if (candidate.StartsWith("/", StringComparison.Ordinal) || candidate.Contains(':', StringComparison.Ordinal)) return false;

        var segments = candidate.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0 || segments.Any(segment => segment is "." or "..")) return false;
        if (segments.Any(segment => segment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)) return false;

        normalized = string.Join('/', segments);
        return true;
    }

    private static bool IsForbidden(string path)
    {
        var segments = path.Split('/');
        return segments.Any(MediaCatalog.ForbiddenRootNames.Contains)
            || MediaCatalog.ForbiddenFileNames.Contains(segments[^1]);
    }
}
