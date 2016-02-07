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
using System.Xml;
using System.Xml.Linq;

namespace Fb2Kindle
{
    [Serializable]
    public class DefaultOptions
    {
        public bool d { get; set; }
        public bool nch { get; set; }
        public bool ni { get; set; }
        public bool ntoc { get; set; }
        public bool c { get; set; }
        public bool s { get; set; }
    }

    internal class Convertor
    {
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
            if (String.IsNullOrEmpty(_defaultCss))
                _defaultCss = Util.GetScriptFromResource("defstyles.css");
        }

        internal bool ConvertBookSequence(string[] books, bool debug)
        {
            string tempDir = null;
            try
            {
                //create temp working folder
                //_tempDir = Path.GetTempPath() + "\\"  + Environment.TickCount.ToString();
                tempDir = Path.GetTempPath() + "\\" + Guid.NewGuid().ToString();
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
                        Console.WriteLine("Processing: " + fileName);
                        var book = LoadBookWithoutNs(books[idx]);
                        if (book == null) return false;

                        if (idx == 0)
                        {
                            commonTitle = fileName;
                            //create instances
                            _opfFile = GetEmptyPackage(book, _currentSettings.s, books.Length > 1);
                            AddPackItem("ncx", NcxName, "application/x-dtbncx+xml", false);
                        }

                        var bookPostfix = idx == 0 ? "" : string.Format("_{0}", idx);
                        var titlePageName = "it" + bookPostfix;
                        AddPackItem(titlePageName, titlePageName + ".html");
                        if (idx == 0)
                            AddGuideItem("Title", titlePageName + ".html", "start");
                        CreateTitlePage(book, tempDir + "\\" + titlePageName + ".html");
                        var bookLi = GetListItem(GetTitle(book), titlePageName + ".html");
                        bookLi.Add(new XElement("ul", ""));
                        tocEl.Elements("body").Elements("ul").First().Add(bookLi);

                        //update images (extract and rewrite refs)
                        if (ProcessImages(book, string.Format("{0}\\image{1}", tempDir, bookPostfix), _currentSettings.ni))
                        {
                            if (!coverDone)
                            {
                                var imgSrc = Util.AttributeValue(book.Elements("description").Elements("title-info").Elements("coverpage").Elements("div").Elements("img"), "src");
                                if (!String.IsNullOrEmpty(imgSrc))
                                {
                                    _opfFile.Elements("metadata").First().Elements("dc-metadata").First().Elements("x-metadata").First().Add(new XElement("EmbeddedCover", imgSrc));
                                    AddGuideItem("Cover", imgSrc, "cover");
                                    coverDone = true;
                                }
                            }
                        }
                        ProcessAllData(book, tempDir, bookPostfix, bookLi);
                    }
                }

                CreateNcxFile(tocEl, commonTitle, tempDir, _currentSettings.ntoc);

                if (!_currentSettings.ntoc)
                {
                    AddPackItem("content", "toc.html");
                    AddGuideItem("toc", "toc.html", "toc");
                    SaveXmlToFile(tocEl, tempDir + @"\toc.html");
                    tocEl.RemoveAll();
                }

                SaveXmlToFile(_opfFile, tempDir + @"\" + commonTitle + ".opf");
                _opfFile.RemoveAll();

                var result = CreateMobi(_workingFolder, tempDir, commonTitle, books[0], _currentSettings.c, _detailedOutput);
                if (result && _currentSettings.d)
                {
                    foreach (var book in books)
                        File.Delete(book);
                }
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unknown error: " + ex.Message);
                return false;
            }
            finally
            {
                if (!debug)
                try
                {
                    if (tempDir != null) Directory.Delete(tempDir, true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error clearing temp folder: " + ex.Message);
                    Console.WriteLine();
                }
            }
        }

        internal bool ConvertBook(string bookPath, bool debug)
        {
            return ConvertBookSequence(new[] { bookPath }, debug);
        }

