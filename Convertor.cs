using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Fb2Kindle
{
    class Convertor
    {
        private string _tempDir;
        private bool imagesPrepared;
        private readonly string _workingFolder;
        private readonly string _defaultCSS;
        private readonly bool _customFontsUsed;
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
            var bookName = Path.GetFileNameWithoutExtension(bookPath);
            Console.WriteLine("Processing: " + bookName);
            XElement book;
            try
            {
                using (Stream file = File.OpenRead(bookPath))
                {
                    book = XElement.Load(file);
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
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unknown file format: " + ex.Message);
                return false;
            }
            //create temp working folder
            _tempDir = Common.PrepareTempFolder(bookName, Common.ImagesFolderName, _workingFolder);
            if (_customFontsUsed && Directory.Exists(_workingFolder + @"\fonts"))
            {
                Directory.CreateDirectory(_tempDir + @"\fonts");
                Common.CopyDirectory(_workingFolder + @"\fonts", _tempDir + @"\fonts", true);
            }
            imagesPrepared = !_currentSettings.noImages && Common.ExtractImages(book, _tempDir, Common.ImagesFolderName);

            XElement htmlElement;
            XElement packElement;
            XElement ncxElement;
            List<DataItem> notesList2;
            List<DataItem> titles;
            string bodyStr;
            bool notesCreated;
            int index;
            int playOrder;
            var element5 = ConvertToHtml(book, out playOrder, out htmlElement, out packElement, 
                out notesList2, out bodyStr, out notesCreated, out ncxElement, out titles, out index);
            if (_currentSettings.noChapters)
            {
                var htmlContent = htmlElement.ToString();
                var htmlFile = bookName + ".html";
                index = 0;
                while (index < titles.Count)
                {
                    var str34 = titles[index].Value;
                    var str35 = titles[index].Name;
                    var navPoint = new XElement("navPoint");
                    navPoint.Add(Common.CreateAttribute("id", "navpoint-" + (index + 2).ToString()));
                    navPoint.Add(Common.CreateAttribute("playOrder", (index + 2).ToString()));
                    var navLabel = new XElement("navLabel");
                    var textEl = new XElement("text");
                    textEl.Add(str34);
                    navLabel.Add(textEl);
                    navPoint.Add(navLabel);
                    navLabel = new XElement("content");
                    navLabel.Add(Common.CreateAttribute("src", htmlFile + "#" + str35));
                    navPoint.Add(navLabel);
                    ncxElement.Elements("navMap").First().Add(navPoint);
                    if (!_currentSettings.ntoc)
                    {
                        navPoint = new XElement("li");
                        navLabel = new XElement("a");
                        navLabel.Add(Common.CreateAttribute("href", htmlFile + "#" + str35));
                        navLabel.Add(str34);
                        navPoint.Add(navLabel);
                        element5.Elements("body").First().Elements("ul").First().Add(navPoint);
                    }
                    index++;
                }
                var itemEl = new XElement("item");
                itemEl.Add(new XAttribute("id", "text"));
                itemEl.Add(new XAttribute("media-type", "text/x-oeb1-document"));
                itemEl.Add(Common.CreateAttribute("href", htmlFile));
                itemEl.Add("");
                packElement.Elements("manifest").First().Add(itemEl);
                itemEl = new XElement("itemref");
                itemEl.Add(new XAttribute("idref", "text"));
                packElement.Elements("spine").First().Add(itemEl);
                itemEl = new XElement("reference");
                itemEl.Add(new XAttribute("type", "text"));
                itemEl.Add(new XAttribute("title", "Book"));
                itemEl.Add(Common.CreateAttribute("href", htmlFile));
                packElement.Elements("guide").First().Add(itemEl);
                bodyStr = Common.TabRep(bodyStr);
                htmlContent = htmlContent.Insert(htmlContent.IndexOf("<body>") + 6, bodyStr).Replace("<sectio1", "<div class=\"book\"").Replace("</sectio1>", "</div>");
                Common.SaveWithEncoding(_tempDir + @"\" + htmlFile, htmlContent);
            }
            else
            {
                playOrder = CreateChapters(bodyStr, htmlElement, packElement, titles, ncxElement, element5, ref index);
            }
            if (!_currentSettings.ntoc && _currentSettings.ContentOf)
            {
                var navPoint = new XElement("navPoint");
                navPoint.Add(Common.CreateAttribute("id", "navpoint-" + (index + 2).ToString()));
                navPoint.Add(Common.CreateAttribute("playOrder", (index + 2).ToString()));
                var navLabel = new XElement("navLabel");
                var textEl = new XElement("text");
                textEl.Add("Contents");
                navLabel.Add(textEl);
                navPoint.Add(navLabel);
                navLabel = new XElement("content");
                navLabel.Add(new XAttribute("src", "toc.html#toc"));
                navPoint.Add(navLabel);
                ncxElement.Elements("navMap").First().Add(navPoint);
                navPoint = new XElement("item");
                navPoint.Add(new XAttribute("id", "content"));
                navPoint.Add(new XAttribute("media-type", "text/x-oeb1-document"));
                navPoint.Add(new XAttribute("href", "toc.html"));
                navPoint.Add("");
                packElement.Elements("manifest").First().Add(navPoint);
                navPoint = new XElement("itemref");
                navPoint.Add(new XAttribute("idref", "content"));
                packElement.Elements("spine").First().Add(navPoint);
                navPoint = new XElement("reference");
                navPoint.Add(new XAttribute("type", "toc"));
                navPoint.Add(new XAttribute("title", "toc"));
                navPoint.Add(new XAttribute("href", "toc.html"));
                packElement.Elements("guide").First().Add(navPoint);
            }
            if (notesCreated)
            {
                foreach (var item in notesList2)
                {
                    var itemEl = new XElement("item");
                    itemEl.Add(Common.CreateAttribute("id", item.Value));
                    itemEl.Add(new XAttribute("media-type", "text/x-oeb1-document"));
                    itemEl.Add(Common.CreateAttribute("href", item.Name));
                    itemEl.Add("");
                    packElement.Elements("manifest").First().Add(itemEl);
                    itemEl = new XElement("itemref");
                    itemEl.Add(Common.CreateAttribute("idref", item.Value));
                    packElement.Elements("spine").First().Add(itemEl);
                    itemEl = new XElement("navPoint");
                    itemEl.Add(Common.CreateAttribute("id", "navpoint-" + item.Value));
                    itemEl.Add(Common.CreateAttribute("playOrder", playOrder));
                    var navLabel = new XElement("navLabel");
                    var textEl = new XElement("text");
                    textEl.Add(item.Value);
                    navLabel.Add(textEl);
                    itemEl.Add(navLabel);
                    navLabel = new XElement("content");
                    navLabel.Add(Common.CreateAttribute("src", item.Name));
                    itemEl.Add(navLabel);
                    ncxElement.Elements("navMap").First().Add(itemEl);
                    if (!_currentSettings.ntoc)
                    {
                        itemEl = new XElement("li");
                        navLabel = new XElement("a");
                        navLabel.Add(Common.CreateAttribute("href", item.Name));
                        navLabel.Add(item.Value);
                        itemEl.Add(navLabel);
                        element5.Elements("body").First().Elements("ul").First().Add(itemEl);
                    }
                    playOrder++;
                }
            }
            File.WriteAllText(_tempDir + @"\book.css", _defaultCSS);
            packElement.Save(_tempDir + @"\" + bookName + ".opf");
            packElement.RemoveAll();
            ncxElement.Save(_tempDir + @"\toc.ncx");
            ncxElement.RemoveAll();
            if (!_currentSettings.ntoc)
            {
                element5.Save(_tempDir + @"\toc.html");
                element5.RemoveAll();
            }
            var parentPath = Path.GetDirectoryName(bookPath);
            if (String.IsNullOrEmpty(parentPath))
            {
                bookPath = Path.Combine(_workingFolder, bookPath);
                parentPath = _workingFolder;
            }
            var result = Common.CreateMobi(_workingFolder, _tempDir, bookName, parentPath, _currentSettings.deleteOrigin, bookPath);
            ClearTempFolder();
            return result;
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

        private int CreateChapters(string bodyStr, XElement element2, XElement element3, List<DataItem> titles, XElement ncxElement, XElement element5, ref int index)
        {
            Console.Write("Chapters creation...");
            var bookNum = 0;
            var num16 = bodyStr.IndexOf("<sectio1", 2);
            var num17 = bodyStr.IndexOf("</sectio1>");
            var start = 0;
            var str40 = "";
            var flag17 = false;
            var num9 = 2;
            while (num17 != -1)
            {
                string bodyContent;
                bool noBookFlag;
                if ((num16 < num17) & (num16 != -1))
                {
                    if (!flag17)
                    {
                        bodyContent = bodyStr.Substring(start, num16 - start) + "</sectio1>";
                        noBookFlag = !XElement.Parse(bodyContent).Elements("p").Any();
                        bodyContent = str40 + bodyContent;
                        str40 = "";
                        bodyContent = Common.TabRep(bodyContent);
                        Common.SaveElementToFile(element2.ToString(), bodyContent, noBookFlag, _tempDir, bookNum);
                        var itemEl = new XElement("item");
                        itemEl.Add(Common.CreateAttribute("id", "text" + bookNum));
                        itemEl.Add(new XAttribute("media-type", "text/x-oeb1-document"));
                        itemEl.Add(Common.CreateAttribute("href", "book" + bookNum + ".html"));
                        itemEl.Add("");
                        element3.Elements("manifest").First().Add(itemEl);
                        itemEl = new XElement("itemref");
                        itemEl.Add(Common.CreateAttribute("idref", "text" + bookNum));
                        element3.Elements("spine").First().Add(itemEl);
                        index = 0;
                        while (index < titles.Count)
                        {
                            var str34 = titles[index].Value;
                            var str35 = titles[index].Name;
                            if (bodyContent.IndexOf("id=\"" + str35 + "\"") != -1)
                            {
                                itemEl = new XElement("navPoint");
                                itemEl.Add(Common.CreateAttribute("id", "navpoint-" + num9));
                                itemEl.Add(Common.CreateAttribute("playOrder", num9));
                                var element14 = new XElement("navLabel");
                                var element13 = new XElement("text");
                                element13.Add(str34);
                                element14.Add(element13);
                                itemEl.Add(element14);
                                element14 = new XElement("content");
                                element14.Add(Common.CreateAttribute("src", "book" + bookNum + ".html#" + str35));
                                itemEl.Add(element14);
                                ncxElement.Elements("navMap").First().Add(itemEl);
                                if (!_currentSettings.ntoc)
                                {
                                    itemEl = new XElement("li");
                                    element14 = new XElement("a");
                                    element14.Add(Common.CreateAttribute("href", "book" + bookNum + ".html#" + str35));
                                    element14.Add(str34);
                                    itemEl.Add(element14);
                                    element5.Elements("body").First().Elements("ul").First().Add(itemEl);
                                }
                                num9++;
                            }
                            index++;
                        }
                    }
                    start = num16;
                    num16 = bodyStr.IndexOf("<sectio1", (num16 + 1));
                    flag17 = false;
                }
                else
                {
                    if (!flag17)
                    {
                        bodyContent = bodyStr.Substring(start, (num17 - start) + 11);
                        noBookFlag = !XElement.Parse(bodyContent).Elements("p").Any();
                        bodyContent = str40 + bodyContent;
                        str40 = "";
                        bodyContent = Common.TabRep(bodyContent);
                        Common.SaveElementToFile(element2.ToString(), bodyContent, noBookFlag, _tempDir, bookNum);

                        var itemEl = new XElement("item");
                        itemEl.Add(Common.CreateAttribute("id", "text" + bookNum));
                        itemEl.Add(new XAttribute("media-type", "text/x-oeb1-document"));
                        itemEl.Add(Common.CreateAttribute("href", "book" + bookNum + ".html"));
                        itemEl.Add("");
                        element3.Elements("manifest").First().Add(itemEl);
                        itemEl = new XElement("itemref");
                        itemEl.Add(Common.CreateAttribute("idref", "text" + bookNum));
                        element3.Elements("spine").First().Add(itemEl);
                        for (index = 0; index < titles.Count; index++)
                        {
                            var str34 = titles[index].Value;
                            var str35 = titles[index].Name;
                            if (bodyContent.IndexOf("id=\"" + str35 + "\"") == -1) continue;
                            itemEl = new XElement("navPoint");
                            itemEl.Add(Common.CreateAttribute("id", "navpoint-" + num9));
                            itemEl.Add(Common.CreateAttribute("playOrder", num9));
                            var navLabel = new XElement("navLabel");
                            var textEl = new XElement("text");
                            textEl.Add(str34);
                            navLabel.Add(textEl);
                            itemEl.Add(navLabel);
                            navLabel = new XElement("content");
                            navLabel.Add(Common.CreateAttribute("src", "book" + bookNum + ".html#" + str35));
                            itemEl.Add(navLabel);
                            ncxElement.Elements("navMap").First().Add(itemEl);

                            if (!_currentSettings.ntoc)
                            {
                                itemEl = new XElement("li");
                                navLabel = new XElement("a");
                                navLabel.Add(Common.CreateAttribute("href", "book" + bookNum + ".html#" + str35));
                                navLabel.Add(str34);
                                itemEl.Add(navLabel);
                                element5.Elements("body").First().Elements("ul").First().Add(itemEl);
                            }
                            num9++;
                        }
                    }
                    flag17 = true;
                    start = num17;
                    num17 = bodyStr.IndexOf("</sectio1>", (num17 + 1));
                }
                if (!flag17)
                {
                    bookNum++;
                }
            }
            var referenceEl = new XElement("reference");
            referenceEl.Add(new XAttribute("type", "text"));
            referenceEl.Add(new XAttribute("title", "Book"));
            referenceEl.Add(Common.CreateAttribute("href", "book0.html"));
            element3.Elements("guide").First().Add(referenceEl);
            Console.WriteLine("(OK)");
            return num9;
        }

        public XElement ConvertToHtml(XElement book, out int playOrder, out XElement htmlElement, out XElement packElement, out List<DataItem> notesList2, out string bodyStr, out bool notesCreated, out XElement ncxElement, out List<DataItem> titles, out int index)
        {
            Console.Write("FB2 to HTML...");

            Common.UpdateImages(book, imagesPrepared);
            var result = book;
            const string str = "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧЩШЬЪЫЭЮЯQWERTYUIOPASDFGHJKLZXCVBNM";

            playOrder = 0;
            Common.CreateTitlePage(book, _tempDir);

            htmlElement = new XElement("html");
            var linkEl = new XElement("link");
            linkEl.Add(new XAttribute("type", "text/css"));
            linkEl.Add(new XAttribute("href", "book.css"));
            linkEl.Add(new XAttribute("rel", "Stylesheet"));
            htmlElement.Add(new XElement("head", linkEl));
            htmlElement.Add(new XElement("body", ""));
            var str20 = Common.Value(book.Elements("description").First().Elements("title-info").First().Elements("lang"));
            if (String.IsNullOrEmpty(str20))
                str20 = "ru";
            var packEl = new XElement("package");
            linkEl = new XElement("meta");
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
            content.Add(Common.Value(book.Elements("description").Elements("title-info").Elements("book-title")));
            linkEl.Add(content);
            content = new XElement(nsHttp.GetName("Language"));
            content.Add(str20);
            linkEl.Add(content);
            content = new XElement(nsHttp.GetName("Creator"));
            content.Add(Common.Value(book.Elements("description").Elements("title-info").Elements("author").First().Elements("last-name")) + " " + Common.Value(book.Elements("description").Elements("title-info").Elements("author").First().Elements("first-name")) + " " + Common.Value(book.Elements("description").Elements("title-info").Elements("author").First().Elements("middle-name")));
            linkEl.Add(content);
            content = new XElement(nsHttp.GetName("Publisher"));
            content.Add(Common.Value(book.Elements("description").Elements("publish-info").Elements("publisher")));
            linkEl.Add(content);
            content = new XElement(nsHttp.GetName("date"));
            content.Add(Common.Value(book.Elements("description").Elements("publish-info").Elements("year")));
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
            packElement = packEl;

            //if (!_currentSettings.nstitle)
            {
                packEl = new XElement("item");
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
                packEl.Add(new XAttribute("title", "Book"));
                packEl.Add(new XAttribute("href", "booktitle.html"));
                packElement.Elements("guide").First().Add(packEl);
            }
            if (!_currentSettings.ntoc && !_currentSettings.ContentOf)
            {
                packEl = new XElement("item");
                packEl.Add(new XAttribute("id", "content"));
                packEl.Add(new XAttribute("media-type", "text/x-oeb1-document"));
                packEl.Add(new XAttribute("href", "toc.html"));
                packEl.Add("");
                packElement.Elements("manifest").First().Add(packEl);
                packEl = new XElement("itemref");
                packEl.Add(new XAttribute("idref", "content"));
                packElement.Elements("spine").First().Add(packEl);
                packEl = new XElement("reference");
                packEl.Add(new XAttribute("type", "toc"));
                packEl.Add(new XAttribute("title", "toc"));
                packEl.Add(new XAttribute("href", "toc.html"));
                packElement.Elements("guide").First().Add(packEl);
            }
            var imgSrc = Common.AttributeValue(book.Elements("description").Elements("title-info").Elements("coverpage").Elements("div").Elements("img"), "src");
            if (!string.IsNullOrEmpty(imgSrc))
            {
                packEl = new XElement("EmbeddedCover");
                packEl.Add(imgSrc);
                packElement.Elements("metadata").First().Elements("dc-metadata").First().Elements("x-metadata").First().Add(packEl);
            }
            var notesList = new List<DataItem>();
            notesList2 = new List<DataItem>();
            notesCreated = false;
            var bodies = new List<XElement>();
            bodies.AddRange(book.Elements("body"));
            if (bodies.Count() > 1)
            {
                for (var i = 1; i < bodies.Count; i++)
                {
                    var bodyName = Common.AttributeValue(bodies[i], "name");
                    if (string.IsNullOrEmpty(bodyName)) continue;
                    notesList2.Add(new DataItem(bodyName + ".html", bodyName));
                    var list = bodies[i].Descendants("section").ToList();
                    if (list.Count > 0)
                    {
                        foreach (var t in list)
                        {
                            var di = new DataItem();
                            if (!_currentSettings.nbox)
                            {
                                di.Name = bodyName + ".html";
                            }
                            else
                            {
                                var list2 = t.Descendants("p").ToList();
                                var boldBox = "<b>";
                                for (var idx2 = 0; idx2 < list2.Count; idx2++)
                                {
                                    if (idx2 == 0)
                                        boldBox = boldBox + list2[idx2].Value + "</b> ";
                                    else
                                        boldBox = boldBox + " " + list2[idx2].Value.Replace('<', '[').Replace('>', ']');
                                }
                                boldBox = boldBox.Replace("&", "");
                                di.Name = boldBox;
                            }
                            di.Value = Common.AttributeValue(t, "id");
                            notesList.Add(di);
                        }
                    }

                    if (!_currentSettings.nbox)
                    {
                        Common.CreateNoteBox(book, i, bodyName, _tempDir);
                        notesCreated = true;
                    }
                }
            }
            bodyStr = bodies[0].ToString();
            bodyStr = UpdateATags(bodyStr, notesList, _currentSettings.nbox);

            var number = 1;
            var numArray = new List<SectionInfo>();
            var startIndex = bodyStr.IndexOf("<section");
            var endIndex = bodyStr.IndexOf("</section>");
            while (endIndex > 0)
            {
                var si = new SectionInfo();
                if ((startIndex < endIndex) & (startIndex != -1))
                {
                    si.Val1 = number;
                    si.Val2 = startIndex;
                    si.Val3 = 1;
                    bodyStr = bodyStr.Remove(startIndex, 8).Insert(startIndex, "<sectio1");
                    number++;
                }
                else
                {
                    number--;
                    si.Val1 = number;
                    si.Val2 = endIndex;
                    si.Val3 = -1;
                    bodyStr = bodyStr.Remove(endIndex, 10).Insert(endIndex, "</sectio1>");
                }
                if (startIndex != -1)
                {
                    startIndex = bodyStr.IndexOf("<section", startIndex);
                }
                endIndex = bodyStr.IndexOf("</section>", endIndex);
                numArray.Add(si);
            }
            startIndex = bodyStr.IndexOf("<title");
            endIndex = bodyStr.IndexOf("</title>");
            while (startIndex > 0)
            {
                number = 0;
                for (var i = 0; i < numArray.Count - 1; i++)
                {
                    if ((startIndex <= numArray[i].Val2) || (startIndex >= numArray[i + 1].Val2)) continue;
                    number = numArray[i].Val1;
                    break;
                }
                if (number > 9)
                    number = 9;
                if (number < 0)
                    number = 0;
                bodyStr = bodyStr.Remove(startIndex, 6).Insert(startIndex, "<titl" + number).Remove(endIndex, 8).Insert(endIndex, "</titl" + number + ">");
                startIndex = bodyStr.IndexOf("<title", endIndex);
                endIndex = bodyStr.IndexOf("</title>", endIndex);
            }

            packEl = new XElement("ncx");
            headEl = new XElement("head");
            headEl.Add("");
            packEl.Add(headEl);
            headEl = new XElement("docTitle");
            linkEl = new XElement("text");
            linkEl.Add("KF8");
            headEl.Add(linkEl);
            packEl.Add(headEl);
            headEl = new XElement("navMap");
            headEl.Add("");
            packEl.Add(headEl);
            ncxElement = packEl;
            //if (!_currentSettings.nstitle)
            {
                packEl = new XElement("navPoint");
                packEl.Add(new XAttribute("id", "navpoint-0"));
                packEl.Add(new XAttribute("playOrder", "0"));
                headEl = new XElement("navLabel");
                linkEl = new XElement("text");
                linkEl.Add("Обложка");
                headEl.Add(linkEl);
                packEl.Add(headEl);
                headEl = new XElement("content");
                headEl.Add(new XAttribute("src", "booktitle.html#booktitle"));
                packEl.Add(headEl);
                ncxElement.Elements("navMap").First().Add(packEl);
            }
            if (!_currentSettings.ntoc && !_currentSettings.ContentOf)
            {
                packEl = new XElement("navPoint");
                packEl.Add(new XAttribute("id", "navpoint-1"));
                packEl.Add(new XAttribute("playOrder", "1"));
                headEl = new XElement("navLabel");
                linkEl = new XElement("text");
                linkEl.Add("Содержание");
                headEl.Add(linkEl);
                packEl.Add(headEl);
                headEl = new XElement("content");
                headEl.Add(new XAttribute("src", "toc.html#toc"));
                packEl.Add(headEl);
                ncxElement.Elements("navMap").First().Add(packEl);
            }
            var titleIdx = 1;
            if (!_currentSettings.ntoc)
            {
                packEl = new XElement("html");
                headEl = new XElement("head");
                linkEl = new XElement("title");
                linkEl.Add("Содержание");
                headEl.Add(linkEl);
                linkEl = new XElement("link");
                linkEl.Add(new XAttribute("type", "text/css"));
                linkEl.Add(new XAttribute("href", "book.css"));
                linkEl.Add(new XAttribute("rel", "Stylesheet"));
                headEl.Add(linkEl);
                packEl.Add(headEl);
                headEl = new XElement("body");
                linkEl = new XElement("div");
                linkEl.Add(new XAttribute("class", "title"));
                content = new XElement("div");
                content.Add(new XAttribute("class", "title1"));
                content.Add(new XAttribute("id", "toc"));
                content.Add(new XElement("p", "Содержание"));
                linkEl.Add(content);
                headEl.Add(linkEl);
                linkEl = new XElement("ul");
                linkEl.Add("");
                headEl.Add(linkEl);
                packEl.Add(headEl);
                result = packEl;
            }
            var prevTag = "";
            var specTag = false;
            var tagClosed = true;
            titles = new List<DataItem>();
            index = bodyStr.IndexOf("<");
            while (index > -1)
            {
                playOrder = index;
                var ch = bodyStr[index];
                var curTag = "";
                while (ch != '>')
                {
                    ch = bodyStr[index];
                    index++;
                    curTag = curTag + ch;
                }
                if (curTag.Contains("<titl") || curTag.Contains("<epigraph") || curTag.Contains("<subtitle") || curTag.Contains("<div") ||
                    curTag.Contains("<poem") || curTag.Contains("<cite"))
                {
                    specTag = false;
                }
                if (curTag.Contains("<p ") || curTag.Contains("<p>"))
                {
                    if (!_currentSettings.noBig)
                    {
                        if (prevTag.Equals("</titl0>") || prevTag.Equals("</titl1>") || prevTag.Equals("</titl2>") || prevTag.Equals("</titl3>") ||
                            prevTag.Equals("</titl4>") || prevTag.Equals("</titl5>") || prevTag.Equals("</titl6>") || prevTag.Equals("</titl7>") ||
                            prevTag.Equals("</titl8>") || prevTag.Equals("</titl9>") || prevTag.Equals("</titl1>") || prevTag.Equals("</subtitle>") || prevTag.Equals("</epigraph>"))
                        {
                            specTag = true;
                            while (ch != '<')
                            {
                                ch = bodyStr[index];
                                if (ch != ' ')
                                {
                                    if (str.IndexOf(ch) != -1)
                                    {
                                        bodyStr = bodyStr.Remove(index, 1).Insert(index, "<span class=\"dropcaps\">" + ch + "</span>").Insert(playOrder + 2, " style=\"text-indent:0px;\"");
                                    }
                                    break;
                                }
                                index++;
                            }
                        }
                        else if (specTag)
                        {
                            while (ch != '<')
                            {
                                ch = bodyStr[index];
                                if (ch != ' ')
                                {
                                    if (str.IndexOf(ch) != -1)
                                        bodyStr = bodyStr.Remove(index, 1).Insert(index, "<span class=\"dropcaps2\">" + ch + "</span>");
                                    break;
                                }
                                index++;
                            }
                        }
                    }
                }
                else if (curTag.Contains("<titl"))
                {
                    titleIdx++;
                    bodyStr = bodyStr.Insert(playOrder + 6, " id=\"title" + titleIdx + "\"");
                    index = bodyStr.IndexOf(">", index);
                    startIndex = bodyStr.IndexOf("</titl", index);
                    var substring = bodyStr.Substring(index + 1, (startIndex - index) - 1);
                    var buf1 = "";
                    var buf2 = "";
                    foreach (var t in substring)
                    {
                        switch (t)
                        {
                            case '<':
                                tagClosed = false;
                                buf2 = "";
                                break;
                            case '>':
                                if (buf2 == "/p")
                                    buf1 = buf1 + " ";
                                tagClosed = true;
                                break;
                            default:
                                if (tagClosed)
                                    buf1 = buf1 + t;
                                else
                                    buf2 = buf2 + t;
                                break;
                        }
                    }
                    titles.Add(new DataItem("title" + titleIdx, buf1));
                }
                if (curTag.Equals("</div>") || curTag.Equals("</cite>") || curTag.Equals("</poem>"))
                    specTag = true;
                if (!curTag.Equals("<empty-line/>") && !curTag.Equals("<empty-line />"))
                    prevTag = curTag;
                index = bodyStr.IndexOf("<", index);
            }
            bodyStr = Common.ReplaceSomeTags(bodyStr);
            Console.WriteLine("(OK)");
            return result;
        }

        private static string UpdateATags(string bodyStr, List<DataItem> notesList, bool nbox)
        {
            bodyStr = bodyStr.Replace("<a ", "<A ").Replace("</a>", "</A> ");
            var str23 = "";
            var startIndex = bodyStr.IndexOf("<A ");
            var endIndex = bodyStr.IndexOf("</A>");
            var num18 = 1;
            while (startIndex > -1)
            {
                var bodyLen = bodyStr.Length - 1;
                bodyStr = bodyStr.Insert(startIndex + 1, "!");
                var oldValue = "<!A ";
                var num47 = bodyLen;
                for (var num30 = startIndex + 4; num30 <= num47; num30++)
                {
                    var ch = bodyStr[num30];
                    if (ch == '>')
                    {
                        oldValue = oldValue + ch;
                        break;
                    }
                    oldValue = oldValue + ch;
                }
                var str22 = "";
                var sharpIdx = oldValue.IndexOf("#");
                if (sharpIdx != -1)
                {
                    var oldLen = oldValue.Length;
                    for (var i = sharpIdx + 1; i <= oldLen; i++)
                    {
                        var ch = oldValue[i];
                        if (ch == '\"')
                            break;
                        str22 = str22 + ch;
                    }
                    str23 = "";
                    foreach (var note in notesList)
                    {
                        if (!str22.Equals(note.Value)) continue;
                        if (nbox)
                            str23 = Common.FormatToHTML(note.Name);
                        else
                            str23 = note.Name + "#" + str22;
                        break;
                    }
                }
                bodyStr = nbox
                              ? bodyStr.Insert(endIndex + 5, "<span class=\"note\">" + str23 + "</span>").Replace(oldValue, "<sup>")
                              : bodyStr.Replace(oldValue, "<a href = \"" + str23 + "\"><sup>");
                num18++;
                startIndex = bodyStr.IndexOf("<A ", startIndex);
                if (startIndex != -1)
                    endIndex = bodyStr.IndexOf("</A>", startIndex);
            }
            bodyStr = bodyStr.Replace("</A>", nbox ? "</sup>" : "</sup></a>");
            return bodyStr;
        }
    }
}
