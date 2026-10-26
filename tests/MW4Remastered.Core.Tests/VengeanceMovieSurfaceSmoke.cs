using System.Security.Cryptography;
using MW4Remastered.Core.Install;

internal static class VengeanceMovieSurfaceSmoke
{
    internal static void Run(List<string> failures)
    {
        var root = Path.Combine(Path.GetTempPath(), "mw4-movie-surface-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            // A synthetic byte fixture, never a distributed game image.
            var original = Enumerable.Repeat((byte)0xCC, 64).ToArray();
            Convert.FromHexString("50E808810100").CopyTo(original, 16);
            var corrected = original.ToArray();
            corrected.AsSpan(16, 6).Fill(0x90);
            var transform = new VengeanceMovieSurfaceTransform(Hash(original), Hash(corrected), 16);
            var source = Path.Combine(root, "source");
            Directory.CreateDirectory(source);
            var executable = Path.Combine(source, "MW4.exe");
            File.WriteAllBytes(executable, original);
            var plan = new InstallPlan("vengeance", [new InstallFile(source, "MW4.exe", "MW4.exe")]);
            File.SetAttributes(executable, FileAttributes.ReadOnly);
            InstallPlan transformed;
            try
            {
                transformed = transform.TransformPlan(plan, Path.Combine(root, "fresh"));
                Check(File.ReadAllBytes(executable).SequenceEqual(original) &&
                      (File.GetAttributes(executable) & FileAttributes.ReadOnly) != 0,
                    "movie correction preserves its read-only source");
            }
            finally { File.SetAttributes(executable, FileAttributes.Normal); }
            var newFile = transformed.Files.Single();
            var newBytes = File.ReadAllBytes(Path.Combine(newFile.SourceRoot, newFile.SourceRelativePath));
            Check(newBytes.SequenceEqual(corrected) && newBytes.Length == original.Length &&
                  original.Zip(newBytes).Count(pair => pair.First != pair.Second) == 6,
                "movie correction changes exactly the premature PUSH/Release pair");
            Check(ReferenceEquals(transform.TransformPlan(transformed, Path.Combine(root, "idempotent")), transformed),
                "current movie correction plan is idempotent");

            foreach (var component in new[] { "clan", "inner-sphere", "black-knight" })
            {
                var adjacent = new InstallPlan("vengeance", plan.Files, ["vengeance", component]);
                Check(ReferenceEquals(transform.TransformPlan(adjacent, Path.Combine(root, "skip")), adjacent),
                    "Patch 3/Black Knight family plan is unchanged: " + component);
            }
            var mercenaries = new InstallPlan("mercenaries", plan.Files);
            Check(ReferenceEquals(transform.TransformPlan(mercenaries, Path.Combine(root, "skip")), mercenaries),
                "Mercenaries plan is unchanged by retail Vengeance movie correction");

            var install = Path.Combine(root, "install");
            var manifest = new StagedInstallTransaction().Execute(plan, install);
            var pilot = Path.Combine(install, "Pilots", "pilot.sav");
            Directory.CreateDirectory(Path.GetDirectoryName(pilot)!);
            File.WriteAllText(pilot, "saved pilot");
            var replacements = transform.CreateUpgradeReplacement(install, manifest, Path.Combine(root, "upgrade"));
            var current = new OwnedInstallFileReplacementTransaction().Execute("vengeance", install, replacements);
            Check(new InstallManifestVerifier().Verify(install, InstallVerificationScope.OwnedFiles).IsValid &&
                  File.ReadAllBytes(Path.Combine(install, "MW4.exe")).SequenceEqual(corrected) &&
                  File.ReadAllText(pilot) == "saved pilot",
                "owned movie upgrade updates the manifest and preserves a pilot");
            Check(transform.CreateUpgradeReplacement(install, current, Path.Combine(root, "repeat")).Count == 0,
                "repeat movie upgrade makes no replacement");
            var unownedRejected = false;
            try { transform.CreateUpgradeReplacement(install, current with { Files = [] }, Path.Combine(root, "unowned")); }
            catch (InvalidDataException) { unownedRejected = true; }
            Check(unownedRejected, "movie upgrade refuses an executable not listed as owned");
            File.AppendAllText(Path.Combine(install, "MW4.exe"), "changed");
            var changedRejected = false;
            try { transform.CreateUpgradeReplacement(install, current, Path.Combine(root, "changed")); }
            catch (InvalidDataException) { changedRejected = true; }
            Check(changedRejected && !Directory.Exists(Path.Combine(root, "changed")),
                "movie upgrade rejects an owned file changed since verification before writing scratch");
            File.WriteAllBytes(Path.Combine(install, "MW4.exe"), corrected);
            var removed = new OwnedInstallUninstaller().Remove(install);
            Check(removed.Status == InstallRemovalStatus.Removed && File.ReadAllText(pilot) == "saved pilot" &&
                  !File.Exists(Path.Combine(install, "MW4.exe")), "movie-corrected ownership uninstalls while preserving the pilot");

            var wrong = original.ToArray();
            wrong[16] = 0x51;
            File.WriteAllBytes(executable, wrong);
            var wrongBytesRejected = false;
            try { new VengeanceMovieSurfaceTransform(Hash(wrong), Hash(corrected), 16).TransformPlan(plan, Path.Combine(root, "wrong")); }
            catch (InvalidDataException) { wrongBytesRejected = true; }
            Check(wrongBytesRejected && !Directory.Exists(Path.Combine(root, "wrong")),
                "movie correction rejects mismatched instructions even when the input pin matches");
            var wrongManifest = new InstallManifest(2, "vengeance", [new InstalledFile("MW4.exe", wrong.Length, Hash(wrong), "fixture")]);
            var wrongHashRejected = false;
            try { transform.CreateUpgradeReplacement(source, wrongManifest, Path.Combine(root, "wrong-hash")); }
            catch (InvalidDataException) { wrongHashRejected = true; }
            Check(wrongHashRejected, "owned movie upgrade rejects an unknown executable hash");

            File.WriteAllBytes(executable, original);
            var collision = Path.Combine(root, "collision");
            Directory.CreateDirectory(collision);
            File.WriteAllText(Path.Combine(collision, "MW4-movie-surface.exe"), "keep");
            var collisionRejected = false;
            try { transform.TransformPlan(plan, collision); }
            catch (IOException) { collisionRejected = true; }
            Check(collisionRejected && File.ReadAllText(Path.Combine(collision, "MW4-movie-surface.exe")) == "keep",
                "movie correction refuses to overwrite an existing scratch output");
            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            var cancellationObserved = false;
            try { transform.TransformPlan(plan, Path.Combine(root, "cancelled"), cancelled.Token); }
            catch (OperationCanceledException) { cancellationObserved = true; }
            Check(cancellationObserved && !Directory.Exists(Path.Combine(root, "cancelled")),
                "cancelled movie correction creates no output");

            void Check(bool condition, string message) { if (!condition) failures.Add("FAIL: " + message); }
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    private static string Hash(ReadOnlySpan<byte> bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
}
