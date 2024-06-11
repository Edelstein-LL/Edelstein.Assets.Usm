// Original by nyaga (?)
// 20190706 Mod by bnnm
// 20240611 C# version by Arasfon

using System.Buffers;
using System.Diagnostics;
using System.Text;

namespace Edelstein.Assets.Usm;

public class UsmDemuxer
{
    private readonly byte[] _videoMask1 = new byte[32];
    private readonly byte[] _videoMask2 = new byte[32];
    private readonly byte[] _audioMask = new byte[32];

    private const uint CridSignature = 0x43524944;
    private const uint SfvSignature = 0x40534656;
    private const uint SfaSignature = 0x40534641;

    private readonly Queue<string> _videoFilenames = new();
    private readonly Queue<string> _audioFilenames = new();

    public UsmDemuxer(uint key1, uint key2) =>
        InitializeMasks(key1, key2);

    public UsmDemuxer(ulong key)
    {
        uint key1 = (uint)(key & 0xFFFFFFFF);
        uint key2 = (uint)(key >> 32);

        InitializeMasks(key1, key2);
    }

    public async Task<DemuxResult> Demux(Stream stream, string directoryPath = "")
    {
        DemuxResult demuxResult = new();

        bool firstSector = true;

        byte? currentVideoChNo = null;
        byte? currentAudioChNo = null;

        FileStream? currentVideoFileStream = null;
        FileStream? currentAudioFileStream = null;

        if (directoryPath != "")
            Directory.CreateDirectory(directoryPath);

        try
        {
            while (stream.Position < stream.Length)
            {
                SectorInfo info = SectorInfo.ReadFromStream(stream);

                if (firstSector)
                {
                    if (info.Signature != CridSignature)
                        throw new Exception("Invalid input file");

                    firstSector = false;
                }

                int actualDataSize = (int)(info.DataSize - info.DataOffset - info.PaddingSize);

                long sectorEnd = stream.Position + info.DataOffset - UsmMetaSectionInfo.StructSize + actualDataSize + info.PaddingSize;

                switch (info.Signature)
                {
                    case CridSignature:
                    {
                        if (info.DataType is not SectorDataType.Meta)
                            break;

                        UsmMetaSection usmMetaSection = UsmMetaSection.Load(stream);

                        for (uint i = 0; i < usmMetaSection.PageCount; i++)
                        {
                            int? x = usmMetaSection.GetElement(i, "stmid")?.GetValue<int>();

                            if (x is null)
                                throw new Exception();

                            switch ((uint)x.Value)
                            {
                                case SfvSignature:
                                {
                                    _videoFilenames.Enqueue(Path.Combine(directoryPath,
                                        SanitizeFilename(usmMetaSection.GetElement(i, "filename")?.GetValue<string>()) + ".m2v"));
                                    break;
                                }
                                case SfaSignature:
                                {
                                    _audioFilenames.Enqueue(Path.Combine(directoryPath,
                                        SanitizeFilename(usmMetaSection.GetElement(i, "filename")?.GetValue<string>()) + ".adx"));
                                    break;
                                }
                            }
                        }

                        break;
                    }
                    case SfvSignature:
                    {
                        if (info.DataType is not SectorDataType.Data)
                            break;

                        byte[] data = ArrayPool<byte>.Shared.Rent(actualDataSize);

                        try
                        {
                            int readDataBytesCount = await stream.ReadAsync(data.AsMemory(0, actualDataSize));

                            Debug.Assert(readDataBytesCount == actualDataSize);

                            MaskVideo(data);

                            if (currentVideoChNo != info.ChNo)
                            {
                                currentVideoChNo = info.ChNo;

                                if (currentVideoFileStream is not null)
                                    await currentVideoFileStream.DisposeAsync();

                                string filename = _videoFilenames.Dequeue();

                                demuxResult.VideoPaths.Add(filename);

                                currentVideoFileStream = File.Open(filename, FileMode.Create, FileAccess.Write);
                            }

                            Debug.Assert(currentVideoFileStream is not null);

                            await currentVideoFileStream.WriteAsync(data.AsMemory(0, actualDataSize));
                        }
                        finally
                        {
                            ArrayPool<byte>.Shared.Return(data);
                        }

                        break;
                    }
                    case SfaSignature:
                    {
                        if (info.DataType is not SectorDataType.Data)
                            break;

                        byte[] data = ArrayPool<byte>.Shared.Rent(actualDataSize);

                        try
                        {
                            int readDataBytesCount = await stream.ReadAsync(data.AsMemory(0, actualDataSize));

                            Debug.Assert(readDataBytesCount == actualDataSize);

                            MaskAudio(data);

                            if (currentAudioChNo != info.ChNo)
                            {
                                currentAudioChNo = info.ChNo;

                                if (currentAudioFileStream is not null)
                                    await currentAudioFileStream.DisposeAsync();

                                string filename = _audioFilenames.Dequeue();

                                demuxResult.AudioPaths.Add(filename);

                                currentAudioFileStream = File.Open(filename, FileMode.Create, FileAccess.Write);
                            }

                            Debug.Assert(currentAudioFileStream is not null);

                            await currentAudioFileStream.WriteAsync(data.AsMemory(0, actualDataSize));
                        }
                        finally
                        {
                            ArrayPool<byte>.Shared.Return(data);
                        }

                        break;
                    }
                }

                await stream.SkipUntil(sectorEnd);
            }
        }
        finally
        {
            if (currentVideoFileStream != null)
                await currentVideoFileStream.DisposeAsync();

            if (currentAudioFileStream != null)
                await currentAudioFileStream.DisposeAsync();
        }

        Debug.Assert(_videoFilenames.Count == 0);
        Debug.Assert(_audioFilenames.Count == 0);

        return demuxResult;
    }

