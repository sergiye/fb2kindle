using Microsoft.Win32;
using sergiye.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Fb2Kindle {

  static class Program {

    private const string UserClassesKey = @"Software\Classes";

    private static readonly string[] ConfigSwitches = [
      "-u", "-update", "-nch", "-dc", "-ni", "-optimize", "-g", "-jpeg", "-ntoc", "-c", "-c1", "-c2", "-s", "-d"
    ];

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
      Util.WriteLine("\t-mailto <user@mail.org>: send document to email (kindle send-by-email delivery, always in epub format, see `-save` option to configure SMTP server)");
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
      Util.WriteLine("\tAppend `-` to -d, -u, -s, -c, -dc, -ntoc, -nch, -optimize, -ni, -g or -jpeg to turn it off (ex: -d-), useful to override saved parameters");

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

      try {
        using (var key = Registry.CurrentUser.CreateSubKey($@"{UserClassesKey}\SystemFileAssociations\{fileExtension}\shell\Fb2Kindle")) {
          key.SetValue("MUIVerb", "Fb2Kindle");
          key.SetValue("Icon", exePath);
          key.SetValue("SubCommands", "");
          AddSubItems(key, $"\"{exePath}\" \"%1\"");
        }
        using (var key = Registry.CurrentUser.CreateSubKey($@"{UserClassesKey}\Directory\shell\Fb2Kindle")) {
          key.SetValue("MUIVerb", "Fb2Kindle");
          key.SetValue("Icon", exePath);
          key.SetValue("SubCommands", "");
          AddSubItems(key, $"\"{exePath}\" \"%1\\*.fb2\" -r -j");
        }

        Util.WriteLine("Context menus successfully added.", Util.MessageColor);
      }
      catch (Exception ex) {
        Util.WriteLine("Error while adding context menus: " + ex.Message, Util.ErrorColor);
      }
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

        Registry.CurrentUser.DeleteSubKeyTree($@"{UserClassesKey}\SystemFileAssociations\{fileExtension}\shell\Fb2Kindle", false);
        Registry.CurrentUser.DeleteSubKeyTree($@"{UserClassesKey}\Directory\shell\Fb2Kindle", false);
        //menus registered by older versions
        try {
          Registry.ClassesRoot.DeleteSubKeyTree($@"{fileType}\shell\Fb2Kindle", false);
          Registry.ClassesRoot.DeleteSubKeyTree(@"Directory\shell\Fb2Kindle", false);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException || ex is System.Security.SecurityException) {
          if (!silent)
            Util.WriteLine("Context menus added by an older version are registered for all users; run -unregister as administrator to remove them.", Util.WarningColor);
        }

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
        Config config = null;
        try {
          config = JsonExtensions.ReadJsonFile<Config>(settingsFile);
        }
        catch (Exception ex) {
          Util.WriteLine($"Settings file '{settingsFile}' is invalid and was ignored: {ex.Message}", Util.WarningColor);
        }
        options = new AppOptions {
          Config = config ?? new Config()
        };
        var appPath = options.AppPath;
        //var settingsFile = Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, ".xml");
        //var currentSettings = XmlSerializerHelper.DeserializeFile<DefaultOptions>(settingsFile) ?? new DefaultOptions();
        var bookPath = string.Empty;

        if (args.Length == 0) {
          ShowHelpText();
          Util.WriteLine("\nDo you want to integrate this app with Windows Explorer (add context menu)?", Util.StatusColor);
          Util.WriteLine("Press Enter to confirm, or any other key to skip...", Util.WarningColor);
          if (ReadKey() == ConsoleKey.Enter) {
            Register(Updater.CurrentFileLocation);
          }
          Util.WriteLine("\nDo you want to process all local files recursively using default settings?", Util.StatusColor);
          Util.WriteLine("Press Enter to continue, or any other key to exit...", Util.WarningColor);
          if (ReadKey() != ConsoleKey.Enter)
            return;
          wait = true;
          bookPath = Path.Combine(appPath, allBooksPattern);
          recursive = true;
        }
        else {
          if (args[0] == "newconsole") {
            var parameters = string.Join(" ", args.Skip(1).Select(QuoteArgument));
            //Console.WriteLine($"Executing external with parameters: '{parameters}'...");
            Process.Start(Updater.CurrentFileLocation, parameters);
            return;
          }
          else if (IsCommand(args[0], "register")) {
            Register(Updater.CurrentFileLocation);
            return;
          }
          else if (IsCommand(args[0], "unregister")) {
            Unregister();
            return;
          }

          Console.WriteLine($"Executing '{Updater.CurrentFileLocation}' with parameters: '{string.Join(" ", args)}'");
          for (var j = 0; j < args.Length; j++) {
            var arg = args[j].ToLower().Trim();
            var enable = true;
            if (arg.Length > 2 && arg.EndsWith("-") && ConfigSwitches.Contains(arg.Substring(0, arg.Length - 1))) {
              arg = arg.Substring(0, arg.Length - 1);
              enable = false;
            }
            switch (arg) {

              #region config

              case "-u":
              case "-update":
                options.Config.CheckUpdates = enable;
                break;
              case "-nch":
                options.Config.NoChapters = enable;
                break;
              case "-dc":
                options.Config.DropCaps = enable;
                break;
              case "-ni":
                options.Config.NoImages = enable;
                break;
              case "-optimize":
                options.Config.OptimizeImages = enable;
                break;
              case "-g":
                options.Config.Grayscaled = enable;
                break;
              case "-jpeg":
                options.Config.Jpeg = enable;
                break;
              case "-ntoc":
                options.Config.SkipToc = enable;
                break;
              case "-c":
              case "-c1":
                options.Config.CompressionLevel = (byte)(enable ? 1 : 0);
                break;
              case "-c2":
                options.Config.CompressionLevel = (byte)(enable ? 2 : 0);
                break;
              case "-s":
                options.Config.AddSequenceInfo = enable;
                break;
              case "-d":
                options.Config.DeleteOriginal = enable;
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
                else
                  Util.WriteLine("-css option requires a styles file path", Util.WarningColor);
                break;
              case "-mailto":
                if (args.Length > j + 1) options.MailTo = args[++j];
                else Util.WriteLine("-mailto option requires an email address", Util.WarningColor);
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
                bookPath = Path.Combine(appPath, allBooksPattern);
                break;
              case "-j":
                join = true;
                break;

              default:
                if (arg.StartsWith("-"))
                  Util.WriteLine($"Unknown option ignored: {args[j]}", Util.WarningColor);
                else if (string.IsNullOrEmpty(bookPath))
                  bookPath = args[j];
                else
                  Util.WriteLine($"Only one input path is supported, ignored: {args[j]}", Util.WarningColor);
                break;

              #endregion
            }
          }
        }
        if (save) {
          options.Config.ToJsonFile(settingsFile);
          Util.WriteLine($"Settings saved to {settingsFile}", Util.MessageColor);
        }
        if (string.IsNullOrEmpty(bookPath)) {
          if (!save)
            Util.WriteLine("No input file", Util.ErrorColor);
          return;
        }
        if (!string.IsNullOrWhiteSpace(options.MailTo) && !options.Epub) {
          Util.WriteLine("Send to Kindle does not accept MOBI anymore, EPUB will be created", Util.WarningColor);
          options.Epub = true;
        }

        if (Directory.Exists(bookPath))
          bookPath = Path.Combine(bookPath, allBooksPattern);
        var workPath = Path.GetDirectoryName(bookPath);
        if (string.IsNullOrEmpty(workPath))
          workPath = Environment.CurrentDirectory;
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

        if (wait && !Console.IsInputRedirected) {
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

    //follows the CommandLineToArgvW rules, so backslashes before quotes survive the round trip
    private static string QuoteArgument(string arg) {
      if (arg.Length > 0 && arg.IndexOfAny([' ', '\t', '"']) < 0)
        return arg;
      var result = new StringBuilder("\"");
      var backslashes = 0;
      foreach (var c in arg) {
        if (c == '\\') {
          backslashes++;
          continue;
        }
        result.Append('\\', c == '"' ? backslashes * 2 + 1 : backslashes);
        result.Append(c);
        backslashes = 0;
      }
      result.Append('\\', backslashes * 2);
      result.Append('"');
      return result.ToString();
    }

    private static ConsoleKey? ReadKey() {
      return Console.IsInputRedirected ? null : Console.ReadKey().Key;
    }

    private static bool IsCommand(string arg, string command) {
      return arg.Equals(command, StringComparison.OrdinalIgnoreCase) ||
             arg.Equals("-" + command, StringComparison.OrdinalIgnoreCase);
    }

    private static int ProcessFolder(Convertor conv, string workPath, string searchMask, bool recursive, bool join) {
      var processedFiles = 0;
      List<string> files;
      DirectoryInfo[] subFolders;
      try {
        files = Directory.GetFiles(workPath, searchMask, SearchOption.TopDirectoryOnly).ToList();
        subFolders = recursive ? new DirectoryInfo(workPath).GetDirectories() : [];
      }
      catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException) {
        Util.WriteLine($"Skipping folder '{workPath}': {ex.Message}", Util.WarningColor);
        return 0;
      }
      if (files.Count > 0) {
        files.Sort();
        if (join) {
          processedFiles += conv.ConvertBookSequence(files);
        }
        else {
          foreach (var file in files) {
            if (conv.ConvertBook(file))
              processedFiles++;
          }
        }
      }

      //junctions and symlinks can point back to a parent folder
      processedFiles += subFolders
        .Where(folder => (folder.Attributes & FileAttributes.ReparsePoint) == 0)
        .Sum(folder => ProcessFolder(conv, folder.FullName, searchMask, true, join));
      return processedFiles;
    }
  }
}
