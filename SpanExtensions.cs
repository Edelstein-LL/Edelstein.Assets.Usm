namespace Edelstein.Assets.Usm;

internal static class SpanExtensions
{
    public static Span<byte> SliceUntilNullTerminator(this Span<byte> span, int startIndex = 0)
    {
        if (startIndex < 0 || startIndex >= span.Length)
            throw new ArgumentOutOfRangeException(nameof(startIndex));

        int endIndex = span[startIndex..].IndexOf((byte)0);

        if (endIndex == -1)
            return span[startIndex..];

        return span.Slice(startIndex, endIndex);
    }

    public static ReadOnlySpan<byte> SliceUntilNullTerminator(this ReadOnlySpan<byte> span, int startIndex = 0)
    {
        if (startIndex < 0 || startIndex >= span.Length)
            throw new ArgumentOutOfRangeException(nameof(startIndex));

        int endIndex = span[startIndex..].IndexOf((byte)0);

        if (endIndex == -1)
            return span[startIndex..];

        return span.Slice(startIndex, endIndex);
    }
}
