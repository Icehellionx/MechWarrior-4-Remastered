namespace MW4Remastered.Core.Install;

using System.Security.Cryptography;

public static class MechPakInstalledEvidence
{
    private static readonly IReadOnlyDictionary<string, QualifiedPackFile[]> Packs =
        new Dictionary<string, QualifiedPackFile[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["inner-sphere"] =
            [
                new("RESOURCE/MAPS/COLSM01.MW4", 9370719, "5d22e4bf7f3abcc109c76d12a245896f2ebbbbd8aaa2149e3857a242106aecca"),
                new("RESOURCE/MAPS/GAGE.MW4", 24652632, "6a73a6971d52bd5bae4a3e97a198bb989f12ccf32bcd6575ca9ddf711cd281a2"),
                new("RESOURCE/MISSIONS/COLISEUM.MW4", 50741, "bb68d148201cf6c61c952c135f39981ea9a89e8d66f15ecbb1aeff344ba7ff16"),
                new("RESOURCE/MISSIONS/COLISEUM.NFO", 777, "39df2ee805da2c39229d173665004f4cc2121589a54f9036835028abdb3fd043"),
                new("RESOURCE/MISSIONS/COLISEUM.NFX", 1264, "b1ca18d03dc8723edf1c6124bcea5a505e3781c6570a30c239db5f86188f1408"),
                new("RESOURCE/MISSIONS/COLISEUM.TGA", 37676, "0248336866978f1b5a1d5e94c01f97674d0de70bef3b09c893721f152360f5d8"),
                new("RESOURCE/MISSIONS/GAGETOWN.MW4", 280807, "aac80bfb9af995e19d25fab0bbd318089c444328412d3d5381895dae051ade56"),
                new("RESOURCE/MISSIONS/GAGETOWN.NFO", 1085, "4df523df33a56827868ffe0c6e01c4870f101e063dda2b0ec46e77d94f1a728f"),
                new("RESOURCE/MISSIONS/GAGETOWN.NFX", 1570, "fe99331b2f1b5116fd77694009d2a4b20588c7bc4dbcd585e146b5aff319325a"),
                new("RESOURCE/MISSIONS/GAGETOWN.TGA", 37676, "4b1f64116718d371b06f03603388b9a764a21fdaaf288b41b398244d0527cb0c"),
            ],
            ["clan"] =
            [
                new("RESOURCE/MAPS/FACT01.MW4", 5409489, "2d5f690ffd2d115eca72c0aab4e6727f3223252b29908373bd1925fd735697e2"),
                new("RESOURCE/MAPS/NGOTH.MW4", 14588733, "bff57c48804bec917f8fa2908e73834bbb0a21462eb0efa230f41e0ffed24f44"),
                new("RESOURCE/MISSIONS/FACTORY.MW4", 62699, "63aa10fdfc0851eff396713de90cef9861d22f43f539c400b9597e2977dd4825"),
                new("RESOURCE/MISSIONS/FACTORY.NFO", 768, "815e0055a16fd444b0cdceef88527fbcfd2e9dd0e3abd266647a61082b40b3a1"),
                new("RESOURCE/MISSIONS/FACTORY.NFX", 1249, "8b93eb1ae808d59ba2923ff6c94d650b14f5aa093df64041e2b8173e9d080992"),
                new("RESOURCE/MISSIONS/FACTORY.TGA", 37676, "2e57c967e9e55381906fdfb40a5962bad3af0cd72f48361fe98ef29364809815"),
                new("RESOURCE/MISSIONS/NEWGOT_1.MW4", 144495, "c3b595e9b74da9aecfbc5fd5debd1b610e93e18ed7e432dda5d8b48a469fefdc"),
                new("RESOURCE/MISSIONS/NEWGOT_1.NFO", 1106, "e8b6ca237c2f501455099d61beaadaa7b90e8b9df08b569b13fd125d13d2b179"),
                new("RESOURCE/MISSIONS/NEWGOT_1.NFX", 1604, "363f5618a9995208d5918c4bbe658d71e148e64a0d8bd2d0c28f8eeaa45a98b8"),
                new("RESOURCE/MISSIONS/NEWGOT_1.TGA", 37676, "1cf0e0c712bbf5663ad8acc77aac9c5b0311d0ba4d8a92d77ebcee07afe8dbc0"),
            ],
        };

    public static bool IsPresent(InstallManifest manifest, string packProductId)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentException.ThrowIfNullOrWhiteSpace(packProductId);
        if (!Packs.TryGetValue(packProductId, out var required)) return false;
        var installed = manifest.Files.ToDictionary(file => file.Path, StringComparer.OrdinalIgnoreCase);
        return required.All(expected => installed.TryGetValue(expected.Path, out var actual) &&
            actual.Length == expected.Length && actual.Sha256.Equals(expected.Sha256, StringComparison.OrdinalIgnoreCase));
    }

    public static void ValidateSource(string mediaRoot, string packProductId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mediaRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(packProductId);
        if (!Packs.TryGetValue(packProductId, out var required))
        {
            throw new InvalidDataException($"No qualified Mech Pak payload exists for '{packProductId}'.");
        }
        var root = Path.GetFullPath(mediaRoot);
        foreach (var expected in required)
        {
            var path = Path.GetFullPath(Path.Combine(root, expected.Path.Replace('/', Path.DirectorySeparatorChar)));
            var relative = Path.GetRelativePath(root, path);
            if (Path.IsPathRooted(relative) || relative.Equals("..", StringComparison.Ordinal) ||
                relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) || !File.Exists(path))
            {
                throw new InvalidDataException($"Qualified Mech Pak file is missing or unsafe: {expected.Path}");
            }
            var info = new FileInfo(path);
            using var stream = File.OpenRead(path);
            var hash = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
            if (info.Length != expected.Length || !hash.Equals(expected.Sha256, StringComparison.Ordinal))
            {
                throw new InvalidDataException($"Unsupported Mech Pak resource revision: {expected.Path}");
            }
        }
    }

    private sealed record QualifiedPackFile(string Path, long Length, string Sha256);
}
