using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace Fb2Kindle
{
    class Program
    {
        public static void ShowHelpText(Assembly asm)
        {
            Console.WriteLine();
            Console.WriteLine(asm.GetName().Name + " <book.fb2> [-css <styles.css>] [-d] [-nb] [-ni]");
            Console.WriteLine();
            Console.WriteLine("<book.fb2>: input fb2 file");
            Console.WriteLine("-css <styles.css>: styles used in destination book");
            Console.WriteLine("-d: delete source file after convertion");
            Console.WriteLine("-c: use compression (slow)");
            Console.WriteLine("-o: hide detailed output");
            Console.WriteLine("-s: add sequence and number to title");
            Console.WriteLine("-ni: no images");
            Console.WriteLine("-ntoc: no table of content");
            Console.WriteLine("-nch: no chapters");
            Console.WriteLine("-save: save parameters to be used at the next start");
            Console.WriteLine("-a: process all files in current folder");
            Console.WriteLine("-r: process files in subfolders (work with -a key)");
            Console.WriteLine("-w: wait for key press on finish");
            Console.WriteLine();
        }

        public static void ShowMainInfo(Assembly asm)
        {
            //            Console.Clear();
            Console.WriteLine();
            var ver = asm.GetName().Version;
            Console.WriteLine(asm.GetName().Name + " Version: " + ver.ToString(3) + "; Build time: " + Util.GetBuildTime(ver).ToString("yyyy/MM/dd HH:mm:ss"));
            var title = Util.GetAttribute<AssemblyTitleAttribute>(asm);
            if (title != null)
                Console.WriteLine(title.Title);
            Console.WriteLine();
        }

        [STAThread]
        public static void Main(string[] args)
        {
            var wait = false;
            var all = false;
            var recursive = false;
            var detailedOutput = true;
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                ShowMainInfo(asm);

                var executingPath = Path.GetDirectoryName(asm.Location);
                var settingsFile = executingPath + @"\config.xml";
                var currentSettings = Util.ReadObjectFromFile<DefaultOptions>(settingsFile) ?? new DefaultOptions();
                var bookPath = string.Empty;
                string cssStyles = null;

                if (args.Length == 0)
                {
                    if (!Debugger.IsAttached)
                    {
                        Console.Write("Process all files with default settings? Enter 'y' to continue: ");
                        if (Console.ReadLine() != "y")
                        {
                            ShowHelpText(asm);
                            return;
                        }
                        wait = true;
                    }
                    all = true;
                    recursive = true;
                    //currentSettings.addSequence = true;
                }
                else
                {
                    for (var j = 0; j < args.Length; j++)
                    {
                        switch (args[j].ToLower().Trim())
                        {
                            case "-css":
                                if (args.Length > (j + 1))
                                {
                                    var cssFile = args[j + 1];
                                    if (!File.Exists(cssFile))
                                        cssFile = executingPath + "\\" + cssFile;
                                    if (!File.Exists(cssFile))
                                    {
                                        Console.WriteLine("css styles file not found");
                                        return;
                                    }
                                    cssStyles = File.ReadAllText(cssFile, Encoding.UTF8);
                                    if (String.IsNullOrEmpty(cssStyles))
                                    {
                                        Console.WriteLine("css styles file is empty");
                                        return;
                                    }
                                    j++;
                                }
                                break;
                            case "-d":
                                currentSettings.deleteOrigin = true;
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
                                all = true;
                                break;
                            case "-r":
                                recursive = true;
                                break;
                            case "-c":
                                currentSettings.compression = true;
                                break;
                            case "-o":
                                detailedOutput = false;
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
                }
                if (currentSettings.save)
                    Util.WriteObjectToFile(settingsFile, currentSettings, true);
                var conv = new Convertor(currentSettings, executingPath, cssStyles, detailedOutput);
                if (all)
                {
                    var searchOptions = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
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