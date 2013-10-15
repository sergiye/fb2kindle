using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace Fb2Kindle 
{
    public static class Common
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
            Console.WriteLine(Assembly.GetExecutingAssembly().GetName().Name + " <book.fb2> [-css <styles.css>] [-d] [-nb] [-nch] [-nh] [-ni]");
            Console.WriteLine();
            Console.WriteLine("<book.fb2>: входной файл формата fb2");
            Console.WriteLine("-css <styles.css>: использование файла стилей <MyStyles.css> при конвертации");
            Console.WriteLine("-d: удалить входной файл формата fb2");
            Console.WriteLine("-nb: без буквицы");
            Console.WriteLine("-nch: без разбивки на главы");
            Console.WriteLine("-nh: без переносов слов");
            Console.WriteLine("-ni: без картинок");
            Console.WriteLine("-ntoc: без оглавления");
            Console.WriteLine("-nbox: сноски в тексте");
            Console.WriteLine("-nstitle: без информации о книге");
            Console.WriteLine("-ntitle0: без разрыва после описания книги");
            Console.WriteLine("-dztitle: удалять пустой заголовок");
            Console.WriteLine("-save: сохранить параметры запуска");
            Console.WriteLine("-a: все файлы в текущей папке");
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

        public static DateTime GetBuildTime(Version ver)
        {
            var buildTime = new DateTime(2000, 1, 1).AddDays(ver.Build).AddSeconds(ver.Revision * 2);
            if (TimeZone.IsDaylightSavingTime(DateTime.Now, TimeZone.CurrentTimeZone.GetDaylightChanges(DateTime.Now.Year)))
                buildTime = buildTime.AddHours(1);
            return buildTime;
        }

        public static void ShowMainInfo()
        {
//            Console.Clear();
            var assembly = Assembly.GetExecutingAssembly();
            Console.WriteLine(assembly.GetName().Name);
            var title = GetAttribute<AssemblyTitleAttribute>(assembly);
            if (title != null)
                Console.WriteLine(title.Title);
            var copyright = GetAttribute<AssemblyCopyrightAttribute>(assembly);
            if (copyright != null)
                Console.WriteLine(copyright.Copyright);
//            var description = GetAttribute<AssemblyDescriptionAttribute>(assembly);
//            if (description != null)
//                Console.WriteLine(description.Description);
            var ver = Assembly.GetExecutingAssembly().GetName().Version;
            Console.WriteLine("Version: " + ver + "; Build time: " + GetBuildTime(ver).ToString("yyyy/MM/dd HH:mm:ss"));
            Console.WriteLine();
        }

        public static bool CreateMobi(string executingPath, string tempDir, string bookName, string parentPath, bool deleteOrigin, string bookPath)
        {
            if (!File.Exists(tempDir + @"\kindlegen.exe"))
            {
                Console.WriteLine("!!!Не найден kindlegen.exe, конвертация в mobi невозможна!!!");
                Directory.Delete(tempDir, true);
                return false;
            }
            Console.Write("Конвертируем в mobi(KF8)...");
            var startInfo = new ProcessStartInfo { FileName = tempDir + @"\kindlegen.exe", Arguments = "\"" + tempDir + @"\" + bookName + ".opf\"", WindowStyle = ProcessWindowStyle.Hidden };
            var process2 = Process.Start(startInfo);
            process2.WaitForExit();
            if (process2.ExitCode == 2)
            {
                Console.WriteLine("Error: Не удалось сконвертировать в mobi");
                return false;
            }
            
            if (deleteOrigin)
                File.Delete(bookPath);
            var versionNumber = 1;
            var resultPath = Path.GetDirectoryName(bookPath);
            var resultName = bookName;
            while (File.Exists(Path.Combine(resultPath, resultName) + ".mobi"))
            {
                resultName = bookName + "(v" + versionNumber + ")";
                versionNumber++;
            }
            File.Move(tempDir + @"\" + bookName + ".mobi", Path.Combine(resultPath, resultName) + ".mobi");
            Console.WriteLine("(Ok)");
            return true;
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

        public static void GetFileFromResource(string resourceName, string filename)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var scriptsPath = String.Format("{0}.{1}", assembly.GetTypes()[0].Namespace, resourceName);
            using (var stream = assembly.GetManifestResourceStream(scriptsPath))
            {
                if (stream == null) return;
                using (Stream file = File.OpenWrite(filename))
                {
                    var buffer = new byte[8 * 1024];
                    int len;
                    while ((len = stream.Read(buffer, 0, buffer.Length)) > 0)
                        file.Write(buffer, 0, len);
                }
            }
        }

        public static bool ExtractImages(string executingPath, string tempDir, string images, string bookPath)
        {
            Console.Write("Извлекаем картинки...");
            if (!File.Exists(tempDir + @"\fb2bin.exe"))
            {
                Console.WriteLine("Отсутствует скрипт извлечения картинок");
                return false;
            }
            var startInfo = new ProcessStartInfo {FileName = tempDir + @"\fb2bin.exe", 
                Arguments = "-x -q -q -d \"" + tempDir + @"\" + images + "\" \"" + bookPath + "\"", 
                WindowStyle = ProcessWindowStyle.Hidden};
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
            ImagesHelper.CompressImagesInFolder(tempDir + "\\images");
            return true;
        }

        public static string PrepareTempFolder(string bookName, string images, string executingPath)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), bookName);
            if (!Directory.Exists(tempDir))
                Directory.CreateDirectory(tempDir);
            if (!Directory.Exists(tempDir + @"\" + images))
                Directory.CreateDirectory(tempDir + @"\" + images);
//            if (Directory.Exists(executingPath + @"\" + images))
//                CopyDirectory(executingPath + @"\" + images, tempDir + @"\" + images, true);
            GetFileFromResource("fb2bin.exe", tempDir + "\\fb2bin.exe");
            GetFileFromResource("kindlegen.exe", tempDir + "\\kindlegen.exe");
            return tempDir;
        }
    }
}
