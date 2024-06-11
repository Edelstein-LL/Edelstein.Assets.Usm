using System.Buffers;

namespace Edelstein.Assets.Usm;

internal static class StreamExtensions
{
    public static async Task SkipUntil(this Stream stream, long position)
    {
        if (position < stream.Position)
            throw new ArgumentOutOfRangeException(nameof(position), "Position cannot be less than the current stream position");

        long bytesToSkip = position - stream.Position;

        if (bytesToSkip == 0)
            return;

        byte[] buffer = ArrayPool<byte>.Shared.Rent(8192);

        while (bytesToSkip > 0)
        {
            int bytesRead = await stream.ReadAsync(buffer.AsMemory(0, (int)Math.Min(buffer.Length, bytesToSkip)));

            if (bytesRead == 0)
                throw new EndOfStreamException("Reached the end of the stream before reaching the specified position");

            bytesToSkip -= bytesRead;
        }
    }
}
