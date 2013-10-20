using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace Fb2Kindle
{
    class Program
    {
        public static void ShowHelpText()
        {
            Console.WriteLine();
            Console.WriteLine(Assembly.GetExecutingAssembly().GetName().Name + " <book.fb2> [-css <styles.css>] [-d] [-nb] [-ni]");
            Console.WriteLine();
            Console.WriteLine("<book.fb2>: input fb2 file");
            Console.WriteLine("-css <styles.css>: styles used in destination book");
            Console.WriteLine("-d: delete source file after convertion");
            Console.WriteLine("-nb: no big letters at the chapter start");
            Console.WriteLine("-nch: no chapters");
            Console.WriteLine("-ni: no images");
            Console.WriteLine("-ntoc: no table of content");
            Console.WriteLine("-c: use compression (slow)");
            Console.WriteLine("-o: hide detailed output");
            Console.WriteLine("-s: add sequence and number to title");
            Console.WriteLine("-save: save parameters to be used at the next start");
            Console.WriteLine("-a: process all files in current folder");
            Console.WriteLine("-r: process files in subfolders (work with -a key)");
            Console.WriteLine("-w: wait for key press on finish");
            Console.WriteLine();
        }

        public static void ShowMainInfo()
        {
            //            Console.Clear();
            Console.WriteLine();
            var assembly = Assembly.GetExecutingAssembly();
            var ver = assembly.GetName().Version;
            Console.WriteLine(assembly.GetName().Name + " Version: " + ver.ToString(3) + "; Build time: " + Util.GetBuildTime(ver).ToString("yyyy/MM/dd HH:mm:ss"));
            var title = Util.GetAttribute<AssemblyTitleAttribute>(assembly);
            if (title != null)
                Console.WriteLine(title.Title);
            Console.WriteLine();
        }

        [STAThread]
        public static void Main(string[] args)
        {
            var wait = false;
            try
            {
                ShowMainInfo();

                var executingPath = Path.GetDirectoryName(Application.ExecutablePath);
                var settingsFile = executingPath + @"\config.xml";
                var currentSettings = Util.ReadObjectFromFile<DefaultOptions>(settingsFile) ?? new DefaultOptions();
                var bookPath = string.Empty;

                if (args.Length == 0)
                {
                    if (!Debugger.IsAttached)
                    {
                        Console.Write("Process all files with default settings? Enter 'y' to continue: ");
                        if (Console.ReadLine() != "y")
                        {
                            ShowHelpText();
                            return;
                        }
                    }
                    currentSettings.all = true;
                    currentSettings.recursive = true;
                    currentSettings.noBig = true;
                    currentSettings.addSequence = true;
                    currentSettings.ntoc = true;
                    wait = !Debugger.IsAttached;
                }
                else
                {
                    wait = ParseInputParameters(args, currentSettings, ref bookPath);
                }
                if (currentSettings.save)
                    Util.WriteObjectToFile(settingsFile, currentSettings, true);

                var conv = new Convertor(currentSettings, executingPath);
                if (currentSettings.all)
                {
                    var searchOptions = currentSettings.recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                    var files = Directory.GetFiles(executingPath, "*.fb2", searchOptions);
                    if (files.Length == 0)
                        Console.WriteLine("No fb2 files found");
                    foreach (var file in files)
                        conv.ConvertBook(file);
                }
                else
                {
                    if (string.IsNullOrEmpty(bookPath) || !File.Exists(bookPath))
                    {
                        if (string.IsNullOrEmpty(bookPath))
                            Console.WriteLine("No input file");
                        else
                            Console.WriteLine("File not found: " + bookPath);
                    }
                    else
                        conv.ConvertBook(bookPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            if (wait)
            {
                Console.WriteLine();
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
        }

        private static bool ParseInputParameters(string[] args, DefaultOptions currentSettings, ref string bookPath)
        {
            var wait = false;
            for (var j = 0; j < args.Length; j++)
            {
                switch (args[j].ToLower().Trim())
                {
                    case "-css":
                        if (args.Length > (j + 1))
                        {
                            currentSettings.defaultCSS = args[j + 1];
                            j++;
                        }
                        break;
                    case "-d":
                        currentSettings.deleteOrigin = true;
                        break;
                    case "-nb":
                        currentSettings.noBig = true;
                        break;
                    case "-nch":
                        currentSettings.nch = true;
                        break;
                    case "-ni":
                        currentSettings.noImages = true;
                        break;
                    case "-ntoc":
                        currentSettings.ntoc = true;
                        break;
                    case "-save":
                        currentSettings.save = true;
                        break;
                    case "-w":
                        wait = true;
                        break;
                    case "-a":
                        currentSettings.all = true;
                        break;
                    case "-r":
                        currentSettings.recursive = true;
                        break;
                    case "-c":
                        currentSettings.compression = true;
                        break;
                    case "-o":
                        currentSettings.detailedOutput = false;
                        break;
                    case "-s":
                        currentSettings.addSequence = true;
                        break;
                    default:
                        if (j == 0)
                            bookPath = args[j];
                        break;
                }
            }
            return wait;
        }
    }
}