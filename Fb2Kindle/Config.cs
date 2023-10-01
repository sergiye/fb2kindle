using System;

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
    public bool NoToc { get; set; }
    public byte CompressionLevel { get; set; }
    public bool AddSequenceInfo { get; set; }
    public bool Grayscaled { get; set; }
    public bool Jpeg { get; set; }

    public string SmtpServer { get; set; } = "smtp.gmail.com";
    public int SmtpPort { get; set; } = 587;
    public string SmtpLogin { get; set; } = "user@gmail.com";
    public string SmtpPassword { get; set; } = "password";
    public int SmtpTimeout { get; set; } = 100000;

    public bool CheckUpdates { get; set; }
  }

  internal class AppOptions {
    
    internal Config Config { get; set; }

    internal ConverterCleanupMode CleanupMode { get; set; }
    internal bool UseSourceAsTempFolder { get; set; }
    internal bool Epub { get; set; }
    internal string MailTo { get; set; }
    internal bool DetailedOutput { get; set; } = true;
    internal string Css { get; set; }
    
    internal string AppPath { get; } = Util.GetAppPath();
    internal string TargetName { get; set; }
    internal string TempFolder { get; set; }
  }
}