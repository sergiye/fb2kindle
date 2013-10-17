using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Fb2Kindle
{
    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var wait = false;
            try
            {
                Common.ShowMainInfo();
                if (args.Length == 0)
                {
                    Common.ShowHelpText();
                    return;
                }
                var executingPath = Path.GetDirectoryName(Application.ExecutablePath);
                var settingsFile = executingPath + @"\config.xml";
                var currentSettings = Common.ReadObjectFromFile<DefaultOptions>(settingsFile) ?? new DefaultOptions();
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
                        default:
                            if (j == 0)
                                bookPath = args[j];
                            break;
                    }
                }
                if (currentSettings.save)
                    Common.WriteObjectToFile(settingsFile, currentSettings, true);

                string defaultCss = null;
                if (File.Exists(currentSettings.defaultCSS))
                    defaultCss = File.ReadAllText(currentSettings.defaultCSS, Encoding.UTF8);
                if (string.IsNullOrEmpty(defaultCss))
                {
                    if (!string.IsNullOrEmpty(currentSettings.defaultCSS))
                        Console.WriteLine("Styles file not found: " + currentSettings.defaultCSS);
                    defaultCss = Common.GetScriptFromResource("defstyles.css");
                }

                var conv = new Convertor(currentSettings, executingPath, defaultCss);
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