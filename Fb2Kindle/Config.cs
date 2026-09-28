using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using sergiye.Common;

namespace Fb2Kindle {
  internal enum ConverterCleanupMode {
    Full = 0,
    Partial = 1, //keep html files, styles & images
    No = 2 //for debug
  }

  [Serializable]
  public class Config {

    public bool DeleteOriginal { get; set; }
    public bool NoChapters { get; set; }
    public bool DropCaps { get; set; }
    public bool NoImages { get; set; }
    public bool SkipToc { get; set; }
    public byte CompressionLevel { get; set; }
    public bool AddSequenceInfo { get; set; }
    public bool OptimizeImages { get; set; }
    public int OptimizeImagesWidth { get; set; } = 824;
    public int OptimizeImagesHeight { get; set; } = 1200;
    public bool Grayscaled { get; set; }
    public bool Jpeg { get; set; }

    public string SmtpServer { get; set; } = "smtp.gmail.com";
    public int SmtpPort { get; set; } = 587;
    public string SmtpLogin { get; set; }
    public string SmtpPassword { get; set; }
    public int SmtpTimeout { get; set; } = 100000;

    public bool CheckUpdates { get; set; }

    private const string ProtectedPrefix = "dpapi:";

    internal string GetSmtpPassword() {
      if (string.IsNullOrEmpty(SmtpPassword) || !SmtpPassword.StartsWith(ProtectedPrefix))
        return SmtpPassword;
      try {
        var data = Convert.FromBase64String(SmtpPassword.Substring(ProtectedPrefix.Length));
        return Encoding.UTF8.GetString(ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser));
      }
      catch (Exception ex) when (ex is CryptographicException || ex is FormatException) {
        throw new InvalidOperationException("Unable to decrypt the SMTP password: it was encrypted by another Windows user or on another computer. Enter the password in the settings file again.", ex);
      }
    }

    internal bool ProtectSecrets() {
      if (string.IsNullOrEmpty(SmtpPassword) || SmtpPassword.StartsWith(ProtectedPrefix))
        return false;
      var data = ProtectedData.Protect(Encoding.UTF8.GetBytes(SmtpPassword), null, DataProtectionScope.CurrentUser);
      SmtpPassword = ProtectedPrefix + Convert.ToBase64String(data);
      return true;
    }
  }

  internal class AppOptions {

    internal Config Config { get; set; }

    internal ConverterCleanupMode CleanupMode { get; set; }
    internal bool UseSourceAsTempFolder { get; set; }
    internal bool Epub { get; set; }
    internal bool Test { get; set; }
    internal bool OptimizeSource { get; set; }
    internal string MailTo { get; set; }
    internal bool DetailedOutput { get; set; } = true;
    internal string Css { get; set; }

    internal string AppPath { get; } = Path.GetDirectoryName(Updater.CurrentFileLocation);
    internal string TargetName { get; set; }
    internal string TempFolder { get; set; }
    internal string DocumentTitle { get; set; }
  }
}
