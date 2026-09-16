using MW4Remastered.Core.Install;

internal static class OwnedInstallOverlayTransactionSmoke
{
    public static void Run(List<string> failures)
    {
        var root = Path.Combine(Path.GetTempPath(), "mw4-overlay-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var baseSource = Path.Combine(root, "base-source");
            Write(baseSource, "MW4.exe", "base executable");
            var install = Path.Combine(root, "installed", "vengeance");
            new StagedInstallTransaction().Execute(
                new InstallPlan("vengeance", new[] { new InstallFile(baseSource, "MW4.exe", "MW4.exe") }),
                install);
            Write(install, "Saves/pilot.sav", "user-owned save");

            var packSource = Path.Combine(root, "pack-source");
            Write(packSource, "RESOURCE/MAPS/PACK.MW4", "pack map");
            Write(packSource, "RESOURCE/MISSIONS/PACK.NFO", "pack metadata");
            var overlay = new MechPakResourceOverlayPlan("inner-sphere", "vengeance", new[]
            {
                new InstallFile(packSource, "RESOURCE/MAPS/PACK.MW4", "RESOURCE/MAPS/PACK.MW4"),
                new InstallFile(packSource, "RESOURCE/MISSIONS/PACK.NFO", "RESOURCE/MISSIONS/PACK.NFO"),
            });
            var manifest = new OwnedInstallOverlayTransaction().Execute(overlay, install);
            Check(manifest.Files.Count == 3 && File.Exists(Path.Combine(install, "RESOURCE", "MAPS", "PACK.MW4")),
                "overlay adds payload and extends the ownership manifest", failures);
            Check(File.Exists(Path.Combine(install, "Saves", "pilot.sav")), "overlay preserves unowned user files", failures);
            Check(new InstallManifestVerifier().Verify(install, InstallVerificationScope.OwnedFiles).IsValid,
                "overlay result passes owned-file verification", failures);

            var collision = new MechPakResourceOverlayPlan("clan", "vengeance", new[]
            {
                new InstallFile(packSource, "RESOURCE/MAPS/PACK.MW4", "RESOURCE/MAPS/PACK.MW4"),
            });
            var collisionRejected = false;
            try
            {
                new OwnedInstallOverlayTransaction().Execute(collision, install);
            }
            catch (InvalidDataException)
            {
                collisionRejected = true;
            }
            Check(collisionRejected && manifest.Files.Count == new InstallManifestVerifier().Verify(install, InstallVerificationScope.OwnedFiles).Manifest?.Files.Count,
                "overlay refuses to replace an owned destination", failures);

            var rollbackSource = Path.Combine(root, "rollback-source");
            Write(rollbackSource, "RESOURCE/MAPS/ROLLBACK.MW4", "rollback map");
            var rollbackOverlay = new MechPakResourceOverlayPlan("clan", "vengeance", new[]
            {
                new InstallFile(rollbackSource, "RESOURCE/MAPS/ROLLBACK.MW4", "RESOURCE/MAPS/ROLLBACK.MW4"),
            });
            var manifestPath = Path.Combine(install, InstallManifest.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            var rollbackObserved = false;
            using (File.Open(manifestPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                try
                {
                    new OwnedInstallOverlayTransaction().Execute(rollbackOverlay, install);
                }
                catch (IOException)
                {
                    rollbackObserved = true;
                }
            }
            Check(rollbackObserved && !File.Exists(Path.Combine(install, "RESOURCE", "MAPS", "ROLLBACK.MW4")),
                "manifest commit failure rolls back newly moved overlay files", failures);
            Check(new InstallManifestVerifier().Verify(install, InstallVerificationScope.OwnedFiles).IsValid,
                "overlay rollback preserves the original valid ownership manifest", failures);

            File.AppendAllText(Path.Combine(install, "MW4.exe"), "tampered");
            var tamperRejected = false;
            try
            {
                new OwnedInstallOverlayTransaction().Execute(rollbackOverlay, install);
            }
            catch (InvalidDataException)
            {
                tamperRejected = true;
            }
            Check(tamperRejected && !File.Exists(Path.Combine(install, "RESOURCE", "MAPS", "ROLLBACK.MW4")),
                "overlay blocks before mutation when existing owned content changed", failures);

            File.WriteAllText(Path.Combine(install, "MW4.exe"), "base executable");
            var removal = new OwnedInstallUninstaller().Remove(install);
            Check(removal.Status == InstallRemovalStatus.Removed
                && !File.Exists(Path.Combine(install, "RESOURCE", "MAPS", "PACK.MW4")),
                "uninstaller removes manifest-owned overlay files", failures);
            Check(File.Exists(Path.Combine(install, "Saves", "pilot.sav")),
                "uninstaller still preserves user data after an overlay", failures);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    private static void Write(string root, string relativePath, string contents)
    {
        var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, contents);
    }

    private static void Check(bool condition, string message, List<string> failures)
    {
        if (!condition) failures.Add("FAIL: " + message);
    }
}
