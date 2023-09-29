using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace Fb2Kindle {
  static class Program {
    private static void ShowHelpText(Assembly asm) {
      Util.WriteLine();
      Util.WriteLine(asm.GetName().Name + " <path> [-css <styles.css>] [-d] [-ni] [-mailto:recipient@mail.org]");
      Util.WriteLine();
      Util.WriteLine("<path>: input fb2 file or files mask (ex: *.fb2) or path to *fb2 files");
      Util.WriteLine("-epub: create file in epub format");
      Util.WriteLine("-css <styles.css>: styles used in destination book");
      Util.WriteLine("-d: delete source file after successful convertion");
      Util.WriteLine("-c: use compression (slow)");
      Util.WriteLine("-o: hide detailed output");
      Util.WriteLine("-s: add sequence and number to title");
      Util.WriteLine("-ni: no images");
      Util.WriteLine("-dc: DropCaps mode");
      Util.WriteLine("-g: grayscaled images");
      Util.WriteLine("-jpeg: save images in jpeg");
      Util.WriteLine("-ntoc: no table of content");
      Util.WriteLine("-nch: no chapters");

      Util.WriteLine("-mailto: - send document to email (kindle delivery)");
      Util.WriteLine("-epub: send file as .epub (experimental)");

      Util.WriteLine("-a: all fb2 books in app folder");
      Util.WriteLine("-r: process files in subfolders (work with -a key)");
      Util.WriteLine("-j: join files from each folder to the single book");

      Util.WriteLine("-save: save parameters to be used at the next start");
      Util.WriteLine("-w: wait for key press on finish");
      
      Util.WriteLine("-preview: keep generated source files");
      Util.WriteLine("-debug: keep all generated source files");
      Util.WriteLine("-u or -update: update application to the latest version");
      Util.WriteLine();
    }

    private static void ShowMainInfo(Assembly asm) {
      //            Console.Clear();
      Util.WriteLine();
      var ver = asm.GetName().Version;
      Util.WriteLine($"{asm.GetName().Name} Version: {ver.ToString(3)}; Build time: {Util.GetBuildTime(ver):yyyy/MM/dd HH:mm:ss}", ConsoleColor.White);
      var title = Util.GetAttribute<AssemblyTitleAttribute>(asm);
      if (title != null)
        Util.WriteLine(title.Title, ConsoleColor.White);
      Util.WriteLine();
    }

    [STAThread]
    public static void Main(string[] args) {
      const string allBooksPattern = "*.fb2";
      var wait = false;
      var join = false;
      var save = false;
      var recursive = false;
      var detailedOutput = true;
      var startedTime = DateTime.Now;
      DefaultOptions currentSettings = null;
      try {
        var asm = Assembly.GetExecutingAssembly();
        ShowMainInfo(asm);

        var appPath = Util.GetAppPath();
        var settingsFile = Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, ".json");
        currentSettings = SerializerHelper.ReadJsonFile<DefaultOptions>(settingsFile) ?? new DefaultOptions();
        //var settingsFile = Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, ".xml");
        //var currentSettings = XmlSerializerHelper.DeserializeFile<DefaultOptions>(settingsFile) ?? new DefaultOptions();
        var bookPath = string.Empty;
        string cssStyles = null;
        string mailTo = null;

        if (args.Length == 0) {
          ShowHelpText(asm);
          Util.Write("Process all files with default parameters (-a -r -w)? Press 'Enter' to continue: ", ConsoleColor.White);
          if (Console.ReadKey().Key != ConsoleKey.Enter)
            return;
          wait = true;
          bookPath = allBooksPattern;
          recursive = true;
        }
        else {
          if (args[0] == "newconsole") {
            string parameters = string.Empty;
            if (args.Length > 1) {
              for (var i = 1; i < args.Length; i++) {
                if (args[i].Contains(" "))
                  parameters += $"\"{args[i]}\" ";
                else
                  parameters += $"{args[i]} ";
              }
            }
            //Console.WriteLine($"Executing external with parameters: '{parameters}'...");
            Process.Start(asm.Location, parameters);
            return;
          }

          Console.WriteLine($"Executing '{asm.Location}' with parameters: '{string.Join(" ", args)}'");
          for (var j = 0; j < args.Length; j++) {
            switch (args[j].ToLower().Trim()) {
              case "-u":
              case "-update":
                currentSettings.CheckUpdates = true;
                break;
              case "-preview":
                currentSettings.CleanupMode = ConverterCleanupMode.Partial;
                currentSettings.UseSourceAsTempFolder = true;
                break;
              case "-debug":
                currentSettings.CleanupMode = ConverterCleanupMode.No;
                currentSettings.UseSourceAsTempFolder = true;
                break;
              case "-nch":
                currentSettings.NoChapters = true;
                break;
              case "-dc":
                currentSettings.DropCaps = true;
                break;
              case "-ni":
                currentSettings.NoImages = true;
                break;
              case "-g":
                currentSettings.Grayscaled = true;
                break;
              case "-jpeg":
                currentSettings.Jpeg = true;
                break;
              case "-ntoc":
                currentSettings.NoToc = true;
                break;
              case "-c":
              case "-c1":
                currentSettings.CompressionLevel = 1;
                break;
              case "-c2":
                currentSettings.CompressionLevel = 2;
                break;
              case "-s":
                currentSettings.Sequence = true;
                break;
              case "-d":
                currentSettings.DeleteOriginal = true;
                break;
              case "-epub":
                currentSettings.Epub = true;
                break;
              case "-save":
                save = true;
                break;
              case "-w":
                wait = true;
                break;
              case "-r":
                recursive = true;
                break;
              case "-a":
                bookPath = allBooksPattern;
                break;
              case "-j":
                join = true;
                break;
              case "-o":
                detailedOutput = false;
                break;
              case "-css":
                if (args.Length > (j + 1)) {
                  var cssFile = args[j + 1];
                  if (!File.Exists(cssFile))
                    cssFile = appPath + "\\" + cssFile;
                  if (!File.Exists(cssFile)) {
                    Util.WriteLine("css styles file not found", ConsoleColor.Red);
                    return;
                  }
                  cssStyles = File.ReadAllText(cssFile, Encoding.UTF8);
                  if (string.IsNullOrEmpty(cssStyles)) {
                    Util.WriteLine("css styles file is empty", ConsoleColor.Red);
                    return;
                  }
                  j++;
                }
                break;
              default:
                if (j == 0)
                  bookPath = args[j];
                break;
            }
            if (args[j].StartsWith("-mailto:")) {
              mailTo = args[j].Split(':')[1];
            }
          }
        }
        if (string.IsNullOrEmpty(bookPath)) {
          Util.WriteLine("No input file", ConsoleColor.Red);
          return;
        }
        if (save) currentSettings.ToJsonFile(settingsFile);
        //if (save) currentSettings.ToXmlFile(settingsFile, true);

        var workPath = Path.GetDirectoryName(bookPath);
        if (string.IsNullOrEmpty(workPath))
          workPath = appPath;
        else
          bookPath = Path.GetFileName(bookPath);
        if (string.IsNullOrEmpty(bookPath))
          bookPath = allBooksPattern;
        var conv = new Convertor(currentSettings, cssStyles, detailedOutput) { MailTo = mailTo };
        ProcessFolder(conv, workPath, bookPath, recursive, join);
      }
      catch (Exception ex) {
        Util.WriteLine(ex.Message, ConsoleColor.Red);
      }
      finally {
        var timeWasted = DateTime.Now - startedTime;
        Util.WriteLine();
        Util.WriteLine($"Time wasted: {timeWasted:G}", ConsoleColor.White);

        if (currentSettings is {CheckUpdates: true}) {
          Util.WriteLine();
          Util.WriteLine($"Checking for updates...", ConsoleColor.White);
          Updater.CheckForUpdates(false);
          Util.WriteLine();
        }
        
        if (wait) {
          Util.WriteLine();
          Util.WriteLine("Press any key to continue...", ConsoleColor.White);
          Console.ReadKey();
        }
      }
    }

    private static void ProcessFolder(Convertor conv, string workPath, string searchMask, bool recursive, bool join) {
      var files = new List<string>();
      files.AddRange(Directory.GetFiles(workPath, searchMask, SearchOption.TopDirectoryOnly));
      if (files.Count > 0) {
        files.Sort();
        if (join)
          conv.ConvertBookSequence(files.ToArray());
        else
          foreach (var file in files)
            conv.ConvertBook(file);
      }
      if (!recursive) return;
      foreach (var folder in Directory.GetDirectories(workPath))
        ProcessFolder(conv, folder, searchMask, true, join);
    }
  }
}