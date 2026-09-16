using MW4Remastered.Core.Media;

internal static class MediaSelectionSetSmoke
{
    public static void Run(List<string> failures)
    {
        var selection = new MediaSelectionSet();
        var first = selection.Add("vengeance-one.iso", Inspection("vengeance-disc-1", excludedPaths: 2));
        Check(!Capability(first, "vengeance").IsComplete, "one Vengeance disc does not claim install readiness", failures);
        Check(first.ExcludedContentCount == 2, "recognized prohibited disc content is counted", failures);

        var complete = selection.Add("vengeance-two.iso", Inspection("vengeance-disc-2"));
        Check(Capability(complete, "vengeance").IsComplete, "both Vengeance discs complete media readiness", failures);
        Check(!Capability(complete, "mercenaries").IsComplete, "unselected games remain incomplete", failures);

        var archive = new MediaSourceInspection(
            MediaSourceKind.Zip,
            new[]
            {
                Item("mercenaries-disc-1", "Disc 1.iso"),
                Item("mercenaries-disc-2", "Disc 2.iso"),
                Item("clan-mech-pak", "Clan.iso"),
            },
            new[] { "Serial.txt", "Crack/readme.txt" });
        var withArchive = selection.Add("collection.zip", archive);
        Check(Capability(withArchive, "mercenaries").IsComplete, "one ZIP can satisfy both Mercenaries discs", failures);
        Check(Capability(withArchive, "clan").IsComplete, "optional pack presence is tracked independently", failures);
        Check(withArchive.ExcludedContentCount == 4, "archive and recognized-disc exclusions are both counted", failures);

        var beforeFailure = selection.Current;
        var mixed = new MediaSourceInspection(
            MediaSourceKind.Zip,
            new[]
            {
                Item("inner-sphere-mech-pak", "Inner Sphere.iso"),
                new InspectedMediaItem("Unknown.iso", new MediaRecognitionResult(
                    MediaRecognitionStatus.Unknown, null, Array.Empty<string>(), "Unknown test media.")),
            },
            Array.Empty<string>());
        var rejected = false;
        try
        {
            selection.Add("mixed.zip", mixed);
        }
        catch (InvalidDataException)
        {
            rejected = true;
        }
        Check(rejected, "a partially unknown source is rejected", failures);
        Check(selection.Current.SourceCount == beforeFailure.SourceCount
            && !Capability(selection.Current, "inner-sphere").IsComplete,
            "failed source intake is atomic", failures);

        var replacement = selection.Add("replacement.iso", Inspection("vengeance-disc-1"));
        Check(replacement.Layouts.Single(item => item.Layout.Id == "vengeance-disc-1").SourcePath.EndsWith("replacement.iso", StringComparison.OrdinalIgnoreCase),
            "the latest valid source replaces earlier evidence for the same layout", failures);
    }

    private static MediaSourceInspection Inspection(string layoutId, int excludedPaths = 0) =>
        new(MediaSourceKind.Iso, new[] { Item(layoutId, null, excludedPaths) }, Array.Empty<string>());

    private static InspectedMediaItem Item(string layoutId, string? archivePath, int excludedPaths = 0)
    {
        var layout = MediaCatalog.Layouts.Single(item => item.Id == layoutId);
        var excluded = Enumerable.Range(1, excludedPaths).Select(index => $"excluded-{index}.bin").ToArray();
        var status = excluded.Length == 0 ? MediaRecognitionStatus.Recognized : MediaRecognitionStatus.RecognizedWithExcludedContent;
        return new InspectedMediaItem(archivePath, new MediaRecognitionResult(status, layout, excluded, "Synthetic recognition."));
    }

    private static MediaCapabilityStatus Capability(MediaSelectionSnapshot snapshot, string productId) =>
        snapshot.Capabilities.Single(item => item.ProductId == productId);

    private static void Check(bool condition, string message, List<string> failures)
    {
        if (!condition) failures.Add("FAIL: " + message);
    }
}
