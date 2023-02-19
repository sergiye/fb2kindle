using System;

namespace Fb2Kindle {
  public enum ConverterCleanupMode {
    Full = 0,
    Partial, //keep html files, styles & images
    No //for debug
  }

  [Serializable]
  public class DefaultOptions {

    public bool DeleteOriginal { get; set; }
    public bool NoChapters { get; set; }
    public bool DropCaps { get; set; }
    public bool NoImages { get; set; }
    public bool NoToc { get; set; }
    public byte CompressionLevel { get; set; }
    public bool Sequence { get; set; }
    public bool Grayscaled { get; set; }
    public bool Jpeg { get; set; }
    public ConverterCleanupMode CleanupMode { get; set; }
    public bool UseSourceAsTempFolder { get; set; }

    public string SmtpServer { get; set; }
    public int SmtpPort { get; set; }
    public string SmtpLogin { get; set; }
    public string SmtpPassword { get; set; }
    public int SmtpTimeout { get; set; }

    public DefaultOptions() {
      SmtpServer = "smtp.gmail.com";
      SmtpPort = 587;
      SmtpLogin = "user@gmail.com";
      SmtpPassword = "password";
      SmtpTimeout = 100000;
    }
  }
}