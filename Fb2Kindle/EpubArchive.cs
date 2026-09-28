using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Fb2Kindle {

  //ZipArchive on .NET Framework deflates every non-empty entry, but OCF requires the mimetype entry to be stored
  internal sealed class EpubArchive : IDisposable {

    private class Entry {
      internal byte[] Name;
      internal ushort Flags;
      internal ushort Method;
      internal uint Crc;
      internal uint CompressedSize;
      internal uint Size;
      internal uint Offset;
    }

    private static readonly uint[] CrcTable = Enumerable.Range(0, 256).Select(n => {
      var c = (uint)n;
      for (var k = 0; k < 8; k++)
        c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
      return c;
    }).ToArray();

    private readonly Stream stream;
    private readonly BinaryWriter writer;
    private readonly List<Entry> entries = new List<Entry>();
    private readonly ushort dosTime;
    private readonly ushort dosDate;

    internal EpubArchive(string fileName) {
      stream = File.Create(fileName);
      writer = new BinaryWriter(stream);
      var now = DateTime.Now;
      dosTime = (ushort)((now.Hour << 11) | (now.Minute << 5) | (now.Second / 2));
      dosDate = (ushort)(((now.Year - 1980) << 9) | (now.Month << 5) | now.Day);
    }

    internal void AddEntry(string name, string content, bool compress = true) {
      AddEntry(name, new UTF8Encoding(false).GetBytes(content), compress);
    }

    internal void AddFile(string name, string fileName) {
      AddEntry(name, File.ReadAllBytes(fileName), true);
    }

    private void AddEntry(string name, byte[] data, bool compress) {
      var payload = data;
      if (compress) {
        using (var output = new MemoryStream()) {
          using (var deflate = new DeflateStream(output, CompressionLevel.Optimal, true))
            deflate.Write(data, 0, data.Length);
          payload = output.ToArray();
        }
      }
      var entry = new Entry {
        Name = Encoding.UTF8.GetBytes(name),
        Flags = (ushort)(name.Any(c => c > 127) ? 0x0800 : 0),
        Method = (ushort)(compress ? 8 : 0),
        Crc = GetCrc(data),
        CompressedSize = (uint)payload.Length,
        Size = (uint)data.Length,
        Offset = (uint)stream.Position,
      };
      entries.Add(entry);

      writer.Write(0x04034b50u);
      writer.Write((ushort)20);
      WriteEntryInfo(entry);
      writer.Write((ushort)0);
      writer.Write(entry.Name);
      writer.Write(payload);
    }

    private void WriteEntryInfo(Entry entry) {
      writer.Write(entry.Flags);
      writer.Write(entry.Method);
      writer.Write(dosTime);
      writer.Write(dosDate);
      writer.Write(entry.Crc);
      writer.Write(entry.CompressedSize);
      writer.Write(entry.Size);
      writer.Write((ushort)entry.Name.Length);
    }

    private static uint GetCrc(byte[] data) {
      var crc = 0xFFFFFFFFu;
      foreach (var b in data)
        crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
      return ~crc;
    }

    public void Dispose() {
      var directoryOffset = (uint)stream.Position;
      foreach (var entry in entries) {
        writer.Write(0x02014b50u);
        writer.Write((ushort)20);
        writer.Write((ushort)20);
        WriteEntryInfo(entry);
        writer.Write((ushort)0);
        writer.Write((ushort)0);
        writer.Write((ushort)0);
        writer.Write((ushort)0);
        writer.Write(0u);
        writer.Write(entry.Offset);
        writer.Write(entry.Name);
      }
      var directorySize = (uint)stream.Position - directoryOffset;
      writer.Write(0x06054b50u);
      writer.Write((ushort)0);
      writer.Write((ushort)0);
      writer.Write((ushort)entries.Count);
      writer.Write((ushort)entries.Count);
      writer.Write(directorySize);
      writer.Write(directoryOffset);
      writer.Write((ushort)0);
      writer.Dispose();
    }
  }
}