    private void InitializeMasks(uint key1, uint key2)
    {
        byte[] table1 = new byte[0x20];
        table1[0x00] = (byte)(key1 & 0xFF);
        table1[0x01] = (byte)(key1 >> 0x8 & 0xFF);
        table1[0x02] = (byte)(key1 >> 0x10 & 0xFF);
        table1[0x03] = (byte)((key1 >> 0x18 & 0xFF) - 0x34);
        table1[0x04] = (byte)((key2 & 0xFF) + 0xF9);
        table1[0x05] = (byte)(key2 >> 0x8 & 0xFF ^ 0x13);
        table1[0x06] = (byte)((key2 >> 0x10 & 0xFF) + 0x61);
        table1[0x07] = (byte)(table1[0x00] ^ 0xFF);
        table1[0x08] = (byte)(table1[0x02] + table1[0x01]);
        table1[0x09] = (byte)(table1[0x01] - table1[0x07]);
        table1[0x0A] = (byte)(table1[0x02] ^ 0xFF);
        table1[0x0B] = (byte)(table1[0x01] ^ 0xFF);
        table1[0x0C] = (byte)(table1[0x0B] + table1[0x09]);
        table1[0x0D] = (byte)(table1[0x08] - table1[0x03]);
        table1[0x0E] = (byte)(table1[0x0D] ^ 0xFF);
        table1[0x0F] = (byte)(table1[0x0A] - table1[0x0B]);
        table1[0x10] = (byte)(table1[0x08] - table1[0x0F]);
        table1[0x11] = (byte)(table1[0x10] ^ table1[0x07]);
        table1[0x12] = (byte)(table1[0x0F] ^ 0xFF);
        table1[0x13] = (byte)(table1[0x03] ^ 0x10);
        table1[0x14] = (byte)(table1[0x04] - 0x32);
        table1[0x15] = (byte)(table1[0x05] + 0xED);
        table1[0x16] = (byte)(table1[0x06] ^ 0xF3);
        table1[0x17] = (byte)(table1[0x13] - table1[0x0F]);
        table1[0x18] = (byte)(table1[0x15] + table1[0x07]);
        table1[0x19] = (byte)(0x21 - table1[0x13]);
        table1[0x1A] = (byte)(table1[0x14] ^ table1[0x17]);
        table1[0x1B] = (byte)(table1[0x16] + table1[0x16]);
        table1[0x1C] = (byte)(table1[0x17] + 0x44);
        table1[0x1D] = (byte)(table1[0x03] + table1[0x04]);
        table1[0x1E] = (byte)(table1[0x05] - table1[0x16]);
        table1[0x1F] = (byte)(table1[0x1D] ^ table1[0x13]);

        byte[] table2 = "URUC"u8.ToArray();
        for (int i = 0; i < 0x20; i++)
        {
            _videoMask1[i] = table1[i];
            _videoMask2[i] = (byte)(table1[i] ^ 0xFF);
            _audioMask[i] = (i & 1) == 1 ? table2[i >> 1 & 3] : (byte)(table1[i] ^ 0xFF);
        }
    }

    private void MaskVideo(Span<byte> data)
    {
        data = data[0x40..];

        if (data.Length < 0x200)
            return;

        byte[] mask = new byte[0x20];

        _videoMask2.CopyTo(mask.AsSpan());

        for (int i = 0x100; i < data.Length; i++)
            mask[i & 0x1F] = (byte)((data[i] ^= mask[i & 0x1F]) ^ _videoMask2[i & 0x1F]);

        _videoMask1.CopyTo(mask.AsSpan());

        for (int i = 0; i < 0x100; i++)
            data[i] ^= mask[i & 0x1F] ^= data[0x100 + i];
    }

    private void MaskAudio(Span<byte> data)
    {
        if (data.Length < 0x140)
            return;

        data = data[0x140..];

        for (int i = 0; i < data.Length; i++)
            data[i] ^= _audioMask[i & 0x1F];
    }

    private static string SanitizeFilename(string? filename)
    {
        if (filename == null)
            return String.Empty;

        char[] invalidChars = Path.GetInvalidFileNameChars();

        StringBuilder sb = new(filename.Length);

        foreach (char ch in filename)
            sb.Append(invalidChars.Contains(ch) ? '_' : ch);

        return sb.ToString().Trim();
    }
}
