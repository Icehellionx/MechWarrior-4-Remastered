using System.Security.Cryptography;
using MW4Remastered.Core.Install;

internal static class OwnedInstallExpansionSmoke
{
    public static void Run(List<string> failures)
    {
        var root = Path.Combine(Path.GetTempPath(), "mw4-expansion-" + Guid.NewGuid().ToString("N"));
        try
        {
            var source = Path.Combine(root, "source");
            Directory.CreateDirectory(source);
            File.WriteAllText(Path.Combine(source, "game"), "retail executable");
            File.WriteAllText(Path.Combine(source, "patched"), "patched executable");
            File.WriteAllText(Path.Combine(source, "pack"), "qualified pack fixture");
            var install = Path.Combine(root, "installed");
            new StagedInstallTransaction().Execute(new InstallPlan("vengeance",
                [new(source, "game", "MW4.exe")]), install);
            File.WriteAllText(Path.Combine(install, "options.ini"), "user settings");
            File.WriteAllText(Path.Combine(install, "pilot.sav"), "user save");
            var plan = new InstallPlan("vengeance",
                [new(source, "patched", "MW4.exe"), new(source, "pack", "RESOURCE/pack.mw4")],
                ["vengeance", "inner-sphere"]);
            var product = new InstallDestinationProduct("vengeance", "Vengeance", install, 1, plan.Components);
            var verifier = new InstallManifestVerifier();
            Check(!product.IsSatisfiedBy(verifier.Verify(install, InstallVerificationScope.OwnedFiles).Manifest),
                "base-only installation cannot satisfy newly selected packs", failures);
            var transaction = new OwnedInstallFileReplacementTransaction();
            var manifestPath = Path.Combine(install, InstallManifest.RelativePath);
            var before = File.ReadAllBytes(manifestPath);

            using (var cancelled = new CancellationTokenSource())
            {
                cancelled.Cancel();
                ExpectRejected(() => transaction.Expand(plan, install, cancelled.Token),
                    "cancelled expansion is rejected", failures);
            }
            Check(before.SequenceEqual(File.ReadAllBytes(manifestPath)) &&
                File.ReadAllText(Path.Combine(install, "MW4.exe")) == "retail executable",
                "cancelled expansion preserves the original manifest and executable", failures);

            // Force failure after owned-file replacement but before manifest commit.
            using (File.Open(manifestPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                ExpectRejected(() => transaction.Expand(plan, install), "locked expansion manifest fails", failures);
            Check(before.SequenceEqual(File.ReadAllBytes(manifestPath)) &&
                File.ReadAllText(Path.Combine(install, "MW4.exe")) == "retail executable" &&
                !File.Exists(Path.Combine(install, "RESOURCE/pack.mw4")),
                "failed expansion restores old files, removes additions and preserves component ownership", failures);

            File.WriteAllText(Path.Combine(install, "collision"), "user-owned content");
            var collision = new InstallPlan("vengeance", [new(source, "pack", "collision")], plan.Components);
            ExpectRejected(() => transaction.Expand(collision, install), "expansion rejects unowned collisions", failures);
            Check(File.ReadAllText(Path.Combine(install, "collision")) == "user-owned content",
                "expansion never replaces unowned files", failures);

            var expanded = transaction.Expand(plan, install);
            Check(product.IsSatisfiedBy(expanded) &&
                verifier.Verify(install, InstallVerificationScope.OwnedFiles).IsValid &&
                File.ReadAllText(Path.Combine(install, "MW4.exe")) == "patched executable",
                "expansion commits new payload and component ownership together", failures);
            Check(File.ReadAllText(Path.Combine(install, "options.ini")) == "user settings" &&
                File.ReadAllText(Path.Combine(install, "pilot.sav")) == "user save",
                "adding packs preserves user settings and saves", failures);
            var digest = SHA256.HashData(File.ReadAllBytes(manifestPath));
            transaction.Expand(plan, install);
            Check(digest.SequenceEqual(SHA256.HashData(File.ReadAllBytes(manifestPath))),
                "repeat expansion is idempotent", failures);

            var metadataOnly = new InstallPlan("vengeance", plan.Files, ["vengeance", "inner-sphere", "clan"]);
            var tagged = transaction.Expand(metadataOnly, install);
            Check(tagged.HasComponent("clan") && tagged.Files.SequenceEqual(expanded.Files) &&
                verifier.Verify(install, InstallVerificationScope.OwnedFiles).Manifest!.HasComponent("clan"),
                "already matching payload still commits missing component metadata", failures);

            var incomplete = new InstallPlan("vengeance", [new(source, "game", "MW4.exe")]);
            ExpectRejected(() => transaction.Expand(incomplete, install),
                "expansion refuses to remove an installed component when its media is omitted", failures);
            File.AppendAllText(Path.Combine(install, "MW4.exe"), "tampered");
            ExpectRejected(() => transaction.Expand(plan, install), "expansion rejects damaged owned trees", failures);
            Check(!Directory.EnumerateDirectories(Path.Combine(install, ".mw4-remastered"), "replacement-*").Any(),
                "expansion cleans owned scratch on success, rejection, cancellation and rollback", failures);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    private static void ExpectRejected(Action action, string message, List<string> failures)
    {
        try { action(); failures.Add("FAIL: " + message); }
        catch (Exception error) when (error is IOException or InvalidDataException or OperationCanceledException) { }
    }

    private static void Check(bool condition, string message, List<string> failures)
    {
        if (!condition) failures.Add("FAIL: " + message);
    }
}
