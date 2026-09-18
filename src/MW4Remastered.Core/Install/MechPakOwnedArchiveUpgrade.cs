using System.Security.Cryptography;

namespace MW4Remastered.Core.Install;

public sealed class MechPakOwnedArchiveUpgrade
{
    internal sealed record QualifiedUpgrade(
        string ProductId,
        string RelativePath,
        int PackMask,
        string PreviousSha256,
        string CurrentSha256);

    private static readonly IReadOnlyList<QualifiedUpgrade> DefaultUpgrades =
    [
        new("vengeance", "RESOURCE/core.mw4", 1,
            "5081414fe1c7e06ccdcfd5f4b1739421778afd9bd08d487daf14915e024060b6",
            "ba29da75cbeb54a54b75f2930851ee0f8461a32fa868b3b2c4b743daf5173582"),
        new("vengeance", "RESOURCE/core.mw4", 2,
            "af51b9c8bad31454892e43618a030e6cd5139e5056da738852f4722ba6f32969",
            "f6dcb5806e93b76222be06e94eba55f0c8b442c23ba7b745db359e3ca0596dbf"),
        new("vengeance", "RESOURCE/core.mw4", 3,
            "6033d799bb8fc1cbc6339f2d7a7ba784657dc0c141f0af88be017f21ab8e2f57",
            "3a1e7015c384e18b377012f71a6ea3e95f08fa8e6d021d888c0cc7334ec03353"),
        new("vengeance", "RESOURCE/corex.mw4", 1,
            "50c56e450d238fbd5b64f819e71e4b05f855590e58c966d03ae223c5b78ef83a",
            "b4014f5fada1f56c597f7905433793b51c7b25f8516a3fb7dca282635f4044fe"),
        new("vengeance", "RESOURCE/corex.mw4", 2,
            "f8a0c5677d9cc7b4ca74fa1893de5e597114d309877a85cb688adc72a99a44f9",
            "8d5c7c6bdd4f32e9db21f5902b1774c9b72c42cb3dd739106163c93028b88545"),
        new("vengeance", "RESOURCE/corex.mw4", 3,
            "019f579955c84fddc12a471e942e7de61ba6076f3a7ca2b47d8ef060759dad60",
            "a6938d776ef2f1bee0fcca229e8439897fcc045cd36d5de2c8eff5be6e77d9e7"),
    ];

    private readonly IReadOnlyList<QualifiedUpgrade> qualifiedUpgrades;

    public MechPakOwnedArchiveUpgrade()
        : this(DefaultUpgrades)
    {
    }

    internal MechPakOwnedArchiveUpgrade(IReadOnlyList<QualifiedUpgrade> qualifiedUpgrades)
    {
        this.qualifiedUpgrades = qualifiedUpgrades ?? throw new ArgumentNullException(nameof(qualifiedUpgrades));
    }

    public IReadOnlyList<InstallFile> CreateReplacements(
        string installRoot,
        InstallManifest manifest,
        string scratchDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(installRoot);
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentException.ThrowIfNullOrWhiteSpace(scratchDirectory);
        var packMask = (manifest.HasComponent("clan") ? 1 : 0) |
                       (manifest.HasComponent("inner-sphere") ? 2 : 0);
        if (packMask == 0) return [];

        var root = Path.GetFullPath(installRoot);
        var scratch = Path.GetFullPath(scratchDirectory);
        Directory.CreateDirectory(scratch);
        if ((File.GetAttributes(scratch) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("Mech Pak archive upgrade scratch directory cannot be a reparse point.");

        var replacements = new List<InstallFile>();
        foreach (var upgrade in qualifiedUpgrades.Where(item =>
                     item.ProductId.Equals(manifest.ProductId, StringComparison.OrdinalIgnoreCase) &&
                     item.PackMask == packMask))
        {
            if (!manifest.Files.Any(file => Normalize(file.Path).Equals(Normalize(upgrade.RelativePath), StringComparison.OrdinalIgnoreCase)))
                continue;
            var source = StagedInstallTransaction.ResolveContainedPath(root, upgrade.RelativePath);
            StagedInstallTransaction.RejectContainedFilePath(root, source);
            var sourceHash = HashFile(source);
            if (sourceHash.Equals(upgrade.CurrentSha256, StringComparison.OrdinalIgnoreCase)) continue;
            if (!sourceHash.Equals(upgrade.PreviousSha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Unsupported installed Mech Pak archive SHA-256 for {upgrade.RelativePath}: {sourceHash}");

            var outputName = $"archive-{replacements.Count:D2}-{Path.GetFileName(upgrade.RelativePath)}";
            var output = Path.Combine(scratch, outputName);
            MechPakActivationTransform.UpgradeInstalledArchiveComponents(
                source, output, PackFlags(packMask), cancellationToken);
            var outputHash = HashFile(output);
            if (!outputHash.Equals(upgrade.CurrentSha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Mech Pak archive upgrade hash mismatch for {upgrade.RelativePath}: {outputHash}");
            replacements.Add(new InstallFile(scratch, outputName, upgrade.RelativePath));
        }
        return replacements;
    }

    private static IReadOnlySet<uint> PackFlags(int packMask)
    {
        var flags = new HashSet<uint>();
        if ((packMask & 1) != 0) flags.Add(1);
        if ((packMask & 2) != 0) flags.Add(2);
        return flags;
    }

    private static string Normalize(string path) => path.Replace('\\', '/');

    private static string HashFile(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }
}
