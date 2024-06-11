using System.Runtime.InteropServices;
using System.Text;

namespace Edelstein.Assets.Usm;

[StructLayout(LayoutKind.Sequential)]
public readonly struct SectorInfo
{
    public uint Signature { get; }
    public uint DataSize { get; }
    public byte R08 { get; }
    public byte DataOffset { get; }
    public ushort PaddingSize { get; }
    public byte ChNo { get; }
    public byte R0D { get; }
    public byte R0E { get; }
    private readonly byte _dataTypeByte;
    public SectorDataType DataType => (SectorDataType)(_dataTypeByte & 0b11);
    public byte R0F1 => (byte)(_dataTypeByte >> 2 & 0b11);
    public byte R0F2 => (byte)(_dataTypeByte >> 4 & 0b1111);
    public uint FrameTime { get; }
    public uint FrameRate { get; }
    public uint R18 { get; }
    public uint R1C { get; }

    private SectorInfo(Stream stream)
    {
        using BinaryReader binaryReader = new(stream, Encoding.UTF8, true);

        Signature = binaryReader.ReadUInt32BigEndian();
        DataSize = binaryReader.ReadUInt32BigEndian();
        R08 = binaryReader.ReadByte();
        DataOffset = binaryReader.ReadByte();
        PaddingSize = binaryReader.ReadUInt16BigEndian();
        ChNo = binaryReader.ReadByte();
        R0D = binaryReader.ReadByte();
        R0E = binaryReader.ReadByte();
        _dataTypeByte = binaryReader.ReadByte();
        FrameTime = binaryReader.ReadUInt32BigEndian();
        FrameRate = binaryReader.ReadUInt32BigEndian();
        R18 = binaryReader.ReadUInt32BigEndian();
        R1C = binaryReader.ReadUInt32BigEndian();
    }

    public static SectorInfo ReadFromStream(Stream stream) =>
        new(stream);
}
