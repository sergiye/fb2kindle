using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Fb2Kindle
{
    internal class Convertor
    {
        private const string TocElement = "ul"; //"ol";
        private const string NcxName = "toc.ncx";
        private const string DropCap = "АБВГДЕЖЗИКЛМНОПРСТУФХЦЧЩШЭЮЯ"; //"АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧЩШЬЪЫЭЮЯQWERTYUIOPASDFGHJKLZXCVBNM";
        private readonly string _workingFolder;
        private readonly string _defaultCss;
        private readonly bool _addGuideLine;
        private readonly bool _detailedOutput;
        private XElement _opfFile;
        private readonly DefaultOptions _currentSettings;

        #region public

        public string MailTo { get; set; }

        internal Convertor(DefaultOptions currentSettings, string css, bool detailedOutput = true, bool addGuideLine = false)
        {
            _currentSettings = currentSettings;
            _workingFolder = Util.GetAppPath();
            _addGuideLine = addGuideLine;
            _detailedOutput = detailedOutput;
            _defaultCss = css;
            if (!string.IsNullOrEmpty(_defaultCss)) return;
            var defStylesFile = Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, ".css");
            if (File.Exists(defStylesFile))
            {
                _defaultCss = File.ReadAllText(defStylesFile);
            }
            if (!string.IsNullOrEmpty(_defaultCss)) return;
            _defaultCss = Util.GetScriptFromResource("Fb2Kindle.css");
        }

        internal bool ConvertBookSequence(string[] books)
        {
            string tempDir = null;
            try
            {
                if (_currentSettings.UseSourceAsTempFolder)
                    tempDir = Path.Combine(Path.GetDirectoryName(books[0]), Path.GetFileNameWithoutExtension(books[0]));

                //create temp working folder
                if (string.IsNullOrWhiteSpace(tempDir))
                    tempDir = string.Format("{0}\\{1}", Path.GetTempPath(), Guid.NewGuid());
                if (!Directory.Exists(tempDir))
                    Directory.CreateDirectory(tempDir);

                if (_defaultCss.Contains("src: url(\"fonts/") && Directory.Exists(_workingFolder + @"\fonts"))
                {
                    Directory.CreateDirectory(tempDir + @"\fonts");
                    Util.CopyDirectory(_workingFolder + @"\fonts", tempDir + @"\fonts", true);
                }
                File.WriteAllText(tempDir + @"\book.css", _defaultCss);

                var commonTitle = string.Empty;
                var coverDone = false;
                var tocEl = GetEmptyToc();
                for (var idx = 0; idx < books.Length; idx++)
                {
                    var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(books[idx]);
                    if (fileNameWithoutExtension != null)
                    {
                        var fileName = fileNameWithoutExtension.Trim();
                        Util.WriteLine("Processing: " + fileName);
                        var book = LoadBookWithoutNs(books[idx]);
                        if (book == null) return false;

                        if (idx == 0)
                        {
                            commonTitle = fileName;
                            //create instances
                            _opfFile = GetEmptyPackage(book, _currentSettings.Sequence, books.Length > 1);
                            AddPackItem("ncx", NcxName, "application/x-dtbncx+xml", false);
                        }

                        var bookPostfix = idx == 0 ? "" : string.Format("_{0}", idx);

                        //update images (extract and rewrite refs)
                        Directory.CreateDirectory(string.Format(tempDir + "\\Images"));
                        if (ProcessImages(book, tempDir, string.Format("Images\\{0}", bookPostfix), _currentSettings.NoImages))
                        {
                            var imgSrc = Util.AttributeValue(book.Elements("description").Elements("title-info").Elements("coverpage").Elements("div").Elements("img"), "src");
                            if (!string.IsNullOrEmpty(imgSrc))
                            {
                                if (!coverDone)
                                {
                                    _opfFile.Elements("metadata").First().Elements("x-metadata").First().Add(new XElement("EmbeddedCover", imgSrc));
                                    AddGuideItem("Cover", imgSrc, "other.ms-coverimage-standard");
                                    AddPackItem("cover", imgSrc, "image/jpeg", false);
                                    coverDone = true;
                                }
                                else
                                {
                                    AddGuideItem(string.Format("Cover{0}", bookPostfix), imgSrc);
                                    AddPackItem(string.Format("Cover{0}", bookPostfix), imgSrc, "image/jpeg");
                                }
                            }
                        }

                        //book root element to contain all the sections
                        var bookFileName = string.Format("book{0}.html", bookPostfix);
                        var bookRoot = new XElement("div");
                        //add title
                        bookRoot.Add(CreateTitlePage(book));
                        if (idx == 0)
                            AddGuideItem("Title", bookFileName, "start");
                        AddPackItem("it" + bookPostfix, bookFileName);
                        //add to TOC
                        var bookLi = GetListItem(GetTitle(book), bookFileName);
                        tocEl.Elements("body").Elements(TocElement).First().Add(bookLi);
                        ProcessAllData(book, bookRoot, bookPostfix, bookLi, bookFileName);
                        ConvertTagsToHtml(bookRoot, true);
                        SaveAsHtmlBook(bookRoot, tempDir + "\\" + bookFileName);
                    }
                }

                CreateNcxFile(tocEl, commonTitle, tempDir, _currentSettings.NoToc);

                if (!_currentSettings.NoToc)
                {
                    SaveXmlToFile(tocEl, tempDir + @"\toc.html");
                    AddPackItem("content", "toc.html");
                    AddGuideItem("toc", "toc.html", "toc");
                    tocEl.RemoveAll();
                }

                SaveXmlToFile(_opfFile, tempDir + @"\" + commonTitle + ".opf");
                _opfFile.RemoveAll();

                var result = CreateMobi(_workingFolder, tempDir, commonTitle, books[0], _currentSettings.Compression, _detailedOutput);
                if (result && _currentSettings.DeleteOriginal)
                {
                    foreach (var book in books)
                        File.Delete(book);
                }
                return result;
            }
            catch (Exception ex)
            {
                Util.WriteLine("Unknown error: " + ex.Message, ConsoleColor.Red);
                return false;
            }
            finally
            {
                try
                {
                    if (_currentSettings.CleanupMode == ConverterCleanupMode.Full)
                    {
                        if (tempDir != null) Directory.Delete(tempDir, true);
                    }
                }
                catch (Exception ex)
                {
                    Util.WriteLine("Error clearing temp folder: " + ex.Message, ConsoleColor.Red);
                    Util.WriteLine();
                }
            }
        }

        internal bool ConvertBook(string bookPath)
        {
            return ConvertBookSequence(new[] { bookPath });
        }

        #endregion public

        #region ncx

        private static XElement AddNcxItem(XElement parent, int playOrder, string label, string href)
        {
            var navPoint = new XElement("navPoint");
            navPoint.Add(new XAttribute("id", string.Format("p{0}", playOrder)));
            navPoint.Add(new XAttribute("playOrder", playOrder.ToString()));
            navPoint.Add(new XElement("navLabel", new XElement("text", label)));
            navPoint.Add(new XElement("content", new XAttribute("src", href)));
            parent.Add(navPoint);
            return navPoint;
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
            AddNcxItem(navMap, 0, "Описание", "book.html#it");
            var playOrder = 2;
            var listEls = toc.Elements("body").Elements(TocElement);
            AddTocListItems(listEls, navMap, ref playOrder, true);
            if (!addToc)
                AddNcxItem(navMap, 1, "Содержание", "toc.html#toc");
            ncx.Add(navMap);
            SaveXmlToFile(ncx, folder + "\\" + NcxName);
            ncx.RemoveAll();
        }

        private static void AddTocListItems(IEnumerable<XElement> listEls, XElement navMap, ref int playOrder, bool rootLevel = false)
        {
            foreach (var list in listEls)
            {
                AddTocListItems(list.Elements(TocElement), navMap, ref playOrder);
                foreach (var li in list.Elements("li"))
                {
                    var navPoint = navMap;
                    var a = li.Elements("a").FirstOrDefault();
                    if (a != null)
                    {
//                        if (rootLevel)
//                        {
//                            navPoint = AddNcxItem(navMap, playOrder++, a.Value, (string) a.Attribute("href"));
//                        }
//                        else
                        {
                            AddNcxItem(navMap, playOrder++, a.Value, (string) a.Attribute("href"));
                        }
                    }
                    AddTocListItems(li.Elements(TocElement), navPoint, ref playOrder);
                }
            }
        }

        #endregion ncx

        private void UpdateLinksInBook(XElement book, string filename)
        {
            var links = new Dictionary<string, string>();
            //store new link targets in dictionary
            foreach (var idEl in book.DescendantsAndSelf().Where(el => el.Name != "div" && el.Attribute("id") != null))
            {
                links.Add(string.Format("#{0}", (string)idEl.Attribute("id")), filename);
            }
            //update found links hrefs
            foreach (var a in book.Descendants("a"))
            {
                var href = a.Attribute("href").Value;
                if (string.IsNullOrEmpty(href) || !links.ContainsKey(href)) continue;
                var value = a.Value;
                a.RemoveAll();
                a.SetAttributeValue("href", links[href] + href);
                a.Add(new XElement("sup", value));
            }
        }

        private int SaveSubSections(XElement section, int bookNum, XElement toc, string postfix, string bookFileName)
        {
            var bookId = "i" + bookNum + postfix;
            var t = section.Elements("title").FirstOrDefault(el => !string.IsNullOrWhiteSpace(el.Value));
            //var t = section.Descendants("title").FirstOrDefault(el => !string.IsNullOrWhiteSpace(el.Value));
//            if (t == null || string.IsNullOrEmpty(t.Value))
//            {
//                t = section.Elements("p").FirstOrDefault();
//            }

            if (t != null && !string.IsNullOrEmpty(t.Value))
            {
                Util.RenameTag(t, "div", "title");
                var inner = new XElement("div");
                inner.SetAttributeValue("class", bookNum == 0 ? "title0" : "title1");
                inner.SetAttributeValue("id", bookId);
                inner.Add(t.Nodes());
                t.RemoveNodes();
                t.Add(inner);
                //t.SetAttributeValue("id", string.Format("title{0}", bookNum + 2));
                var li = GetListItem(t.Value.Trim(), string.Format("{0}#{1}", bookFileName, bookId)); 
                toc.Add(li);
                toc = li;
            }
            bookNum++;
            foreach (var subSection in section.Descendants("section"))
            {
                if (subSection == null) continue;
                var si = toc.Descendants(TocElement).FirstOrDefault();
                if (si == null)
                {
                    si = new XElement(TocElement);
                    toc.Add(si);
                }
                bookNum = SaveSubSections(subSection, bookNum, si, postfix, bookFileName);
            }
            return bookNum;
        }

        private void ProcessAllData(XElement book, XElement bookRoot, string postfix, XElement bookLi, string bookFileName)
        {
            var listItem = new XElement(TocElement, "");
            bookLi.Add(listItem);

            Util.Write("FB2 to HTML...", ConsoleColor.White);
            UpdateLinksInBook(book, bookFileName);
            var bodies = book.Elements("body").ToArray();
            //process other "bodies" (notes)
            var additionalParts = new List<KeyValuePair<string, XElement>>();
            for (var i = 1; i < bodies.Length; i++)
            {
                Util.RenameTag(bodies[i], "section");
//                if (i < bodies.Length - 1)
//                {
//                    //all but last -> merge into first body
//                    if (bodies[i].Parent != null)
//                        bodies[i].Remove();
//                    bodies[0].Add(bodies[i]);
//                    continue;
//                }
                additionalParts.Add(new KeyValuePair<string, XElement>(string.Format("body{0}", i), bodies[i]));
            }

            bodies[0].Name = "section";
            if (_defaultCss.Contains("span.dc{"))
                SetBigFirstLetters(bodies[0]);

            if (_currentSettings.NoChapters)
            {
                var i = 0;
                var ts = bodies[0].Descendants("title");
                foreach (var t in ts)
                {
                    if (!string.IsNullOrEmpty(t.Value))
                        listItem.Add(GetListItem(t.Value.Trim(), string.Format("{0}#title{1}", bookFileName, i + 2)));
                    Util.RenameTag(t, "div", "title");
                    var inner = new XElement("div");
                    inner.SetAttributeValue("class", i == 0 ? "title0" : "title1");
                    inner.SetAttributeValue("id", string.Format("title{0}", i + 2));
                    inner.Add(t.Nodes());
                    t.RemoveNodes();
                    t.Add(inner);
                    //t.SetAttributeValue("id", string.Format("title{0}", i + 2));
                    i++;
                }
            }
            else
            {
                SaveSubSections(bodies[0], 0, listItem, postfix, bookFileName);
            }
            bookRoot.Add(bodies[0]);

            foreach (var part in additionalParts)
            {
                var item = part.Value;
                if (string.IsNullOrWhiteSpace((string)item.Attribute("id")))
                    item.Add(new XAttribute("id", part.Key));
                string bodyName = null;
                var titleEl = item.Descendants("title").FirstOrDefault(el => !string.IsNullOrWhiteSpace(el.Value));
                if (titleEl != null)
                    bodyName = titleEl.Value.Trim();
                if (string.IsNullOrEmpty(bodyName))
                {
                    bodyName = (string)item.Attribute("name");
                }
                bookRoot.Add(item);
                listItem.Add(GetListItem(bodyName, string.Format("{0}#{1}", bookFileName, part.Key)));
            }

            Util.WriteLine("(OK)", ConsoleColor.Green);
        }

        private void SetBigFirstLetters(XElement body)
        {
            var regex = new Regex(@"^<p>(\w{1})([\s\w]+.+?)</p>$");
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
                            if (t.IsEmpty || t.HasAttributes) continue;
                            var pVal = t.ToString().Trim().Replace("\r", "").Replace("\n", "");
                            var matches = regex.Matches(pVal);
                            if (matches.Count <= 0 || matches[0].Groups.Count != 3)
                            {
                                newPart = false; 
                                continue;
                            }
                            var firstSymbol = matches[0].Groups[1].Value;
                            if (!DropCap.Contains(firstSymbol))
                            {
                                newPart = false;
                                continue;
                            }
                            t.RemoveAll();
                            var newEl = XElement.Parse("<p>" + matches[0].Groups[2].Value + "</p>");
                            var span = new XElement("span", firstSymbol);
                            if (newPart)
                            {
                                newEl.SetAttributeValue("style", "text-indent:0px;");
                                span.SetAttributeValue("class", "dc");
                                newPart = false;
                            }
                            else
                                span.SetAttributeValue("class", "dc2");
                            newEl.AddFirst(span);
                            t.ReplaceWith(newEl);
                            break;
                    }
                }
            }
        }

        private bool CreateMobi(string workFolder, string tempDir, string bookName, string bookPath, bool compress, bool showOutput)
        {
            Util.WriteLine("Creating mobi (KF8)...", ConsoleColor.White);
            var kindleGenPath = string.Format("{0}\\kindlegen.exe", workFolder);
            if (!File.Exists(kindleGenPath))
            {
                kindleGenPath = string.Format("{0}\\kindlegen.exe", tempDir);
                if (!Util.GetFileFromResource("kindlegen.exe", kindleGenPath))
                {
                    Util.WriteLine("kindlegen.exe not found", ConsoleColor.Red);
                    return false;
                }
            }

            var args = string.Format("\"{0}\\{1}.opf\" {2}", tempDir, bookName, compress ? "-c2" : "-c0");
            var res = Util.StartProcess(kindleGenPath, args, showOutput);
            if (res == 2)
            {
                Util.WriteLine("Error converting to mobi", ConsoleColor.Red);
                return false;
            }

            var tmpBookPath = string.Format("{0}\\{1}.mobi", tempDir, bookName);
            bool saveLocal = true;
            if (!string.IsNullOrWhiteSpace(MailTo))
            {
                // Wait for it to finish
                saveLocal = !SendBookByMail(bookName, tmpBookPath).GetAwaiter().GetResult();
                //Task.Run(async() => await SendBookByMail(bookName, tmpBookPath));
            }

            if (saveLocal)
            {
                //save to output folder
                var versionNumber = 1;
                var resultPath = Path.GetDirectoryName(bookPath);
                var resultName = bookName;
                while (File.Exists(string.Format("{0}\\{1}.mobi", resultPath, resultName)))
                {
                    resultName = bookName + "(v" + versionNumber + ")";
                    versionNumber++;
                }

                File.Move(tmpBookPath, string.Format("{0}\\{1}.mobi", resultPath, resultName));

                if (_currentSettings.CleanupMode == ConverterCleanupMode.Partial)
                {
                    if (!string.IsNullOrWhiteSpace(tempDir))
                    {
                        foreach (var f in Directory.EnumerateFiles(tempDir, "*.opf"))
                            File.Delete(f);
                        //File.Delete(Path.Combine(tempDir, Path.GetFileNameWithoutExtension(inputFile) + ".opf"));
                        File.Delete(Path.Combine(tempDir, "kindlegen.exe"));
                        File.Delete(Path.Combine(tempDir, "toc.ncx"));

                        var destFolder = string.Format("{0}\\{1}", resultPath, resultName);
                        if (!tempDir.Equals(destFolder, StringComparison.OrdinalIgnoreCase))
                            Directory.Move(tempDir, destFolder);
                    }
                }
            }
            else
            {
                File.Move(tmpBookPath, "NUL");
            }

            if (!showOutput)
                Util.WriteLine("(OK)", ConsoleColor.Green);

            return true;
        }

        private async Task<bool> SendBookByMail(string bookName, string tmpBookPath)
        {
            Util.Write(string.Format("Sending to {0}...", MailTo), ConsoleColor.White);
            try
            {
                const string smtpLogin = "trial.develop@gmail.com";
                const string smtpPassword = "TrI@lDeVeL0peR";
                const string smtpServer = "smtp.gmail.com";
                using (var smtp = new SmtpClient(smtpServer, 587)
                {
                    Credentials = new NetworkCredential(smtpLogin, smtpPassword),
                    EnableSsl = true,
//                    UseDefaultCredentials = false,
//                    DeliveryMethod = SmtpDeliveryMethod.Network,
                })
                {
                    using (var message = new MailMessage(new MailAddress(smtpLogin, "Simpl's converter"),
                            new MailAddress(MailTo))
                                      {
                                          Subject = bookName,
                                          Body = "Hello! Please, check book(s) attached"
                                      })
                    {
                        using (var att = new Attachment(tmpBookPath))
                        {
                            message.Attachments.Add(att);
                            await smtp.SendMailAsync(message);
                        }
                    }
                }
                Util.WriteLine("OK", ConsoleColor.Green);
                return true;
            }
            catch (Exception ex)
            {
                Util.WriteLine(string.Format("Error: {0}", ex.Message), ConsoleColor.Red);
            }
            return false;
        }

        private static XElement AddAuthorsInfo(IEnumerable<XElement> avtorbook)
        {
            var info = new XElement("h2");
            foreach (var ai in avtorbook)
            {
                info.Add(Util.Value(ai.Elements("last-name"), "Неизвестный"), new XElement("br"), 
                    Util.Value(ai.Elements("first-name")), new XElement("br"), 
                    Util.Value(ai.Elements("middle-name")), new XElement("br"));
            }
            return info;
        }

        private static XElement CreateTitlePage(XElement book)
        {
            var linkEl = new XElement("div", new XAttribute("id", "it"));
            linkEl.Add(new XAttribute("class", "supertitle"));
            linkEl.Add(new XAttribute("align", "center"));
            linkEl.Add(AddAuthorsInfo(book.Elements("description").Elements("title-info").Elements("author")));
            linkEl.Add(new XElement("p", string.Format("{0} {1}", Util.AttributeValue(book.Elements("description").Elements("title-info").Elements("sequence"), "name"), 
                Util.AttributeValue(book.Elements("description").Elements("title-info").Elements("sequence"), "number"))));
            linkEl.Add(new XElement("br"));
            var pEl = new XElement("p");
            pEl.Add(new XAttribute("class", "text-name"));
            pEl.Add(Util.Value(book.Elements("description").Elements("title-info").Elements("book-title"), "Книга"));
            linkEl.Add(pEl, new XElement("br"));
            linkEl.Add(new XElement("p", Util.Value(book.Elements("description").Elements("title-info").Elements("annotation"))));
            linkEl.Add(new XElement("br"), new XElement("br"));
            linkEl.Add(new XElement("p", Util.Value(book.Elements("description").Elements("publish-info").Elements("publisher"))));
            linkEl.Add(new XElement("p", Util.Value(book.Elements("description").Elements("publish-info").Elements("city"))));
            linkEl.Add(new XElement("p", Util.Value(book.Elements("description").Elements("publish-info").Elements("year"))));
            linkEl.Add(new XElement("br"), new XElement("br"), new XElement("br"));
            linkEl.Add(new XElement("p", string.Format("Converted by © Fb2Kindle (ver. {0})", 
                Assembly.GetExecutingAssembly().GetName().Version.ToString(3))));
            linkEl.Add(new XElement("p", "developed by Sergiy Yegoshyn (egoshin.sergey@gmail.com)"));
            return linkEl;
        }

        #region helper methods

        private static void ConvertTagsToHtml(XElement book, bool full = false)
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
            Util.RenameTags(book, "stanza", "em");
            if (!full) return;
            Util.RenameTags(book, "title", "div", "subtitle");
        }

        private static XDocument ReadXDocumentWithInvalidCharacters(string filename)
        {
            XDocument xDocument;
            var xmlReaderSettings = new XmlReaderSettings { CheckCharacters = false };
            using (var xmlReader = XmlReader.Create(filename, xmlReaderSettings))
            {
                // Load our XDocument
                xmlReader.MoveToContent();
                xDocument = XDocument.Load(xmlReader);
            }
            return xDocument;
        }

        private static XElement LoadBookWithoutNs(string bookPath)
        {
            try
            {
                XElement book;
                //book = XDocument.Parse(File.ReadAllText(bookPath), LoadOptions.PreserveWhitespace).Root;
                //book = ReadXDocumentWithInvalidCharacters(bookPath).Root;
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
                Util.WriteLine("Unknown file format: " + ex.Message, ConsoleColor.Red);
                return null;
            }
        }

        private static void SaveXmlToFile(XElement xml, string file)
        {
            if (Debugger.IsAttached)
                xml.Save(file);
            else
                xml.Save(file, SaveOptions.DisableFormatting);
        }

        private static void SaveAsHtmlBook(XElement bodyEl, string fileName)
        {
            var doc = new XElement("html");
            doc.Add(new XElement("head", new XElement("link", new XAttribute("type", "text/css"), new XAttribute("href", "book.css"), new XAttribute("rel", "Stylesheet"))));
            doc.Add(new XElement("body", bodyEl));
            Util.RenameTags(doc, "section", "div", "book");
            Util.RenameTags(doc, "annotation", "em");
            SaveXmlToFile(doc, fileName);
            doc.RemoveAll();
        }

        private static string GetTitle(XElement book)
        {
            return Util.Value(book.Elements("description").Elements("title-info").Elements("book-title"), "Книга").Trim();
        }

        private static XElement GetEmptyPackage(XElement book, bool addSequenceToTitle, bool useSequenceNameOnly = false)
        {
            var opfFile = new XElement("package");
            opfFile.Add(new XAttribute("unique-identifier", "DOI"));
            opfFile.Add(new XAttribute(XNamespace.Get("http://www.w3.org/2000/xmlns/").GetName("fo"), "http://www.w3.org/1999/XSL/Format"));
            opfFile.Add(new XAttribute(XNamespace.Get("http://www.w3.org/2000/xmlns/").GetName("fb"), "http://www.gribuser.ru/xml/fictionbook/2.0"));
            opfFile.Add(new XAttribute(XNamespace.Get("http://www.w3.org/2000/xmlns/").GetName("xlink"), "http://www.w3.org/1999/xlink"));
            var linkEl = new XElement("meta", new XAttribute("name", "zero-gutter"), new XAttribute("content", "true"));
            var headEl = new XElement("metadata", linkEl);
            linkEl = new XElement("meta", new XAttribute("name", "zero-margin"), new XAttribute("content", "true"));
            headEl.Add(linkEl);
            linkEl = new XElement("dc-metadata");
            XNamespace dc = "http://purl.org/metadata/dublin_core";
            linkEl.Add(new XAttribute(XNamespace.Xmlns + "dc", dc));
            linkEl.Add(new XAttribute(XNamespace.Xmlns + "oebpackage", "http://openebook.org/namespaces/oeb-package/1.0/"));

            var content = new XElement(dc + "Title");

            var bookTitle = GetTitle(book);
            var seqName = Util.AttributeValue(book.Elements("description").Elements("title-info").Elements("sequence"), "name");
            if (useSequenceNameOnly)
            {
                 bookTitle = string.IsNullOrEmpty(seqName) ? string.Format("{0} (Сборник)", bookTitle) : seqName;
            }
            else
            {
                if (addSequenceToTitle)
                    bookTitle = string.Format("{0} {1} {2}", seqName,
                                              Util.AttributeValue(book.Elements("description").Elements("title-info").Elements("sequence"), "number"),
                                              bookTitle);
            }
            content.Add(bookTitle);

            linkEl.Add(content);
            content = new XElement(dc + "Creator");
            content.Add(Util.Value(book.Elements("description").Elements("title-info").Elements("author").First().Elements("last-name"), "Вася") + " " + Util.Value(book.Elements("description").Elements("title-info").Elements("author").First().Elements("first-name")) + " " + Util.Value(book.Elements("description").Elements("title-info").Elements("author").First().Elements("middle-name")));
            linkEl.Add(content);
            content = new XElement(dc + "Publisher");
            content.Add(Util.Value(book.Elements("description").Elements("publish-info").Elements("publisher")));
            linkEl.Add(content);
            //content.Add(Util.Value(book.Elements("description").Elements("publish-info").Elements("year")));
            linkEl.Add(new XElement(dc + "Date", DateTime.Today.ToString("yyyy-MM-dd")));
            linkEl.Add(new XElement(dc + "Identifier", new XAttribute("id", "DOI"), Guid.NewGuid().ToString()));
            content = new XElement(dc + "Language");
            var bookLang = Util.Value(book.Elements("description").First().Elements("title-info").First().Elements("lang"));
            if (string.IsNullOrEmpty(bookLang))
                bookLang = "ru";
            content.Add(bookLang);
            linkEl.Add(content);
            linkEl.Add(new XElement(dc + "Description", Util.Value(book.Elements("description").Elements("title-info").Elements("annotation"))));
            headEl.Add(linkEl);
            headEl.Add(new XElement("x-metadata", new XElement("output", new XAttribute("encoding", "utf-8"))));
            opfFile.Add(headEl);

            opfFile.Add(new XElement("manifest"));
            opfFile.Add(new XElement("spine", new XAttribute("toc", "ncx")));
            return opfFile;
        }

        private static XElement GetEmptyToc()
        {
            var toc = new XElement("html", new XAttribute("type", "toc"));
            toc.Add(new XElement("head", new XElement("title", "Содержание"), 
                    new XElement("link", new XAttribute("type", "text/css"), 
                        new XAttribute("href", "book.css"), new XAttribute("rel", "Stylesheet"))));
            toc.Add(new XElement("body", new XElement("div", new XAttribute("class", "title"), 
                new XAttribute("id", "toc"), "Содержание"), new XElement(TocElement, "")));
            return toc;
        }

        private static XElement GetListItem(string name, string href)
        {
            return new XElement("li", new XElement("a", new XAttribute("href", href), name));
        }

        private void AddPackItem(string id, string href, string mediaType = "text/x-oeb1-document", bool addSpine = true)
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
            if (string.IsNullOrEmpty(guideType)) return;
            if (!_addGuideLine && guideType.Equals("text")) return;
            var itemEl = new XElement("reference");
            itemEl.Add(new XAttribute("type", guideType)); //"text"
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

        #region Images
     
        private bool ProcessImages(XElement book, string workFolder, string imagesPrefix, bool removeImages)
        {
            var imagesCreated = !removeImages && ExtractImages(book, workFolder, imagesPrefix);
            var list = Util.RenameTags(book, "image", "div", "image");
            foreach (var element in list)
            {
                if (!imagesCreated)
                    element.Remove();
                else
                {
                    var src = element.Attribute("href").Value;
                    element.RemoveAll();
                    if (string.IsNullOrEmpty(src)) continue;
                    src = src.Replace("#", "");
                    var imgEl = new XElement("img");
                    imgEl.SetAttributeValue("src", GetImageNameWithExt(string.Format("{0}{1}", imagesPrefix, src)));
                    element.Add(imgEl);
                }
            }
            return imagesCreated;
        }

        private string GetImageNameWithExt(string original)
        {
            var ext = Path.GetExtension(original);
            if (string.IsNullOrWhiteSpace(ext))
                return original + ".jpg";
            return original;
        }

        private bool ExtractImages(XElement book, string workFolder, string imagesPrefix)
        {
            if (book == null) return true;
            Util.Write("Extracting images...", ConsoleColor.White);
            foreach (var binEl in book.Elements("binary"))
            {
                try
                {
                    var file = GetImageNameWithExt(string.Format("{0}\\{1}{2}", workFolder, imagesPrefix, binEl.Attribute("id").Value));
                    var format = GetImageFormatFromMimeType(binEl.Attribute("content-type").Value, _currentSettings.Jpeg ? ImageFormat.Jpeg : ImageFormat.Png);
                    //todo: we can get format from img.RawFormat
                    var fileBytes = Convert.FromBase64String(binEl.Value);
                    try
                    {
                        using (Stream str = new MemoryStream(fileBytes))
                        {
                            using (var img = Image.FromStream(str))
                            {
//                                var pngCodec = Util.GetEncoderInfo(ImageFormat.Png);
//                                if (pngCodec != null)
//                                {
//                                    var parameters = new EncoderParameters(1) {
//                                            Param = {
//                                                [0] = new EncoderParameter(Encoder.ColorDepth, 24)
//                                            }
//                                        };
//                                    img.Save(file, pngCodec, parameters);
//                                }
//                                else
                                img.Save(file, format);
                            }
                        }
                        if (_currentSettings.Grayscaled)
                        {
                            Image gsImage;
                            using (var img = Image.FromFile(file))
                            {
                                gsImage = GrayScale(img, true, format);
                            }
                            gsImage.Save(file, format);
                        }
                    }
                    catch (Exception ex)
                    {
                        Util.WriteLine("Error compressing image: " + ex.Message, ConsoleColor.Red);
                        File.WriteAllBytes(file, fileBytes);
                    }
                }
                catch (Exception ex)
                {
                    Util.WriteLine(ex.Message, ConsoleColor.Red);
                }
            }
            Util.WriteLine("(OK)", ConsoleColor.Green);
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

        private static ImageCodecInfo GetEncoderInfo(ImageFormat format)
        {
            return ImageCodecInfo.GetImageEncoders().FirstOrDefault(codec => codec.FormatID.Equals(format.Guid));
        }

        private static string GetMimeType(Image image)
        {
            return GetMimeType(image.RawFormat);
        }

        private static string GetMimeType(ImageFormat imageFormat)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            return codecs.First(codec => codec.FormatID == imageFormat.Guid).MimeType;
        }

        private static ImageFormat GetImageFormatFromMimeType(string contentType, ImageFormat defaultResult)
        {
            if (contentType.Equals(GetMimeType(ImageFormat.Jpeg), StringComparison.OrdinalIgnoreCase))
            {
                return ImageFormat.Jpeg;
            }
            if (contentType.Equals(GetMimeType(ImageFormat.Bmp), StringComparison.OrdinalIgnoreCase))
            {
                return ImageFormat.Bmp;
            }
            if (contentType.Equals(GetMimeType(ImageFormat.Png), StringComparison.OrdinalIgnoreCase))
            {
                return ImageFormat.Png;
            }
//            foreach (var codecInfo in ImageCodecInfo.GetImageEncoders())
//            {
//                if (codecInfo.MimeType.Equals(contentType, StringComparison.OrdinalIgnoreCase))
//                {
//                    return codecInfo.FormatID;
//                }
//            }
            return defaultResult;
        }

        private static Image GrayScale(Image img, bool fast, ImageFormat format)
        {
            Stream imageStream = new MemoryStream();
            if (fast)
            {
                using (var bmp = new Bitmap(img))
                {
                    var gsBmp = MakeGrayscale3(bmp);
                    gsBmp.Save(imageStream, format);
                }
            }
            else
            {
                using (var bmp = new Bitmap(img))
                {
                    for (var y = 0; y < bmp.Height; y++)
                    for (var x = 0; x < bmp.Width; x++)
                    {
                        var c = bmp.GetPixel(x, y);
                        var rgb = (c.R + c.G + c.B) / 3;
                        bmp.SetPixel(x, y, Color.FromArgb(rgb, rgb, rgb));
                    }
                    bmp.Save(imageStream, format);
                }
            }
            return Image.FromStream(imageStream);
        }

        private static Bitmap MakeGrayscale3(Bitmap original)
        {
            var newBitmap = new Bitmap(original.Width, original.Height);
            var g = Graphics.FromImage(newBitmap);
            var colorMatrix = new ColorMatrix(new[]
                                              {
                                                  new[] {.3f, .3f, .3f, 0, 0},
                                                  new[] {.59f, .59f, .59f, 0, 0},
                                                  new[] {.11f, .11f, .11f, 0, 0},
                                                  new float[] {0, 0, 0, 1, 0},
                                                  new float[] {0, 0, 0, 0, 1}
                                              });
            var attributes = new ImageAttributes();
            attributes.SetColorMatrix(colorMatrix);
            g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
                0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);
            g.Dispose();
            return newBitmap;
        }

        #endregion Images
    }
}
