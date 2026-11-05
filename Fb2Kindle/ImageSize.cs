namespace Fb2Kindle {

  //reads image dimensions from the file header without decoding the image
  internal static class ImageSize {

    internal static bool TryGet(byte[] data, out int width, out int height) {
      width = height = 0;
      if (data.Length >= 24 && data[0] == 0x89 && data[1] == 'P' && data[2] == 'N' && data[3] == 'G') {
        width = ReadInt32BigEndian(data, 16);
        height = ReadInt32BigEndian(data, 20);
        return true;
      }
      if (data.Length >= 10 && data[0] == 'G' && data[1] == 'I' && data[2] == 'F') {
        width = data[6] | data[7] << 8;
        height = data[8] | data[9] << 8;
        return true;
      }
      if (data.Length >= 26 && data[0] == 'B' && data[1] == 'M') {
        width = System.Math.Abs(System.BitConverter.ToInt32(data, 18));
        height = System.Math.Abs(System.BitConverter.ToInt32(data, 22));
        return true;
      }
      if (data.Length >= 4 && data[0] == 0xFF && data[1] == 0xD8)
        return TryGetJpeg(data, out width, out height);
      return false;
    }

    //returns a format name detected by its signature, or null for unknown data
    internal static string DetectFormat(byte[] data) {
      if (data.Length >= 4 && data[0] == 0xFF && data[1] == 0xD8)
        return "JPEG";
      if (data.Length >= 8 && data[0] == 0x89 && data[1] == 'P' && data[2] == 'N' && data[3] == 'G')
        return "PNG";
      if (data.Length >= 6 && data[0] == 'G' && data[1] == 'I' && data[2] == 'F')
        return "GIF";
      if (data.Length >= 2 && data[0] == 'B' && data[1] == 'M')
        return "BMP";
      if (data.Length >= 4 && (data[0] == 'I' && data[1] == 'I' && data[2] == 42 && data[3] == 0 || data[0] == 'M' && data[1] == 'M' && data[2] == 0 && data[3] == 42))
        return "TIFF";
      if (data.Length >= 12 && data[0] == 'R' && data[1] == 'I' && data[2] == 'F' && data[3] == 'F' && data[8] == 'W' && data[9] == 'E' && data[10] == 'B' && data[11] == 'P')
        return "WebP";
      if (data.Length >= 12 && data[4] == 'f' && data[5] == 't' && data[6] == 'y' && data[7] == 'p')
        return "AVIF/HEIF";
      var head = System.Text.Encoding.ASCII.GetString(data, 0, System.Math.Min(data.Length, 1024));
      if (head.IndexOf("<svg", System.StringComparison.OrdinalIgnoreCase) >= 0)
        return "SVG";
      return null;
    }

    internal static bool IsSupported(string format) {
      return format == "JPEG" || format == "PNG" || format == "GIF" || format == "BMP" || format == "TIFF";
    }

    private static bool TryGetJpeg(byte[] data, out int width, out int height) {
      width = height = 0;
      var pos = 2;
      while (pos + 3 < data.Length) {
        if (data[pos] != 0xFF)
          return false;
        var marker = data[pos + 1];
        if (marker == 0xFF) {
          pos++;
          continue;
        }
        if (marker == 0x01 || marker >= 0xD0 && marker <= 0xD8) {
          pos += 2;
          continue;
        }
        if (marker == 0xD9 || marker == 0xDA)
          return false;
        var length = data[pos + 2] << 8 | data[pos + 3];
        var isFrameHeader = marker >= 0xC0 && marker <= 0xCF && marker != 0xC4 && marker != 0xC8 && marker != 0xCC;
        if (isFrameHeader) {
          if (pos + 8 >= data.Length)
            return false;
          height = data[pos + 5] << 8 | data[pos + 6];
          width = data[pos + 7] << 8 | data[pos + 8];
          return true;
        }
        pos += 2 + length;
      }
      return false;
    }

    private static int ReadInt32BigEndian(byte[] data, int offset) {
      return data[offset] << 24 | data[offset + 1] << 16 | data[offset + 2] << 8 | data[offset + 3];
    }
  }
}
