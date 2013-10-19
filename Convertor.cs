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
        private XElement _book;
        private XElement _opfFile;
        private XElement _tocEl;
        private DefaultOptions _currentSettings { get; set; }

        #region public

        public Convertor(DefaultOptions currentSettings, string workingFolder)
        {
            _currentSettings = currentSettings;
            _workingFolder = workingFolder;

            if (File.Exists(currentSettings.defaultCSS))
                _defaultCss = File.ReadAllText(currentSettings.defaultCSS, Encoding.UTF8);

            if (!string.IsNullOrEmpty(_defaultCss)) return;
            if (!string.IsNullOrEmpty(currentSettings.defaultCSS))
                Console.WriteLine("Styles file not found: " + currentSettings.defaultCSS);
            _defaultCss = GetScriptFromResource("defstyles.css");
        }

        public bool ConvertBook(string bookPath)
        {
            try
            {
                var bookName = Path.GetFileNameWithoutExtension(bookPath).Trim();
                Console.WriteLine("Processing: " + bookName);
                _book = LoadBookWithoutNs(bookPath);
                if (_book == null) return false;
                //create temp working folder
                _tempDir = PrepareTempFolder(bookName, ImagesFolderName);
                if (_defaultCss.Contains("src: url(\"fonts/") && Directory.Exists(_workingFolder + @"\fonts"))
                {
                    Directory.CreateDirectory(_tempDir + @"\fonts");
                    CopyDirectory(_workingFolder + @"\fonts", _tempDir + @"\fonts", true);
                }
                File.WriteAllText(_tempDir + @"\book.css", _defaultCss);

                //create instances
                _opfFile = GetEmptyPackage(_book);
                _tocEl = GetEmptyToc();

                AddPackItem("ncx", "toc.ncx", null, "application/x-dtbncx+xml", false);
                AddPackItem("booktitle", "booktitle.html", "title-page");
                if (!_currentSettings.ntoc)
                {
                    AddPackItem("content", "toc.html", "toc");
                }

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

                ProcessAllData();

                SaveXmlToFile(_opfFile, _tempDir + @"\" + bookName + ".opf");
                _opfFile.RemoveAll();

                PrepareNcxFile(bookName);
                if (!_currentSettings.ntoc)
                {
                    SaveXmlToFile(_tocEl, _tempDir + @"\toc.html");
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
                    if (!Debugger.IsAttached)
                        Directory.Delete(_tempDir, true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error clearing temp folder: " + ex.Message);
                }
                Console.WriteLine();
            }
        }

        private void PrepareNcxFile(string bookName)
        {
            var ncx = new XElement("ncx");
            ncx.Add(new XElement("head", ""));
            ncx.Add(new XElement("docTitle", new XElement("text", bookName)));
            ncx.Add(new XElement("navMap", ""));
            AddNcxItem(ncx, 0, "Обложка", "booktitle.html#booktitle");
            AddNcxItem(ncx, 1, "Содержание", "toc.html#toc");
            var tocItems = _tocEl.Descendants("a");
            var playOrder = 2;
            foreach (var a in tocItems)
                AddNcxItem(ncx, playOrder++, a.Value, (string) a.Attribute("href"));
            SaveXmlToFile(ncx, _tempDir + @"\toc.ncx");
            ncx.RemoveAll();
        }

        #endregion public

        private void AddNotesList(List<KeyValuePair<string, string>> items)
        {
            if (items == null || items.Count <= 0) return;
            var tocStart = _tocEl.Descendants("ul").First();
            var prevMenuEl = tocStart;
            foreach (var item in items)
            {
                var subItem = item.Value.Contains("#");
                if (!subItem)
                {
                    AddPackItem(item.Key, item.Value, null);
                    var li = GetListItem(item.Key, item.Value);
                    tocStart.Add(li);
                    prevMenuEl = li;
                }
                else
                {
                    var si = prevMenuEl.Descendants("ul").FirstOrDefault();
                    if (si == null)
                    {
                        si = new XElement("ul");
                        prevMenuEl.Add(si);
                    }
                    si.Add(GetListItem(item.Key, item.Value));
                }
            }
        }

        private XElement AddTitleToToc(string title, string path, XElement toc)
        {
            title = title.Trim();
            var li = GetListItem(title, path); 
            toc.Add(li);
            return li;
        }

        private int SaveSubSections(XElement section, int bookNum, XElement toc)
        {
            var bookId = "book" + bookNum;
            var href = bookId + ".html";
            AddPackItem(bookId, href, bookNum == 0 ? "start" : "text");
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
                    string.Format("title{0}", bookNum + 2)), toc);
                if (section.Parent != null)
                    section.Remove();
            }
            bookNum++;
            while (true)
            {
                var firstSubSection = section.Descendants("section").FirstOrDefault();
                if (firstSubSection == null) break;
                firstSubSection.Remove();
                var si = toc.Descendants("ul").FirstOrDefault();
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
            var _notesList = new List<KeyValuePair<string, string>>();
            for (var i = 1; i < bodies.Count; i++)
            {
                var bodyName = (string) bodies[i].Attribute("name");
                if (String.IsNullOrEmpty(bodyName)) continue;
                _notesList.Add(new KeyValuePair<string, string>(bodyName, bodyName + ".html"));
                var list = bodies[i].Descendants("section").ToList();
                if (list.Count > 0)
                    foreach (var t in list)
                    {
                        var noteId = (string) t.Attribute("id");
                        var noteTitle = Value(t.Elements("title"));
                        if (string.IsNullOrEmpty(noteTitle))
                            noteTitle = Value(t.Elements("p"));
                        if (string.IsNullOrEmpty(noteTitle))
                            noteTitle = noteId;
                        _notesList.Add(new KeyValuePair<string, string>(noteTitle, bodyName + ".html#" + noteId));
                    }
                ConvertTagsToHTML(bodies[i], true);
                SaveAsHtmlBook(bodies[i], _tempDir + @"\" + bodyName + ".html");
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
                        AddTitleToToc(t.Value.Trim(), "book.html#" + string.Format("title{0}", i + 2), _tocEl.Descendants("ul").First());
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
                    if (!note.Value.EndsWith(src, StringComparison.OrdinalIgnoreCase)) continue;
                    var value = a.Value;
                    a.RemoveAll();
                    a.SetAttributeValue("href", note.Value);
                    a.Add(new XElement("sup", value));
                    break;
                }
            }

            if (_currentSettings.nch)
            {
                const string htmlFile = "book.html";
                AddPackItem("text", htmlFile, "start");
                SaveAsHtmlBook(body, _tempDir + @"\" + htmlFile);
            }
            else
                SaveSubSections(body, 0, _tocEl.Descendants("ul").First());
            AddNotesList(_notesList);
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

        private static string PrepareTempFolder(string bookName, string images)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), bookName);
            if (!Directory.Exists(tempDir))
                Directory.CreateDirectory(tempDir);
            if (!Directory.Exists(tempDir + @"\" + images))
                Directory.CreateDirectory(tempDir + @"\" + images);
            GetFileFromResource("kindlegen.exe", tempDir + "\\kindlegen.exe");
            return tempDir;
        }

        private static XElement AddAuthorsInfo(IEnumerable<XElement> avtorbook)
        {
            var element2 = new XElement("h2");
            foreach (var ai in avtorbook)
            {
                element2.Add(Value(ai.Elements("last-name"), "Неизвестный"));
                element2.Add(new XElement("br"));
                element2.Add(Value(ai.Elements("first-name"), "Безымян"));
                element2.Add(new XElement("br"));
                element2.Add(Value(ai.Elements("middle-name")));
                element2.Add(new XElement("br"));
            }
            return element2;
        }

        private static XElement GetCssLink()
        {
            var linkEl = new XElement("link");
            linkEl.Add(new XAttribute("type", "text/css"));
            linkEl.Add(new XAttribute("href", "book.css"));
            linkEl.Add(new XAttribute("rel", "Stylesheet"));
            return linkEl;
        }

        private static void CreateTitlePage(XElement book, string folder)
        {
            var headEl = new XElement("head");
            headEl.Add(GetCssLink());
            var content = new XElement("html");
            content.Add(headEl);
            headEl = new XElement("body");
            var linkEl = new XElement("div");
            linkEl.Add(new XAttribute("class", "supertitle"));
            linkEl.Add(new XAttribute("align", "center"));
            linkEl.Add(new XAttribute("id", "booktitle"));
            linkEl.Add(AddAuthorsInfo(book.Elements("description").Elements("title-info").Elements("author")));
            linkEl.Add(new XElement("p", String.Format("{0} {1}", AttributeValue(book.Elements("description").Elements("title-info").Elements("sequence"), "name"), 
                AttributeValue(book.Elements("description").Elements("title-info").Elements("sequence"), "number"))));
            linkEl.Add(new XElement("br"));
            var pEl = new XElement("p");
            pEl.Add(new XAttribute("class", "text-name"));
            pEl.Add(Value(book.Elements("description").Elements("title-info").Elements("book-title"), "Книга"));
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
            SaveXmlToFile(content, folder + @"\booktitle.html");
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
                        try
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
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error compressing image: " + ex.Message);
                            File.WriteAllBytes(file, fileBytes);
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

        private static string Value(IEnumerable<XElement> source, string defaultResult = null)
        {
            var value = source.Select(element => element.Value).FirstOrDefault();
            if (value == null || string.IsNullOrEmpty(value.Trim()))
                return defaultResult;
            return value.Trim();
        }

        private static string AttributeValue(IEnumerable<XElement> source, XName name, string defaultResult = null)
        {
            var value = source.Select(element => (string)element.Attribute(name)).FirstOrDefault();
            if (value == null || string.IsNullOrEmpty(value.Trim()))
                return defaultResult;
            return value.Trim();

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

        private static void ConvertTagsToHTML(XElement book, bool full = false)
        {
            RenameTags(book, "text-author", "P", "text-author");
            RenameTags(book, "empty-line", "br");
            RenameTags(book, "epigraph", "div", "epigraph");
            RenameTags(book, "subtitle", "div", "subtitle");
            RenameTags(book, "cite", "div", "cite");
            RenameTags(book, "emphasis", "i");
            RenameTags(book, "strong", "b");
            RenameTags(book, "poem", "div", "poem");
            RenameTags(book, "v", "p");
            if (!full) return;
            RenameTags(book, "stanza", "br");
            RenameTags(book, "title", "div", "subtitle");
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
            doc.Add(new XElement("head", GetCssLink()));
            doc.Add(new XElement("body", ""));
            return doc;
        }

        private static void SaveAsHtmlBook(XElement bodyEl, string fileName, string style = "book")
        {
            var htmlDoc = InitEmptyHtmlDoc();
            htmlDoc.Elements("body").First().Add(bodyEl);
            RenameTags(htmlDoc, "section", "div", style);
            SaveXmlToFile(htmlDoc, fileName);
            htmlDoc.RemoveAll();
        }

        private static void SaveXmlToFile(XElement xml, string file)
        {
            if (Debugger.IsAttached)
                xml.Save(file);
            else
                xml.Save(file, SaveOptions.DisableFormatting);
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
            content.Add(Value(book.Elements("description").Elements("title-info").Elements("book-title"), "Книга"));
            linkEl.Add(content);
            content = new XElement(nsHttp.GetName("Language"));
            var bookLang = Value(book.Elements("description").First().Elements("title-info").First().Elements("lang"));
            if (String.IsNullOrEmpty(bookLang))
                bookLang = "ru";
            content.Add(bookLang);
            linkEl.Add(content);
            content = new XElement(nsHttp.GetName("Creator"));
            content.Add(Value(book.Elements("description").Elements("title-info").Elements("author").First().Elements("last-name"), "Вася") + " " + Value(book.Elements("description").Elements("title-info").Elements("author").First().Elements("first-name")) + " " + Value(book.Elements("description").Elements("title-info").Elements("author").First().Elements("middle-name")));
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
            opfFile.Add(new XElement("spine", new XAttribute("toc", "ncx")));
            opfFile.Add(new XElement("guide", ""));
            return opfFile;
        }

        private static XElement GetEmptyToc()
        {
            var toc = InitEmptyHtmlDoc();
            toc.Add(new XAttribute("type", "toc"));
            toc.Elements("head").First().Add(new XElement("title", "Содержание"));
            var linkEl = new XElement("div", new XAttribute("class", "title"));
            var content = new XElement("div");
            content.Add(new XAttribute("class", "title1"));
            content.Add(new XAttribute("id", "toc"));
            content.Add(new XElement("p", "Содержание"));
            linkEl.Add(content);
            var body = toc.Elements("body").First();
            body.Add(linkEl);
            body.Add(new XElement("ul", ""));
            return toc;
        }

        private static XElement GetListItem(string name, string href)
        {
            return new XElement("li", new XElement("a", new XAttribute("href", href), name));
        }

        private void AddPackItem(string id, string href, string guideType = "text", string mediaType = "application/xhtml+xml", bool addSpine = true)
        {
            var packEl = new XElement("item");
            packEl.Add(new XAttribute("id", id));
            packEl.Add(new XAttribute("href", href));
            packEl.Add(new XAttribute("media-type", mediaType));
            _opfFile.Elements("manifest").First().Add(packEl);
            if (addSpine)
                _opfFile.Elements("spine").First().Add(new XElement("itemref", new XAttribute("idref", id)));
            if (!string.IsNullOrEmpty(guideType))
            {
                var itemEl = new XElement("reference");
                itemEl.Add(new XAttribute("type", guideType));
                itemEl.Add(new XAttribute("title", id));
                itemEl.Add(new XAttribute("href", href));
                _opfFile.Elements("guide").First().Add(itemEl);
            }
        }

        private static void AddNcxItem(XElement ncx, int playOrder, string label, string href)
        {
            var navPoint = new XElement("navPoint");
            navPoint.Add(new XAttribute("id", "navpoint-" + playOrder));
            navPoint.Add(new XAttribute("playOrder", playOrder.ToString()));
            navPoint.Add(new XElement("navLabel", new XElement("text", label)));
            navPoint.Add(new XElement("content", new XAttribute("src", href)));
            ncx.Elements("navMap").First().Add(navPoint);
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

    #endregion subclasses
}
