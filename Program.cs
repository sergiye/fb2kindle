using System;
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
            Console.WriteLine("-o: show detailed output");
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
            var ver = Assembly.GetExecutingAssembly().GetName().Version;
            Console.WriteLine(assembly.GetName().Name + " Version: " + ver.ToString(3) + "; Build time: " + GetBuildTime(ver).ToString("yyyy/MM/dd HH:mm:ss"));
            var title = GetAttribute<AssemblyTitleAttribute>(assembly);
            if (title != null)
                Console.WriteLine(title.Title);
            Console.WriteLine();
        }

        private static DateTime GetBuildTime(Version ver)
        {
            var buildTime = new DateTime(2000, 1, 1).AddDays(ver.Build).AddSeconds(ver.Revision * 2);
            if (TimeZone.IsDaylightSavingTime(DateTime.Now, TimeZone.CurrentTimeZone.GetDaylightChanges(DateTime.Now.Year)))
                buildTime = buildTime.AddHours(1);
            return buildTime;
        }

        private static T GetAttribute<T>(ICustomAttributeProvider assembly, bool inherit = false) where T : Attribute
        {
            var attr = assembly.GetCustomAttributes(typeof(T), inherit);
            foreach (var o in attr)
                if (o is T)
                    return o as T;
            return null;
        }

        [STAThread]
        public static void Main(string[] args)
        {
            var wait = false;
            try
            {
                ShowMainInfo();
                if (args.Length == 0)
                {
                    ShowHelpText();
                    return;
                }
                var executingPath = Path.GetDirectoryName(Application.ExecutablePath);
                var settingsFile = executingPath + @"\config.xml";
                var currentSettings = Convertor.ReadObjectFromFile<DefaultOptions>(settingsFile) ?? new DefaultOptions();
                var bookPath = string.Empty;
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
                            currentSettings.detailedOutput = true;
                            break;
                        default:
                            if (j == 0)
                                bookPath = args[j];
                            break;
                    }
                }
                if (currentSettings.save)
                    Convertor.WriteObjectToFile(settingsFile, currentSettings, true);

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
    }
}