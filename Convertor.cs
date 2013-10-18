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
    class Convertor
    {
        private string _tempDir;
        private bool imagesPrepared;
        private readonly string _workingFolder;
        private readonly string _defaultCSS;
        private readonly bool _customFontsUsed;
        private XElement _ncxElement;
        private XElement _book;
        private XElement _opfFile;
        private XElement _tocEl;
        private List<LinkItem> _notesList;
        private List<LinkItem> _titles;
        private DefaultOptions _currentSettings { get; set; }

        public Convertor(DefaultOptions currentSettings, string workingFolder, string defaultCSS)
        {
            _currentSettings = currentSettings;
            _workingFolder = workingFolder;
            _defaultCSS = defaultCSS;
            if (_defaultCSS != null) 
                _customFontsUsed = _defaultCSS.Contains("src: url(\"fonts/");
        }

        public bool ConvertBook(string bookPath)
        {
            try
            {
                var bookName = Path.GetFileNameWithoutExtension(bookPath);
                _book = LoadBookWithoutNs(bookPath, bookName);
                if (_book == null) return false;
                //create temp working folder
                _tempDir = PrepareTempFolder(bookName, ImagesFolderName, _workingFolder);
                if (_customFontsUsed && Directory.Exists(_workingFolder + @"\fonts"))
                {
                    Directory.CreateDirectory(_tempDir + @"\fonts");
                    CopyDirectory(_workingFolder + @"\fonts", _tempDir + @"\fonts", true);
                }
                File.WriteAllText(_tempDir + @"\book.css", _defaultCSS);
                imagesPrepared = !_currentSettings.noImages && ExtractImages(_book, _tempDir);

                _opfFile = GetEmptyPackage(_book);
                UpdateImages();
                _ncxElement = CreareNcx();
                var imgSrc = AttributeValue(_book.Elements("description").Elements("title-info").Elements("coverpage").Elements("div").Elements("img"), "src");
                AddCoverImage(_opfFile, imgSrc);

                _tocEl = CreateEmptyToc();
                if (!_currentSettings.ntoc)
                {
                    AddTocToNcx();
                    AddTocToPack();
                }

                string bodyStr;
                bool notesCreated;
                var playOrder = 0;

                var bodyEl = ConvertToHtml();

                if (_currentSettings.nch)
                    CreateSingleBook(bodyEl);
                else
                    playOrder = CreateChapters(bodyEl);

                AddNotesList(playOrder, _notesList);

                _opfFile.Save(_tempDir + @"\" + bookName + ".opf");
                _opfFile.RemoveAll();

                _ncxElement.Save(_tempDir + @"\toc.ncx");
                _ncxElement.RemoveAll();

                if (!_currentSettings.ntoc)
                {
                    _tocEl.Save(_tempDir + @"\toc.html");
                    _tocEl.RemoveAll();
                }

                var result = CreateMobi(_tempDir, bookName, bookPath, _currentSettings.compression);
                if (result && _currentSettings.deleteOrigin)
                    File.Delete(bookPath);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unknown error: " + ex.Message);
                return false;
            }
            finally
            {
//                if (!Debugger.IsAttached)
                    ClearTempFolder();
                Console.WriteLine();
            }
        }

        private void AddNotesList(int playOrder, List<LinkItem> items)
        {
            if (items == null || items.Count <= 0) return;
            var tocStart = _tocEl.Elements("body").First().Elements("ul").First();
            var prevMenuEl = tocStart;
            foreach (var item in items)
            {
                var subItem = item.Href.Contains("#");
                if (!subItem)
                {
                    AddPackNoteItem(item, false);
                    AddNcxNoteItem(item, playOrder);
                }
                if (!_currentSettings.ntoc)
                {
                    if (subItem)
                    {
                        var si = prevMenuEl.Elements("ul").FirstOrDefault();
                        if (si == null)
                        {
                            si = new XElement("ul");
                            prevMenuEl.Add(si);
                        }
                        si.Add(GetListItem(item.Name, item.Href));
                    }
                    else
                    {
                        var li = GetListItem(item.Name, item.Href);
                        tocStart.Add(li);
                        prevMenuEl = li;
                    }
                }
                playOrder++;
            }
        }

        private static XElement GetListItem(string name, string href)
        {
            var itemEl = new XElement("li");
            var navLabel = new XElement("a");
            navLabel.Add(new XAttribute("href", href));
            navLabel.Add(name);
            itemEl.Add(navLabel);
            return itemEl;
        }

        private static XElement InitEmptyHtmlDoc()
        {
            var doc = new XElement("html");
            var linkEl = new XElement("link");
            linkEl.Add(new XAttribute("type", "text/css"));
            linkEl.Add(new XAttribute("href", "book.css"));
            linkEl.Add(new XAttribute("rel", "Stylesheet"));
            doc.Add(new XElement("head", linkEl));
            doc.Add(new XElement("body", ""));
            return doc;
        }

        private static XElement CreareNcx()
        {
            var ncxElement = new XElement("ncx");
            ncxElement.Add(new XElement("head", ""));
            ncxElement.Add(new XElement("docTitle", new XElement("text", "KF8")));
            ncxElement.Add(new XElement("navMap", ""));
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
            return ncxElement;
        }

        public void CreateSingleBook(XElement bodyEl)
        {
            const string htmlFile = "book.html";
            for (var i = 0; i < _titles.Count; i++)
                AddTitleToToc(_titles[i].Name, htmlFile + "#" + _titles[i].Href, i, _tocEl.Elements("body").First().Elements("ul").First());
            var itemEl = new XElement("item");
            itemEl.Add(new XAttribute("id", "text"));
            itemEl.Add(new XAttribute("media-type", "text/x-oeb1-document"));
            itemEl.Add(new XAttribute("href", htmlFile));
            itemEl.Add("");
            _opfFile.Elements("manifest").First().Add(itemEl);
            _opfFile.Elements("spine").First().Add(new XElement("itemref", new XAttribute("idref", "text")));
            itemEl = new XElement("reference");
            itemEl.Add(new XAttribute("type", "text"));
            itemEl.Add(new XAttribute("title", "Название"));
            itemEl.Add(new XAttribute("href", htmlFile));
            _opfFile.Elements("guide").First().Add(itemEl);
            SaveHtmlBook(bodyEl, _tempDir + @"\" + htmlFile);
        }

        private void SaveHtmlBook(XElement bodyEl, string fileName, string style = "book")
        {
            var htmlDoc = InitEmptyHtmlDoc();
            htmlDoc.Elements("body").First().Add(bodyEl);
            RenameTags(htmlDoc, "section", "div", style);
            htmlDoc.Save(fileName);
            htmlDoc.RemoveAll();
        }

        private XElement AddTitleToToc(string title, string path, int playOrder, XElement toc)
        {
            title = title.Trim();

            var navPoint = new XElement("navPoint");
            navPoint.Add(new XAttribute("id", "navpoint-" + (playOrder + 2).ToString()));
            navPoint.Add(new XAttribute("playOrder", (playOrder + 2).ToString()));
            var navLabel = new XElement("navLabel");
            navLabel.Add(new XElement("text", title));
            navPoint.Add(navLabel);
            navLabel = new XElement("content");
            navLabel.Add(new XAttribute("src", path));
            navPoint.Add(navLabel);
            _ncxElement.Elements("navMap").First().Add(navPoint);

            if (_currentSettings.ntoc) return toc;

            navPoint = new XElement("reference");
            navPoint.Add(new XAttribute("type", "text"));
            navPoint.Add(new XAttribute("title", title));
            navPoint.Add(new XAttribute("href", path));
            _opfFile.Elements("guide").First().Add(navPoint);

            navPoint = GetListItem(title, path); 
            toc.Add(navPoint);
            return navPoint;
        }

        private void ClearTempFolder()
        {
            try
            {
                Directory.Delete(_tempDir, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error clearing temp folder: " + ex.Message);
            }
        }

        private int SaveSubSections(XElement section, int bookNum, XElement toc)
        {
            var bookId = "book" + bookNum;
            var href = bookId + ".html";
            var itemEl = new XElement("item");
            itemEl.Add(new XAttribute("id", bookId));
            itemEl.Add(new XAttribute("media-type", "text/x-oeb1-document"));
            itemEl.Add(new XAttribute("href", href));
            itemEl.Add("");
            _opfFile.Elements("manifest").First().Add(itemEl);
            _opfFile.Elements("spine").First().Add(new XElement("itemref", new XAttribute("idref", bookId)));

            var t = section.Elements("title").FirstOrDefault();
            if (t != null && !string.IsNullOrEmpty(t.Value))
            {
                RenameTag(t, "div", "title");
                var inner = new XElement("div");
                inner.SetAttributeValue("class", bookNum == 0 ? "title0" : "title1");
                inner.SetAttributeValue("id", string.Format("title{0}", bookNum + 2));
                inner.Add(t.Nodes());
                t.RemoveNodes();
                t.Add(inner);
                toc = AddTitleToToc(t.Value.Trim(), string.Format("book{0}.html#{1}", bookNum,
                    string.Format("title{0}", bookNum + 2)), bookNum, toc);
                if (section.Parent != null)
                    section.Remove();
            }
            //process childs
            bookNum++;
            while (true)
            {
                var firstSubSection = section.Descendants("section").FirstOrDefault();
                if (firstSubSection == null) break;
                firstSubSection.Remove();
                var si = toc.Elements("ul").FirstOrDefault();
                if (si == null)
                {
                    si = new XElement("ul");
                    toc.Add(si);
                }
                bookNum = SaveSubSections(firstSubSection, bookNum, si);
            }
            SaveHtmlBook(section, _tempDir + "\\" + href);
            return bookNum;
        }
        
        private int CreateChapters(XElement bodyEl)
        {
            Console.Write("Chapters creation...");
            var curBookNum = SaveSubSections(bodyEl, 0, _tocEl.Elements("body").First().Elements("ul").First());
            Console.WriteLine("(OK)");
            return curBookNum;
        }

        public XElement ConvertToHtml()
        {
            Console.Write("FB2 to HTML...");
            const string str = "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧЩШЬЪЫЭЮЯQWERTYUIOPASDFGHJKLZXCVBNM";
            CreateTitlePage(_book, _tempDir);

            var bodies = new List<XElement>();
            bodies.AddRange(_book.Elements("body"));
            _notesList = new List<LinkItem>();
            for (var i = 1; i < bodies.Count; i++)
            {
                var bodyName = (string) bodies[i].Attribute("name");
                if (String.IsNullOrEmpty(bodyName)) continue;
                _notesList.Add(new LinkItem(bodyName, bodyName + ".html"));
                var list = bodies[i].Descendants("section").ToList();
                if (list.Count > 0)
                    foreach (var t in list)
                    {
                        var noteTitle = t.Elements("title").First().Value.Trim();
                        _notesList.Add(new LinkItem(string.IsNullOrEmpty(noteTitle) ? (string) t.Attribute("id") : noteTitle, bodyName + ".html#" + (string) t.Attribute("id")));
                    }
                CreateNoteBox(_book, i, bodyName, _tempDir);
            }

            var body = _book.Elements("body").First();
            body.Name = "section";
//            bodies[0].SetAttributeValue("class", "book");
//            var els = bodies[0].Descendants("section");
//            foreach (var el in els)
//            {
//                el.Name = "div";
//                el.SetAttributeValue("class", "book");
//            }

            if (_currentSettings.nch)
            {
                _titles = new List<LinkItem>();
                var idx = 0;
                var ts = body.Descendants("title");
                foreach (var t in ts)
                {
                    if (!string.IsNullOrEmpty(t.Value))
                        _titles.Add(new LinkItem(t.Value.Trim(), string.Format("title{0}", idx + 2)));
                    RenameTag(t, "div", "title");
                    var inner = new XElement("div");
                    inner.SetAttributeValue("class", idx == 0 ? "title0" : "title1");
                    inner.SetAttributeValue("id", string.Format("title{0}", idx + 2));
                    inner.Add(t.Nodes());
                    t.RemoveNodes();
                    t.Add(inner);
                    idx++;
                }
            }
            var els = body.Descendants("stanza");
            foreach (var el in els)
            {
                el.Name = "br";
                var parent = el.Parent;
                if (parent == null) continue;
                parent.Add(el.Nodes());
                el.RemoveAll();
                parent.Add(new XElement("br"));
            }
            ReplaceSomeTags(body);
            UpdateATags(body);
            Console.WriteLine("(OK)");
            return body;
        }

        private void AddTocToNcx()
        {
            var navPoint = new XElement("navPoint");
            navPoint.Add(new XAttribute("id", "navpoint-1"));
            navPoint.Add(new XAttribute("playOrder", "1"));
            navPoint.Add(new XElement("navLabel", new XElement("text", "Содержание")));
            navPoint.Add(new XElement("content", new XAttribute("src", "toc.html#toc")));
            _ncxElement.Elements("navMap").First().Add(navPoint);
        }

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
            Console.WriteLine("-c: use compression (slow)");
            Console.WriteLine("-save: save parameters to be used at the next start");
            Console.WriteLine("-a: process all files in current folder");
            Console.WriteLine("-r: process files in subfolders (work with -a key)");
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
            Console.WriteLine();
        }

        public static bool CreateMobi(string tempDir, string bookName, string bookPath, bool compress)
        {
            if (!File.Exists(tempDir + @"\kindlegen.exe"))
            {
                Console.WriteLine("kindlegen.exe not found");
                Directory.Delete(tempDir, true);
                return false;
            }
            Console.Write("Creating mobi (KF8)...");
            var args = String.Format("\"{0}\\{1}.opf\"", tempDir, bookName);
            if (compress)
                args += " -c2";
            var startInfo = new ProcessStartInfo { FileName = tempDir + @"\kindlegen.exe", Arguments = args, WindowStyle = ProcessWindowStyle.Hidden };
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

        public static void SaveElementToFile(string bodyContent, bool noBookFlag, string folder, int bookNum)
        {
            var text = InitEmptyHtmlDoc().ToString();
            //text = text.Replace(Convert.ToChar(160).ToString(), "&nbsp;").Replace(Convert.ToChar(0xad).ToString(), "&shy;");
            text = text.Insert(text.IndexOf("<body>") + 6, bodyContent);
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

        private static bool ExtractImages(XElement book, string tempDir)
        {
            if (book == null) return true;
            Console.Write("Extracting images...");
            foreach (var binEl in book.Elements("binary"))
            {
                try
                {
                    var file = String.Format("{0}\\{1}\\{2}", tempDir, ImagesFolderName, binEl.Attribute("id").Value);
                    var contentType = binEl.Attribute("content-type").Value;
                    var fileBytes = Convert.FromBase64String(binEl.Value);
                    if (contentType == "image/jpeg")
                    {
                        using (Stream str = new MemoryStream(fileBytes))
                        {
                            using (var img = Image.FromStream(str))
                            {
                                var parList = new List<EncoderParameter>
                                    {
                                        new EncoderParameter(Encoder.Quality, 50L), 
                                        //new EncoderParameter(Encoder.ColorDepth, 8L)
                                    };
                                var encoderParams = new EncoderParameters(parList.Count);
                                for (var i = 0; i < parList.Count; i++)
                                    encoderParams.Param[i] = parList[i];
                                var codec = GetEncoderInfo(Path.GetExtension(file));
                                img.Save(file, codec, encoderParams);
                            }
                        }
                    }
                    else
                    {
                        File.WriteAllBytes(file, fileBytes);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
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

        public static XElement[] RenameTags(XElement root, string tagName, string newName, string className = null, bool clearData = false)
        {
            var list = root.Descendants(tagName).ToArray();
            foreach (var element in list)
                RenameTag(element, newName, className, clearData);
            return list;
        }

        private static void RenameTag(XElement element, string newName, string className = null, bool clearData = false)
        {
            element.Name = newName;
            if (clearData)
            {
                element.Attributes().Remove();
                element.RemoveNodes();
            }
            if (!String.IsNullOrEmpty(className))
                element.SetAttributeValue("class", className);
        }

        private void UpdateImages()
        {
            var list = RenameTags(_book, "image", "div", "image");
            foreach (var element in list)
            {
                if (imagesPrepared)
                {
                    var src = element.Attribute("href").Value;
                    element.RemoveAll();
                    if (String.IsNullOrEmpty(src)) continue;
                    src = src.Replace("#", "");
                    var imgEl = new XElement("img");
                    imgEl.SetAttributeValue("src", ImagesFolderName + "/" + src);
                    element.Add(imgEl);
                }
                else
                    element.Remove();
            }
        }

        public static void ReplaceSomeTags(XElement el)
        {
            RenameTags(el, "text-author", "p", "text-author");
            RenameTags(el, "empty-line", "br");
            RenameTags(el, "emphasis", "i");
            RenameTags(el, "strong", "b");
            RenameTags(el, "v", "p");
            RenameTags(el, "epigraph", "div", "epigraph");
            RenameTags(el, "subtitle", "div", "subtitle");
            RenameTags(el, "cite", "div", "cite");
            RenameTags(el, "poem", "div", "poem");
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
            packEl.Save(folder + @"\" + bodyName + ".html");
            packEl.RemoveAll();
        }

        public static XElement CreateEmptyToc()
        {
            var toc = new XElement("html");
            toc.Add(new XAttribute("type", "toc"));
            var headEl = new XElement("head");
            headEl.Add(new XElement("title", "Содержание"));
            var linkEl = new XElement("link");
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

        public string UpdateATags(XElement body)
        {
            foreach (var a in body.Descendants("a"))
            {
                var src = a.Attribute("href").Value;
                if (String.IsNullOrEmpty(src)) continue;
                foreach (var note in _notesList)
                {
                    if (!note.Href.EndsWith(src, StringComparison.OrdinalIgnoreCase)) continue;
                    var value = a.Value;
                    a.RemoveAll();
                    a.SetAttributeValue("href", note.Href);
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
            var opfFile = new XElement("package");
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
            opfFile.Add(headEl);
            headEl = new XElement("manifest");
            linkEl = new XElement("item");
            linkEl.Add(new XAttribute("id", "ncx"));
            linkEl.Add(new XAttribute("media-type", "application/x-dtbncx+xml"));
            linkEl.Add(new XAttribute("href", "toc.ncx"));
            headEl.Add(linkEl);
            opfFile.Add(headEl);
            opfFile.Add(new XElement("spine", new XAttribute("toc", "ncx")));
            opfFile.Add(new XElement("guide", ""));

            var packEl = new XElement("item");
            packEl.Add(new XAttribute("id", "booktitle"));
            packEl.Add(new XAttribute("media-type", "text/x-oeb1-document"));
            packEl.Add(new XAttribute("href", "booktitle.html"));
            packEl.Add("");
            opfFile.Elements("manifest").First().Add(packEl);
            opfFile.Elements("spine").First().Add(new XElement("itemref", new XAttribute("idref", "booktitle")));
            packEl = new XElement("reference");
            packEl.Add(new XAttribute("type", "start"));
            packEl.Add(new XAttribute("title", "Название"));
            packEl.Add(new XAttribute("href", "booktitle.html"));
            opfFile.Elements("guide").First().Add(packEl);

            return opfFile;
        }

        public static void AddCoverImage(XElement opfFile, string imgSrc)
        {
            if (String.IsNullOrEmpty(imgSrc)) return;
            var coverEl = new XElement("EmbeddedCover", imgSrc);
            opfFile.Elements("metadata").First().Elements("dc-metadata").First().Elements("x-metadata").First().Add(coverEl);
            //<guide> <reference type="cover" title="Cover Image" href="cover.html" /> </guide> 
        }

        private void AddTocToPack()
        {
            var packEl = new XElement("item");
            packEl.Add(new XAttribute("id", "content"));
            packEl.Add(new XAttribute("media-type", "text/x-oeb1-document"));
            packEl.Add(new XAttribute("href", "toc.html"));
            packEl.Add("");
            _opfFile.Elements("manifest").First().Add(packEl);
            _opfFile.Elements("spine").First().Add(new XElement("itemref", new XAttribute("idref", "content")));

            packEl = new XElement("reference");
            packEl.Add(new XAttribute("type", "toc"));
            packEl.Add(new XAttribute("title", "Содержание"));
            packEl.Add(new XAttribute("href", "toc.html"));
            _opfFile.Elements("guide").First().Add(packEl);
        }

        private void AddNcxNoteItem(LinkItem item, int playOrder)
        {
            var itemEl = new XElement("navPoint");
            itemEl.Add(new XAttribute("id", "navpoint-" + item.Name));
            itemEl.Add(new XAttribute("playOrder", playOrder));
            var navLabel = new XElement("navLabel");
            var textEl = new XElement("text");
            textEl.Add(item.Name);
            navLabel.Add(textEl);
            itemEl.Add(navLabel);
            navLabel = new XElement("content");
            navLabel.Add(new XAttribute("src", item.Href));
            itemEl.Add(navLabel);
            _ncxElement.Elements("navMap").First().Add(itemEl);
        }

        private void AddPackNoteItem(LinkItem item, bool addToc)
        {
            var itemEl = new XElement("item");
            itemEl.Add(new XAttribute("id", item.Name));
            itemEl.Add(new XAttribute("media-type", "text/x-oeb1-document"));
            itemEl.Add(new XAttribute("href", item.Href));
            itemEl.Add("");
            _opfFile.Elements("manifest").First().Add(itemEl);
            _opfFile.Elements("spine").First().Add(new XElement("itemref", new XAttribute("idref", item.Name)));

            if (addToc)
            {
                itemEl = new XElement("reference");
                itemEl.Add(new XAttribute("type", "text"));
                itemEl.Add(new XAttribute("title", item.Name));
                itemEl.Add(new XAttribute("href", item.Href));
                _opfFile.Elements("guide").First().Add(itemEl);
            }
        }
    }

    #region subclasses

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
        public bool recursive { get; set; }
        public bool compression { get; set; }
        [XmlIgnore]
        public bool save { get; set; }
    }

    public class SectionInfo
    {
        public int Val1 { get; set; }
        public int Val2 { get; set; }
        public int Val3 { get; set; }
    }

    public class LinkItem
    {
        public string Name { get; set; }
        public string Href { get; set; }

        public LinkItem(string name, string href)
        {
            Name = name;
            Href = href;
        }
    }

    #endregion subclasses
}
