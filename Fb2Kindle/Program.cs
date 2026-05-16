using Microsoft.Win32;
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
      Util.WriteLine($"\t-register: add explorer integration (context menu)");
      Util.WriteLine($"\t-unregister: remove explorer integration");
      // Util.WriteLine("\t-preview: keep generated source files");
      // Util.WriteLine("\t-debug: keep all generated files");
      Util.WriteLine();
      Util.WriteLine("\t-d: delete source file after successful conversion");
      Util.WriteLine("\t-u or -update: update application to the latest version. You can combine it with the `-save` option to enable auto-update on every run");
      Util.WriteLine("\t-s: add sequence and number to the document title");
      Util.WriteLine("\t-c (same as -c1) or -c2: use compression (slow)");
      Util.WriteLine("\t-dc: DropCaps mode");
      Util.WriteLine("\t-ntoc: no table of content");
      Util.WriteLine("\t-nch: no chapters");
      Util.WriteLine();
      Util.WriteLine("\t-optimizeSource: optimize images in source file (decrease to 824x1200 by default)");
      Util.WriteLine("\t-optimize: optimize images in target (decrease to 824x1200 by default)");
      Util.WriteLine("\t-ni: no images");
      Util.WriteLine("\t-g: grayscale images");
      Util.WriteLine("\t-jpeg: save images in jpeg");

      Util.WriteLine();
    }

    private static void ShowMainInfo() {
      //Console.Clear();
      Util.Write($"{Updater.ApplicationName} {(Environment.Is64BitProcess ? "x64" : "x32")} version: ");
      Util.Write(Updater.CurrentVersion, Util.StatusColor);
#if DEBUG
      Util.Write(" (DEBUG version) ", Util.WarningColor);
#endif
      Util.WriteLine();
    }

    private static void Register(string exePath) {

      Unregister(true);

      var fileExtension = ".fb2";
      //string baseKey = $@"SystemFileAssociations\{fileExtension}\shell\Fb2Kindle";
      //using (var mainKey = Registry.ClassesRoot.CreateSubKey(baseKey)) {
      //  //mainKey.SetValue("", "Fb2Kindle");
      //  mainKey.SetValue("MUIVerb", "Fb2Kindle");
      //  mainKey.SetValue("Icon", exePath);
      //  mainKey.SetValue("subcommands", "");
      //}
      //using (var subShell = Registry.ClassesRoot.CreateSubKey(baseKey + @"\shell")) {
      //  using (var key = subShell.CreateSubKey("a_convert_mobi")) {
      //    key.SetValue("", "Convert to .mobi");
      //    //0x08	Entry is a separator. Consecutive separators are collapsed into a single separator
      //    //0x10  Shows a UAC shield next to the menu item
      //    //0x20  Show a separator above this item
      //    //0x40  Show a separator below this item
      //    //mobiKey.SetValue("CommandFlags", 0x00000040);
      //    using (var cmdKey = key.CreateSubKey("command")) {
      //      cmdKey.SetValue("", $"\"{exePath}\" \"%1\"");
      //    }
      //  }
      //  using (var key = subShell.CreateSubKey("b_convert_epub")) {
      //    key.SetValue("", "Convert to .epub");
      //    using (var cmdKey = key.CreateSubKey("command")) {
      //      cmdKey.SetValue("", $"\"{exePath}\" \"%1\" -epub");
      //    }
      //  }
      //}

      string fileType = Registry.GetValue($@"HKEY_CLASSES_ROOT\{fileExtension}", "", null) as string;
      if (string.IsNullOrEmpty(fileType)) {
        fileType = fileExtension.TrimStart('.') + "_auto_file";
        Registry.SetValue($@"HKEY_CLASSES_ROOT\{fileExtension}", "", fileType);
      }

      static void AddSubItems(RegistryKey key, string baseCommand) {
        using (var subKey = key.CreateSubKey(@"shell\convert_epub")) {
          subKey.SetValue("", "Convert to .epub");
          using (var cmdKey = subKey.CreateSubKey("command")) {
            cmdKey.SetValue("", $"{baseCommand} -epub");
          }
        }
#if DEBUG
        using (var subKey = key.CreateSubKey(@"shell\preview")) {
          subKey.SetValue("", "Create preview");
          using (var cmdKey = subKey.CreateSubKey("command")) {
            cmdKey.SetValue("", $"{baseCommand} -test -preview -optimize");
          }
        }
#endif
        using (var subKey = key.CreateSubKey(@"shell\convert_mobi")) {
          subKey.SetValue("", "Convert to .mobi");
          using (var cmdKey = subKey.CreateSubKey("command")) {
            cmdKey.SetValue("", $"{baseCommand} -optimize");
          }
        }
        using (var subKey = key.CreateSubKey(@"shell\optimize")) {
          subKey.SetValue("", "Optimize source");
          using (var cmdKey = subKey.CreateSubKey("command")) {
            cmdKey.SetValue("", $"{baseCommand} -optimizeSource");
          }
        }
      }

      using (var key = Registry.ClassesRoot.CreateSubKey($@"{fileType}\shell\Fb2Kindle")) {
        key.SetValue("MUIVerb", "Fb2Kindle");
        key.SetValue("Icon", exePath);
        key.SetValue("SubCommands", "");
        AddSubItems(key, $"\"{exePath}\" \"%1\"");
      }
      using (var key = Registry.ClassesRoot.CreateSubKey(@"Directory\shell\Fb2Kindle")) {
        key.SetValue("MUIVerb", "Fb2Kindle");
        key.SetValue("Icon", exePath);
        key.SetValue("SubCommands", "");
        AddSubItems(key, $"\"{exePath}\" \"%1\\*.fb2\" -r -j");
      }

      Util.WriteLine("Context menus successfully added.", Util.MessageColor);
    }

    static void Unregister(bool silent = false) {
      try {
        string fileExtension = ".fb2";
        string fileType = Registry.GetValue($@"HKEY_CLASSES_ROOT\{fileExtension}", "", null) as string;
        if (string.IsNullOrEmpty(fileType))
          fileType = fileExtension.TrimStart('.') + "_auto_file";

        //Registry.ClassesRoot.DeleteSubKeyTree($@"SystemFileAssociations\{fileType}\shell\Fb2Kindle", false);
        //Registry.LocalMachine.DeleteSubKeyTree($@"SOFTWARE\Classes\SystemFileAssociations\{fileType}\shell\Fb2Kindle", false);
        //Registry.LocalMachine.DeleteSubKeyTree($@"SOFTWARE\Classes\SystemFileAssociations\{fileExtension}\shell\Fb2Kindle", false);

        Registry.ClassesRoot.DeleteSubKeyTree($@"{fileType}\shell\Fb2Kindle", false);
        Registry.ClassesRoot.DeleteSubKeyTree(@"Directory\shell\Fb2Kindle", false);

        if (!silent)
          Util.WriteLine("Context menus successfully removed.", Util.MessageColor);
      }
      catch (Exception ex) {
        if (!silent)
          Util.WriteLine("Error while removing context menus: " + ex.Message, Util.ErrorColor);
      }
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

        var settingsFile = Path.ChangeExtension(Updater.CurrentFileLocation, ".json");
        options = new AppOptions {
          Config = JsonSerializeHelper.ReadJsonFile<Config>(settingsFile) ?? new Config()
        };
        var appPath = options.AppPath;
        //var settingsFile = Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, ".xml");
        //var currentSettings = XmlSerializerHelper.DeserializeFile<DefaultOptions>(settingsFile) ?? new DefaultOptions();
        var bookPath = string.Empty;

        if (args.Length == 0) {
          ShowHelpText();
          Util.WriteLine("\nDo you want to integrate this app with Windows Explorer (add context menu)?", Util.StatusColor);
          Util.WriteLine("Press Enter to confirm, or any other key to skip...", Util.WarningColor);
          if (Console.ReadKey().Key == ConsoleKey.Enter) {
            Register(Updater.CurrentFileLocation);
          }
          Util.WriteLine("\nDo you want to process all local files recursively using default settings?", Util.StatusColor);
          Util.WriteLine("Press Enter to continue, or any other key to exit...", Util.WarningColor);
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
          else if (args[0] == "register") {
            Register(Updater.CurrentFileLocation);
            return;
          }
          else if (args[0] == "unregister") {
            Unregister();
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
              case "-optimize":
                options.Config.OptimizeImages = true;
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
                    Util.WriteLine("css styles file not found", Util.ErrorColor);
                    return;
                  }
                  options.Css = File.ReadAllText(cssFile, Encoding.UTF8);
                  if (string.IsNullOrEmpty(options.Css)) {
                    Util.WriteLine("css styles file is empty", Util.ErrorColor);
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
#if DEBUG
              case "-debug":
                options.CleanupMode = ConverterCleanupMode.No;
                options.UseSourceAsTempFolder = true;
                break;
#endif
              case "-epub":
                options.Epub = true;
                break;
              case "-test":
                options.Test = true;
                break;
              case "-optimizesource":
                options.OptimizeSource = true;
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
          Util.WriteLine("No input file", Util.ErrorColor);
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
        Util.WriteLine(ex.Message, Util.ErrorColor);
      }
      finally {
        if (processedFiles > 0) {
          var timeWasted = DateTime.Now - startedTime;
          Util.Write($"\nProcessed ", Util.InfoColor);
          Util.Write($"{processedFiles}", Util.MessageColor);
          Util.Write(" files in: ", Util.InfoColor);
          Util.WriteLine($"{timeWasted:G}", Util.MessageColor);
        }
        else {
          Util.WriteLine("\nNo files processed", Util.WarningColor);
        }

        if (wait) {
          Util.WriteLine("\nPress any key to continue...", Util.InfoColor);
          Console.ReadKey();
        }
      }

      if (options?.Config != null && options.Config.CheckUpdates) {
        Updater.Subscribe(
          (message, isError) => { Util.WriteLine(message, isError ? Util.ErrorColor : Util.InfoColor); },
          message => { Util.WriteLine(message, Util.StatusColor); return true; }
        );
        Console.WriteLine("\nChecking for updates...");
        Updater.CheckForUpdates(Updater.CheckUpdatesMode.AutoUpdate);
      }
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
