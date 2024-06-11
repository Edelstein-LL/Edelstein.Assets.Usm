using System.Runtime.InteropServices;
using System.Text;

namespace Edelstein.Assets.Usm;

[StructLayout(LayoutKind.Sequential)]
public readonly struct UsmMetaSectionInfo
{
    public const nint StructSize = 0x18;

    public uint ValueOffset { get; }
    public uint StringOffset { get; }
    public uint DataOffset { get; }
    public uint NameOffset { get; }
    public ushort ElementCount { get; }
    public ushort ValueSize { get; }
    public uint ValueCount { get; }

    private UsmMetaSectionInfo(Stream stream)
    {
        using BinaryReader binaryReader = new(stream, Encoding.UTF8, true);

        ValueOffset = binaryReader.ReadUInt32BigEndian();
        StringOffset = binaryReader.ReadUInt32BigEndian();
        DataOffset = binaryReader.ReadUInt32BigEndian();
        NameOffset = binaryReader.ReadUInt32BigEndian();
        ElementCount = binaryReader.ReadUInt16BigEndian();
        ValueSize = binaryReader.ReadUInt16BigEndian();
        ValueCount = binaryReader.ReadUInt32BigEndian();
    }

    public static UsmMetaSectionInfo ReadFromStream(Stream stream) =>
        new(stream);
}
