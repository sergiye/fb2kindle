using System;
using System.Security.Cryptography;
using System.Text;

namespace jail {
  public static class Extensions {

    public static string Shorten(this string str, int maxLen) {
      return string.IsNullOrWhiteSpace(str) ? str : str.Length <= maxLen ? str : str.Substring(0, maxLen - 3) + "...";
    }

    public static string ToFileSizeStr(this long size) {
      string[] sizes = {"B", "KB", "MB", "GB", "TB"};
      double total = size;
      var order = 0;
      while (total >= 1024 && order < sizes.Length - 1) {
        order++;
        total = total / 1024;
      }
      return string.Format("{0:0.##} {1}", total, sizes[order]);
    }

    #region Hash

    public enum HashType {
      Md5,
      Sha1,
      Sha256
    }

    private static HashAlgorithm GetHashAlgorithm(HashType hashType) {
      switch (hashType) {
        case HashType.Sha1:
          return new SHA1Managed();
        case HashType.Sha256:
          return new SHA256Managed();
        //case HashType.Md5:
        default:
          return MD5.Create();
      }
    }

    public static string GetHash(this string input, HashType hashType = HashType.Md5) {
      if (string.IsNullOrWhiteSpace(input))
        return null;
      using (var alg = GetHashAlgorithm(hashType)) {
        return BitConverter.ToString(alg.ComputeHash(Encoding.UTF8.GetBytes(input))).Replace("-", "").ToLower();
      }
    }

    #endregion Hash
  }
}