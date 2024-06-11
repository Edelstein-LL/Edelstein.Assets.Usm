using System.Runtime.InteropServices;
using System.Text;

namespace Edelstein.Assets.Usm;

[StructLayout(LayoutKind.Sequential)]
public readonly struct UsmMetaSectionHeader
{
    public const nint StructSize = 0x8;

    public uint Signature { get; }
    public uint DataSize { get; }

    private UsmMetaSectionHeader(Stream stream)
    {
        using BinaryReader binaryReader = new(stream, Encoding.UTF8, true);

        Signature = binaryReader.ReadUInt32BigEndian();
        DataSize = binaryReader.ReadUInt32BigEndian();
    }

    public static UsmMetaSectionHeader ReadFromStream(Stream stream) =>
        new(stream);
}
