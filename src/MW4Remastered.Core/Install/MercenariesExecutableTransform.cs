namespace MW4Remastered.Core.Install;

public sealed record PreparedMercenariesExecutable(string ExecutablePath, string TransformId);

public interface IMercenariesExecutableTransform
{
    PreparedMercenariesExecutable Transform(
        string discOneRoot,
        string scratchDirectory,
        CancellationToken cancellationToken = default);
}

public sealed class MercenariesRetailExecutableTransform : IMercenariesExecutableTransform
{
    public const string TransformId = "mercenaries-retail-50.06.09.3002-sd15020-v6";
    public const string LoaderSha256 = "fa317099c1336acabd1c1a3e9736d05533c6e19fb6603fa16e31a87d362bf8cf";
    public const string EncryptedImageSha256 = "37cdc4025c5dc2e8c48f22b2e02a2ef296878e5114453de40bcfaa835f0cc39c";
    public const string PlayerSha256 = "b77a09889b0e8731db4510ff1e44936bcff851fc97d666a89456d57882919110";
    public const string OutputSha256 = "a6ad2a76a391fb61e79ae5ed6f263b7ae0adfa204727e0a0fddee35901208739";

    private static readonly uint[] CipherKey = [0xece71f20, 0x7a186bc0, 0x32298540, 0x1aaec1c0];
    private static readonly uint[] MissingThunkSeeds = [0xecddd048, 0xecddca0e];

    public PreparedMercenariesExecutable Transform(
        string discOneRoot,
        string scratchDirectory,
        CancellationToken cancellationToken = default)
    {
        var prepared = VengeanceRetailExecutableTransform.TransformCore(
            discOneRoot,
            scratchDirectory,
            "MW4MERCS.EXE",
            "MW4MERCS.ICD",
            "DPLAYERX.DLL",
            "MW4Mercs.exe",
            TransformId,
            LoaderSha256,
            EncryptedImageSha256,
            PlayerSha256,
            OutputSha256,
            0x35a29c,
            CipherKey,
            MissingThunkSeeds,
            0x47,
            false,
            SafeDisc15020TitlePatch.Mercenaries,
            cancellationToken);
        return new PreparedMercenariesExecutable(prepared.ExecutablePath, prepared.TransformId);
    }
}

public sealed class MercenariesPr1ExecutableTransform : IMercenariesExecutableTransform
{
    public const string TransformId = "mercenaries-pr1-50.07.01.2105-sd15020-v1";
    public const string LoaderSha256 = "c75d1c053290a5a00da0fb5efa1802304aa5f689a7a57c19917b3649e2742810";
    public const string EncryptedImageSha256 = "aafff190e08119ff3ac86b0628813315cde05de46c0e36840ac3eba986a7d550";
    public const string PlayerSha256 = MercenariesRetailExecutableTransform.PlayerSha256;
    public const string OutputSha256 = "7a806308fb3ebdaf1270845faa518d2ad708dce85f6c747ab9d9331c3ea505b5";

    private static readonly uint[] CipherKey = [0xece75640, 0x7a18a2e0, 0x3229bc60, 0x1aaef8e0];
    private static readonly uint[] MissingThunkSeeds = [0xecdc5b38, 0xecdc4f6c];

    public PreparedMercenariesExecutable Transform(
        string discOneRoot,
        string scratchDirectory,
        CancellationToken cancellationToken = default)
    {
        var prepared = VengeanceRetailExecutableTransform.TransformCore(
            discOneRoot,
            scratchDirectory,
            "MW4Mercs.exe",
            "MW4MERCS.ICD",
            "DPLAYERX.DLL",
            "MW4Mercs.exe",
            TransformId,
            LoaderSha256,
            EncryptedImageSha256,
            PlayerSha256,
            OutputSha256,
            0x35d9bc,
            CipherKey,
            MissingThunkSeeds,
            0x47,
            false,
            SafeDisc15020TitlePatch.MercenariesPr1,
            cancellationToken);
        return new PreparedMercenariesExecutable(prepared.ExecutablePath, prepared.TransformId);
    }
}
