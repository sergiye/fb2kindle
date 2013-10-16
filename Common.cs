using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Encoder = System.Drawing.Imaging.Encoder;

namespace Fb2Kindle 
{
    [Serializable]
    public class DefaultOptions
    {
        public DefaultOptions()
        {
            DropCap = "АБВГДЕЖЗИКЛМНОПРСТУФХЦЧЩШЭЮЯ";
        }

        public bool deleteOrigin { get; set; }
        public bool noBig { get; set; }
        public bool noChapters { get; set; }
        public bool nh { get; set; }
        public bool noImages { get; set; }
        public bool ntoc { get; set; }
        public bool nstitle { get; set; }
        public bool ntitle0 { get; set; }
        public bool dztitle { get; set; } //del zero title
        public string defaultCSS { get; set; }
        public string DropCap { get; set; }
        public bool ContentOf { get; set; }
        public bool nbox { get; set; } //note box
        [XmlIgnore]
        public bool save { get; set; }
        [XmlIgnore]
        public bool all { get; set; }
    }

    public class SectionInfo
    {
        public int Val1 { get; set; }
        public int Val2 { get; set; }
        public int Val3 { get; set; }
    }

    public class DataItem
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public DataItem()
        {
        }

        public DataItem(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }

    public static class Common
    {
        public static string CodStr(string str)
        {
            return String.IsNullOrEmpty(str) ? str : Convert.ToBase64String(Encoding.Unicode.GetBytes(str));
        }

        public static string DeCodStr(string str)
        {
            return String.IsNullOrEmpty(str) ? str : Encoding.Unicode.GetString(Convert.FromBase64String(str));
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

        public static string TransText(string text)
        {
            var lowerText = text.ToLower();
            const string str6 = "ьъ";
            const string str2 = "аеёийоуыэюяaeiouy";
            const string str3 = "бвгджзклмнпрстфхцчшщbcdfghjklmnpqrstvwxz";
            var num2 = lowerText.Length - 1;
            var flag = true;
            var num6 = num2;
            for (var i = 0; i <= num6; i++)
            {
                var ch = lowerText[i];
                switch (ch)
                {
                    case '<':
                        flag = false;
                        break;
                    case '>':
                        flag = true;
                        break;
                    default:
                        if (!flag)
                            lowerText = lowerText.Remove(i, 1).Insert(i, "_");
                        if ((str6.IndexOf(ch) != -1) & flag)
                            lowerText = lowerText.Remove(i, 1).Insert(i, "x");
                        else if ((str2.IndexOf(ch) != -1) & flag)
                            lowerText = lowerText.Remove(i, 1).Insert(i, "g");
                        else if ((str3.IndexOf(ch) != -1) & flag)
                            lowerText = lowerText.Remove(i, 1).Insert(i, "s");
                        break;
                }
            }
            var source = new List<int>();
            var num4 = lowerText.IndexOf("xgg");
            while (num4 != -1)
            {
                source.Add(num4 + 1);
                num4 = lowerText.IndexOf("xgg", (num4 + 1));
            }
            num4 = lowerText.IndexOf("xgs");
            while (num4 != -1)
            {
                source.Add(num4 + 1);
                num4 = lowerText.IndexOf("xgs", (num4 + 1));
            }
            num4 = lowerText.IndexOf("xsg");
            while (num4 != -1)
            {
                source.Add(num4 + 1);
                num4 = lowerText.IndexOf("xsg", (num4 + 1));
            }
            num4 = lowerText.IndexOf("xss");
            while (num4 != -1)
            {
                source.Add(num4 + 1);
                num4 = lowerText.IndexOf("xss", (num4 + 1));
            }
            num4 = lowerText.IndexOf("gssssg");
            while (num4 != -1)
            {
                source.Add(num4 + 3);
                num4 = lowerText.IndexOf("gssssg", (num4 + 1));
            }
            num4 = lowerText.IndexOf("gsssg");
            while (num4 != -1)
            {
                source.Add(num4 + 2);
                source.Add(num4 + 3);
                num4 = lowerText.IndexOf("gsssg", (num4 + 1));
            }
            num4 = lowerText.IndexOf("sgsg");
            while (num4 != -1)
            {
                source.Add(num4 + 2);
                num4 = lowerText.IndexOf("sgsg", (num4 + 1));
            }
            num4 = lowerText.IndexOf("gssg");
            while (num4 != -1)
            {
                source.Add(num4 + 2);
                num4 = lowerText.IndexOf("gssg", (num4 + 1));
            }
            num4 = lowerText.IndexOf("sggg");
            while (num4 != -1)
            {
                source.Add(num4 + 2);
                num4 = lowerText.IndexOf("sggg", (num4 + 1));
            }
            num4 = lowerText.IndexOf("sggs");
            while (num4 != -1)
            {
                source.Add(num4 + 2);
                num4 = lowerText.IndexOf("sggs", (num4 + 1));
            }
            source.Sort((i, i1) => i.CompareTo(i1));
            var index = 0;
            foreach (var i in source)
            {
                if (i == 0) continue;
                text = text.Insert(index + i, "&shy;");
                index += 5;
            }
            return text;
        }

        public static void CopyDirectory(string sourceDirName, string destDirName, bool copySubDirs)
        {
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
            Console.WriteLine(Assembly.GetExecutingAssembly().GetName().Name + " <book.fb2> [-css <styles.css>] [-d] [-nh] [-nb] [-nch] [-ni]");
            Console.WriteLine();
            Console.WriteLine("<book.fb2>: input fb2 file");
            Console.WriteLine("-css <styles.css>: styles used in destination book");
            Console.WriteLine("-d: delete source file after convertion");
            Console.WriteLine("-nb: no big letters at the chapter start");
            Console.WriteLine("-nch: no chapters");
            Console.WriteLine("-nh: no words breaking");
            Console.WriteLine("-ni: no images");
            Console.WriteLine("-ntoc: no table of content");
            Console.WriteLine("-nbox: notes in the text");
            Console.WriteLine("-nstitle: no title page");
            Console.WriteLine("-ntitle0: skip title separation");
            Console.WriteLine("-dztitle: skip empty header");
            Console.WriteLine("-save: save parameters to be used at the next start");
            Console.WriteLine("-a: process all files in current folder");
            Console.WriteLine();
        }

        private static T GetAttribute<T>(ICustomAttributeProvider assembly, bool inherit = false)where T : Attribute
        {
            var attr = assembly.GetCustomAttributes(typeof (T), inherit);
            foreach (var o in attr)
                if (o is T)
                    return o as T;
            return null;
        }

        private static DateTime GetBuildTime(Version ver)
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
            var ver = Assembly.GetExecutingAssembly().GetName().Version;
            Console.WriteLine("Version: " + ver + "; Build time: " + GetBuildTime(ver).ToString("yyyy/MM/dd HH:mm:ss"));
            Console.WriteLine();
        }

