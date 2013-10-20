using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;
using Encoder = System.Drawing.Imaging.Encoder;

namespace Fb2Kindle
{
    class Convertor
    {
        private const string ImagesFolderName = "image";
        private const string NcxName = "toc.ncx";

        private string _tempDir;
        private readonly string _workingFolder;
        private readonly string _defaultCss;
        private readonly bool _addGuideLine;
        private XElement _book;
        private XElement _opfFile;
        private XElement _tocEl;
        private readonly string _kindleGenPath;
        private DefaultOptions _currentSettings { get; set; }

        #region public

        public Convertor(DefaultOptions currentSettings, string workingFolder, bool addGuideLine = false)
        {
            _currentSettings = currentSettings;
            _workingFolder = workingFolder;
            _addGuideLine = addGuideLine;

            if (File.Exists(currentSettings.defaultCSS))
                _defaultCss = File.ReadAllText(currentSettings.defaultCSS, Encoding.UTF8);

            if (!String.IsNullOrEmpty(_defaultCss)) return;
            if (!String.IsNullOrEmpty(currentSettings.defaultCSS))
                Console.WriteLine("Styles file not found: " + currentSettings.defaultCSS);
            _defaultCss = Util.GetScriptFromResource("defstyles.css");

            _kindleGenPath = _workingFolder + "\\kindlegen.exe";
            if (!File.Exists(_kindleGenPath))
                Util.GetFileFromResource("kindlegen.exe", _kindleGenPath);
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
                _tempDir = Path.Combine(Path.GetTempPath(), bookName);
                if (!Directory.Exists(_tempDir))
                    Directory.CreateDirectory(_tempDir);

                if (_defaultCss.Contains("src: url(\"fonts/") && Directory.Exists(_workingFolder + @"\fonts"))
                {
                    Directory.CreateDirectory(_tempDir + @"\fonts");
                    Util.CopyDirectory(_workingFolder + @"\fonts", _tempDir + @"\fonts", true);
                }
                File.WriteAllText(_tempDir + @"\book.css", _defaultCss);

                //create instances
                _opfFile = GetEmptyPackage(_book, _currentSettings.addSequence);
                _tocEl = GetEmptyToc();

                AddPackItem("ncx", NcxName, "application/x-dtbncx+xml", false);
                AddPackItem("booktitle", "booktitle.html");
                AddGuideItem("Title", "booktitle.html", "title-page");

                if (!_currentSettings.ntoc)
                {
                    AddPackItem("content", "toc.html");
                    AddGuideItem("toc", "toc.html", "toc");
                }

                //update images (extract and rewrite hrefs
                var imagesCreated = !_currentSettings.noImages && ExtractImages(_book, _tempDir);
                var list = Util.RenameTags(_book, "image", "div", "image");
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
                    var imgSrc = Util.AttributeValue(_book.Elements("description").Elements("title-info").Elements("coverpage").Elements("div").Elements("img"), "src");
                    if (!String.IsNullOrEmpty(imgSrc))
                    {
                        _opfFile.Elements("metadata").First().Elements("dc-metadata").First().Elements("x-metadata").First().Add(new XElement("EmbeddedCover", imgSrc));
                        AddGuideItem("Cover", imgSrc, "cover");
                    }
                }

                ProcessAllData();

