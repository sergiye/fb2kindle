using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace Fb2Kindle 
{
    public static class ConverterHelper
    {
        public static string CodStr(string Str)
        {
            return Str == "" ? "" : Convert.ToBase64String(Encoding.Unicode.GetBytes(Str));
        }

        public static string DeCodStr(string Str)
        {
            if (Str == "")
                return "";
            var bytes = Convert.FromBase64String(Str);
            return Encoding.Unicode.GetString(bytes);
        }

        public static string FormatToHTML(string htmltxt2)
        {
            htmltxt2 = htmltxt2.Replace("<text-author>", "<p class=\"text-author\">");
            htmltxt2 = htmltxt2.Replace("</text-author>", "</p>");
            htmltxt2 = htmltxt2.Replace("<empty-line />", "<br/>");
            htmltxt2 = htmltxt2.Replace("<epigraph ", "<div class = \"epigraph\" ");
            htmltxt2 = htmltxt2.Replace("<epigraph>", "<div class = \"epigraph\">");
            htmltxt2 = htmltxt2.Replace("</epigraph>", "</div>");
            htmltxt2 = htmltxt2.Replace("<empty-line/>", "<br/>");
            htmltxt2 = htmltxt2.Replace("<subtitle ", "<div class = \"subtitle\" ");
            htmltxt2 = htmltxt2.Replace("<subtitle>", "<div class = \"subtitle\">");
            htmltxt2 = htmltxt2.Replace("</subtitle>", "</div>");
            htmltxt2 = htmltxt2.Replace("<cite ", "<div class = \"cite\" ");
            htmltxt2 = htmltxt2.Replace("<cite>", "<div class = \"cite\">");
            htmltxt2 = htmltxt2.Replace("</cite>", "</div>");
            htmltxt2 = htmltxt2.Replace("<emphasis>", "<i>");
            htmltxt2 = htmltxt2.Replace("</emphasis>", "</i>");
            htmltxt2 = htmltxt2.Replace("<strong>", "<b>");
            htmltxt2 = htmltxt2.Replace("</strong>", "</b>");
            htmltxt2 = htmltxt2.Replace("<poem", "<div class=\"poem\"");
            htmltxt2 = htmltxt2.Replace("</poem>", "</div>");
            htmltxt2 = htmltxt2.Replace("<stanza>", "<br/>");
            htmltxt2 = htmltxt2.Replace("</stanza>", "<br/>");
            htmltxt2 = htmltxt2.Replace("<v>", "<p>");
            htmltxt2 = htmltxt2.Replace("</v>", "</p>");
            htmltxt2 = htmltxt2.Replace("<title", "<div class = \"subtitle\"");
            htmltxt2 = htmltxt2.Replace("</title>", "</div>");
            return htmltxt2;
        }

        public static object FormatToHTMLBox(string htmltxt2)
        {
            htmltxt2 = htmltxt2.Replace("<text-author>", "<b>");
            htmltxt2 = htmltxt2.Replace("</text-author>", "</b>");
            htmltxt2 = htmltxt2.Replace("<empty-line />", "<br/>");
            htmltxt2 = htmltxt2.Replace("<epigraph>", "");
            htmltxt2 = htmltxt2.Replace("</epigraph>", "");
            htmltxt2 = htmltxt2.Replace("<empty-line/>", "<br/>");
            htmltxt2 = htmltxt2.Replace("<subtitle ", "<div class = \"subtitle\" ");
            htmltxt2 = htmltxt2.Replace("<subtitle>", "<div class = \"subtitle\">");
            htmltxt2 = htmltxt2.Replace("</subtitle>", "</div>");
            htmltxt2 = htmltxt2.Replace("<cite ", "<div class = \"cite\" ");
            htmltxt2 = htmltxt2.Replace("<cite>", "<div class = \"cite\">");
            htmltxt2 = htmltxt2.Replace("</cite>", "</div>");
            htmltxt2 = htmltxt2.Replace("<emphasis>", "<i>");
            htmltxt2 = htmltxt2.Replace("</emphasis>", "</i>");
            htmltxt2 = htmltxt2.Replace("<strong>", "<b>");
            htmltxt2 = htmltxt2.Replace("</strong>", "</b>");
            htmltxt2 = htmltxt2.Replace("<poem", "<div class=\"poem\"");
            htmltxt2 = htmltxt2.Replace("</poem>", "</div>");
            htmltxt2 = htmltxt2.Replace("<stanza>", "<br/>");
            htmltxt2 = htmltxt2.Replace("</stanza>", "<br/>");
            htmltxt2 = htmltxt2.Replace("<v>", "<p>");
            htmltxt2 = htmltxt2.Replace("</v>", "</p>");
            htmltxt2 = htmltxt2.Replace("<title", "<div class = \"subtitle\"");
            htmltxt2 = htmltxt2.Replace("</title>", "</div>");
            htmltxt2 = htmltxt2.Replace("<section", "<div class = \"note\"");
            htmltxt2 = htmltxt2.Replace("</section>", "</div>");
            return htmltxt2;
        }

        public static string GipherHTML(string htmltxt)
        {
            htmltxt = htmltxt.Replace("<p>", "<p1>");
            htmltxt = htmltxt.Replace("<p ", "<p1 ");
            htmltxt = htmltxt.Replace("</p>", "</p1>");
            var index = htmltxt.IndexOf("<p1");
            var startIndex = htmltxt.IndexOf("</p1>");
            var length = (startIndex - index) + 6;
            for (var i = 1; index > 0; i++)
            {
                var txt = htmltxt.Substring(index, length);
                htmltxt = htmltxt.Remove(index, length);
                txt = TransText(txt).Remove(0, 4);
                htmltxt = htmltxt.Insert(index, "<p" + txt.Remove(txt.Length - 5, 5) + "</p>");
                index = htmltxt.IndexOf("<p1", index);
                startIndex = htmltxt.IndexOf("</p1>", startIndex);
                length = (startIndex - index) + 6;
            }
            return htmltxt;
        }

        public static string TransText(string Txt)
        {
            var str5 = Txt.ToLower();
            const string str6 = "ьъ";
            const string str2 = "аеёийоуыэюяaeiouy";
            const string str3 = "бвгджзклмнпрстфхцчшщbcdfghjklmnpqrstvwxz";
            var num2 = str5.Length - 1;
            var flag = true;
            var num6 = num2;
            for (var i = 0; i <= num6; i++)
            {
                var str = str5[i];
                switch (str)
                {
                    case '<':
                        flag = false;
                        break;
                    case '>':
                        flag = true;
                        break;
                    default:
                        if (!flag)
                        {
                            str5 = str5.Remove(i, 1).Insert(i, "_");
                        }
                        if ((str6.IndexOf(str) != -1) & flag)
                        {
                            str5 = str5.Remove(i, 1).Insert(i, "x");
                        }
                        else if ((str2.IndexOf(str) != -1) & flag)
                        {
                            str5 = str5.Remove(i, 1).Insert(i, "g");
                        }
                        else if ((str3.IndexOf(str) != -1) & flag)
                        {
                            str5 = str5.Remove(i, 1).Insert(i, "s");
                        }
                        break;
                }
            }
            var source = new List<int>();
            var num4 = str5.IndexOf("xgg");
            while (num4 != -1)
            {
                source.Add(num4 + 1);
                num4 = str5.IndexOf("xgg", (num4 + 1));
            }
            num4 = str5.IndexOf("xgs");
            while (num4 != -1)
            {
                source.Add(num4 + 1);
                num4 = str5.IndexOf("xgs", (num4 + 1));
            }
            num4 = str5.IndexOf("xsg");
            while (num4 != -1)
            {
                source.Add(num4 + 1);
                num4 = str5.IndexOf("xsg", (num4 + 1));
            }
            num4 = str5.IndexOf("xss");
            while (num4 != -1)
            {
                source.Add(num4 + 1);
                num4 = str5.IndexOf("xss", (num4 + 1));
            }
            num4 = str5.IndexOf("gssssg");
            while (num4 != -1)
            {
                source.Add(num4 + 3);
                num4 = str5.IndexOf("gssssg", (num4 + 1));
            }
            num4 = str5.IndexOf("gsssg");
            while (num4 != -1)
            {
                source.Add(num4 + 2);
                source.Add(num4 + 3);
                num4 = str5.IndexOf("gsssg", (num4 + 1));
            }
            num4 = str5.IndexOf("sgsg");
            while (num4 != -1)
            {
                source.Add(num4 + 2);
                num4 = str5.IndexOf("sgsg", (num4 + 1));
            }
            num4 = str5.IndexOf("gssg");
            while (num4 != -1)
            {
                source.Add(num4 + 2);
                num4 = str5.IndexOf("gssg", (num4 + 1));
            }
            num4 = str5.IndexOf("sggg");
            while (num4 != -1)
            {
                source.Add(num4 + 2);
                num4 = str5.IndexOf("sggg", (num4 + 1));
            }
            num4 = str5.IndexOf("sggs");
            while (num4 != -1)
            {
                source.Add(num4 + 2);
                num4 = str5.IndexOf("sggs", (num4 + 1));
            }
            source.Sort((i, i1) => i.CompareTo(i1));
            var index = 0;
            foreach (var i in source)
            {
                if (i == 0) continue;
                Txt = Txt.Insert(index + i, "&shy;");
                index += 5;
            }
            return Txt;
        }

        public static string RandomMas(string Str)
        {
            var num2 = 0;
            var index = Str.IndexOf("{");
            if (index != -1)
            {
                num2 = Str.IndexOf("}");
            }
            var rnd = new Random();
            while (index != -1)
            {
                var strArray = ReturnMasStr(Str, ref index, ref num2, '{', '}');
                var num3 = strArray.Length * rnd.Next();
                Str = Str.Substring(0, index) + strArray[num3] + Str.Substring(num2 + 1, (Str.Length - num2) - 1);
                index = Str.IndexOf("{");
                num2 = Str.IndexOf("}");
            }
            return Str;
        }

        public static string[] ReturnMasStr(string Str, ref int i1, ref int i2, char ChBegin = '[', char ChEnd = ']', char ChR = '|', string N = "")
        {
            if ((i1 == 0) & (i2 == 0))
            {
                i1 = Str.IndexOf(ChBegin, i2);
                if (i1 != -1)
                    i2 = Str.IndexOf(ChEnd, i1);
            }
            if ((i1 != -1) & (i2 != -1))
                return Str.Substring(i1 + 1, (i2 - i1) - 1).Split(new[] {ChR});
            return new[] {""};
        }

        public static void CopyDirectory(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            var dir = new DirectoryInfo(sourceDirName);
            var dirs = dir.GetDirectories();

            if (!dir.Exists)
                throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + sourceDirName);

            if (!Directory.Exists(destDirName))
                Directory.CreateDirectory(destDirName);

            var files = dir.GetFiles();
            foreach (var file in files)
                file.CopyTo(Path.Combine(destDirName, file.Name), true);

            if (!copySubDirs) return;
            foreach (var subdir in dirs)
                CopyDirectory(subdir.FullName, Path.Combine(destDirName, subdir.Name), true);
        }

        public static void ShowHelpText()
        {
            Console.WriteLine();
            Console.Write(Assembly.GetExecutingAssembly().GetName().Name + " <InBook.fb2> [<OutBook.mobi>] [-css <styles.css>] [-d]");
            Console.WriteLine();
            Console.WriteLine();
            Console.Write("<InBook.fb2>: входной файл формата fb2");
            Console.WriteLine();
            Console.Write("<OutBook.mobi>: имя файла на выходе, в формате mobi");
            Console.WriteLine();
            Console.Write("-css <styles.css>: использование файла стилей <MyStyles.css> при конвертации");
            Console.WriteLine();
            Console.Write("-d: удалить входной файл формата fb2");
            Console.WriteLine();
            Console.Write("-nb: без буквицы");
            Console.WriteLine();
            Console.Write("-nch: без разбивки на главы");
            Console.WriteLine();
            Console.Write("-nh: без переносов слов");
            Console.WriteLine();
        }

        public static T GetAttribute<T>(ICustomAttributeProvider assembly, bool inherit = false)where T : Attribute
        {
            var attr = assembly.GetCustomAttributes(typeof (T), inherit);
            foreach (var o in attr)
                if (o is T)
                    return o as T;
            return null;
        }

        public static void ShowMainInfo()
        {
            Console.Clear();
            var assembly = Assembly.GetExecutingAssembly();
            var description = GetAttribute<AssemblyDescriptionAttribute>(assembly);
            Console.WriteLine(description != null ? description.Description : assembly.GetName().Name);
            var copyright = GetAttribute<AssemblyCopyrightAttribute>(assembly);
            if (copyright != null)
                Console.WriteLine(copyright.Copyright);
            Console.WriteLine("Version: " + assembly.GetName().Version);
            Console.WriteLine();
        }

        public static void CreateMobi(string executingPath, string tempDir, string bookName, string parentPath, bool deleteOrigin, string bookPath)
        {
            if (!File.Exists(executingPath + @"\kindlegen.exe"))
            {
                Console.Write("!!!Не найден kindlegen.exe, конвертация в mobi невозможна!!!");
                Console.WriteLine();
                Directory.Delete(tempDir, true);
            }
            else
            {
                Console.Write("Конвертируем в mobi(KF8)...");
                var startInfo = new ProcessStartInfo {FileName = executingPath + @"\kindlegen.exe", Arguments = "\"" + tempDir + @"\" + bookName + ".opf\"", WindowStyle = ProcessWindowStyle.Hidden};
                var process2 = Process.Start(startInfo);
                process2.WaitForExit();
                if (process2.ExitCode == 2)
                {
                    if (!Directory.Exists(parentPath + @"\" + bookName))
                    {
                        Directory.CreateDirectory(parentPath + @"\" + bookName);
                    }
                    Directory.Move(tempDir, parentPath + @"\" + bookName);
                    Console.Write("(xErr)");
                    Console.WriteLine();
                    Console.Write("!!!Не удалось сконвертировать в mobi!!!");
                    Console.WriteLine();
                }
                else
                {
                    if (deleteOrigin)
                    {
                        File.Delete(bookPath);
                    }
                    var versionNumber = 1;
                    var resultPath = Path.GetDirectoryName(bookPath);
                    var resultName = bookName;
                    while (File.Exists(Path.Combine(resultPath, resultName) + ".mobi"))
                    {
                        resultName = bookName + "(v" + versionNumber + ")";
                        versionNumber++;
                    }
                    File.Move(tempDir + @"\" + bookName + ".mobi", Path.Combine(resultPath, resultName) + ".mobi");
                    Directory.Delete(tempDir, true);
                    Console.Write("(Ok)");
                }
            }
        }

        public static string AddEncodingToXml(string text)
        {
            return "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + text;
        }

        public static void SaveElementToFile(string elementData, string bodyContent, bool noBookFlag, string folder, int bookNum)
        {
            var text = AddEncodingToXml(elementData);
            text = text.Insert(text.IndexOf("<body>") + 6, bodyContent);
            text = text.Replace("<sectio1", noBookFlag ? "<div class=\"nobook\"" : "<div class=\"book\"");
            text = text.Replace("</sectio1>", "</div>");
            File.WriteAllText(folder + @"\book" + bookNum + ".html", text);
        }

        public static string GetScriptFromResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var scriptsPath = String.Format("{0}.{1}", assembly.GetTypes()[0].Namespace, resourceName);
            using (var stream = assembly.GetManifestResourceStream(scriptsPath))
            {
                if (stream != null)
                    using (var reader = new StreamReader(stream))
                    {
                        return reader.ReadToEnd();
                    }
                return null;
            }
        }

        public static bool ExtractImages(string executingPath, string tempDir, string images, string bookPath)
        {
            //extract images
            var processingAppFound = true;
            var startInfo = new ProcessStartInfo();
            if (File.Exists(executingPath + @"\fb2bin.exe"))
            {
                Console.WriteLine("Извлекаем картинки...");
                startInfo.FileName = executingPath + @"\fb2bin.exe";
                startInfo.Arguments = "-x -q -q -d \"" + tempDir + @"\" + images + "\" \"" + bookPath + "\"";
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                var process = Process.Start(startInfo);
                process.WaitForExit();
                switch (process.ExitCode)
                {
                    case 0:
                        Console.WriteLine("(Ok)");
                        break;
                    case 1:
                        Console.WriteLine("(Картинки извлечены, но могут быть ошибки!)");
                        break;
                    case 2:
                        Console.WriteLine("(Невалидный исходный файл - выполнение невозможно!)");
                        break;
                    case 3:
                        Console.WriteLine("(Приключилась фатальная ошибка!)");
                        break;
                    case 4:
                        Console.WriteLine("(Ошибка в параметрах командной строки!)");
                        break;
                }
            }
            else
            {
                processingAppFound = false;
                Console.Write("Невозможно извлечь картинки");
                Console.WriteLine();
            }
            return processingAppFound;
        }

        public static string PrepareTempFolder(string bookName, string images, string executingPath)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), bookName);
            if (!Directory.Exists(tempDir))
                Directory.CreateDirectory(tempDir);
            if (!Directory.Exists(tempDir + @"\" + images))
                Directory.CreateDirectory(tempDir + @"\" + images);
            if (Directory.Exists(executingPath + @"\" + images))
                CopyDirectory(executingPath + @"\" + images, tempDir + @"\" + images, true);
            return tempDir;
        }
    }
}
