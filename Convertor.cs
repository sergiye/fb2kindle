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
        private const string ImagesFolderName = "images";

        private string _tempDir;
        private readonly string _workingFolder;
        private readonly string _defaultCss;
        private XElement _ncxElement;
        private XElement _book;
        private XElement _opfFile;
        private XElement _tocEl;
        private DefaultOptions _currentSettings { get; set; }

        #region public

        public Convertor(DefaultOptions currentSettings, string workingFolder)
        {
            _currentSettings = currentSettings;
            _workingFolder = workingFolder;

            string defaultCss = null;
            if (File.Exists(currentSettings.defaultCSS))
                defaultCss = File.ReadAllText(currentSettings.defaultCSS, Encoding.UTF8);

            if (string.IsNullOrEmpty(defaultCss))
            {
                if (!string.IsNullOrEmpty(currentSettings.defaultCSS))
                    Console.WriteLine("Styles file not found: " + currentSettings.defaultCSS);
                _defaultCss = GetScriptFromResource("defstyles.css");
            }
        }

        public bool ConvertBook(string bookPath)
        {
            try
            {
                var bookName = Path.GetFileNameWithoutExtension(bookPath);
                Console.WriteLine("Processing: " + bookName);
                _book = LoadBookWithoutNs(bookPath);
                if (_book == null) return false;
                //create temp working folder
                _tempDir = PrepareTempFolder(bookName, ImagesFolderName, _workingFolder);
                if (_defaultCss.Contains("src: url(\"fonts/") && Directory.Exists(_workingFolder + @"\fonts"))
                {
                    Directory.CreateDirectory(_tempDir + @"\fonts");
                    CopyDirectory(_workingFolder + @"\fonts", _tempDir + @"\fonts", true);
                }
                File.WriteAllText(_tempDir + @"\book.css", _defaultCss);
                //create instances
                _opfFile = GetEmptyPackage(_book);
                _ncxElement = GetEmptyNcx();
                _tocEl = GetEmptyToc();
                if (!_currentSettings.ntoc)
                {
                    AddPackItem("ncx", "toc.ncx", "application/x-dtbncx+xml");
                    _opfFile.Add(new XElement("guide", ""));
                }
                AddPackItem("booktitle", "booktitle.html");
                AddGuideItem("Название", "booktitle.html", "start");

                //update images (extract and rewrite hrefs
                var imagesCreated = !_currentSettings.noImages && ExtractImages(_book, _tempDir);
                var list = RenameTags(_book, "image", "div", "image");
                foreach (var element in list)
                {
                    if (!imagesCreated)
                        element.Remove();
                    else
                    {
                        var src = element.Attribute("href").Value;
                        element.RemoveAll();
                        if (String.IsNullOrEmpty(src)) continue;
                        src = src.Replace("#", "");
                        var imgEl = new XElement("img");
                        imgEl.SetAttributeValue("src", ImagesFolderName + "/" + src);
                        element.Add(imgEl);
                    }
                }

                if (!_currentSettings.noImages)
                {
                    var imgSrc = AttributeValue(_book.Elements("description").Elements("title-info").Elements("coverpage").Elements("div").Elements("img"), "src");
                    AddCoverImage(_opfFile, imgSrc);
                }

                if (!_currentSettings.ntoc)
                {
                    AddNcxItem(0, "Обложка", "booktitle.html#booktitle");
                    AddNcxItem(1, "Содержание", "toc.html#toc");
                    AddPackItem("content", "toc.html");
                    AddGuideItem("Содержание", "toc.html", "toc");
                }

                ProcessAllData();

                _opfFile.Save(_tempDir + @"\" + bookName + ".opf");
                _opfFile.RemoveAll();

                if (!_currentSettings.ntoc)
                {
                    _ncxElement.Save(_tempDir + @"\toc.ncx");
                    _ncxElement.RemoveAll();

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
                try
                {
                    Directory.Delete(_tempDir, true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error clearing temp folder: " + ex.Message);
                }
                Console.WriteLine();
            }
        }

        #endregion public

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
                    AddPackItem(item.Name, item.Href);
                    AddGuideItem(item.Name, item.Href);
                    AddNcxItem(playOrder, item.Name, item.Href);
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

        private XElement AddTitleToToc(string title, string path, int playOrder, XElement toc)
        {
            if (_currentSettings.ntoc) return toc;
            title = title.Trim();
            AddNcxItem(playOrder + 2, title, path);
            AddGuideItem(title, path);
            var li = GetListItem(title, path); 
            toc.Add(li);
            return li;
        }

        private int SaveSubSections(XElement section, int bookNum, XElement toc)
        {
            var bookId = "book" + bookNum;
            var href = bookId + ".html";
            AddPackItem(bookId, href);
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
            SaveAsHtmlBook(section, _tempDir + "\\" + href);
            return bookNum;
        }

        private void ProcessAllData()
        {
            Console.Write("FB2 to HTML...");
            const string str = "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧЩШЬЪЫЭЮЯQWERTYUIOPASDFGHJKLZXCVBNM";
            CreateTitlePage(_book, _tempDir);

            var bodies = new List<XElement>();
            bodies.AddRange(_book.Elements("body"));
            var _notesList = new List<LinkItem>();
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
            if (_currentSettings.nch)
            {
                var i = 0;
                var ts = body.Descendants("title");
                foreach (var t in ts)
                {
                    if (!string.IsNullOrEmpty(t.Value))
                        AddTitleToToc(t.Value.Trim(), "book.html#" + string.Format("title{0}", i + 2), i,
                                      _tocEl.Elements("body").First().Elements("ul").First());
                    RenameTag(t, "div", "title");
                    var inner = new XElement("div");
                    inner.SetAttributeValue("class", i == 0 ? "title0" : "title1");
                    inner.SetAttributeValue("id", string.Format("title{0}", i + 2));
                    inner.Add(t.Nodes());
                    t.RemoveNodes();
                    t.Add(inner);
                    i++;
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
            ConvertTagsToHTML(body);
            //UpdateATags
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

            var playOrder = 0;
            if (_currentSettings.nch)
            {
                const string htmlFile = "book.html";
                AddPackItem("text", htmlFile);
                AddGuideItem("Название", htmlFile);
                SaveAsHtmlBook(body, _tempDir + @"\" + htmlFile);
            }
            else
            {
                Console.Write("Chapters creation...");
                playOrder = SaveSubSections(body, 0, _tocEl.Elements("body").First().Elements("ul").First());
                Console.WriteLine("(OK)");
            }
            AddNotesList(playOrder, _notesList);
            Console.WriteLine("(OK)");
        }

        private static bool CreateMobi(string tempDir, string bookName, string bookPath, bool compress)
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

        private static string PrepareTempFolder(string bookName, string images, string executingPath)
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

        private static XElement AddAuthorsInfo(IEnumerable<XElement> avtorbook)
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

        private static void CreateTitlePage(XElement book, string folder)
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

        private static void CreateNoteBox(XElement book, int i, string bodyName, string folder)
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
            ConvertTagsToHTML(packEl);
            packEl.Save(folder + @"\" + bodyName + ".html");
            packEl.RemoveAll();
        }

        private static void AddCoverImage(XElement opfFile, string imgSrc)
        {
            if (String.IsNullOrEmpty(imgSrc)) return;
            var coverEl = new XElement("EmbeddedCover", imgSrc);
            opfFile.Elements("metadata").First().Elements("dc-metadata").First().Elements("x-metadata").First().Add(coverEl);
            //<guide> <reference type="cover" title="Cover Image" href="cover.html" /> </guide> 
        }

        #region helper methods

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

        private static string GetScriptFromResource(string resourceName)
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

        private static void CopyDirectory(string sourceDirName, string destDirName, bool copySubDirs)
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

        private static ImageCodecInfo GetEncoderInfo(string extension)
        {
            extension = extension.ToLower();
            var codecs = ImageCodecInfo.GetImageEncoders();
            for (var i = 0; i < codecs.Length; i++)
                if (codecs[i].FilenameExtension.ToLower().Contains(extension))
                    return codecs[i];
            return null;
        }

        private static string Value(IEnumerable<XElement> source)
        {
            return source.Select(element => element.Value).FirstOrDefault();
        }

        private static string AttributeValue(IEnumerable<XElement> source, XName name)
        {
            return source.Select(element => (string) element.Attribute(name)).FirstOrDefault();
        }

        private static XElement[] RenameTags(XElement root, string tagName, string newName, string className = null, bool clearData = false)
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

        private static void ConvertTagsToHTML(XElement book)
        {
            RenameTags(book, "text-author", "P", "text-author");
            RenameTags(book, "empty-line", "br");
            RenameTags(book, "epigraph", "div", "epigraph");
            RenameTags(book, "subtitle", "div", "subtitle");
            RenameTags(book, "cite", "div", "cite");
            RenameTags(book, "emphasis", "i");
            RenameTags(book, "strong", "b");
            RenameTags(book, "poem", "div", "poem");
//            RenameTags(book, "stanza", "br");
            RenameTags(book, "v", "p");
//            RenameTags(book, "title", "div", "subtitle");
        }

        private static XElement LoadBookWithoutNs(string bookPath)
        {
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

        private void SaveAsHtmlBook(XElement bodyEl, string fileName, string style = "book")
        {
            var htmlDoc = InitEmptyHtmlDoc();
            htmlDoc.Elements("body").First().Add(bodyEl);
            RenameTags(htmlDoc, "section", "div", style);
            htmlDoc.Save(fileName);
            htmlDoc.RemoveAll();
        }

        private static XElement GetEmptyPackage(XElement book)
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

            opfFile.Add(new XElement("manifest"));
            opfFile.Add(new XElement("spine"));
            opfFile.Add(new XElement("guide"));

            return opfFile;
        }

        private static XElement GetEmptyNcx()
        {
            var ncxElement = new XElement("ncx");
            ncxElement.Add(new XElement("head", ""));
            ncxElement.Add(new XElement("docTitle", new XElement("text", "KF8")));
            ncxElement.Add(new XElement("navMap", ""));
            return ncxElement;
        }

        private static XElement GetEmptyToc()
        {
            var toc = new XElement("html", new XAttribute("type", "toc"));
            var headEl = new XElement("head");
            headEl.Add(new XElement("title", "Содержание"));
            var linkEl = new XElement("link");
            linkEl.Add(new XAttribute("type", "text/css"));
            linkEl.Add(new XAttribute("href", "book.css"));
            linkEl.Add(new XAttribute("rel", "Stylesheet"));
            headEl.Add(linkEl);
            toc.Add(headEl);
            headEl = new XElement("body");
            var content = new XElement("div");
            content.Add(new XAttribute("class", "title1"));
            content.Add(new XAttribute("id", "toc"));
            content.Add(new XElement("p", "Содержание"));
            linkEl = new XElement("div", new XAttribute("class", "title"));
            linkEl.Add(content);
            headEl.Add(linkEl);
            headEl.Add(new XElement("ul", ""));
            toc.Add(headEl);
            return toc;
        }

        private static XElement GetListItem(string name, string href)
        {
            return new XElement("li", new XElement("a", new XAttribute("href", href), name));
        }

        private void AddGuideItem(string name, string href, string itemType = "text")
        {
            if (_currentSettings.ntoc) return;
            var itemEl = new XElement("reference");
            itemEl.Add(new XAttribute("type", itemType));
            itemEl.Add(new XAttribute("title", name));
            itemEl.Add(new XAttribute("href", href));
            _opfFile.Elements("guide").First().Add(itemEl);
        }

        private void AddPackItem(string id, string href, string mediaType = "text/x-oeb1-document")
        {
            var packEl = new XElement("item");
            packEl.Add(new XAttribute("id", id));
            packEl.Add(new XAttribute("media-type", mediaType));
            packEl.Add(new XAttribute("href", href));
            packEl.Add("");
            _opfFile.Elements("manifest").First().Add(packEl);
            _opfFile.Elements("spine").First().Add(new XElement("itemref", new XAttribute("idref", id)));
        }

        private void AddNcxItem(int playOrder, string label, string href)
        {
            var navPoint = new XElement("navPoint");
            navPoint.Add(new XAttribute("id", "navpoint-" + playOrder));
            navPoint.Add(new XAttribute("playOrder", playOrder.ToString()));
            navPoint.Add(new XElement("navLabel", new XElement("text", label)));
            navPoint.Add(new XElement("content", new XAttribute("src", href)));
            _ncxElement.Elements("navMap").First().Add(navPoint);
        }

        #endregion helper methods
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
