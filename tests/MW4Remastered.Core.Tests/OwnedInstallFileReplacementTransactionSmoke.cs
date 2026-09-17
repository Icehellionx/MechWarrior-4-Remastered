using MW4Remastered.Core.Install;

internal static class OwnedInstallFileReplacementTransactionSmoke
{
    public static void Run(List<string> failures)
    {
        var root = Path.Combine(Path.GetTempPath(), "mw4-owned-replacement-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var original = Path.Combine(root, "original");
            Write(original, "ddraw.dll", "old wrapper");
            Write(original, "old-profile.ini", "old profile");
            Write(original, "MW4.exe", "game executable");
            var install = Path.Combine(root, "installed", "vengeance");
            new StagedInstallTransaction().Execute(new InstallPlan("vengeance", new[]
            {
                new InstallFile(original, "ddraw.dll", "ddraw.dll"),
                new InstallFile(original, "old-profile.ini", "old-profile.ini"),
                new InstallFile(original, "MW4.exe", "MW4.exe"),
            }), install);
            Write(install, "Saves/pilot.sav", "user save");

            var replacement = Path.Combine(root, "replacement");
            Write(replacement, "new-ddraw.dll", "new wrapper");
            Write(replacement, "new-profile.ini", "new profile");
            var files = new[]
            {
                new InstallFile(replacement, "new-ddraw.dll", "ddraw.dll"),
                new InstallFile(replacement, "new-profile.ini", "old-profile.ini"),
            };
            var transaction = new OwnedInstallFileReplacementTransaction();
            var manifest = transaction.Execute("vengeance", install, files);

            Check(File.ReadAllText(Path.Combine(install, "ddraw.dll")) == "new wrapper" &&
                  File.ReadAllText(Path.Combine(install, "old-profile.ini")) == "new profile",
                "owned replacement atomically updates the requested owned files", failures);
            Check(File.ReadAllText(Path.Combine(install, "MW4.exe")) == "game executable" &&
                  File.ReadAllText(Path.Combine(install, "Saves", "pilot.sav")) == "user save",
                "owned replacement preserves unrelated owned files and unowned saves", failures);
            Check(manifest.Files.Count == 3 &&
                  new InstallManifestVerifier().Verify(install, InstallVerificationScope.OwnedFiles).IsValid,
                "owned replacement rewrites a valid manifest without changing ownership breadth", failures);
            Check(!Directory.EnumerateDirectories(Path.Combine(install, ".mw4-remastered"), "replacement-*").Any(),
                "owned replacement cleans transaction scratch after success", failures);

            var noOp = transaction.Execute("vengeance", install, files);
            Check(noOp.Files.SequenceEqual(manifest.Files), "reapplying identical replacements is a no-op", failures);

            var unownedRejected = false;
            try
            {
                transaction.Execute("vengeance", install,
                    new[] { new InstallFile(replacement, "new-profile.ini", "unowned.ini") });
            }
            catch (InvalidDataException)
            {
                unownedRejected = true;
            }
            Check(unownedRejected && !File.Exists(Path.Combine(install, "unowned.ini")),
                "owned replacement refuses to claim a new destination", failures);

            Write(replacement, "rollback-ddraw.dll", "rollback candidate");
            var rollbackFiles = new[] { new InstallFile(replacement, "rollback-ddraw.dll", "ddraw.dll") };
            var manifestPath = Path.Combine(install, InstallManifest.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            var rollbackObserved = false;
            using (File.Open(manifestPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                try
                {
                    transaction.Execute("vengeance", install, rollbackFiles);
                }
                catch (IOException)
                {
                    rollbackObserved = true;
                }
            }
            Check(rollbackObserved && File.ReadAllText(Path.Combine(install, "ddraw.dll")) == "new wrapper",
                "manifest replacement failure restores the prior owned file", failures);
            Check(new InstallManifestVerifier().Verify(install, InstallVerificationScope.OwnedFiles).IsValid,
                "owned replacement rollback preserves the prior valid manifest", failures);

            Write(replacement, "new-runtime.dll", "new runtime");
            Write(replacement, "new-runtime.conf", "new runtime profile");
            var migrated = transaction.Migrate("vengeance", install,
                new[]
                {
                    new InstallFile(replacement, "new-runtime.dll", "new-runtime.dll"),
                    new InstallFile(replacement, "new-runtime.conf", "new-runtime.conf"),
                },
                new[] { "old-profile.ini", "already-retired.ini" });
            Check(File.Exists(Path.Combine(install, "new-runtime.dll")) &&
                  File.Exists(Path.Combine(install, "new-runtime.conf")) &&
                  !File.Exists(Path.Combine(install, "old-profile.ini")),
                "owned migration can add absent project files and retire an owned legacy profile", failures);
            Check(migrated.Files.Count == 4 &&
                  new InstallManifestVerifier().Verify(install, InstallVerificationScope.OwnedFiles).IsValid,
                "owned migration records additions and retirement in a valid manifest", failures);

            Write(install, "unowned-collision.dll", "user file");
            var collisionRejected = false;
            try
            {
                transaction.Migrate("vengeance", install,
                    new[] { new InstallFile(replacement, "new-runtime.dll", "unowned-collision.dll") }, []);
            }
            catch (InvalidDataException)
            {
                collisionRejected = true;
            }
            Check(collisionRejected && File.ReadAllText(Path.Combine(install, "unowned-collision.dll")) == "user file",
                "owned migration refuses to overwrite an unowned destination", failures);

            File.AppendAllText(Path.Combine(install, "MW4.exe"), "tampered");
            var tamperRejected = false;
            try
            {
                transaction.Execute("vengeance", install, rollbackFiles);
            }
            catch (InvalidDataException)
            {
                tamperRejected = true;
            }
            Check(tamperRejected && File.ReadAllText(Path.Combine(install, "ddraw.dll")) == "new wrapper",
                "owned replacement refuses a tree with any tampered owned file", failures);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
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