        public static bool CreateMobi(string executingPath, string tempDir, string bookName, string parentPath, bool deleteOrigin, string bookPath)
        {
            if (!File.Exists(tempDir + @"\kindlegen.exe"))
            {
                Console.WriteLine("kindlegen.exe not found");
                Directory.Delete(tempDir, true);
                return false;
            }
            Console.Write("Creating mobi (KF8)...");
            var startInfo = new ProcessStartInfo { FileName = tempDir + @"\kindlegen.exe", Arguments = "\"" + tempDir + @"\" + bookName + ".opf\"", WindowStyle = ProcessWindowStyle.Hidden };
            var process2 = Process.Start(startInfo);
            process2.WaitForExit();
            if (process2.ExitCode == 2)
            {
                Console.WriteLine("Error converting to mobi");
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
            Console.WriteLine("(OK)");
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

        private static void GetFileFromResource(string resourceName, string filename)
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

        public static bool ExtractImages(XElement book, string tempDir, string images)
        {
            if (book == null) return true;
            Console.Write("Extracting images...");
            foreach (var img in book.Elements("binary"))
            {
                var filePath = String.Format("{0}\\{1}\\{2}", tempDir, images, img.Attribute("id").Value);
                File.WriteAllBytes(filePath, Convert.FromBase64String(img.Value));
            }
            CompressImagesInFolder(tempDir + "\\images");
            Console.WriteLine("(OK)");
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
            GetFileFromResource("kindlegen.exe", tempDir + "\\kindlegen.exe");
            return tempDir;
        }

        private static void CompressImagesInFolder(string folder)
        {
            var files = Directory.GetFiles(folder, "*.jp*");
            foreach (var file in files)
            {
                try
                {
                    var tempFileName = Path.GetTempFileName();
                    using (var img = Image.FromFile(file))
                    {
                        var parList = new List<EncoderParameter>
                            {
                                new EncoderParameter(Encoder.Quality, 50L), 
                                new EncoderParameter(Encoder.ColorDepth, 8L)
                            };
                        var encoderParams = new EncoderParameters(parList.Count);
                        for (var i = 0; i < parList.Count; i++ )
                            encoderParams.Param[i] = parList[i];
                        var codec = GetEncoderInfo(Path.GetExtension(file));
                        img.Save(tempFileName, codec, encoderParams);
                    }
                    File.Delete(file);
                    File.Move(tempFileName, file);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private static ImageCodecInfo GetEncoderInfo(string extension)
        {
            extension = extension.ToLower();
            var codecs = ImageCodecInfo.GetImageEncoders();
            for (var i = 0; i < codecs.Length; i++)
                if (codecs[i].FilenameExtension.ToLower().Contains(extension))
                    return codecs[i];
            return null;
        }

        public static XElement AddAuthorsInfo(IEnumerable<XElement> avtorbook)
        {
            var element2 = new XElement("h2");
            foreach (var ai in avtorbook)
            {
                element2.Add(Value(ai.Elements("last-name")));
                element2.Add(new XElement("br"));
                element2.Add(Value(ai.Elements("first-name")));
                element2.Add(new XElement("br"));
                element2.Add(Value(ai.Elements("middle-name")));
                element2.Add(new XElement("br"));
            }
            return element2;
        }

        public static string TabRep(string Str)
        {
            return Str.Replace(Convert.ToChar(160).ToString(), "&nbsp;").Replace(Convert.ToChar(0xad).ToString(), "&shy;");
        }

        public static void CreateTitlePage(XElement element, string folder)
        {
            var linkEl = new XElement("link");
            linkEl.Add(new XAttribute("type", "text/css"));
            linkEl.Add(new XAttribute("href", "book.css"));
            linkEl.Add(new XAttribute("rel", "Stylesheet"));
            var headEl = new XElement("head");
            headEl.Add(linkEl);
            var content = new XElement("html");
            content.Add(headEl);
            headEl = new XElement("body");
            linkEl = new XElement("div");
            linkEl.Add(new XAttribute("class", "supertitle"));
            linkEl.Add(new XAttribute("align", "center"));
            linkEl.Add(new XAttribute("id", "booktitle"));
            linkEl.Add(AddAuthorsInfo(element.Elements("description").Elements("title-info").Elements("author")));
            linkEl.Add(new XElement("p", String.Format("{0} {1}", AttributeValue(element.Elements("description").Elements("title-info").Elements("sequence"), "name"), AttributeValue(element.Elements("description").Elements("title-info").Elements("sequence"), "number"))));
            linkEl.Add(new XElement("br"));
            var pEl = new XElement("p");
            pEl.Add(new XAttribute("class", "text-name"));
            pEl.Add(Value(element.Elements("description").Elements("title-info").Elements("book-title")));
            linkEl.Add(pEl);
            linkEl.Add(new XElement("br"));
            linkEl.Add(new XElement("p", Value(element.Elements("description").Elements("title-info").Elements("annotation"))));
            linkEl.Add(new XElement("br"));
            linkEl.Add(new XElement("br"));
            linkEl.Add(new XElement("p", Value(element.Elements("description").Elements("publish-info").Elements("publisher"))));
            linkEl.Add(new XElement("p", Value(element.Elements("description").Elements("publish-info").Elements("city"))));
            linkEl.Add(new XElement("p", Value(element.Elements("description").Elements("publish-info").Elements("year"))));
            linkEl.Add(new XElement("br"));
            linkEl.Add(new XElement("br"));
            linkEl.Add(new XElement("br"));
            linkEl.Add(new XElement("p", "Kindle version created by © Fb2Kindle"));
            linkEl.Add(new XElement("p", DateTime.Now.ToLongDateString()));
            linkEl.Add(new XElement("p", "Copyright © Sergey Egoshin (egoshin.sergey@gmail.com)"));
            linkEl.Add(new XElement("br"));
            headEl.Add(linkEl);
            content.Add(headEl);
            content.Save(folder + @"\booktitle.html");
        }

        public static string Value(IEnumerable<XElement> source)
        {
            foreach (var element in source)
                return element.Value;
            return null;
        }

        public static string AttributeValue(XElement source, XName name)
        {
            return (string) source.Attribute(name);
        }

        public static string AttributeValue(IEnumerable<XElement> source, XName name)
        {
            foreach (var element in source)
                return (string) element.Attribute(name);
            return null;
        }

        public static XAttribute CreateAttribute(XName name, object value)
        {
            return value == null ? null : new XAttribute(name, value);
        }

        public static void WriteObjectToFile(string filePath, object value, bool useFormatting = false)
        {
            if (value == null) return;
            var builder = new StringBuilder();
            var xmlFormatting = new XmlWriterSettings { OmitXmlDeclaration = true };
            if (useFormatting)
            {
                xmlFormatting.ConformanceLevel = ConformanceLevel.Document;
                xmlFormatting.Indent = true;
                xmlFormatting.NewLineOnAttributes = true;
            }
            using (Stream file = File.OpenWrite(filePath))
            {
                var ns = new XmlSerializerNamespaces();
                ns.Add("", "");
                new XmlSerializer(value.GetType()).Serialize(file, value, ns);
            }
        }

        public static T ReadObjectFromFile<T>(string fileName) where T : class
        {
            try
            {
                if (!File.Exists(fileName))
                    return null;
                using (Stream file = File.OpenRead(fileName))
                {
                    var serializer = new XmlSerializer(typeof(T));
                    return (T)serializer.Deserialize(file);
                }
            }
            catch (SerializationException)
            {
                return null;
            }
        }
    }
}
