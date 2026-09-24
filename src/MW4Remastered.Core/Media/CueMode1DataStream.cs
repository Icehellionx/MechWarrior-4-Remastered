using System.Text.RegularExpressions;

namespace MW4Remastered.Core.Media;

/// <summary>
/// Exposes the 2048-byte data portions of one raw MODE1/2352 BIN track.
/// The CUE is only a local descriptor; its sibling BIN remains the media source.
/// </summary>
internal sealed class CueMode1DataStream : Stream
{
    private const int RawSectorLength = 2352;
    private const int DataOffset = 16;
    private const int DataLength = 2048;
    private const long MaximumLogicalLength = 4L * 1024 * 1024 * 1024;

    private static readonly Regex FileLine = new(
        @"^FILE\s+""([^""]+)""\s+BINARY$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex TrackLine = new(
        @"^TRACK\s+01\s+MODE1/2352$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex IndexLine = new(
        @"^INDEX\s+01\s+00:00:00$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private readonly FileStream bin;
    private readonly byte[] sector = new byte[RawSectorLength];
    private long position;
    private long cachedSector = -1;

    public CueMode1DataStream(string cuePath)
    {
        var cue = Path.GetFullPath(cuePath);
        if (!File.Exists(cue)) throw new FileNotFoundException("CUE sheet does not exist.", cue);
        if ((File.GetAttributes(cue) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("CUE sheet cannot be a reparse point.");
        if (new FileInfo(cue).Length > 64 * 1024)
            throw new InvalidDataException("CUE sheet is too large.");

        var lines = File.ReadAllLines(cue)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !Regex.IsMatch(line, @"^REM\s", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            .ToArray();
        if (lines.Length != 3 ||
            !TrackLine.IsMatch(lines[1]) ||
            !IndexLine.IsMatch(lines[2]))
            throw new InvalidDataException("Only single-track MODE1/2352 CUE/BIN images starting at 00:00:00 are supported.");

        var file = FileLine.Match(lines[0]);
        if (!file.Success) throw new InvalidDataException("CUE sheet must name one quoted BINARY track file.");
        var name = file.Groups[1].Value;
        if (!name.EndsWith(".bin", StringComparison.OrdinalIgnoreCase) ||
            name is "." or ".." ||
            name.IndexOfAny(['/', '\\', ':']) >= 0 ||
            !string.Equals(Path.GetFileName(name), name, StringComparison.Ordinal))
            throw new InvalidDataException("CUE track must name a sibling BIN file without path components.");

        var track = Path.Combine(Path.GetDirectoryName(cue)!, name);
        if (!File.Exists(track))
            throw new FileNotFoundException("The BIN track named by the CUE sheet is missing. Keep the matching CUE and BIN together.", track);
        if ((File.GetAttributes(track) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("BIN track cannot be a reparse point.");

        bin = new FileStream(track, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (bin.Length == 0 || bin.Length % RawSectorLength != 0 ||
            bin.Length / RawSectorLength > MaximumLogicalLength / DataLength)
        {
            bin.Dispose();
            throw new InvalidDataException("BIN track size is not a supported MODE1/2352 image.");
        }
        Length = bin.Length / RawSectorLength * DataLength;
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length { get; }
    public override long Position
    {
        get => position;
        set
        {
            if (value < 0 || value > Length) throw new ArgumentOutOfRangeException(nameof(value));
            position = value;
        }
    }

    public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

    public override int Read(Span<byte> destination)
    {
        var available = (int)Math.Min(destination.Length, Length - position);
        var written = 0;
        while (written < available)
        {
            var sectorIndex = position / DataLength;
            if (cachedSector != sectorIndex)
            {
                bin.Position = sectorIndex * RawSectorLength;
                bin.ReadExactly(sector);
                ValidateSector(sector);
                cachedSector = sectorIndex;
            }
            var within = (int)(position % DataLength);
            var count = Math.Min(available - written, DataLength - within);
            sector.AsSpan(DataOffset + within, count).CopyTo(destination.Slice(written, count));
            written += count;
            position += count;
        }
        return written;
    }

    private static void ValidateSector(byte[] bytes)
    {
        if (bytes[0] != 0 || bytes[11] != 0 || bytes[15] != 1 ||
            bytes.AsSpan(1, 10).IndexOfAnyExcept((byte)0xff) >= 0)
            throw new InvalidDataException("BIN track contains a sector that is not raw CD MODE1/2352 data.");
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        Position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => checked(position + offset),
            SeekOrigin.End => checked(Length + offset),
            _ => throw new ArgumentOutOfRangeException(nameof(origin)),
        };
        return position;
    }

    public override void Flush() { }
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing) bin.Dispose();
        base.Dispose(disposing);
    }
}
