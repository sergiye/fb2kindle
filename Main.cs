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
            var currentSettings = XmlSerializerHelper.ReadObjectFromFile<DefaultOptions>(executingPath + @"\fb2kf8.set") ?? new DefaultOptions();
            var bookPath = string.Empty;
            for (var j = 0; j < args.Length; j++)
            {
                switch (args[j].ToLower().Trim())
                {
                    case "-css":
                        if (args.Length > (j + 1))
                        {
                            if (File.Exists(args[j + 1]))
                                currentSettings.defaultCSS = args[j + 1];
                            else
                                Console.WriteLine("Error: Не найден указанный файл стилей: " + args[j + 1]);
                            j++;
                        }
                        break;
                    case "-d":
                        currentSettings.d = "True";
                        break;
                    case "-nb":
                        currentSettings.nb = "True";
                        break;
                    case "-nch":
                        currentSettings.nc = "True";
                        break;
                    case "-nh":
                        currentSettings.nh = "True";
                        break;
                    default:
//                        if (j == 0)
                            bookPath = args[j];
                        break;
                }
            }
            var defaultCss = File.Exists(currentSettings.defaultCSS) 
                ? File.ReadAllText(currentSettings.defaultCSS, Encoding.UTF8) 
                : Common.GetScriptFromResource("defstyles.css");
            if (string.IsNullOrEmpty(defaultCss))
            {
                Console.WriteLine("Пустой файл стилей: " + currentSettings.defaultCSS);
                return;
            }
            if (string.IsNullOrEmpty(bookPath) || !File.Exists(bookPath))
            {
                Console.WriteLine("Файл не найден: " + bookPath);
                return;
            }
            var conv = new Convertor(currentSettings, executingPath, defaultCss);
            if (!conv.ConvertBook(bookPath) || Debugger.IsAttached)
            {
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("Нажмите любую клавишу для продолжения...");
                Console.ReadKey();
            }
        }
    }
}