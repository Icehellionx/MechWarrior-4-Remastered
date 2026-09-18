using System.Security.Cryptography;
using MW4Remastered.Core.Install;

internal static class MechPakOwnedExecutableUpgradeSmoke
{
    public static void Run(List<string> failures)
    {
        var root = Path.Combine(Path.GetTempPath(), "mw4-mech-pak-owned-upgrade-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var install = Path.Combine(root, "install");
            var scratch = Path.Combine(root, "scratch");
            Directory.CreateDirectory(install);
            var executable = Path.Combine(install, "MW4.exe");
            var oldBytes = StockGate();
            File.WriteAllBytes(executable, oldBytes);
            var currentBytes = StockGate();
            var changed = MechPakActivationTransform.ActivateInstantActionStockPresence(
                currentBytes, new HashSet<uint> { 1, 2 });
            var oldHash = Hash(oldBytes);
            var currentHash = Hash(currentBytes);
            var manifest = new InstallManifest(2, "vengeance",
                [new InstalledFile("MW4.exe", oldBytes.Length, oldHash, "test")],
                ["vengeance", "clan", "inner-sphere"]);
            var upgrade = new MechPakOwnedExecutableUpgrade(
            [
                new MechPakOwnedExecutableUpgrade.QualifiedUpgrade(
                    "vengeance", "MW4.exe", 3, oldHash, currentHash),
            ]);

            var replacements = upgrade.CreateReplacements(install, manifest, scratch);
            var replacement = replacements.Single();
            var output = File.ReadAllBytes(Path.Combine(replacement.SourceRoot, replacement.SourceRelativePath));
            Check(changed == 1 && Hash(output) == currentHash && File.ReadAllBytes(executable).SequenceEqual(oldBytes),
                "owned Mech Pak upgrade derives the exact current executable without mutating the installed source", failures);

            File.WriteAllBytes(executable, currentBytes);
            var currentManifest = manifest with
            {
                Files = [new InstalledFile("MW4.exe", currentBytes.Length, currentHash, "test")],
            };
            Check(upgrade.CreateReplacements(install, currentManifest, Path.Combine(root, "current")).Count == 0,
                "owned Mech Pak upgrade is a no-op for the current executable", failures);

            File.WriteAllBytes(executable, StockGate().Append((byte)0).ToArray());
            var rejected = false;
            try
            {
                _ = upgrade.CreateReplacements(install, manifest, Path.Combine(root, "rejected"));
            }
            catch (InvalidDataException exception)
            {
                rejected = exception.Message.Contains("Unsupported installed Mech Pak executable SHA-256", StringComparison.Ordinal);
            }
            Check(rejected, "owned Mech Pak upgrade rejects an unknown installed executable", failures);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static byte[] StockGate() =>
    [
        0x8b, 0x4e, 0x3c, 0x32, 0xdb,
        0x81, 0xf1, 0x31, 0x95, 0x73, 0x85,
        0xc6, 0x44, 0x24, 0x12, 0x00,
        0x88, 0x5c, 0x24, 0x13,
        0x74, 0x05, 0xc6, 0x44, 0x24, 0x12, 0x01,
        0x8b, 0x56, 0x48,
        0x81, 0xf2, 0x31, 0x95, 0x73, 0x85,
        0x74, 0x09, 0xc6, 0x44, 0x24, 0x13, 0x01,
        0x8a, 0x5c, 0x24, 0x13,
    ];

    private static string Hash(ReadOnlySpan<byte> bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static void Check(bool condition, string message, List<string> failures)
    {
        if (!condition) failures.Add("FAIL: " + message);
    }
}
