using System.Diagnostics;
using System.Text;

namespace Edelstein.Assets.Usm;

public class UsmMetaSection
{
    public string Name { get; private set; } = null!;
    public uint PageCount { get; private set; }

    private UsmMetaPage[] _pages = null!;

    private const uint MetaSectionSignature = 0x40555446;

    private UsmMetaSection() { }

    public static UsmMetaSection Load(Stream data)
    {
        long dataStartPosition = data.Position;

        UsmMetaSection usmMetaSection = new();

        UsmMetaSectionHeader sectionHeader = UsmMetaSectionHeader.ReadFromStream(data);

        if (sectionHeader.Signature != MetaSectionSignature)
            throw new Exception("Invalid input file");

        UsmMetaSectionInfo sectionInfo = UsmMetaSectionInfo.ReadFromStream(data);

        // TODO: Move strings and metaData reading to the end to allow full streaming
        data.Position = dataStartPosition + UsmMetaSectionHeader.StructSize + (int)sectionInfo.StringOffset;

        Span<byte> strings = new byte[(int)(sectionInfo.DataOffset - sectionInfo.StringOffset)];
        int stringsBytesReadCount = data.Read(strings);
        Debug.Assert(stringsBytesReadCount == (int)(sectionInfo.DataOffset - sectionInfo.StringOffset));

        Span<byte> metaData = new byte[(int)(sectionHeader.DataSize - sectionInfo.DataOffset)];
        int metaDataBytesReadCount = data.Read(metaData);
        Debug.Assert(metaDataBytesReadCount == (int)(sectionHeader.DataSize - sectionInfo.DataOffset));

        usmMetaSection.Name = Encoding.UTF8.GetString(strings.SliceUntilNullTerminator((int)sectionInfo.NameOffset));

        usmMetaSection.PageCount = sectionInfo.ValueCount;
        usmMetaSection._pages = new UsmMetaPage[sectionInfo.ValueCount];

        if (usmMetaSection.PageCount == 0)
            return usmMetaSection;

        for (int i = 0; i < usmMetaSection._pages.Length; i++)
            usmMetaSection._pages[i] = new UsmMetaPage();

        PriorityQueue<(UsmMetaElement, UsmMetaElementType), uint> valuesToSetQueue = new();

        data.Position = dataStartPosition + UsmMetaSectionHeader.StructSize + UsmMetaSectionInfo.StructSize;

        using BinaryReader binaryReader = new(data, Encoding.UTF8, true);

        for (uint i = 0; i < sectionInfo.ElementCount; i++)
        {
            byte type = binaryReader.ReadByte();
            UsmMetaElementType elementType = (UsmMetaElementType)((type & 0x1f) - 0xF);

            uint offset = binaryReader.ReadUInt32BigEndian();

            switch (type >> 5)
            {
                case 0:
                {
                    for (uint j = 0; j < sectionInfo.ValueCount; j++)
                    {
                        usmMetaSection._pages[j]
                            .AddNewElement(Encoding.UTF8.GetString(strings.SliceUntilNullTerminator((int)offset)))
                            .SetValue<object?>(null);
                    }

                    break;
                }
                case 1:
                {
                    string elementName = Encoding.UTF8.GetString(strings.SliceUntilNullTerminator((int)offset));

                    UsmMetaElement firstPageElement = usmMetaSection._pages[0].AddNewElement(elementName);

                    ReadElementValue(firstPageElement, elementType, binaryReader, strings, metaData);

                    for (uint j = 1; j < sectionInfo.ValueCount; j++)
                    {
                        usmMetaSection._pages[j]
                            .AddNewElement(elementName)
                            .CopyValueFrom(firstPageElement);
                    }

                    break;
                }
                case 2:
                {
                    string elementName = Encoding.UTF8.GetString(strings.SliceUntilNullTerminator((int)offset));

                    for (uint j = 0; j < sectionInfo.ValueCount; j++)
                    {
                        UsmMetaElement element = usmMetaSection._pages[j]
                            .AddNewElement(elementName);

                        valuesToSetQueue.Enqueue((element, elementType), sectionInfo.ElementCount * j + i);
                    }

                    break;
                }
            }
        }

        while (valuesToSetQueue.Count > 0)
        {
            (UsmMetaElement element, UsmMetaElementType elementType) = valuesToSetQueue.Dequeue();

            ReadElementValue(element, elementType, binaryReader, strings, metaData);
        }

        return usmMetaSection;

        static void ReadElementValue(UsmMetaElement element, UsmMetaElementType type, BinaryReader binaryReader, ReadOnlySpan<byte> strings,
            ReadOnlySpan<byte> metaData)
        {
            switch (type)
            {
                case UsmMetaElementType.SByte:
                {
                    element.SetValue(binaryReader.ReadSByte());
                    break;
                }
                case UsmMetaElementType.Byte:
                {
                    element.SetValue(binaryReader.ReadByte());
                    break;
                }
                case UsmMetaElementType.Int16:
                {
                    element.SetValue(binaryReader.ReadInt16BigEndian());
                    break;
                }
                case UsmMetaElementType.UInt16:
                {
                    element.SetValue(binaryReader.ReadUInt16BigEndian());
                    break;
                }
                case UsmMetaElementType.Int32:
                {
                    element.SetValue(binaryReader.ReadInt32BigEndian());
                    break;
                }
                case UsmMetaElementType.UInt32:
                {
                    element.SetValue(binaryReader.ReadUInt32BigEndian());
                    break;
                }
                case UsmMetaElementType.Int64:
                {
                    element.SetValue(binaryReader.ReadInt64BigEndian());
                    break;
                }
                case UsmMetaElementType.UInt64:
                {
                    element.SetValue(binaryReader.ReadUInt64BigEndian());
                    break;
                }
                case UsmMetaElementType.Single:
                {
                    element.SetValue(binaryReader.ReadSingleBigEndian());
                    break;
                }
                case UsmMetaElementType.String:
                {
                    element.SetValue(Encoding.UTF8.GetString(strings.SliceUntilNullTerminator((int)binaryReader.ReadUInt32BigEndian())));
                    break;
                }
                case UsmMetaElementType.ByteArray:
                {
                    int dataStartIndex = (int)binaryReader.ReadUInt32BigEndian();
                    int dataSize = (int)binaryReader.ReadUInt32BigEndian();

                    element.SetValue(metaData.Slice(dataStartIndex, dataSize).ToArray());
                    break;
                }
            }
        }
    }

    public UsmMetaElement? GetElement(uint pageIndex, string? name = null)
    {
        if (name == null)
            return _pages[pageIndex].First;

        UsmMetaElement? element = _pages[pageIndex].First;

        if (element is null)
            return null;

        do
        {
            if (element.Name == name)
                return element;
        } while ((element = element.Next) != null);

        return null;
    }
}
