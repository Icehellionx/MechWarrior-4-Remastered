using System.Security.Cryptography;
using System.Text;
using MW4Remastered.Core.Launch;

namespace MW4Remastered.Core.Install;

public enum DisplayResampling
{
    Lanczos3,
    Bilinear,
    PointSampled,
}

public enum TextureFiltering { Off, Two, Four, Eight, Sixteen }
public enum DisplayAntialiasing { Off, Two, Four, Eight }
public sealed record GraphicsChoices(DisplayResampling Resampling, TextureFiltering Filtering,
    DisplayAntialiasing Antialiasing)
{
    public static GraphicsChoices Default { get; } = new(DisplayResampling.Lanczos3,
        TextureFiltering.Sixteen, DisplayAntialiasing.Four);
}

// Changes only the final display resampling of the pinned MW4 profile.
// Every installed profile remains manifest-owned and exactly verifiable.
public sealed class GraphicsSettingsService
{
    private const string DefaultLine = "Resampling                           = lanczos-3";
    private const string FilteringLine = "Filtering                           = 16";
    private const string AntialiasingLine = "Antialiasing                        = 4x";
    private readonly string profileSource;
    private readonly OwnedInstallFileReplacementTransaction replacement;
    private readonly InstallManifestVerifier verifier;
    private readonly IGameProcessState processState;

    public GraphicsSettingsService(string applicationRoot)
        : this(applicationRoot, new OwnedInstallFileReplacementTransaction(),
            new InstallManifestVerifier(), new SystemGameProcessState()) { }

