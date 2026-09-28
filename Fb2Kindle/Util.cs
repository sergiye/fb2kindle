using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Fb2Kindle {

  internal static class Util {

    internal static string GetScriptFromResource(string resourceName) {
      using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Fb2Kindle.{resourceName}")) {
        if (stream == null) return null;
        using (var reader = new StreamReader(stream))
          return reader.ReadToEnd();
      }
    }

    internal static bool GetFileFromResource(string resourceName, string filename) {
      using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Fb2Kindle.{resourceName}")) {
        if (stream == null) return false;
        using (Stream file = File.Create(filename)) {
          var buffer = new byte[8 * 1024];
          int len;
          while ((len = stream.Read(buffer, 0, buffer.Length)) > 0)
            file.Write(buffer, 0, len);
          return true;
        }
      }
    }

    internal static void CopyDirectory(string sourceDirName, string destDirName, bool copySubDirs) {
      var dir = new DirectoryInfo(sourceDirName);
      if (!dir.Exists)
        throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + sourceDirName);
      var dirs = dir.GetDirectories();
      if (!Directory.Exists(destDirName))
        Directory.CreateDirectory(destDirName);
      var files = dir.GetFiles();
      foreach (var file in files)
        file.CopyTo(Path.Combine(destDirName, file.Name), true);
      if (!copySubDirs) return;
      foreach (var subDir in dirs)
        CopyDirectory(subDir.FullName, Path.Combine(destDirName, subDir.Name), true);
    }

    internal static string Value(IEnumerable<XElement> source, string defaultResult = null) {
      var value = source.Select(element => element.Value).FirstOrDefault();
      if (value == null || String.IsNullOrEmpty(value.Trim()))
        return defaultResult;
      return value.Trim();
    }

    internal static string AttributeValue(IEnumerable<XElement> source, XName name, string defaultResult = null) {
      var value = source.Select(element => (string)element.Attribute(name)).FirstOrDefault();
      if (value == null || String.IsNullOrEmpty(value.Trim()))
        return defaultResult;
      return value.Trim();
    }

    internal static XElement[] RenameTags(XElement root, string tagName, string newName, string className = null, bool clearData = false) {
      var list = root.Descendants(tagName).ToArray();
      foreach (var element in list)
        RenameTag(element, newName, className, clearData);
      return list;
    }

    internal static void RenameTag(XElement element, string newName, string className = null, bool clearData = false) {
      element.Name = newName;
      if (clearData) {
        element.Attributes().Remove();
        element.RemoveNodes();
      }
      if (!String.IsNullOrEmpty(className))
        element.SetAttributeValue("class", className);
    }

    internal static int StartProcess(string fileName, string args, bool addToConsole) {
      var startInfo = new ProcessStartInfo {
        FileName = fileName,
        Arguments = args,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true,
        StandardOutputEncoding = Encoding.UTF8
        //WindowStyle = ProcessWindowStyle.Hidden
      };

      // using (var process = Process.Start(startInfo)) {
      //   if (addToConsole) {
      //     while (!process.StandardOutput.EndOfStream)
      //       WriteLine(process.StandardOutput.ReadLine());
      //   }
      //   process.WaitForExit();
      //   return process.ExitCode;
      // }
      //
      // using (var process = Process.Start(startInfo)) {
      //   using (var reader = process.StandardOutput) {
      //     string result = reader.ReadToEnd();
      //     if (addToConsole) WriteLine(result);
      //   }
      //   return process.ExitCode;
      // }

      using (var process = new Process()) {
        process.StartInfo = startInfo;
        process.OutputDataReceived += (_, e) => {
          if (addToConsole) WriteLine(e.Data);
        };
        process.ErrorDataReceived += (_, e) => {
          if (addToConsole) WriteLine(e.Data, ErrorColor);
        };
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();
        return process.ExitCode;
      }
    }

    public const ConsoleColor ErrorColor = ConsoleColor.Red;
    public const ConsoleColor WarningColor = ConsoleColor.DarkYellow;
    public const ConsoleColor InfoColor = ConsoleColor.White;
    public const ConsoleColor StatusColor = ConsoleColor.DarkCyan;
    public const ConsoleColor MessageColor = ConsoleColor.Green;

    internal static void WriteLine(string message = null, ConsoleColor? color = null, ConsoleColor? backColor = null) {
      Write(message, color, backColor, true);
    }

    internal static void Write(string message = null, ConsoleColor? color = null, ConsoleColor? backColor = null, bool newLine = false) {
      if (backColor.HasValue)
        Console.BackgroundColor = backColor.Value;
      if (color.HasValue)
        Console.ForegroundColor = color.Value;
      if (newLine)
        Console.WriteLine(message);
      else
        Console.Write(message);
      Console.ResetColor();
    }

    internal static string GetValidFileName(string origin) {
      if (string.IsNullOrWhiteSpace(origin))
        throw new ArgumentException("File name can not be empty.");
      var result = Path.GetInvalidFileNameChars().Aggregate(origin, (current, c) => current.Replace(c, '-')).TrimEnd('.', ' ');
      if (result.Length == 0 || ReservedFileName.IsMatch(result))
        result = "_" + result;
      return result;
    }

    private static readonly Regex ReservedFileName = new Regex(@"^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(\..*)?$", RegexOptions.IgnoreCase);
  }
}