        private static bool ProcessImages(XElement book, string imagesFolder, bool removeImages)
        {
            var imagesCreated = !removeImages && ExtractImages(book, imagesFolder);
            var list = Util.RenameTags(book, "image", "div", "image");
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
                    imgEl.SetAttributeValue("src", string.Format("{0}\\{1}", imagesFolder, src));
                    element.Add(imgEl);
                }
            }
            return imagesCreated;
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
            AddNcxItem(navMap, 0, "Обложка", "it.html#it");
            var tocItems = toc.Descendants("a");
            var playOrder = 2;
            foreach (var a in tocItems)
                AddNcxItem(navMap, playOrder++, a.Value, (string)a.Attribute("href"));
            if (!addToc)
                AddNcxItem(navMap, 1, "Содержание", "toc.html#toc");
            ncx.Add(navMap);
            SaveXmlToFile(ncx, folder + "\\" + NcxName);
            ncx.RemoveAll();
        }

        #endregion ncx

        #endregion public

        private static XElement AddTitleToToc(string title, string path, XElement toc)
        {
            var li = GetListItem(title.Trim(), path); 
            toc.Add(li);
            return li;
        }

        private Dictionary<string, string> linksDictionary = new Dictionary<string, string>();
        
        private void UpdateLinksInBook(XElement section, string sectionFilename)
        {
            //store new link targets in dictionary
            foreach (var idEl in section.DescendantsAndSelf().Where(el => el.Name != "div" && el.Attribute("id") != null))
            {
                linksDictionary.Add(string.Format("#{0}", (string)idEl.Attribute("id")), sectionFilename);
            }

            //update found links hrefs
            foreach (var a in section.Descendants("a"))
            {
                var href = a.Attribute("href").Value;
                //if (string.IsNullOrEmpty(href) || !noteId.Equals(href, StringComparison.OrdinalIgnoreCase)) continue;
                if (string.IsNullOrEmpty(href) || !linksDictionary.ContainsKey(href)) continue;
                var value = a.Value;
                a.RemoveAll();
                a.SetAttributeValue("href", linksDictionary[href] + href);
                a.Add(new XElement("sup", value));
            }
        }

        private int SaveSubSections(XElement section, int bookNum, XElement toc, string bookDir, string postfix)
        {
            var bookId = "i" + bookNum + postfix;
            var href = bookId + ".html";
            var t = section.Elements("title").FirstOrDefault();
            if (t != null && !String.IsNullOrEmpty(t.Value))
            {
                Util.RenameTag(t, "div", "title");
                var inner = new XElement("div");
                inner.SetAttributeValue("class", bookNum == 0 ? "title0" : "title1");
                inner.SetAttributeValue("id", bookId);
                inner.Add(t.Nodes());
                t.RemoveNodes();
                t.Add(inner);
                //t.SetAttributeValue("id", String.Format("title{0}", bookNum + 2));
                toc = AddTitleToToc(t.Value.Trim(), String.Format("{0}#{1}", href, bookId), toc);
                if (section.Parent != null)
                    section.Remove();
            }
            AddPackItem(bookId, href);
            AddGuideItem(bookId, href);
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
                bookNum = SaveSubSections(firstSubSection, bookNum, si, bookDir, postfix);
            }
            UpdateLinksInBook(section, href);
            SaveAsHtmlBook(section, bookDir + "\\" + href);
            return bookNum;
        }

        private void ProcessAllData(XElement book, string bookDir, string postfix, XElement bookLi)
        {
            linksDictionary.Clear();
            Console.Write("FB2 to HTML...");
            var bodies = book.Elements("body").ToArray();

            //process other "bodies" (notes)
            var additionalParts = new List<KeyValuePair<string, string>>();
            for (var i = 1; i < bodies.Length; i++)
            {
                Util.RenameTag(bodies[i], "section");
                if (i < bodies.Length - 1)
                {
                    //all but last -> merge into first body
                    if (bodies[i].Parent != null)
                        bodies[i].Remove();
                    bodies[0].Add(bodies[i]);
                    continue;
                }
                string bodyName = null;
                var titleEl = bodies[i].Descendants("title").FirstOrDefault(el => el.Value != null);
                if (titleEl != null)
                    bodyName = titleEl.Value.Trim();
                if (String.IsNullOrEmpty(bodyName))
                {
                    bodyName = (string)bodies[i].Attribute("name");
                }
                var resHtmlFileName = string.Format("{0}{1}.html", Path.GetRandomFileName(), postfix);
                UpdateLinksInBook(bodies[i], resHtmlFileName);
                ConvertTagsToHtml(bodies[i], true);
                SaveAsHtmlBook(bodies[i], bookDir + @"\" + resHtmlFileName);

                additionalParts.Add(new KeyValuePair<string, string>(bodyName, resHtmlFileName));
            }

            bodies[0].Name = "section";
            if (_defaultCss.Contains("span.dc{"))
                SetBigFirstLetters(bodies[0]);

            if (_currentSettings.nch)
            {
                var i = 0;
                var ts = bodies[0].Descendants("title");
                foreach (var t in ts)
                {
                    if (!String.IsNullOrEmpty(t.Value))
                        bookLi.Elements("ul").First().Add(GetListItem(t.Value.Trim(), "i.html#" + String.Format("title{0}", i + 2)));
                    Util.RenameTag(t, "div", "title");
                    var inner = new XElement("div");
                    inner.SetAttributeValue("class", i == 0 ? "title0" : "title1");
                    inner.SetAttributeValue("id", String.Format("title{0}", i + 2));
                    inner.Add(t.Nodes());
                    t.RemoveNodes();
                    t.Add(inner);
                    //t.SetAttributeValue("id", String.Format("title{0}", i + 2));
                    i++;
                }
            }
            var els = bodies[0].Descendants("stanza");
            foreach (var el in els)
            {
                el.Name = "br";
                var parent = el.Parent;
                if (parent == null) continue;
                parent.Add(el.Nodes());
                el.RemoveAll();
                parent.Add(new XElement("br"));
            }
            ConvertTagsToHtml(bodies[0]);
            if (_currentSettings.nch)
            {
                var htmlFile = string.Format("i{0}.html", postfix);
                AddPackItem("text", htmlFile);
                AddGuideItem("Book", htmlFile);
                UpdateLinksInBook(bodies[0], htmlFile);
                SaveAsHtmlBook(bodies[0], bookDir + @"\" + htmlFile);
            }
            else
                SaveSubSections(bodies[0], 0, bookLi.Elements("ul").First(), bookDir, postfix);

            foreach (var part in additionalParts)
            {
                AddPackItem(part.Value, part.Value);
                bookLi.Elements("ul").First().Add(GetListItem(part.Key, part.Value));
            }

            Console.WriteLine("(OK)");
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
            Console.WriteLine("Creating mobi (KF8)...");
            var kindleGenPath = string.Format("{0}\\kindlegen.exe", workFolder);
            if (!File.Exists(kindleGenPath))
            {
                kindleGenPath = string.Format("{0}\\kindlegen.exe", tempDir);
                if (!Util.GetFileFromResource("kindlegen.exe", kindleGenPath))
                {
                    Console.WriteLine("kindlegen.exe not found");
                    return false;
                }
            }

            var args = String.Format("\"{0}\\{1}.opf\"", tempDir, bookName);
            if (compress)
                args += " -c2";

            var res = Util.StartProcess(kindleGenPath, args, showOutput);
            if (res == 2)
            {
                Console.WriteLine("Error converting to mobi");
                return false;
            }

            var saveLocal = true;
            var tmpBookPath = string.Format("{0}\\{1}.mobi", tempDir, bookName);
            if (!string.IsNullOrWhiteSpace(MailTo))
            {
                //send book to email
                Console.Write("Sending to {0}...", MailTo);
                try
                {
                    using (var smtp = new SmtpClient
                    {
                        Host = "smtp.mailgun.org",
                        Port = 587,
                        EnableSsl = true,
                        DeliveryMethod = SmtpDeliveryMethod.Network,
                        UseDefaultCredentials = false,
                        Credentials =
                            new NetworkCredential("postmaster@sandbox9bf1b495570048b9b31dabddddbccadf.mailgun.org",
                                "2851987cc3314263118267b62744f3fc")
                    })
                    {
                        using (
                            var message = new MailMessage(new MailAddress("simpl@Fb2Kindle.org", "Simpl's converter"),
                                new MailAddress(MailTo))
                            {
                                Subject = bookName,
                                Body = "Hello! Please, check book(s) attached"
                            })
                        {
                            using (var att = new Attachment(tmpBookPath))
                            {
                                message.Attachments.Add(att);
                                smtp.Send(message);
                            }
                        }
                    }
                    Console.WriteLine("OK");
                    saveLocal = false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: {0}", ex.Message);
                }
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
            }
            else
            {
                File.Move(tmpBookPath, "NUL");
            }
            if (!showOutput)
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
                element2.Add(Util.Value(ai.Elements("first-name")));
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

        private static void CreateTitlePage(XElement book, string fileName)
        {
            var content = new XElement("html");
            content.Add(new XElement("head", GetCssLink()));
            var body = new XElement("body");
            var linkEl = new XElement("div");
            linkEl.Add(new XAttribute("class", "supertitle"));
            linkEl.Add(new XAttribute("align", "center"));
            linkEl.Add(new XAttribute("id", "it"));
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
            body.Add(linkEl);
            content.Add(body);
            SaveXmlToFile(content, fileName);
        }

        #region helper methods

        private static bool ExtractImages(XElement book, string imagesFolder)
        {
            if (book == null) return true;
            Console.Write("Extracting images...");
            if (!Directory.Exists(imagesFolder))
                Directory.CreateDirectory(imagesFolder);
            foreach (var binEl in book.Elements("binary"))
            {
                try
                {
                    var file = String.Format("{0}\\{1}", imagesFolder, binEl.Attribute("id").Value);
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
            if (!full) return;
            Util.RenameTags(book, "stanza", "br");
            Util.RenameTags(book, "title", "div", "subtitle");
        }

        static XDocument ReadXDocumentWithInvalidCharacters(string filename)
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

        private static void SaveAsHtmlBook(XElement bodyEl, string fileName)
        {
            var htmlDoc = InitEmptyHtmlDoc();
            htmlDoc.Elements("body").First().Add(bodyEl);
            Util.RenameTags(htmlDoc, "section", "div", "book");
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

        private static string GetTitle(XElement book)
        {
            return Util.Value(book.Elements("description").Elements("title-info").Elements("book-title"), "Книга").Trim();
        }

        private static XElement GetEmptyPackage(XElement book, bool addSequenceToTitle, bool useSequenceNameOnly = false)
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

            var bookTitle = GetTitle(book);
            var seqName = Util.AttributeValue(book.Elements("description").Elements("title-info").Elements("sequence"), "name");
            if (useSequenceNameOnly)
            {
                 if (string.IsNullOrEmpty(seqName))
                    bookTitle = "Сборник";
                else
                    bookTitle = seqName;
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
            var toc = new XElement("html", new XAttribute("type", "toc"));
            toc.Add(new XElement("head", new XElement("title", "Содержание")));
            var body = new XElement("body", "");
            toc.Add(body);
            var linkEl = new XElement("div", new XAttribute("style", "font-size: 130%;text-align:center;font-weight:bold;"));
            linkEl.Add(new XAttribute("id", "toc"));
            linkEl.Add(new XElement("p", "Содержание"));
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
            if (String.IsNullOrEmpty(guideType)) return;
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
    }
}
