using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Fb2Kindle
{
    static class Program
    {
        private static void ShowHelpText(Assembly asm)
        {
            Util.WriteLine();
            Util.WriteLine(asm.GetName().Name + " <path> [-css <styles.css>] [-d] [-ni] [-mailto:recipient@mail.org]");
            Util.WriteLine();
            Util.WriteLine("<path>: input fb2 file or files mask (ex: *.fb2) or path to *fb2 files");
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

            Util.WriteLine("-a: all fb2 books in app folder");
            Util.WriteLine("-r: process files in subfolders (work with -a key)");
            Util.WriteLine("-j: join files from each folder to the single book");

            Util.WriteLine("-save: save parameters to be used at the next start");
            Util.WriteLine("-w: wait for key press on finish");
            Util.WriteLine();
        }

        private static void ShowMainInfo(Assembly asm)
        {
            //            Console.Clear();
            Util.WriteLine();
            var ver = asm.GetName().Version;
            Util.WriteLine(string.Format("{0} Version: {1}; Build time: {2:yyyy/MM/dd HH:mm:ss}", 
                asm.GetName().Name, ver.ToString(3), Util.GetBuildTime(ver)), ConsoleColor.White);
            var title = Util.GetAttribute<AssemblyTitleAttribute>(asm);
            if (title != null)
                Util.WriteLine(title.Title, ConsoleColor.White);
            Util.WriteLine();
        }

        [STAThread]
        public static async Task Main(string[] args)
        {
            const string allBooksPattern = "*.fb2";
            var wait = false;
            var join = false;
            var save = false;
            var recursive = false;
            var detailedOutput = true;
            var startedTime = DateTime.Now;
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                ShowMainInfo(asm);

                var appPath = Util.GetAppPath();
                var settingsFile = Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, ".xml");
                var currentSettings = XmlSerializerHelper.DeserializeFile<DefaultOptions>(settingsFile) ?? new DefaultOptions();
                var bookPath = string.Empty;
                string cssStyles = null;
                string mailTo = null;

                if (args.Length == 0)
                {
                    ShowHelpText(asm);
                    Util.Write("Process all files with default parameters (-a -r -w)? Press 'Enter' to continue: ", ConsoleColor.White);
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
                            case "-preview":
                                currentSettings.CleanupMode = ConverterCleanupMode.Partial;
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
                                currentSettings.Compression = true;
                                break;
                            case "-s":
                                currentSettings.Sequence = true;
                                break;
                            case "-d":
                                currentSettings.DeleteOriginal = true;
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
                                if (args.Length > (j + 1))
                                {
                                    var cssFile = args[j + 1];
                                    if (!File.Exists(cssFile))
                                        cssFile = appPath + "\\" + cssFile;
                                    if (!File.Exists(cssFile))
                                    {
                                        Util.WriteLine("css styles file not found", ConsoleColor.Red);
                                        return;
                                    }
                                    cssStyles = File.ReadAllText(cssFile, Encoding.UTF8);
                                    if (string.IsNullOrEmpty(cssStyles))
                                    {
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
                        if (args[j].StartsWith("-mailto:"))
                        {
                            mailTo = args[j].Split(':')[1];
                        }
                    }
                }
                if (string.IsNullOrEmpty(bookPath))
                {
                    Util.WriteLine("No input file", ConsoleColor.Red);
                    return;
                }
                if (save)
                {
                    var settingsToSave = currentSettings.XmlCopy();
                    settingsToSave.SmtpPassword = null;
                    settingsToSave.ToXmlFile(settingsFile, true);
                }

                var workPath = Path.GetDirectoryName(bookPath);
                if (string.IsNullOrEmpty(workPath))
                    workPath = appPath;
                else
                    bookPath = Path.GetFileName(bookPath);
                if (string.IsNullOrEmpty(bookPath))
                    bookPath = allBooksPattern;
                var conv = new Convertor(currentSettings, cssStyles, detailedOutput) { MailTo = mailTo };
                await ProcessFolder(conv, workPath, bookPath, recursive, join);
            }
            catch (Exception ex)
            {
                Util.WriteLine(ex.Message, ConsoleColor.Red);
            }
            finally
            {
                var timeWasted = DateTime.Now - startedTime;
                Util.WriteLine();
                Util.WriteLine(string.Format("Time wasted: {0:G}", timeWasted), ConsoleColor.White);
                if (wait)
                {
                    Util.WriteLine();
                    Util.WriteLine("Press any key to continue...", ConsoleColor.White);
                    Console.ReadKey();
                }
            }
        }

        private static async Task ProcessFolder(Convertor conv, string workPath, string searchMask, bool recursive, bool join)
        {
            var files = new List<string>();
            files.AddRange(Directory.GetFiles(workPath, searchMask, SearchOption.TopDirectoryOnly));
            if (files.Count > 0)
            {
                files.Sort();
                if (join)
                    await conv.ConvertBookSequence(files.ToArray());
                else
                    foreach (var file in files)
                        await conv.ConvertBook(file);
            }
            if (!recursive) return;
            foreach (var folder in Directory.GetDirectories(workPath))
                await ProcessFolder(conv, folder, searchMask, true, join);
        }
    }
}