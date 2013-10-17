using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
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
        public bool nch { get; set; }
        public bool noImages { get; set; }
        public bool ntoc { get; set; }
        public string defaultCSS { get; set; }
        public string DropCap { get; set; }
        public bool all { get; set; }
        [XmlIgnore]
        public bool save { get; set; }
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
        public const string ImagesFolderName = "images";

        public static void FormatToHTML(XElement book)
        {
            RenameTags(book, "text-author", "P", "text-author");
            RenameTags(book, "empty-line", "br");
            RenameTags(book, "epigraph", "div", "epigraph");
            RenameTags(book, "epigraph", "div", "epigraph");
            RenameTags(book, "subtitle", "div", "subtitle");
            RenameTags(book, "cite", "div", "cite");
            RenameTags(book, "emphasis", "i");
            RenameTags(book, "strong", "b");
            RenameTags(book, "poem", "div", "poem");
            RenameTags(book, "stanza", "br");
            RenameTags(book, "v", "p");
            RenameTags(book, "title", "div", "subtitle");
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
            Console.WriteLine(Assembly.GetExecutingAssembly().GetName().Name + " <book.fb2> [-css <styles.css>] [-d] [-nb] [-nch] [-ni]");
            Console.WriteLine();
            Console.WriteLine("<book.fb2>: input fb2 file");
            Console.WriteLine("-css <styles.css>: styles used in destination book");
            Console.WriteLine("-d: delete source file after convertion");
            Console.WriteLine("-nb: no big letters at the chapter start");
            Console.WriteLine("-nch: no chapters");
            Console.WriteLine("-ni: no images");
            Console.WriteLine("-ntoc: no table of content");
            Console.WriteLine("-save: save parameters to be used at the next start");
            Console.WriteLine("-a: process all files in current folder");
            Console.WriteLine("-w: wait for key press on finish");
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
            Console.WriteLine();
            var assembly = Assembly.GetExecutingAssembly();
            var ver = Assembly.GetExecutingAssembly().GetName().Version;
            Console.WriteLine(assembly.GetName().Name + " Version: " + ver.ToString(3) + "; Build time: " + GetBuildTime(ver).ToString("yyyy/MM/dd HH:mm:ss"));
            var title = GetAttribute<AssemblyTitleAttribute>(assembly);
            if (title != null)
                Console.WriteLine(title.Title);
//            var copyright = GetAttribute<AssemblyCopyrightAttribute>(assembly);
//            if (copyright != null)
//                Console.WriteLine(copyright.Copyright);
            Console.WriteLine();
        }

        public static bool CreateMobi(string tempDir, string bookName, string bookPath)
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

        public static void SaveWithEncoding(string filePath, string text)
        {
            File.WriteAllText(filePath, text, Encoding.UTF8);
//            File.WriteAllText(filePath, "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + text, Encoding.UTF8);
        }

        public static void SaveElementToFile(string elementData, string bodyContent, bool noBookFlag, string folder, int bookNum)
        {
            var text = elementData;
            text = text.Insert(text.IndexOf("<body>") + 6, bodyContent);
            text = text.Replace("<sectio1", noBookFlag ? "<div class=\"nobook\"" : "<div class=\"book\"");
            text = text.Replace("</sectio1>", "</div>");
            SaveWithEncoding(folder + @"\book" + bookNum + ".html", text);
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

        public static void CreateTitlePage(XElement book, string folder)
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
            linkEl.Add(AddAuthorsInfo(book.Elements("description").Elements("title-info").Elements("author")));
            linkEl.Add(new XElement("p", String.Format("{0} {1}", AttributeValue(book.Elements("description").Elements("title-info").Elements("sequence"), "name"), AttributeValue(book.Elements("description").Elements("title-info").Elements("sequence"), "number"))));
            linkEl.Add(new XElement("br"));
            var pEl = new XElement("p");
            pEl.Add(new XAttribute("class", "text-name"));
            pEl.Add(Value(book.Elements("description").Elements("title-info").Elements("book-title")));
            linkEl.Add(pEl);
            linkEl.Add(new XElement("br"));
            linkEl.Add(new XElement("p", Value(book.Elements("description").Elements("title-info").Elements("annotation"))));
            linkEl.Add(new XElement("br"));
            linkEl.Add(new XElement("br"));
            linkEl.Add(new XElement("p", Value(book.Elements("description").Elements("publish-info").Elements("publisher"))));
            linkEl.Add(new XElement("p", Value(book.Elements("description").Elements("publish-info").Elements("city"))));
            linkEl.Add(new XElement("p", Value(book.Elements("description").Elements("publish-info").Elements("year"))));
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

        public static string AttributeValue(IEnumerable<XElement> source, XName name)
        {
            foreach (var element in source)
                return (string) element.Attribute(name);
            return null;
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

        public static XElement[] RenameTags(XElement root, string tagName, string newName, string css = null,bool clearData = false)
        {
            var list = root.Descendants(tagName).ToArray();
            foreach (var element in list)
            {
                element.Name = newName;
                if (clearData)
                {
                    element.Attributes().Remove();
                    element.RemoveNodes();
                }
                if (!String.IsNullOrEmpty(css))
                    element.SetAttributeValue("class", css);
            }
            return list;
        }

        public static void UpdateImages(XElement book, bool imagesPrepared)
        {
            var list = RenameTags(book, "image", "div", "image");
            foreach (var element in list)
            {
                if (!imagesPrepared) continue;
                var src = element.Attribute("href").Value;
                element.RemoveAll();
                if (String.IsNullOrEmpty(src)) continue;
                src = src.Replace("#", "");
                var imgEl = new XElement("img");
                imgEl.SetAttributeValue("src", ImagesFolderName + "/" + src);
                element.Add(imgEl);
            }
        }

        public static string ReplaceSomeTags(string bodyStr)
        {
            return bodyStr.Replace("<text-author>", "<p class=\"text-author\">").Replace("</text-author>", "</p>").
                           Replace("<empty-line />", "<br/>").Replace("<epigraph ", "<div class = \"epigraph\" ").
                           Replace("<epigraph>", "<div class = \"epigraph\">").Replace("</epigraph>", "</div>").
                           Replace("<empty-line/>", "<br/>").Replace("<subtitle ", "<div class = \"subtitle\" ").
                           Replace("<subtitle>", "<div class = \"subtitle\">").Replace("<cite ", "<div class = \"cite\" ").
                           Replace("<cite>", "<div class = \"cite\">").Replace("</cite>", "</div>").Replace("</subtitle>", "</div>").
                           Replace("<emphasis>", "<i>").Replace("</emphasis>", "</i>").Replace("<strong>", "<b>").
                           Replace("</strong>", "</b>").Replace("<poem", "<div class=\"poem\"").Replace("</poem>", "</div>").
                           Replace("<stanza>", "<br/>").Replace("</stanza>", "<br/>").Replace("<v>", "<p>").Replace("</v>", "</p>").
                           Replace("<titl1", "<div class = \"title\"><div class = \"title1\"").Replace("</titl1>", "</div></div>").
                           Replace("<titl2", "<div class = \"title\"><div class = \"title2\"").Replace("</titl2>", "</div></div>").
                           Replace("<titl3", "<div class = \"title\"><div class = \"title3\"").Replace("</titl3>", "</div></div>").
                           Replace("<titl4", "<div class = \"title\"><div class = \"title4\"").Replace("</titl4>", "</div></div>").
                           Replace("<titl5", "<div class = \"title\"><div class = \"title5\"").Replace("</titl5>", "</div></div>").
                           Replace("<titl6", "<div class = \"title\"><div class = \"title6\"").Replace("</titl6>", "</div></div>").
                           Replace("<titl7", "<div class = \"title\"><div class = \"title7\"").Replace("</titl7>", "</div></div>").
                           Replace("<titl8", "<div class = \"title\"><div class = \"title8\"").Replace("</titl8>", "</div></div>").
                           Replace("<titl9", "<div class = \"title\"><div class = \"title9\"").Replace("</titl9>", "</div></div>").
                           Replace("<body", "<sectio1").Replace("</body>", "</sectio1>").
                           Replace("<titl0", "<div class = \"title\"><div class = \"title0\"").Replace("</titl0>", "</div></div>");
        }

        public static void CreateNoteBox(XElement book, int i, string bodyName, string folder)
        {
            if (book == null) return;
            var packEl = new XElement("html");
            var headEl = new XElement("head");
            var linkEl = new XElement("link");
            linkEl.Add(new XAttribute("type", "text/css"));
            linkEl.Add(new XAttribute("href", "book.css"));
            linkEl.Add(new XAttribute("rel", "Stylesheet"));
            headEl.Add(linkEl);
            packEl.Add(headEl);
            headEl = new XElement("body");
            var body = book.Elements("body").ElementAtOrDefault(i);
            if (body != null)
                headEl.Add(body.Nodes());
            packEl.Add(headEl);
            FormatToHTML(packEl);
            //SaveWithEncoding(folder + @"\" + bodyName + ".html", packEl.ToString());
            packEl.Save(folder + @"\" + bodyName + ".html");
            packEl.RemoveAll();
        }

        public static XElement CreateEmptyToc()
        {
            var toc = new XElement("html");
            var headEl = new XElement("head");
            var linkEl = new XElement("title");
            linkEl.Add("Содержание");
            headEl.Add(linkEl);
            linkEl = new XElement("link");
            linkEl.Add(new XAttribute("type", "text/css"));
            linkEl.Add(new XAttribute("href", "book.css"));
            linkEl.Add(new XAttribute("rel", "Stylesheet"));
            headEl.Add(linkEl);
            toc.Add(headEl);
            headEl = new XElement("body");
            linkEl = new XElement("div");
            linkEl.Add(new XAttribute("class", "title"));
            var content = new XElement("div");
            content.Add(new XAttribute("class", "title1"));
            content.Add(new XAttribute("id", "toc"));
            content.Add(new XElement("p", "Содержание"));
            linkEl.Add(content);
            headEl.Add(linkEl);
            linkEl = new XElement("ul");
            linkEl.Add("");
            headEl.Add(linkEl);
            toc.Add(headEl);
            return toc;
        }

        public static string UpdateATags(XElement body, List<DataItem> notesList)
        {
            foreach (var a in body.Descendants("a"))
            {
                var src = a.Attribute("href").Value;
                if (String.IsNullOrEmpty(src)) continue;
                foreach (var note in notesList)
                {
                    if (!src.Equals(note.Value, StringComparison.OrdinalIgnoreCase)) continue;
                    var value = a.Value;
                    a.RemoveAll();
                    a.SetAttributeValue("href", note.Name + src);
                    a.Add(new XElement("sup", value));
                    break;
                }
            }
            return body.ToString();
        }

        public static XElement LoadBookWithoutNs(string bookPath, string bookName)
        {
            Console.WriteLine("Processing: " + bookName);
            try
            {
//                var xmlDoc = new XmlDocument();
//                xmlDoc.Load(bookPath);
                XElement book;
                using (Stream file = File.OpenRead(bookPath))
                {
                    book = XElement.Load(file, LoadOptions.PreserveWhitespace);
                }
                XNamespace ns = "";
                foreach (var el in book.DescendantsAndSelf())
                {
                    el.Name = ns.GetName(el.Name.LocalName);
                    var atList = el.Attributes().ToList();
                    el.Attributes().Remove();
                    foreach (var at in atList)
                        el.Add(new XAttribute(ns.GetName(at.Name.LocalName), at.Value));
                }
                book = new XElement("book", book.Elements("description"), book.Elements("body"), book.Elements("binary"));
                return book;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unknown file format: " + ex.Message);
                return null;
            }
        }

        public static XElement GetEmptyPackage(XElement book)
        {
            var packEl = new XElement("package");
            var linkEl = new XElement("meta");
            linkEl.Add(new XAttribute("name", "zero-gutter"));
            linkEl.Add(new XAttribute("content", "true"));
            var headEl = new XElement("metadata");
            headEl.Add(linkEl);
            linkEl = new XElement("meta");
            linkEl.Add(new XAttribute("name", "zero-margin"));
            linkEl.Add(new XAttribute("content", "true"));
            headEl.Add(linkEl);
            linkEl = new XElement("dc-metadata");
            linkEl.Add(new XAttribute(XNamespace.Get("http://www.w3.org/2000/xmlns/").GetName("dc"), "http://"));

            var nsHttp = XNamespace.Get("http://");
            var content = new XElement(nsHttp.GetName("Title"));
            content.Add(Value(book.Elements("description").Elements("title-info").Elements("book-title")));
            linkEl.Add(content);
            content = new XElement(nsHttp.GetName("Language"));
            var bookLang = Value(book.Elements("description").First().Elements("title-info").First().Elements("lang"));
            if (String.IsNullOrEmpty(bookLang))
                bookLang = "ru";
            content.Add(bookLang);
            linkEl.Add(content);
            content = new XElement(nsHttp.GetName("Creator"));
            content.Add(Value(book.Elements("description").Elements("title-info").Elements("author").First().Elements("last-name")) + " " + Value(book.Elements("description").Elements("title-info").Elements("author").First().Elements("first-name")) + " " + Value(book.Elements("description").Elements("title-info").Elements("author").First().Elements("middle-name")));
            linkEl.Add(content);
            content = new XElement(nsHttp.GetName("Publisher"));
            content.Add(Value(book.Elements("description").Elements("publish-info").Elements("publisher")));
            linkEl.Add(content);
            content = new XElement(nsHttp.GetName("date"));
            content.Add(Value(book.Elements("description").Elements("publish-info").Elements("year")));
            linkEl.Add(content);
            content = new XElement("x-metadata");
            content.Add("");
            linkEl.Add(content);
            headEl.Add(linkEl);
            packEl.Add(headEl);
            headEl = new XElement("manifest");
            linkEl = new XElement("item");
            linkEl.Add(new XAttribute("id", "ncx"));
            linkEl.Add(new XAttribute("media-type", "application/x-dtbncx+xml"));
            linkEl.Add(new XAttribute("href", "toc.ncx"));
            headEl.Add(linkEl);
            packEl.Add(headEl);
            headEl = new XElement("spine");
            headEl.Add(new XAttribute("toc", "ncx"));
            headEl.Add("");
            packEl.Add(headEl);
            headEl = new XElement("guide");
            headEl.Add("");
            packEl.Add(headEl);
            return packEl;
        }

        public static void AddTitleToPackage(XElement packElement)
        {
            var packEl = new XElement("item");
            packEl.Add(new XAttribute("id", "booktitle"));
            packEl.Add(new XAttribute("media-type", "text/x-oeb1-document"));
            packEl.Add(new XAttribute("href", "booktitle.html"));
            packEl.Add("");
            packElement.Elements("manifest").First().Add(packEl);
            packEl = new XElement("itemref");
            packEl.Add(new XAttribute("idref", "booktitle"));
            packElement.Elements("spine").First().Add(packEl);
            packEl = new XElement("reference");
            packEl.Add(new XAttribute("type", "start"));
            packEl.Add(new XAttribute("title", "Заглавие"));
            packEl.Add(new XAttribute("href", "booktitle.html"));
            packElement.Elements("guide").First().Add(packEl);
        }

        public static void AddCoverImage(XElement ncxElement, XElement packElement, string imgSrc)
        {
            var packEl = new XElement("navPoint");
            packEl.Add(new XAttribute("id", "navpoint-0"));
            packEl.Add(new XAttribute("playOrder", "0"));
            var headEl = new XElement("navLabel");
            headEl.Add(new XElement("text", "Обложка"));
            packEl.Add(headEl);
            headEl = new XElement("content");
            headEl.Add(new XAttribute("src", "booktitle.html#booktitle"));
            packEl.Add(headEl);
            ncxElement.Elements("navMap").First().Add(packEl);

            if (String.IsNullOrEmpty(imgSrc)) return;
            var coverEl = new XElement("EmbeddedCover");
            coverEl.Add(imgSrc);
            packElement.Elements("metadata").First().Elements("dc-metadata").First().Elements("x-metadata").First().Add(coverEl);
        }

        public static void AddTocToNcx(int playOrder, XElement ncxElement)
        {
            var navPoint = new XElement("navPoint");
            navPoint.Add(new XAttribute("id", "navpoint-" + playOrder.ToString()));
            navPoint.Add(new XAttribute("playOrder", playOrder.ToString()));
            navPoint.Add(new XElement("navLabel", new XElement("text", "Содержание")));
            navPoint.Add(new XElement("content", new XAttribute("src", "toc.html#toc")));
            ncxElement.Elements("navMap").First().Add(navPoint);
        }

        public static void AddTocToPack(XElement packElement, string id)
        {
            var packEl = new XElement("item");
            packEl.Add(new XAttribute("id", id));
            packEl.Add(new XAttribute("media-type", "text/x-oeb1-document"));
            packEl.Add(new XAttribute("href", "toc.html"));
            packEl.Add("");
            packElement.Elements("manifest").First().Add(packEl);
            packEl = new XElement("itemref");
            packEl.Add(new XAttribute("idref", id));
            packElement.Elements("spine").First().Add(packEl);
            packEl = new XElement("reference");
            packEl.Add(new XAttribute("type", "toc"));
            packEl.Add(new XAttribute("title", "Содержание"));
            packEl.Add(new XAttribute("href", "toc.html"));
            packElement.Elements("guide").First().Add(packEl);
        }

        public static void AddTocNoteItem(DataItem item, XElement tocEl)
        {
            var itemEl = new XElement("li");
            var navLabel = new XElement("a");
            navLabel.Add(new XAttribute("href", item.Name));
            navLabel.Add(item.Value);
            itemEl.Add(navLabel);
            tocEl.Elements("body").First().Elements("ul").First().Add(itemEl);
        }

        public static void AddNcxNoteItem(DataItem item, int playOrder, XElement ncxElement)
        {
            var itemEl = new XElement("navPoint");
            itemEl.Add(new XAttribute("id", "navpoint-" + item.Value));
            itemEl.Add(new XAttribute("playOrder", playOrder));
            var navLabel = new XElement("navLabel");
            var textEl = new XElement("text");
            textEl.Add(item.Value);
            navLabel.Add(textEl);
            itemEl.Add(navLabel);
            navLabel = new XElement("content");
            navLabel.Add(new XAttribute("src", item.Name));
            itemEl.Add(navLabel);
            ncxElement.Elements("navMap").First().Add(itemEl);
        }

        public static void AddPackNoteItem(DataItem item, XElement packElement)
        {
            var itemEl = new XElement("item");
            itemEl.Add(new XAttribute("id", item.Value));
            itemEl.Add(new XAttribute("media-type", "text/x-oeb1-document"));
            itemEl.Add(new XAttribute("href", item.Name));
            itemEl.Add("");
            packElement.Elements("manifest").First().Add(itemEl);
            itemEl = new XElement("itemref");
            itemEl.Add(new XAttribute("idref", item.Value));
            packElement.Elements("spine").First().Add(itemEl);
        }
    }
}
