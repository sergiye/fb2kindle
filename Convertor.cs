using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Fb2Kindle
{
    class Convertor
    {
        const string images = "images";
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
            _tempDir = Common.PrepareTempFolder(bookName, images, _workingFolder);
            if (_customFontsUsed && Directory.Exists(_workingFolder + @"\fonts"))
            {
                Directory.CreateDirectory(_tempDir + @"\fonts");
                Common.CopyDirectory(_workingFolder + @"\fonts", _tempDir + @"\fonts", true);
            }
            imagesPrepared = !_currentSettings.noImages && Common.ExtractImages(book, _tempDir, images);

            XElement htmlElement;
            XElement packElement;
            XElement ncxElement;
            List<DataItem> notesList2;
            List<DataItem> titles;
            string bodyStr;
            bool flag13;
            int index;
            int num9;
            var element5 = ConvertToHtml(book, out num9, out htmlElement, out packElement, 
                out notesList2, out bodyStr, out flag13, out ncxElement, out titles, out index);
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
                if (!_currentSettings.nh)
                    bodyStr = Common.GipherHTML(bodyStr);
                htmlContent = htmlContent.Insert(htmlContent.IndexOf("<body>") + 6, bodyStr).Replace("<sectio1", "<div class=\"book\"").Replace("</sectio1>", "</div>");
                Common.SaveWithEncoding(_tempDir + @"\" + htmlFile, htmlContent);
            }
            else
            {
                num9 = CreateChapters(bodyStr, htmlElement, packElement, titles, ncxElement, element5, ref index);
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
            if (flag13)
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
                    itemEl.Add(Common.CreateAttribute("playOrder", num9));
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
                    num9++;
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
            try
            {
                Directory.Delete(_tempDir, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error clearing temp folder: " + ex.Message);
            }
            return result;
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
            var flag18 = false;
            var noBookFlag = false;
            var num9 = 2;
            while (num17 != -1)
            {
                string bodyContent;
                if ((num16 < num17) & (num16 != -1))
                {
                    if (!flag17)
                    {
                        bodyContent = bodyStr.Substring(start, num16 - start) + "</sectio1>";
                        if (_currentSettings.dztitle && (bookNum == 0))
                        {
                            flag18 = true;
                        }
                        else if (!XElement.Parse(bodyContent).Elements("p").Any())
                        {
                            if (_currentSettings.ntitle0)
                            {
                                str40 = str40 + bodyContent;
                                flag18 = true;
                            }
                            noBookFlag = true;
                        }
                        else
                        {
                            noBookFlag = false;
                        }
                        if (!flag18)
                        {
                            bodyContent = str40 + bodyContent;
                            str40 = "";
                            bodyContent = Common.TabRep(bodyContent);
                            if (!_currentSettings.nh)
                                bodyContent = Common.GipherHTML(bodyContent);
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
                        if (_currentSettings.dztitle && (bookNum == 0))
                        {
                            flag18 = true;
                        }
                        else if (!XElement.Parse(bodyContent).Elements("p").Any())
                        {
                            if (_currentSettings.ntitle0)
                            {
                                str40 = str40 + bodyContent;
                                flag18 = true;
                            }
                            noBookFlag = true;
                        }
                        else
                        {
                            noBookFlag = false;
                        }
                        if (!flag18)
                        {
                            bodyContent = str40 + bodyContent;
                            str40 = "";
                            bodyContent = Common.TabRep(bodyContent);
                            if (!_currentSettings.nh)
                                bodyContent = Common.GipherHTML(bodyContent);
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
                    }
                    flag17 = true;
                    start = num17;
                    num17 = bodyStr.IndexOf("</sectio1>", (num17 + 1));
                }
                if (!flag17 & !flag18)
                {
                    bookNum++;
                }
                flag18 = false;
            }
            var referenceEl = new XElement("reference");
            referenceEl.Add(new XAttribute("type", "text"));
            referenceEl.Add(new XAttribute("title", "Book"));
            referenceEl.Add(Common.CreateAttribute("href", "book0.html"));
            element3.Elements("guide").First().Add(referenceEl);
            Console.WriteLine("(OK)");
            return num9;
        }

        public XElement ConvertToHtml(XElement element, out int num9, out XElement htmlElement, out XElement packElement, out List<DataItem> notesList2, out string bodyStr, out bool flag13, out XElement ncxElement, out List<DataItem> titles, out int index)
        {
            Console.Write("FB2 to HTML...");

            var allText = UpdateImages(element.ToString());
            element = XElement.Parse(allText);
            var element5 = element;
            const string str = "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧЩШЬЪЫЭЮЯQWERTYUIOPASDFGHJKLZXCVBNM";

            num9 = 0;
            var num = element.Elements("body").Count();
            if (!_currentSettings.nstitle)
                Common.CreateTitlePage(element, _tempDir);

            htmlElement = new XElement("html");
            var linkEl = new XElement("link");
            linkEl.Add(new XAttribute("type", "text/css"));
            linkEl.Add(new XAttribute("href", "book.css"));
            linkEl.Add(new XAttribute("rel", "Stylesheet"));
            htmlElement.Add(new XElement("head", linkEl));
            htmlElement.Add(new XElement("body", ""));
            var str20 = Common.Value(element.Elements("description").First().Elements("title-info").First().Elements("lang"));
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
            content.Add(Common.Value(element.Elements("description").Elements("title-info").Elements("book-title")));
            linkEl.Add(content);
            content = new XElement(nsHttp.GetName("Language"));
            content.Add(str20);
            linkEl.Add(content);
            content = new XElement(nsHttp.GetName("Creator"));
            content.Add(Common.Value(element.Elements("description").Elements("title-info").Elements("author").First().Elements("last-name")) + " " + Common.Value(element.Elements("description").Elements("title-info").Elements("author").First().Elements("first-name")) + " " + Common.Value(element.Elements("description").Elements("title-info").Elements("author").First().Elements("middle-name")));
            linkEl.Add(content);
            content = new XElement(nsHttp.GetName("Publisher"));
            content.Add(Common.Value(element.Elements("description").Elements("publish-info").Elements("publisher")));
            linkEl.Add(content);
            content = new XElement(nsHttp.GetName("date"));
            content.Add(Common.Value(element.Elements("description").Elements("publish-info").Elements("year")));
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
            if (!_currentSettings.nstitle)
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
            var str3 = Common.AttributeValue(element.Elements("description").Elements("title-info").Elements("coverpage").Elements("div").Elements("img"), "src");
            if (!string.IsNullOrEmpty(str3))
            {
                packEl = new XElement("EmbeddedCover");
                packEl.Add(str3);
                packElement.Elements("metadata").First().Elements("dc-metadata").First().Elements("x-metadata").First().Add(packEl);
            }
            var notesList = new List<DataItem>();
            notesList2 = new List<DataItem>();
            bodyStr = element.Elements("body").First().ToString();
            flag13 = false;
            if (num > 1)
            {
                for (var i = 1; i < num; i++)
                {
                    var bodyName = Common.AttributeValue(element.Elements("body").ElementAtOrDefault(i), "name");
                    if (string.IsNullOrEmpty(bodyName)) continue;
                    notesList2.Add(new DataItem(bodyName + ".html", bodyName));
                    var list = element.Elements("body").ElementAtOrDefault(i).Descendants("section").ToList();
                    if (list.Count > 0)
                    {
                        foreach (var t in list)
                        {
                            var di = new DataItem();
                            if (_currentSettings.nbox)
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
                            else
                            {
                                di.Name = bodyName + ".html";
                            }
                            di.Value = Common.AttributeValue(t, "id");
                            notesList.Add(di);
                        }
                    }
                    if (!_currentSettings.nbox)
                    {
                        packEl = new XElement("html");
                        headEl = new XElement("head");
                        linkEl = new XElement("link");
                        linkEl.Add(new XAttribute("type", "text/css"));
                        linkEl.Add(new XAttribute("href", "book.css"));
                        linkEl.Add(new XAttribute("rel", "Stylesheet"));
                        headEl.Add(linkEl);
                        packEl.Add(headEl);
                        headEl = new XElement("body");
                        headEl.Add(element.Elements("body").ElementAtOrDefault(i).Nodes());
                        packEl.Add(headEl);
                        var htmltxt = Common.FormatToHTML(packEl.ToString());
                        if (!_currentSettings.nh)
                            htmltxt = Common.GipherHTML(htmltxt);
                        Common.SaveWithEncoding(_tempDir + @"\" + bodyName + ".html", htmltxt);
                        flag13 = true;
                    }
                }
            }
            bodyStr = bodyStr.Replace("<a ", "<A ").Replace("</a>", "</A> ");
            var str23 = "";
            var startIndex = bodyStr.IndexOf("<A ");
            var num15 = bodyStr.IndexOf("</A>");
            var num18 = 1;
            while (startIndex > -1)
            {
                var num12 = bodyStr.Length - 1;
                bodyStr = bodyStr.Insert(startIndex + 1, "!");
                var oldValue = "<!A ";
                var num47 = num12;
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
                var num29 = oldValue.IndexOf("#");
                if (num29 != -1)
                {
                    var num48 = oldValue.Length;
                    for (var num31 = num29 + 1; num31 <= num48; num31++)
                    {
                        var ch = oldValue[num31];
                        if (ch == '\"')
                            break;
                        str22 = str22 + ch;
                    }
                    str23 = "";
                    for (var i = 0; i < notesList.Count; i++)
                    {
                        if (str22 == notesList[i].Value)
                        {
                            if (_currentSettings.nbox)
                                str23 = Common.FormatToHTML(notesList[i].Name);
                            else
                                str23 = notesList[i].Name + "#" + str22;
                            break;
                        }
                    }
                }
                bodyStr = _currentSettings.nbox
                              ? bodyStr.Insert(num15 + 5, "<span class=\"note\">" + str23 + "</span>").Replace(oldValue, "<sup>")
                              : bodyStr.Replace(oldValue, "<a href = \"" + str23 + "\"><sup>");
                num18++;
                startIndex = bodyStr.IndexOf("<A ", startIndex);
                if (startIndex != -1)
                    num15 = bodyStr.IndexOf("</A>", startIndex);
            }
            bodyStr = bodyStr.Replace("</A>", _currentSettings.nbox ? "</sup>" : "</sup></a>");
            var num2 = bodyStr.IndexOf("<body>");
            var number = 1;
            var numArray = new List<SectionInfo>();
            var num10 = 1;
            var num16 = bodyStr.IndexOf("<section");
            var num17 = bodyStr.IndexOf("</section>");
            while (num17 > 0)
            {
                var si = new SectionInfo();
                if ((num16 < num17) & (num16 != -1))
                {
                    si.Val1 = number;
                    si.Val2 = num16;
                    si.Val3 = 1;
                    bodyStr = bodyStr.Remove(num16, 8).Insert(num16, "<sectio1");
                    number++;
                }
                else
                {
                    number--;
                    si.Val1 = number;
                    si.Val2 = num17;
                    si.Val3 = -1;
                    bodyStr = bodyStr.Remove(num17, 10).Insert(num17, "</sectio1>");
                }
                if (num16 != -1)
                {
                    num16 = bodyStr.IndexOf("<section", num16);
                }
                num17 = bodyStr.IndexOf("</section>", num17);
                numArray.Add(si);
                num10++;
            }
            num16 = bodyStr.IndexOf("<title");
            num17 = bodyStr.IndexOf("</title>");
            while (num16 > 0)
            {
                number = 0;
                for (var i = 1; i <= num10 - 2; i++)
                {
                    if ((num16 > numArray[i].Val2) && (num16 < numArray[i + 1].Val2))
                    {
                        number = numArray[i].Val1;
                        break;
                    }
                }
                if (number > 9)
                    number = 9;
                if (number < 0)
                    number = 0;
                bodyStr = bodyStr.Remove(num16, 6).Insert(num16, "<titl" + number).Remove(num17, 8).Insert(num17, "</titl" + number + ">");
                num16 = bodyStr.IndexOf("<title", num17);
                num17 = bodyStr.IndexOf("</title>", num17);
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
            if (!_currentSettings.nstitle)
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
                element5 = packEl;
            }
            var prevTag = "";
            var flag11 = false;
            var flag15 = true;
            titles = new List<DataItem>();
            index = bodyStr.IndexOf("<");
            while (index > -1)
            {
                num9 = index;
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
                    flag11 = false;
                }
                if (curTag.Contains("<p ") || curTag.Contains("<p>"))
                {
                    if (!_currentSettings.noBig)
                    {
                        if (prevTag.Equals("</titl0>") || prevTag.Equals("</titl1>") || prevTag.Equals("</titl2>") || prevTag.Equals("</titl3>") ||
                            prevTag.Equals("</titl4>") || prevTag.Equals("</titl5>") || prevTag.Equals("</titl6>") || prevTag.Equals("</titl7>") ||
                            prevTag.Equals("</titl8>") || prevTag.Equals("</titl9>") || prevTag.Equals("</titl1>") || prevTag.Equals("</subtitle>") || prevTag.Equals("</epigraph>"))
                        {
                            flag11 = true;
                            while (ch != '<')
                            {
                                ch = bodyStr[index];
                                if (ch != ' ')
                                {
                                    if (str.IndexOf(ch) != -1)
                                    {
                                        bodyStr = bodyStr.Remove(index, 1).Insert(index, "<span class=\"dropcaps\">" + ch + "</span>").Insert(num9 + 2, " style=\"text-indent:0px;\"");
                                    }
                                    break;
                                }
                                index++;
                            }
                        }
                        else if (flag11)
                        {
                            while (ch != '<')
                            {
                                ch = bodyStr[index];
                                if (ch != ' ')
                                {
                                    if (str.IndexOf(ch) != -1)
                                    {
                                        bodyStr = bodyStr.Remove(index, 1).Insert(index, "<span class=\"dropcaps2\">" + ch + "</span>");
                                    }
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
                    bodyStr = bodyStr.Insert(num9 + 6, " id=\"title" + titleIdx + "\"");
                    index = bodyStr.IndexOf(">", index);
                    num16 = bodyStr.IndexOf("</titl", index);
                    var substring = bodyStr.Substring(index + 1, (num16 - index) - 1);
                    var buf1 = "";
                    var buf2 = "";
                    for (var num34 = 0; num34 < substring.Length; num34++)
                    {
                        ch = substring[num34];
                        switch (ch)
                        {
                            case '<':
                                flag15 = false;
                                buf2 = "";
                                break;
                            case '>':
                                if (buf2 == "/p")
                                    buf1 = buf1 + " ";
                                flag15 = true;
                                break;
                            default:
                                if (flag15)
                                {
                                    buf1 = buf1 + ch;
                                }
                                else
                                {
                                    buf2 = buf2 + ch;
                                }
                                break;
                        }
                    }
                    titles.Add(new DataItem("title" + titleIdx, buf1));
                }
                if (curTag.Equals("</div>") || curTag.Equals("</cite>") || curTag.Equals("</poem>"))
                {
                    flag11 = true;
                }
                if (!curTag.Equals("<empty-line/>") && !curTag.Equals("<empty-line />"))
                {
                    prevTag = curTag;
                }
                index = bodyStr.IndexOf("<", index);
            }
            bodyStr = bodyStr.Replace("<text-author>", "<p class=\"text-author\">").Replace("</text-author>", "</p>").
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
            Console.WriteLine("(OK)");
            return element5;
        }

        private string UpdateImages(string allText)
        {
            var startIndex = allText.IndexOf("<image");
            var num12 = allText.Length - 1;
            while (startIndex > 0)
            {
                var imgSrc = "";
                var oldValue = "<image";
                var num42 = num12;
                char ch;
                for (var k = startIndex + 6; k <= num42; k++)
                {
                    ch = allText[k];
                    if (ch == '>')
                    {
                        oldValue = oldValue + ch;
                        break;
                    }
                    oldValue = oldValue + ch;
                }
                string newValue;
                if (imagesPrepared)
                {
                    var idx = oldValue.IndexOf("#");
                    if (idx != -1)
                    {
                        var length = oldValue.Length;
                        for (var i = idx + 1; i <= length; i++)
                        {
                            ch = oldValue[i];
                            if (ch == '\"')
                                break;
                            imgSrc = imgSrc + ch;
                        }
                    }
                    newValue = "<div class=\"image\"><img src=\"" + images + "/" + imgSrc + "\"/></div>";
                }
                else
                    newValue = " ";
                allText = allText.Replace(oldValue, newValue);
                startIndex = allText.IndexOf("<image", startIndex);
            }
            return allText;
        }
    }
}
