using sergiye.Common;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Fb2Kindle {
  
  static class Program {
    
    private static void ShowHelpText() {
      Util.WriteLine($"Usage: {Updater.ApplicationName} [options]");
      Util.WriteLine("Available options:");
      
      Util.WriteLine("\t<path>: input fb2 file path or files mask (ex: *.fb2) or path to .fb2 files");
      Util.WriteLine("\t-epub: create file in epub format");
      Util.WriteLine("\t-css <styles.css>: styles used in destination book");
      Util.WriteLine("\t-a: process all .fb2 books in app folder");
      Util.WriteLine("\t-r: process files in subfolders (work with -a key)");
      Util.WriteLine("\t-j: join files from each folder to the single book");
      Util.WriteLine("\t-o: hide detailed output");
      Util.WriteLine("\t-w: wait for key press on finish");
      Util.WriteLine("\t-mailto <user@mail.org>: send document to email (kindle send-by-email delivery, see `-save` option to configure SMTP server)");
      Util.WriteLine($"\t-save: save parameters (listed below) to be used at the next start (`{Updater.ApplicationName}.json` file)");
      // Util.WriteLine("\t-preview: keep generated source files");
      // Util.WriteLine("\t-debug: keep all generated files");
      Util.WriteLine();
      
      Util.WriteLine("\t-d: delete source file after successful conversion");
      Util.WriteLine("\t-u or -update: update application to the latest version. You can combine it with the `-save` option to enable auto-update on every run");
      Util.WriteLine("\t-s: add sequence and number to the document title");
      Util.WriteLine("\t-c (same as -c1) or -c2: use compression (slow)");
      Util.WriteLine("\t-ni: no images");
      Util.WriteLine("\t-dc: DropCaps mode");
      Util.WriteLine("\t-g: grayscale images");
      Util.WriteLine("\t-jpeg: save images in jpeg");
      Util.WriteLine("\t-ntoc: no table of content");
      Util.WriteLine("\t-nch: no chapters");
      
      Util.WriteLine();
    }

    private static void ShowMainInfo() {
      //Console.Clear();
      Util.Write($"{Updater.ApplicationName} {(Environment.Is64BitProcess ? "x64" : "x32")} version: ");
      Util.Write(Updater.CurrentVersion, ConsoleColor.DarkCyan);
#if DEBUG
      Util.Write(" (DEBUG version) ", ConsoleColor.DarkYellow);
#endif
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
      var processedFiles = 0;
      AppOptions options = null;
      try {
        ShowMainInfo();

        var appPath = Util.GetAppPath();
        var settingsFile = Path.ChangeExtension(Updater.CurrentFileLocation, ".json");
        options = new AppOptions {
          Config = SerializeHelper.ReadJsonFile<Config>(settingsFile) ?? new Config()
        };
        //var settingsFile = Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, ".xml");
        //var currentSettings = XmlSerializerHelper.DeserializeFile<DefaultOptions>(settingsFile) ?? new DefaultOptions();
        var bookPath = string.Empty;

        if (args.Length == 0) {
          ShowHelpText();
          Util.Write("Process all local files (-a) recursively (-r) with default parameters?\nPress 'Enter' to continue, or any other key to exit...", ConsoleColor.White);
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

              #region config
              
              case "-u":
              case "-update":
                options.Config.CheckUpdates = true;
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
                options.Config.SkipToc = true;
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
              
              #endregion
              
              #region options
              
              case "-css":
                if (args.Length > j + 1) {
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
              case "-mailto":
                if (args.Length > j + 1) options.MailTo = args[++j];
                break;
              case "-preview":
                options.CleanupMode = ConverterCleanupMode.Partial;
                options.UseSourceAsTempFolder = true;
                break;
              case "-debug":
                options.CleanupMode = ConverterCleanupMode.No;
                options.UseSourceAsTempFolder = true;
                break;
              case "-epub":
                options.Epub = true;
                break;
              case "-o":
                options.DetailedOutput = false;
                break;

              #endregion
              
              #region behavior
              
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
              
              default:
                if (j == 0)
                  bookPath = args[j];
                break;
              
              #endregion
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
        processedFiles = ProcessFolder(conv, workPath, bookPath, recursive, join);
      }
      catch (Exception ex) {
        Util.WriteLine(ex.Message, ConsoleColor.Red);
      }
      finally {
        if (processedFiles > 0) {
          var timeWasted = DateTime.Now - startedTime;
          Util.Write($"\nProcessed ", ConsoleColor.White);
          Util.Write($"{processedFiles}", ConsoleColor.Green);
          Util.Write(" files in: ", ConsoleColor.White);
          Util.WriteLine($"{timeWasted:G}", ConsoleColor.Green);
        }
        else {
          Util.WriteLine("\nNo files processed", ConsoleColor.DarkYellow);
        }

        if (wait) {
          Util.WriteLine("\nPress any key to continue...", ConsoleColor.White);
          Console.ReadKey();
        }
      }
        
      if (options?.Config != null && options.Config.CheckUpdates) 
        Updater.CheckForUpdates(true);
    }

    private static int ProcessFolder(Convertor conv, string workPath, string searchMask, bool recursive, bool join) {
      var processedFiles = 0;
      var files = Directory.GetFiles(workPath, searchMask, SearchOption.TopDirectoryOnly).ToList();
      if (files.Count > 0) {
        files.Sort();
        if (join) {
          conv.ConvertBookSequence(files);
          processedFiles += files.Count;
        }
        else {
          foreach (var file in files) {
            conv.ConvertBook(file);
            processedFiles++;
          }
        }
      }

      if (recursive)
        processedFiles += Directory.GetDirectories(workPath)
          .Sum(folder => ProcessFolder(conv, folder, searchMask, true, join));
      return processedFiles;
    }
  }
}