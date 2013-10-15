using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Fb2Kindle
{
    class Module1
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Common.ShowMainInfo();
            if (args.Length == 0)
            {
                Common.ShowHelpText();
                return;
            }

            var executingPath = Path.GetDirectoryName(Application.ExecutablePath);
            var settingsFile = executingPath + @"\config.xml";
            var currentSettings = XmlSerializerHelper.ReadObjectFromFile<DefaultOptions>(settingsFile) ?? new DefaultOptions();
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
                        currentSettings.noChapters = true;
                        break;
                    case "-nh":
                        currentSettings.nh = true;
                        break;
                    case "-ni":
                        currentSettings.noImages = true;
                        break;
                    case "-ntoc":
                        currentSettings.ntoc = true;
                        break;
                    case "-nstitle":
                        currentSettings.nstitle = true;
                        break;
                    case "-ntitle0":
                        currentSettings.ntitle0 = true;
                        break;
                    case "-dztitle":
                        currentSettings.dztitle = true;
                        break;
                    case "-nbox":
                        currentSettings.nbox = true;
                        break;
                    case "-save":
                        currentSettings.save = true;
                        break;
                    case "-a":
                        currentSettings.all = true;
                        break;
                    default:
                        if (j == 0)
                            bookPath = args[j];
                        break;
                }
            }
            string defaultCss;
            if (File.Exists(currentSettings.defaultCSS))
                defaultCss = File.ReadAllText(currentSettings.defaultCSS, Encoding.UTF8);
            else
            {
                if (!string.IsNullOrEmpty(currentSettings.defaultCSS))
                    Console.WriteLine("Error: Не найден указанный файл стилей: " + currentSettings.defaultCSS);
                defaultCss = Common.GetScriptFromResource("defstyles.css");
            }
            if (currentSettings.save)
                XmlSerializerHelper.WriteObjectToFile(settingsFile, currentSettings, true);
            if (string.IsNullOrEmpty(defaultCss))
            {
                Console.WriteLine("Пустой файл стилей: " + currentSettings.defaultCSS);
                return;
            }
            var conv = new Convertor(currentSettings, executingPath, defaultCss);
            if (currentSettings.all)
            {
                var files = Directory.GetFiles(executingPath, "*.fb2");
                if (files.Length == 0)
                    Console.WriteLine("Исходные файлы не найдены");
                foreach (var file in files)
                    conv.ConvertBook(file);
            }
            else
            {
                if (string.IsNullOrEmpty(bookPath) || !File.Exists(bookPath))
                {
                    if (string.IsNullOrEmpty(bookPath))
                        Console.WriteLine("Не задан исходный файл");
                    else
                        Console.WriteLine("Файл не найден: " + bookPath);
                }
                else
                    conv.ConvertBook(bookPath);
            }
//            if (Debugger.IsAttached)
//            {
//                Console.WriteLine();
//                Console.WriteLine("Нажмите любую клавишу для продолжения...");
//                Console.ReadKey();
//            }
        }
    }
}