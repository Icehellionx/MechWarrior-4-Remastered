namespace MW4Remastered.Core.Install;

public sealed record PreparedVengeanceExecutable(string ExecutablePath, string TransformId);

public interface IVengeanceExecutableTransform
{
    PreparedVengeanceExecutable Transform(
        string discOneRoot,
        string scratchDirectory,
        CancellationToken cancellationToken = default);
}

public sealed class UnavailableVengeanceExecutableTransform : IVengeanceExecutableTransform
{
    public PreparedVengeanceExecutable Transform(
        string discOneRoot,
        string scratchDirectory,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        throw new InvalidDataException(
            "The media-derived Vengeance executable transform has not been qualified for release.");
    }
}
