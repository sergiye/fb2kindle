using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Fb2Kindle {
  static class Program {
    private static void ShowHelpText() {
      Util.WriteLine();
      Util.WriteLine(Updater.AppName + " <path> [-css <styles.css>] [-d] [-ni] [-mailto:recipient@mail.org]");
      Util.WriteLine();
      Util.WriteLine("<path>: input fb2 file or files mask (ex: *.fb2) or path to *fb2 files");
      
      Util.WriteLine("-epub: create file in epub format");
      Util.WriteLine("-css <styles.css>: styles used in destination book");
      Util.WriteLine("-a: all fb2 books in app folder");
      Util.WriteLine("-r: process files in subfolders (work with -a key)");
      Util.WriteLine("-j: join files from each folder to the single book");
      Util.WriteLine("-o: hide detailed output");
      Util.WriteLine("-w: wait for key press on finish");

      Util.WriteLine("-preview: keep generated source files");
      Util.WriteLine("-mailto: - send document to email (kindle delivery)");

      Util.WriteLine("-save: save parameters (listed below) to be used at the next start");
      
      Util.WriteLine("-d: delete source file after successful conversion");
      Util.WriteLine("-c: use compression (slow)");
      Util.WriteLine("-s: add sequence and number to title");
      Util.WriteLine("-ni: no images");
      Util.WriteLine("-dc: DropCaps mode");
      Util.WriteLine("-g: grayscale images");
      Util.WriteLine("-jpeg: save images in jpeg");
      Util.WriteLine("-ntoc: no table of content");
      Util.WriteLine("-nch: no chapters");
      Util.WriteLine("-u or -update: update application to the latest version");
      
      Util.WriteLine();
    }

    private static void ShowMainInfo() {
      //Console.Clear();
      Util.WriteLine();
      Util.WriteLine($"{Updater.AppName} Version: {Updater.CurrentVersion}", ConsoleColor.White);
      if (Updater.AppTitle != null)
        Util.WriteLine(Updater.AppTitle, ConsoleColor.White);
      Util.WriteLine();
    }

    [STAThread]
    public static void Main(string[] args) {
      const string allBooksPattern = "*.fb2";
      var wait = false;
      var join = false;
      var save = false;
      var recursive = false;
      var startedTime = DateTime.Now;
      AppOptions options = null;
      try {
        ShowMainInfo();

        var appPath = Util.GetAppPath();
        var settingsFile = Path.ChangeExtension(Updater.CurrentFileLocation, ".json");
        options = new AppOptions {
          Config = SerializerHelper.ReadJsonFile<Config>(settingsFile) ?? new Config()
        };
        //var settingsFile = Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, ".xml");
        //var currentSettings = XmlSerializerHelper.DeserializeFile<DefaultOptions>(settingsFile) ?? new DefaultOptions();
        var bookPath = string.Empty;

        if (args.Length == 0) {
          ShowHelpText();
          Util.Write("Process all files with default parameters (-a -r -w)? Press 'Enter' to continue: ", ConsoleColor.White);
          if (Console.ReadKey().Key != ConsoleKey.Enter)
            return;
          wait = true;
          bookPath = allBooksPattern;
          recursive = true;
        }
        else {
          if (args[0] == "newconsole") {
            var parameters = string.Empty;
            if (args.Length > 1) {
              for (var i = 1; i < args.Length; i++) {
                if (args[i].Contains(" "))
                  parameters += $"\"{args[i]}\" ";
                else
                  parameters += $"{args[i]} ";
              }
            }
            //Console.WriteLine($"Executing external with parameters: '{parameters}'...");
            Process.Start(Updater.CurrentFileLocation, parameters);
            return;
          }

          Console.WriteLine($"Executing '{Updater.CurrentFileLocation}' with parameters: '{string.Join(" ", args)}'");
          for (var j = 0; j < args.Length; j++) {
            switch (args[j].ToLower().Trim()) {
              case "-u":
              case "-update":
                options.Config.CheckUpdates = true;
                break;
              case "-preview":
                options.CleanupMode = ConverterCleanupMode.Partial;
                options.UseSourceAsTempFolder = true;
                break;
              case "-debug":
                options.CleanupMode = ConverterCleanupMode.No;
                options.UseSourceAsTempFolder = true;
                break;
              case "-nch":
                options.Config.NoChapters = true;
                break;
              case "-dc":
                options.Config.DropCaps = true;
                break;
              case "-ni":
                options.Config.NoImages = true;
                break;
              case "-g":
                options.Config.Grayscaled = true;
                break;
              case "-jpeg":
                options.Config.Jpeg = true;
                break;
              case "-ntoc":
                options.Config.NoToc = true;
                break;
              case "-c":
              case "-c1":
                options.Config.CompressionLevel = 1;
                break;
              case "-c2":
                options.Config.CompressionLevel = 2;
                break;
              case "-s":
                options.Config.AddSequenceInfo = true;
                break;
              case "-d":
                options.Config.DeleteOriginal = true;
                break;
              case "-epub":
                options.Epub = true;
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
                options.DetailedOutput = false;
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
                  options.Css = File.ReadAllText(cssFile, Encoding.UTF8);
                  if (string.IsNullOrEmpty(options.Css)) {
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
              options.MailTo = args[j].Split(':')[1];
            }
          }
        }
        if (string.IsNullOrEmpty(bookPath)) {
          Util.WriteLine("No input file", ConsoleColor.Red);
          return;
        }
        if (save) options.Config.ToJsonFile(settingsFile);

        var workPath = Path.GetDirectoryName(bookPath);
        if (string.IsNullOrEmpty(workPath))
          workPath = appPath;
        else
          bookPath = Path.GetFileName(bookPath);
        if (string.IsNullOrEmpty(bookPath))
          bookPath = allBooksPattern;
        var conv = new Convertor(options);
        ProcessFolder(conv, workPath, bookPath, recursive, join);
      }
      catch (Exception ex) {
        Util.WriteLine(ex.Message, ConsoleColor.Red);
      }
      finally {
        var timeWasted = DateTime.Now - startedTime;
        Util.WriteLine();
        Util.WriteLine($"Time wasted: {timeWasted:G}", ConsoleColor.White);

        if (wait) {
          Util.WriteLine();
          Util.WriteLine("Press any key to continue...", ConsoleColor.White);
          Console.ReadKey();
        }
      }
        
      if (options?.Config != null && options.Config.CheckUpdates) 
        Updater.CheckForUpdates(false);
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