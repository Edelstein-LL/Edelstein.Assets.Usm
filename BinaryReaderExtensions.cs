using System.Buffers.Binary;

namespace Edelstein.Assets.Usm;

internal static class BinaryReaderExtensions
{
    public static short ReadInt16BigEndian(this BinaryReader binaryReader) =>
        BinaryPrimitives.ReadInt16BigEndian(binaryReader.ReadBytes(sizeof(short)));

    public static ushort ReadUInt16BigEndian(this BinaryReader binaryReader) =>
        BinaryPrimitives.ReadUInt16BigEndian(binaryReader.ReadBytes(sizeof(ushort)));

    public static int ReadInt32BigEndian(this BinaryReader binaryReader) =>
        BinaryPrimitives.ReadInt32BigEndian(binaryReader.ReadBytes(sizeof(int)));

    public static uint ReadUInt32BigEndian(this BinaryReader binaryReader) =>
        BinaryPrimitives.ReadUInt32BigEndian(binaryReader.ReadBytes(sizeof(uint)));

    public static long ReadInt64BigEndian(this BinaryReader binaryReader) =>
        BinaryPrimitives.ReadInt64BigEndian(binaryReader.ReadBytes(sizeof(long)));

    public static ulong ReadUInt64BigEndian(this BinaryReader binaryReader) =>
        BinaryPrimitives.ReadUInt64BigEndian(binaryReader.ReadBytes(sizeof(ulong)));

    public static float ReadSingleBigEndian(this BinaryReader binaryReader) =>
        BinaryPrimitives.ReadSingleBigEndian(binaryReader.ReadBytes(sizeof(float)));
}
