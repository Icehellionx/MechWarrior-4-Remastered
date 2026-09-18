using System.Security.Cryptography;

namespace MW4Remastered.Core.Install;

public sealed class MechPakOwnedExecutableUpgrade
{
    internal sealed record QualifiedUpgrade(
        string ProductId,
        string RelativePath,
        int PackMask,
        string PreviousSha256,
        string CurrentSha256);

    private static readonly IReadOnlyList<QualifiedUpgrade> DefaultUpgrades =
    [
        new("vengeance", "MW4.exe", 1,
            "77425c38e4b0318685f9d44ec47edde2d083cbdaf1b92779f30b0c1edc19ea72",
            "c645842e0ea5a2c35c8cc498df341c987b6129b9588a81b4a2657e30ccf25bce"),
        new("vengeance", "MW4.exe", 2,
            "336f76879fcaaa465479d82e6043c9a2e66b825b9bf851ecf3da5a436cc95f47",
            "79cae18d9b8591b40cccf218ef9c3fd7cf1a33bcd1a68c786029f02a8033a80d"),
        new("vengeance", "MW4.exe", 3,
            "58bceed9312ad40c13363143be92e2f80236543150e76be9d5f03638cbb7f1b2",
            "62b9958c4cf4e3e751c85d5f14d1890de63b190001f479986f0844e2ba72cb94"),
        new("vengeance", "MW4X/MW4x.exe", 1,
            "14bd3d188afdc67b7343f99d3de72db4e53eb53dbcce2e13c52d17631d7f2ec5",
            "a13588cbb08c32f47d2b5c53c65189752e49cbec692ac3889b7df83ee9b47471"),
        new("vengeance", "MW4X/MW4x.exe", 2,
            "4500b32e1a79986a9f3549a964c7d3675e75d72a85197c78779066a846b364d5",
            "18516b63e07a6347272f3aede0b053def2f7a1ed978642857378123c12ec6af4"),
        new("vengeance", "MW4X/MW4x.exe", 3,
            "30aa9e2204c97556b999f6d414be86587d647c4b84bf68079c668da403b5a618",
            "8ff3eeb53730eb1d0d88eb0f678b93aab38ec49a0fa1d9a6e3f2cb3d0307777c"),
    ];

    private readonly IReadOnlyList<QualifiedUpgrade> qualifiedUpgrades;

    public MechPakOwnedExecutableUpgrade()
        : this(DefaultUpgrades)
    {
    }

    internal MechPakOwnedExecutableUpgrade(IReadOnlyList<QualifiedUpgrade> qualifiedUpgrades)
    {
        this.qualifiedUpgrades = qualifiedUpgrades ?? throw new ArgumentNullException(nameof(qualifiedUpgrades));
    }

    public IReadOnlyList<InstallFile> CreateReplacements(
        string installRoot,
        InstallManifest manifest,
        string scratchDirectory)
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
            throw new InvalidDataException("Mech Pak executable upgrade scratch directory cannot be a reparse point.");

        var replacements = new List<InstallFile>();
        foreach (var upgrade in qualifiedUpgrades.Where(item =>
                     item.ProductId.Equals(manifest.ProductId, StringComparison.OrdinalIgnoreCase) &&
                     item.PackMask == packMask))
        {
            if (!manifest.Files.Any(file =>
                    Normalize(file.Path).Equals(Normalize(upgrade.RelativePath), StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var source = StagedInstallTransaction.ResolveContainedPath(root, upgrade.RelativePath);
            StagedInstallTransaction.RejectContainedFilePath(root, source);
            var sourceBytes = File.ReadAllBytes(source);
            var sourceHash = Hash(sourceBytes);
            if (sourceHash.Equals(upgrade.CurrentSha256, StringComparison.OrdinalIgnoreCase)) continue;
            if (!sourceHash.Equals(upgrade.PreviousSha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    $"Unsupported installed Mech Pak executable SHA-256 for {upgrade.RelativePath}: {sourceHash}");
            }

            var changes = MechPakActivationTransform.ActivateInstantActionStockPresence(
                sourceBytes,
                PackFlags(packMask));
            if (changes != 1)
                throw new InvalidDataException($"Installed Mech Pak executable exposed {changes} Instant Action stock gates; expected 1.");
            var outputHash = Hash(sourceBytes);
            if (!outputHash.Equals(upgrade.CurrentSha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Mech Pak executable upgrade hash mismatch for {upgrade.RelativePath}: {outputHash}");

            var outputName = $"{replacements.Count:D2}-{Path.GetFileName(upgrade.RelativePath)}";
            File.WriteAllBytes(Path.Combine(scratch, outputName), sourceBytes);
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

    private static string Hash(ReadOnlySpan<byte> bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
}
