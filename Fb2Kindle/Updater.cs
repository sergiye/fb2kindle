using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;

namespace Fb2Kindle {
  public class GitHubRelease {
    public string Tag_name { get; set; }
    public string Name { get; set; }
    public bool Prerelease { get; set; }
    public Asset[] Assets { get; set; }
  }

  public class Asset {
    public string Name { get; set; }
    // public string content_type { get; set; }
    public string State { get; set; }
    // public long size { get; set; }
    public string Browser_download_url { get; set; }
  }

  internal static class Updater {
    private const string GITHUB_LANDING_PAGE = "sergiye/fb2kindle";
    private static readonly string selfFileName;

    internal static readonly string AppName;
    internal static readonly string CurrentVersion;
    internal static readonly string CurrentFileLocation;

    static Updater() {
      var asm = Assembly.GetExecutingAssembly();
      AppName = asm.GetName().Name;
      // AppTitle = GetAttribute<AssemblyTitleAttribute>(asm)?.Title;
      CurrentVersion = asm.GetName().Version.ToString(3);
      CurrentFileLocation = asm.Location;
      selfFileName = Path.GetFileName(CurrentFileLocation);
    }

    // private static T GetAttribute<T>(ICustomAttributeProvider assembly, bool inherit = false) where T : Attribute {
    //   foreach (var o in assembly.GetCustomAttributes(typeof(T), inherit))
    //     if (o is T attribute)
    //       return attribute;
    //   return null;
    // }

    private static string GetJsonData(string uri, int timeout = 10, string method = "GET") {
      var request = (HttpWebRequest) WebRequest.Create(uri);
      request.Method = method;
      request.Timeout = timeout * 1000;
      request.UserAgent =
        "Mozilla/5.0 (Windows NT 11.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/100.0.500.27 Safari/537.36";
      //request.Accept = "text/xml,text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
      request.ContentType = "application/json; charset=utf-8";
      using (var webResp = request.GetResponse()) {
        using (var stream = webResp.GetResponseStream()) {
          if (stream == null) return null;
          var answer = new StreamReader(stream, Encoding.UTF8);
          var result = answer.ReadToEnd();
          return result;
        }
      }
    }

    internal static void CheckForUpdates(bool silent) {
      string newVersionUrl = null;
      bool update;
      try {
        Util.WriteLine();
        Util.Write("Checking for updates... ", ConsoleColor.White);
        var jsonString = GetJsonData($"https://api.github.com/repos/{GITHUB_LANDING_PAGE}/releases").TrimEnd();
        var releases = jsonString.FromJson<GitHubRelease[]>();
        if (releases == null || releases.Length == 0)
          throw new Exception("Error getting list of releases.");

        var newVersion = releases[0].Tag_name;
        newVersionUrl = releases[0].Assets
          .FirstOrDefault(a => selfFileName.Equals(a.Name, StringComparison.OrdinalIgnoreCase))
          ?.Browser_download_url;

        if (newVersionUrl == null) {
          if (!silent)
            Util.WriteLine("Error getting released asset information.", ConsoleColor.White);
          return;
        }
        
        if (string.Compare(CurrentVersion, newVersion, StringComparison.Ordinal) >= 0) {
          Util.WriteLine("You have the latest version.", ConsoleColor.White);
          return;
        }

        Util.WriteLine($"New version found: {newVersion}, app will be updated after close.", ConsoleColor.White);
        update = true;
      }
      catch (Exception ex) {
        if (!silent)
          Util.WriteLine($"Error checking for a new version: {ex.Message}", ConsoleColor.Red);
        update = false;
      }

      if (!update) return;

      try {
        var tempPath = Path.GetTempPath();
        var updateFilePath = $"{tempPath}{selfFileName}{Environment.TickCount}";

        using (var wc = new WebClient())
          wc.DownloadFile(newVersionUrl, updateFilePath);

        var cmdFilePath = Path.GetTempPath() + $"{selfFileName}_updater.cmd";
        using (var batFile = new StreamWriter(File.Create(cmdFilePath))) {
          batFile.WriteLine("@ECHO OFF");
          batFile.WriteLine("TIMEOUT /t 3 /nobreak > NUL");
          batFile.WriteLine("TASKKILL /IM \"{0}\" > NUL", selfFileName);
          batFile.WriteLine("MOVE \"{0}\" \"{1}\"", updateFilePath, CurrentFileLocation);
          batFile.WriteLine("DEL \"%~f0\"");
        }

        var startInfo = new ProcessStartInfo(cmdFilePath) {
          CreateNoWindow = true,
          UseShellExecute = false,
          WorkingDirectory = tempPath
        };
        Process.Start(startInfo);
        Environment.Exit(0);
      }
      catch (Exception ex) {
        if (!silent)
          Util.WriteLine($"Error downloading new version: {ex.Message}", ConsoleColor.Red);
      }
    }
  }
}