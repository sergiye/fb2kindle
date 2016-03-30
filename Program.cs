using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace Fb2Kindle
{
    class Program
    {
        private static void ShowHelpText(Assembly asm)
        {
            Console.WriteLine();
            Console.WriteLine(asm.GetName().Name + " <path> [-css <styles.css>] [-d] [-ni] [-mailto:recipient@mail.org]");
            Console.WriteLine();
            Console.WriteLine("<path>: input fb2 file or files mask (ex: *.fb2) or path to *fb2 files");
            Console.WriteLine("-css <styles.css>: styles used in destination book");
            Console.WriteLine("-d: delete source file after successful convertion");
            Console.WriteLine("-c: use compression (slow)");
            Console.WriteLine("-o: hide detailed output");
            Console.WriteLine("-s: add sequence and number to title");
            Console.WriteLine("-ni: no images");
            Console.WriteLine("-ntoc: no table of content");
            Console.WriteLine("-nch: no chapters");

            Console.WriteLine("-mailto: - send document to email (kindle delivery)");

            Console.WriteLine("-a: all fb2 books in app folder");
            Console.WriteLine("-r: process files in subfolders (work with -a key)");
            Console.WriteLine("-j: join files from each folder to the single book");

            Console.WriteLine("-save: save parameters to be used at the next start");
            Console.WriteLine("-w: wait for key press on finish");
            Console.WriteLine();
        }

        private static void ShowMainInfo(Assembly asm)
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
            const string allBooksPattern = "*.fb2";
            var wait = false;
            var join = false;
            var save = false;
            var recursive = false;
            var detailedOutput = true;
            var debug = Debugger.IsAttached;
            var startedTime = DateTime.Now;
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                ShowMainInfo(asm);

                var appPath = Util.GetAppPath();
                var settingsFile = appPath + @"\config.xml";
                var currentSettings = Util.ReadObjectFromFile<DefaultOptions>(settingsFile) ?? new DefaultOptions();
                var bookPath = string.Empty;
                string cssStyles = null;
                string mailTo = null;

                if (args.Length == 0)
                {
                    ShowHelpText(asm);
                    Console.Write("Process all files with default parameters (-a -r -w)? Press 'Enter' to continue: ");
                    if (Console.ReadKey().Key != ConsoleKey.Enter)
                        return;
                    wait = true;
                    bookPath = allBooksPattern;
                    recursive = true;
                }
                else
                {
                    for (var j = 0; j < args.Length; j++)
                    {
                        switch (args[j].ToLower().Trim())
                        {
                            case "-nch":
                                currentSettings.nch = true;
                                break;
                            case "-ni":
                                currentSettings.ni = true;
                                break;
                            case "-ntoc":
                                currentSettings.ntoc = true;
                                break;
                            case "-c":
                                currentSettings.c = true;
                                break;
                            case "-s":
                                currentSettings.s = true;
                                break;
                            case "-d":
                                currentSettings.d = true;
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
                            case "-nc":
                                debug = true;
                                break;
                            case "-css":
                                if (args.Length > (j + 1))
                                {
                                    var cssFile = args[j + 1];
                                    if (!File.Exists(cssFile))
                                        cssFile = appPath + "\\" + cssFile;
                                    if (!File.Exists(cssFile))
                                    {
                                        Console.WriteLine("css styles file not found");
                                        return;
                                    }
                                    cssStyles = File.ReadAllText(cssFile, Encoding.UTF8);
                                    if (string.IsNullOrEmpty(cssStyles))
                                    {
                                        Console.WriteLine("css styles file is empty");
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
                        if (args[j].StartsWith("-mailto:"))
                        {
                            mailTo = args[j].Split(':')[1];
                        }
                    }
                }
                if (string.IsNullOrEmpty(bookPath))
                {
                    Console.WriteLine("No input file");
                    return;
                }
                if (save)
                    Util.WriteObjectToFile(settingsFile, currentSettings, true);

                var workPath = Path.GetDirectoryName(bookPath);
                if (string.IsNullOrEmpty(workPath))
                    workPath = appPath;
                else
                    bookPath = Path.GetFileName(bookPath);
                if (string.IsNullOrEmpty(bookPath))
                    bookPath = allBooksPattern;
                var conv = new Convertor(currentSettings, cssStyles, detailedOutput) { MailTo = mailTo };
                ProcessFolder(conv, workPath, bookPath, recursive, join, debug);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                var timeWasted = DateTime.Now - startedTime;
                Console.WriteLine();
                Console.WriteLine("Time wasted: {0:G}", timeWasted);
                if (wait)
                {
                    Console.WriteLine();
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                }
            }
        }

        private static void ProcessFolder(Convertor conv, string workPath, string searchMask, bool recursive, bool join, bool debug)
        {
            var files = new List<string>();
            files.AddRange(Directory.GetFiles(workPath, searchMask, SearchOption.TopDirectoryOnly));
            if (files.Count > 0)
            {
                files.Sort();
                if (join)
                    conv.ConvertBookSequence(files.ToArray(), debug);
                else
                    foreach (var file in files)
                        conv.ConvertBook(file, debug);
            }
            if (!recursive) return;
            foreach (var folder in Directory.GetDirectories(workPath))
                ProcessFolder(conv, folder, searchMask, true, join, debug);
        }
    }
}