                SaveXmlToFile(_opfFile, _tempDir + @"\" + bookName + ".opf");
                _opfFile.RemoveAll();

                CreateNcxFile(_tocEl, bookName, _tempDir, _currentSettings.ntoc);
                if (!_currentSettings.ntoc)
                {
                    SaveXmlToFile(_tocEl, _tempDir + @"\toc.html");
                    _tocEl.RemoveAll();
                }

                var result = CreateMobi(_tempDir, bookName, bookPath);
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

        #region ncx

        private static void AddNcxItem(XElement navMap, int playOrder, string label, string href)
        {
            var navPoint = new XElement("navPoint");
            navPoint.Add(new XAttribute("id", "p_" + playOrder));
            navPoint.Add(new XAttribute("playOrder", playOrder.ToString()));
            navPoint.Add(new XElement("navLabel", new XElement("text", label)));
            navPoint.Add(new XElement("content", new XAttribute("src", href)));
            navMap.Add(navPoint);
        }

        private static void CreateNcxFile(XElement toc, string bookName, string folder, bool addToc)
        {
            var ncx = new XElement("ncx");
            var head = new XElement("head", "");
            head.Add(new XElement("meta", new XAttribute("name", "dtb:uid"), new XAttribute("content", "BookId")));
            head.Add(new XElement("meta", new XAttribute("name", "dtb:depth"), new XAttribute("content", "1")));
            head.Add(new XElement("meta", new XAttribute("name", "dtb:totalPageCount"), new XAttribute("content", "0")));
            head.Add(new XElement("meta", new XAttribute("name", "dtb:maxPageNumber"), new XAttribute("content", "0")));
            ncx.Add(head);
            ncx.Add(new XElement("docTitle", new XElement("text", bookName)));
            ncx.Add(new XElement("docAuthor", new XElement("text", "fb2Kindle")));
            var navMap = new XElement("navMap", "");
            AddNcxItem(navMap, 0, "Обложка", "booktitle.html#booktitle");
            if (!addToc)
                AddNcxItem(navMap, 1, "Содержание", "toc.html#toc");
            var tocItems = toc.Descendants("a");
            var playOrder = 2;
            foreach (var a in tocItems)
                AddNcxItem(navMap, playOrder++, a.Value, (string)a.Attribute("href"));
            ncx.Add(navMap);
            SaveXmlToFile(ncx, folder + "\\" + NcxName);
            ncx.RemoveAll();
        }

        #endregion ncx

        #endregion public

        private void AddNotesList(List<KeyValuePair<string, string>> items)
        {
            if (items == null || items.Count <= 0) return;
            var tocStart = _tocEl.Descendants("ul").First();
            var prevMenuEl = tocStart;
            foreach (var item in items)
            {
                if (!item.Value.Contains("#"))
                {
                    AddPackItem(item.Key, item.Value);
                    var li = GetListItem(item.Key, item.Value);
                    tocStart.Add(li);
                    prevMenuEl = li;
                }
                else
                {
                    if (!_currentSettings.addNotesToToc) continue;
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

        private static XElement AddTitleToToc(string title, string path, XElement toc)
        {
            var li = GetListItem(title.Trim(), path); 
            toc.Add(li);
            return li;
        }

        private int SaveSubSections(XElement section, int bookNum, XElement toc)
        {
            var bookId = "book" + bookNum;
            var href = bookId + ".html";
            var t = section.Elements("title").FirstOrDefault();
            if (t != null && !String.IsNullOrEmpty(t.Value))
            {
                Util.RenameTag(t, "div", "title");
                var inner = new XElement("div");
                inner.SetAttributeValue("class", bookNum == 0 ? "title0" : "title1");
                inner.SetAttributeValue("id", String.Format("title{0}", bookNum + 2));
                inner.Add(t.Nodes());
                t.RemoveNodes();
                t.Add(inner);
                toc = AddTitleToToc(t.Value.Trim(), String.Format("book{0}.html#{1}", bookNum,
                    String.Format("title{0}", bookNum + 2)), toc);
                if (section.Parent != null)
                    section.Remove();
            }
            AddPackItem(bookId, href);
            AddGuideItem(bookId, href, bookNum == 0 ? "start" : "text");
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

            CreateTitlePage(_book, _tempDir);

            var _notesList = new List<KeyValuePair<string, string>>();
            var bodies = _book.Elements("body").ToArray();
            for (var i = 1; i < bodies.Length; i++)
            {
                var bodyName = (string)bodies[i].Attribute("name");
                if (String.IsNullOrEmpty(bodyName)) continue;
                _notesList.Add(new KeyValuePair<string, string>(bodyName, bodyName + ".html"));
                var list = bodies[i].Descendants("section").ToList();
                if (list.Count > 0)
                    foreach (var t in list)
                    {
                        var noteId = (string) t.Attribute("id");
                        var noteTitle = Util.Value(t.Elements("title"));
                        if (String.IsNullOrEmpty(noteTitle))
                            noteTitle = Util.Value(t.Elements("p"));
                        if (String.IsNullOrEmpty(noteTitle))
                            noteTitle = noteId;
                        _notesList.Add(new KeyValuePair<string, string>(noteTitle, bodyName + ".html#" + noteId));
                    }
                bodies[i].Name = "section";
                ConvertTagsToHTML(bodies[i], true);
                SaveAsHtmlBook(bodies[i], _tempDir + @"\" + bodyName + ".html");
            }

            var body = _book.Elements("body").First();
            body.Name = "section";
            if (!_currentSettings.noBig)
                SetBigFirstLetters(body);

            if (_currentSettings.nch)
            {
                var i = 0;
                var ts = body.Descendants("title");
                foreach (var t in ts)
                {
                    if (!String.IsNullOrEmpty(t.Value))
                        AddTitleToToc(t.Value.Trim(), "book.html#" + String.Format("title{0}", i + 2), _tocEl.Descendants("ul").First());
                    Util.RenameTag(t, "div", "title");
                    var inner = new XElement("div");
                    inner.SetAttributeValue("class", i == 0 ? "title0" : "title1");
                    inner.SetAttributeValue("id", String.Format("title{0}", i + 2));
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
            UpdateNotesRefs(body, _notesList);

            if (_currentSettings.nch)
            {
                const string htmlFile = "book.html";
                AddPackItem("text", htmlFile);
                AddGuideItem("Book", htmlFile, "start");
                SaveAsHtmlBook(body, _tempDir + @"\" + htmlFile);
            }
            else
                SaveSubSections(body, 0, _tocEl.Descendants("ul").First());

            AddNotesList(_notesList);
            Console.WriteLine("(OK)");
        }

        private static void UpdateNotesRefs(XElement body, List<KeyValuePair<string, string>> _notesList)
        {
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
        }

        private void SetBigFirstLetters(XElement body)
        {
            var sections = body.Descendants("section");
            foreach (var sec in sections)
            {
                var newPart = true;
                foreach (var t in sec.Elements())
                {
                    switch (t.Name.ToString())
                    {
                        case "title":
                        case "subtitle":
                            newPart = true;
                            break;
                        case "p":
                            var pVal = t.Value;
                            if (String.IsNullOrEmpty(pVal))
                                continue;
                            pVal = pVal.Trim();
                            var firstSymbol = pVal.Substring(0, 1);
                            if (!_currentSettings.DropCap.Contains(firstSymbol))
                                continue;
                            t.RemoveAll();
                            var span = new XElement("span", firstSymbol);
                            if (newPart)
                            {
                                t.SetAttributeValue("style", "text-indent:0px;");
                                span.SetAttributeValue("class", "dropcaps");
                                newPart = false;
                            }
                            else
                                span.SetAttributeValue("class", "dropcaps2");
                            t.Add(span);
                            t.Add(pVal.Substring(1));
                            break;
                        default:
                            continue;
                    }
                }
            }
        }

        private bool CreateMobi(string tempDir, string bookName, string bookPath)
        {
            Console.WriteLine("Creating mobi (KF8)...");
            if (!File.Exists(_kindleGenPath))
            {
                Console.WriteLine("kindlegen not found");
                return false;
            }
            var args = String.Format("\"{0}\\{1}.opf\"", tempDir, bookName);
            if (_currentSettings.compression)
                args += " -c2";

            var res = Util.StartProcess(_kindleGenPath, args, _currentSettings.detailedOutput);
            if (res == 2)
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
            if (!_currentSettings.detailedOutput)
                Console.WriteLine("(OK)");
            return true;
        }

        private static XElement AddAuthorsInfo(IEnumerable<XElement> avtorbook)
        {
            var element2 = new XElement("h2");
            foreach (var ai in avtorbook)
            {
                element2.Add(Util.Value(ai.Elements("last-name"), "Неизвестный"));
                element2.Add(new XElement("br"));
                element2.Add(Util.Value(ai.Elements("first-name"), "Безымян"));
                element2.Add(new XElement("br"));
                element2.Add(Util.Value(ai.Elements("middle-name")));
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
            linkEl.Add(new XElement("p", String.Format("{0} {1}", Util.AttributeValue(book.Elements("description").Elements("title-info").Elements("sequence"), "name"), 
                Util.AttributeValue(book.Elements("description").Elements("title-info").Elements("sequence"), "number"))));
            linkEl.Add(new XElement("br"));
            var pEl = new XElement("p");
            pEl.Add(new XAttribute("class", "text-name"));
            pEl.Add(Util.Value(book.Elements("description").Elements("title-info").Elements("book-title"), "Книга"));
            linkEl.Add(pEl);
            linkEl.Add(new XElement("br"));
            linkEl.Add(new XElement("p", Util.Value(book.Elements("description").Elements("title-info").Elements("annotation"))));
            linkEl.Add(new XElement("br"));
            linkEl.Add(new XElement("br"));
            linkEl.Add(new XElement("p", Util.Value(book.Elements("description").Elements("publish-info").Elements("publisher"))));
            linkEl.Add(new XElement("p", Util.Value(book.Elements("description").Elements("publish-info").Elements("city"))));
            linkEl.Add(new XElement("p", Util.Value(book.Elements("description").Elements("publish-info").Elements("year"))));
            linkEl.Add(new XElement("br"));
            linkEl.Add(new XElement("br"));
            linkEl.Add(new XElement("br"));
            var ver = Assembly.GetExecutingAssembly().GetName().Version;
            linkEl.Add(new XElement("p", "Kindle book was created by © Fb2Kindle (ver. " + ver.ToString(3) + ")"));
            linkEl.Add(new XElement("p", "Copyright © Sergey Egoshin (egoshin.sergey@gmail.com)"));
            linkEl.Add(new XElement("br"));
            headEl.Add(linkEl);
            content.Add(headEl);
            SaveXmlToFile(content, folder + @"\booktitle.html");
        }

        #region helper methods

        private static bool ExtractImages(XElement book, string tempDir)
        {
            if (book == null) return true;
            Console.Write("Extracting images...");
            if (!Directory.Exists(tempDir + @"\" + ImagesFolderName))
                Directory.CreateDirectory(tempDir + @"\" + ImagesFolderName);
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
                                    var codec = Util.GetEncoderInfo(Path.GetExtension(file));
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

        private static void ConvertTagsToHTML(XElement book, bool full = false)
        {
            Util.RenameTags(book, "text-author", "P", "text-author");
            Util.RenameTags(book, "empty-line", "br");
            Util.RenameTags(book, "epigraph", "div", "epigraph");
            Util.RenameTags(book, "subtitle", "div", "subtitle");
            Util.RenameTags(book, "cite", "div", "cite");
            Util.RenameTags(book, "emphasis", "i");
            Util.RenameTags(book, "strong", "b");
            Util.RenameTags(book, "poem", "div", "poem");
            Util.RenameTags(book, "v", "p");
            if (!full) return;
            Util.RenameTags(book, "stanza", "br");
            Util.RenameTags(book, "title", "div", "subtitle");
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
            Util.RenameTags(htmlDoc, "section", "div", style);
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

        private static XElement GetEmptyPackage(XElement book, bool addSequenceToTitle)
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
            var bookTitle = Util.Value(book.Elements("description").Elements("title-info").Elements("book-title"), "Книга");
            if (addSequenceToTitle)
            {
                bookTitle = string.Format("{0} {1} {2}", 
                    Util.AttributeValue(book.Elements("description").Elements("title-info").Elements("sequence"), "name"),
                    Util.AttributeValue(book.Elements("description").Elements("title-info").Elements("sequence"), "number"), 
                    bookTitle);
            }

            content.Add(bookTitle);
            linkEl.Add(content);
            content = new XElement(nsHttp.GetName("Language"));
            var bookLang = Util.Value(book.Elements("description").First().Elements("title-info").First().Elements("lang"));
            if (String.IsNullOrEmpty(bookLang))
                bookLang = "ru";
            content.Add(bookLang);
            linkEl.Add(content);
            content = new XElement(nsHttp.GetName("Creator"));
            content.Add(Util.Value(book.Elements("description").Elements("title-info").Elements("author").First().Elements("last-name"), "Вася") + " " + Util.Value(book.Elements("description").Elements("title-info").Elements("author").First().Elements("first-name")) + " " + Util.Value(book.Elements("description").Elements("title-info").Elements("author").First().Elements("middle-name")));
            linkEl.Add(content);
            content = new XElement(nsHttp.GetName("Publisher"));
            content.Add(Util.Value(book.Elements("description").Elements("publish-info").Elements("publisher")));
            linkEl.Add(content);
            content = new XElement(nsHttp.GetName("date"));
            content.Add(Util.Value(book.Elements("description").Elements("publish-info").Elements("year")));
            linkEl.Add(content);
            content = new XElement("x-metadata");
            content.Add("");
            linkEl.Add(content);
            headEl.Add(linkEl);
            opfFile.Add(headEl);

            opfFile.Add(new XElement("manifest"));
            opfFile.Add(new XElement("spine", new XAttribute("toc", "ncx")));
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

        private void AddPackItem(string id, string href, string mediaType = "application/xhtml+xml", bool addSpine = true)
        {
            var packEl = new XElement("item");
            packEl.Add(new XAttribute("id", id));
            packEl.Add(new XAttribute("href", href));
            packEl.Add(new XAttribute("media-type", mediaType));
            _opfFile.Elements("manifest").First().Add(packEl);
            if (addSpine)
                _opfFile.Elements("spine").First().Add(new XElement("itemref", new XAttribute("idref", id)));
        }

        private void AddGuideItem(string id, string href, string guideType = "text")
        {
            if (!_addGuideLine) return;
            if (String.IsNullOrEmpty(guideType)) return;
            var itemEl = new XElement("reference");
            itemEl.Add(new XAttribute("type", guideType));
            itemEl.Add(new XAttribute("title", id));
            itemEl.Add(new XAttribute("href", href));
            var guide = _opfFile.Elements("guide").FirstOrDefault();
            if (guide == null)
            {
                guide = new XElement("guide", "");
                _opfFile.Add(guide);
            }
            guide.Add(itemEl);
        }

        #endregion helper methods
    }

    #region subclasses

    [Serializable]
    public class DefaultOptions
    {
        public DefaultOptions()
        {
            detailedOutput = true;
            DropCap = "АБВГДЕЖЗИКЛМНОПРСТУФХЦЧЩШЭЮЯ"; //"АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧЩШЬЪЫЭЮЯQWERTYUIOPASDFGHJKLZXCVBNM";
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
        public bool detailedOutput { get; set; }
        public bool addSequence { get; set; }
        public bool addNotesToToc { get; set; }
        [XmlIgnore]
        public bool save { get; set; }
    }

    #endregion subclasses
}