    public GraphicsSettingsService(
        string applicationRoot,
        OwnedInstallFileReplacementTransaction replacement,
        InstallManifestVerifier verifier,
        IGameProcessState processState)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationRoot);
        profileSource = Path.Combine(Path.GetFullPath(applicationRoot), "Compatibility", "dgVoodoo2", "dgVoodoo.conf");
        this.replacement = replacement ?? throw new ArgumentNullException(nameof(replacement));
        this.verifier = verifier ?? throw new ArgumentNullException(nameof(verifier));
        this.processState = processState ?? throw new ArgumentNullException(nameof(processState));
    }

    public DisplayResampling? Read(IReadOnlyList<ProductStatus> statuses) => ReadChoices(statuses)?.Resampling;

    public GraphicsChoices? ReadChoices(IReadOnlyList<ProductStatus> statuses)
    {
        var original = DefaultProfile();
        var found = new HashSet<GraphicsChoices>();
        foreach (var target in Targets(statuses))
        {
            foreach (var relative in target.Profiles)
            {
                var path = StagedInstallTransaction.ResolveContainedPath(target.Root, relative);
                StagedInstallTransaction.RejectContainedFilePath(target.Root, path);
                var bytes = File.ReadAllBytes(path);
                found.Add(ParseProfile(original, bytes));
            }
        }
        return found.Count == 1 ? found.Single() : null;
    }

    public void Apply(IReadOnlyList<ProductStatus> statuses, DisplayResampling selected)
    {
        if (!Enum.IsDefined(selected)) throw new ArgumentOutOfRangeException(nameof(selected));
        ApplyChoices(statuses, (ReadChoices(statuses) ?? GraphicsChoices.Default) with { Resampling = selected });
    }

    public void ApplyChoices(IReadOnlyList<ProductStatus> statuses, GraphicsChoices selected)
    {
        ArgumentNullException.ThrowIfNull(selected);
        if (!Enum.IsDefined(selected.Resampling) || !Enum.IsDefined(selected.Filtering) ||
            !Enum.IsDefined(selected.Antialiasing)) throw new ArgumentOutOfRangeException(nameof(selected));
        var targets = Targets(statuses);
        foreach (var target in targets) VerifyTarget(target);
        _ = ReadChoices(statuses);
        foreach (var status in statuses.Where(item => item.Product.Kind == ProductKind.Game && item.State == ProductInstallState.Ready))
        {
            if (status.LaunchPath is not null && processState.IsRunning(status.LaunchPath))
                throw new InvalidOperationException("Close all MechWarrior 4 games before changing display settings.");
        }

        var selectedBytes = MakeProfile(DefaultProfile(), selected);
        var scratch = Directory.CreateTempSubdirectory("mw4-graphics-").FullName;
        var completed = new List<Target>();
        var preserveRecovery = false;
        try
        {
            var selectionRoot = Path.Combine(scratch, "graphics-settings-profile");
            Directory.CreateDirectory(selectionRoot);
            var proposed = Path.Combine(selectionRoot, "dgVoodoo.conf");
            File.WriteAllBytes(proposed, selectedBytes);
            foreach (var target in targets)
            {
                var oldRoot = Path.Combine(scratch, $"old-{completed.Count}");
                Directory.CreateDirectory(oldRoot);
                foreach (var relative in target.Profiles)
                {
                    var old = StagedInstallTransaction.ResolveContainedPath(oldRoot, relative);
                    Directory.CreateDirectory(Path.GetDirectoryName(old)!);
                    File.Copy(StagedInstallTransaction.ResolveContainedPath(target.Root, relative), old);
                }
                replacement.Execute(target.ProductId, target.Root,
                    target.Profiles.Select(relative => new InstallFile(selectionRoot, "dgVoodoo.conf", relative)).ToArray());
                completed.Add(target with { BackupRoot = oldRoot });
            }
        }
        catch (Exception changeError)
        {
            try
            {
                foreach (var target in completed.AsEnumerable().Reverse())
                    replacement.Execute(target.ProductId, target.Root,
                        target.Profiles.Select(relative => new InstallFile(target.BackupRoot!, relative, relative)).ToArray());
            }
            catch (Exception rollbackError)
            {
                preserveRecovery = true;
                throw new AggregateException($"Display settings failed and prior profiles could not be restored. Recovery copies are in: {scratch}", changeError, rollbackError);
            }
            throw;
        }
        finally
        {
            if (!preserveRecovery) Directory.Delete(scratch, recursive: true);
        }
    }

    private byte[] DefaultProfile()
    {
        if (!File.Exists(profileSource)) throw new FileNotFoundException("Packaged dgVoodoo profile is missing.", profileSource);
        var original = File.ReadAllBytes(profileSource);
        var hash = Convert.ToHexString(SHA256.HashData(original));
        if (!hash.Equals(LegacyPresentationCompatibility.ConfigSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Packaged dgVoodoo profile does not match the qualified default.");
        return original;
    }

    private static byte[] MakeProfile(byte[] original, GraphicsChoices choices)
    {
        var text = Encoding.UTF8.GetString(original);
        text = ReplaceUnique(text, DefaultLine, choices.Resampling switch
        {
            DisplayResampling.Lanczos3 => "lanczos-3",
            DisplayResampling.Bilinear => "bilinear",
            _ => "pointsampled",
        });
        text = ReplaceUnique(text, FilteringLine, choices.Filtering switch
        {
            TextureFiltering.Off => "1", TextureFiltering.Two => "2", TextureFiltering.Four => "4",
            TextureFiltering.Eight => "8", _ => "16",
        });
        text = ReplaceUnique(text, AntialiasingLine, choices.Antialiasing switch
        {
            DisplayAntialiasing.Off => "off", DisplayAntialiasing.Two => "2x",
            DisplayAntialiasing.Four => "4x", _ => "8x",
        });
        return Encoding.UTF8.GetBytes(text);
    }

    private static string ReplaceUnique(string text, string line, string value)
    {
        if (text.Split(line, StringSplitOptions.None).Length != 2)
            throw new InvalidDataException("Qualified dgVoodoo profile has a missing or duplicate setting.");
        return text.Replace(line, line[..(line.IndexOf('=') + 1)] + " " + value, StringComparison.Ordinal);
    }

    private static GraphicsChoices ParseProfile(byte[] original, byte[] bytes)
    {
        foreach (var resampling in Enum.GetValues<DisplayResampling>())
        foreach (var filtering in Enum.GetValues<TextureFiltering>())
        foreach (var antialiasing in Enum.GetValues<DisplayAntialiasing>())
        {
            var choices = new GraphicsChoices(resampling, filtering, antialiasing);
            if (bytes.AsSpan().SequenceEqual(MakeProfile(original, choices))) return choices;
        }
        throw new InvalidDataException("Installed dgVoodoo profile is outside the supported settings choices.");
    }

    private static IReadOnlyList<Target> Targets(IReadOnlyList<ProductStatus> statuses)
    {
        ArgumentNullException.ThrowIfNull(statuses);
        var games = statuses.Where(item => item.Product.Kind == ProductKind.Game).ToArray();
        if (games.Any(item => item.State == ProductInstallState.NeedsRepair))
            throw new InvalidOperationException("Repair the affected game installation before changing global display settings.");
        var ready = games.Where(item => item.State == ProductInstallState.Ready).ToArray();
        if (ready.Length == 0) throw new InvalidOperationException("Install a game before changing display settings.");
        return ready.GroupBy(item => Path.GetFullPath(item.InstallPath!), StringComparer.OrdinalIgnoreCase)
            .Select(group => new Target(
                group.First().Product.Id == "mercenaries" ? "mercenaries" : "vengeance",
                group.Key,
                group.Select(item => item.Product.Id == "black-knight" ? "MW4X/dgVoodoo.conf" : "dgVoodoo.conf")
                    .Distinct(StringComparer.OrdinalIgnoreCase).ToArray(), null))
            .OrderBy(item => item.Root, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private void VerifyTarget(Target target)
    {
        var verification = verifier.Verify(target.Root, InstallVerificationScope.OwnedFiles);
        if (!verification.IsValid || verification.Manifest is null ||
            !verification.Manifest.ProductId.Equals(target.ProductId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Display settings require a verified owned game tree.");
        var owned = verification.Manifest.Files.Select(item => item.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (target.Profiles.Any(relative => !owned.Contains(relative)))
            throw new InvalidDataException("The game installation does not own its dgVoodoo profile.");
    }

    private sealed record Target(string ProductId, string Root, IReadOnlyList<string> Profiles, string? BackupRoot);
}
