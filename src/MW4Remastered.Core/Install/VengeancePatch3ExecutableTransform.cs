namespace MW4Remastered.Core.Install;

/// <summary>
/// Rebuilds the executable produced by the official Vengeance Patch 3 update
/// after that update has been applied to validated, user-supplied retail files.
/// </summary>
public sealed class VengeancePatch3ExecutableTransform : IVengeanceExecutableTransform
{
    public const string TransformId = "vengeance-patch3-01.30.04.1908-sd15020-v1";
    public const string LoaderSha256 = "578800c3f3d8d74c366a7229240c34850a85379573059fc8b7e7dc5603563e80";
    public const string EncryptedImageSha256 = "9e13cfda761d655222da523f4c603ea91f46c1669341fe82d8b6630ccbe3ecf6";
    public const string PlayerSha256 = "eb751d527e8c9b893ae0f5dd6ade3e15bbce468c5b0331afc87d0a23ba621ff7";

    public const string OutputSha256 = "25b921583ee8114a34f35e83bf5f78bb48cb71086ea39e12d7a386e9fddd019e";

    private static readonly uint[] CipherKey = [0xf3a5e812, 0xe7937512, 0x865ad10a, 0x9b72167a];
    private static readonly uint[] MissingThunkSeeds = [0xf3927c72, 0xf392771a];

    public PreparedVengeanceExecutable Transform(
        string discOneRoot,
        string scratchDirectory,
        CancellationToken cancellationToken = default) => VengeanceRetailExecutableTransform.TransformCore(
            discOneRoot,
            scratchDirectory,
            "MW4.EXE",
            "MW4.ICD",
            "DPLAYERX.DLL",
            "MW4.exe",
            TransformId,
            LoaderSha256,
            EncryptedImageSha256,
            PlayerSha256,
            OutputSha256,
            0x32a590,
            CipherKey,
            MissingThunkSeeds,
            0x58,
            false,
            SafeDisc15020TitlePatch.VengeancePatch3,
            cancellationToken);
}
