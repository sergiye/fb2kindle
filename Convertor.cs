using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Fb2Kindle
{
    class Convertor
    {
        const string images = "images";
        private string _tempDir;
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
            //create temp working folder
            _tempDir = Common.PrepareTempFolder(bookName, images, _workingFolder);
            if (_customFontsUsed && Directory.Exists(_workingFolder + @"\fonts"))
            {
                Directory.CreateDirectory(_tempDir + @"\fonts");
                Common.CopyDirectory(_workingFolder + @"\fonts", _tempDir + @"\fonts", true);
            }
            var imagesPrepared = Common.ExtractImages(_workingFolder, _tempDir, images, bookPath);
            var fData = File.ReadAllText(bookPath);
            if (fData.Length == 0)
            {
                Console.WriteLine("Файл " + bookPath + " пустой или недоступный для чтения!");
                return false;
            }

            XElement hemlElement;
            XElement packElement;
            XElement ncxElement;
            List<DataItem> notesList2;
            List<DataItem> titles;
            string bodyStr;
            bool flag13;
            int index;
            int num9;
            var element5 = ConvertToHtml(bookPath, _currentSettings, fData, imagesPrepared, out num9, out hemlElement, out packElement, 
                out notesList2, out bodyStr, out flag13, out ncxElement, out titles, out index);
            var str17 = hemlElement.ToString();
            if (_currentSettings.nc != "True")
            {
                num9 = CreateChapters(_currentSettings, bodyStr, hemlElement, packElement, titles, ncxElement, element5, ref index);
            }
            else
            {
                var htmlFile = bookName + ".html";
                index = 0;
                while (index < titles.Count)
                {
                    var str34 = titles[index].Value;
                    var str35 = titles[index].Name;
                    var navPoint = new XElement("navPoint");
                    navPoint.Add(XHelper.CreateAttribute("id", "navpoint-" + (index + 2).ToString()));
                    navPoint.Add(XHelper.CreateAttribute("playOrder", (index + 2).ToString()));
                    var navLabel = new XElement("navLabel");
                    var textEl = new XElement("text");
                    textEl.Add(str34);
                    navLabel.Add(textEl);
                    navPoint.Add(navLabel);
                    navLabel = new XElement("content");
                    navLabel.Add(XHelper.CreateAttribute("src", htmlFile + "#" + str35));
                    navPoint.Add(navLabel);
                    XHelper.First(ncxElement.Elements("navMap")).Add(navPoint);
                    if (_currentSettings.ntoc != "True")
                    {
                        navPoint = new XElement("li");
                        navLabel = new XElement("a");
                        navLabel.Add(XHelper.CreateAttribute("href", htmlFile + "#" + str35));
                        navLabel.Add(str34);
                        navPoint.Add(navLabel);
                        XHelper.First(XHelper.First(element5.Elements("body")).Elements("ul")).Add(navPoint);
                    }
                    index++;
                }
                var itemEl = new XElement("item");
                itemEl.Add(new XAttribute("id", "text"));
                itemEl.Add(new XAttribute("media-type", "text/x-oeb1-document"));
                itemEl.Add(XHelper.CreateAttribute("href", htmlFile));
                itemEl.Add("");
                XHelper.First(packElement.Elements("manifest")).Add(itemEl);
                itemEl = new XElement("itemref");
                itemEl.Add(new XAttribute("idref", "text"));
                XHelper.First(packElement.Elements("spine")).Add(itemEl);
                itemEl = new XElement("reference");
                itemEl.Add(new XAttribute("type", "text"));
                itemEl.Add(new XAttribute("title", "Book"));
                itemEl.Add(XHelper.CreateAttribute("href", htmlFile));
                XHelper.First(packElement.Elements("guide")).Add(itemEl);
                bodyStr = Common.TabRep(bodyStr);
                if (_currentSettings.nh != "True")
                {
                    bodyStr = Common.GipherHTML(bodyStr);
                }
                str17 = str17.Insert(str17.IndexOf("<body>") + 6, bodyStr).Replace("<sectio1", "<div class=\"book\"").Replace("</sectio1>", "</div>");
                File.WriteAllText(_tempDir + @"\" + htmlFile, str17);
            }
            if (_currentSettings.ntoc != "True" && _currentSettings.ContentOf == "True")
            {
                var navPoint = new XElement("navPoint");
                navPoint.Add(XHelper.CreateAttribute("id", "navpoint-" + (index + 2).ToString()));
                navPoint.Add(XHelper.CreateAttribute("playOrder", (index + 2).ToString()));
                var navLabel = new XElement("navLabel");
                var textEl = new XElement("text");
                textEl.Add("Contents");
                navLabel.Add(textEl);
                navPoint.Add(navLabel);
                navLabel = new XElement("content");
                navLabel.Add(new XAttribute("src", "toc.html#toc"));
                navPoint.Add(navLabel);
                XHelper.First(ncxElement.Elements("navMap")).Add(navPoint);
                navPoint = new XElement("item");
                navPoint.Add(new XAttribute("id", "content"));
                navPoint.Add(new XAttribute("media-type", "text/x-oeb1-document"));
                navPoint.Add(new XAttribute("href", "toc.html"));
                navPoint.Add("");
                XHelper.First(packElement.Elements("manifest")).Add(navPoint);
                navPoint = new XElement("itemref");
                navPoint.Add(new XAttribute("idref", "content"));
                XHelper.First(packElement.Elements("spine")).Add(navPoint);
                navPoint = new XElement("reference");
                navPoint.Add(new XAttribute("type", "toc"));
                navPoint.Add(new XAttribute("title", "toc"));
                navPoint.Add(new XAttribute("href", "toc.html"));
                XHelper.First(packElement.Elements("guide")).Add(navPoint);
            }
            if (flag13)
            {
                foreach (var item in notesList2)
                {
                    var itemEl = new XElement("item");
                    itemEl.Add(XHelper.CreateAttribute("id", item.Value));
                    itemEl.Add(new XAttribute("media-type", "text/x-oeb1-document"));
                    itemEl.Add(XHelper.CreateAttribute("href", item.Name));
                    itemEl.Add("");
                    XHelper.First(packElement.Elements("manifest")).Add(itemEl);
                    itemEl = new XElement("itemref");
                    itemEl.Add(XHelper.CreateAttribute("idref", item.Value));
                    XHelper.First(packElement.Elements("spine")).Add(itemEl);
                    itemEl = new XElement("navPoint");
                    itemEl.Add(XHelper.CreateAttribute("id", "navpoint-" + item.Value));
                    itemEl.Add(XHelper.CreateAttribute("playOrder", num9));
                    var navLabel = new XElement("navLabel");
                    var textEl = new XElement("text");
                    textEl.Add(item.Value);
                    navLabel.Add(textEl);
                    itemEl.Add(navLabel);
                    navLabel = new XElement("content");
                    navLabel.Add(XHelper.CreateAttribute("src", item.Name));
                    itemEl.Add(navLabel);
                    XHelper.First(ncxElement.Elements("navMap")).Add(itemEl);
                    if (_currentSettings.ntoc != "True")
                    {
                        itemEl = new XElement("li");
                        navLabel = new XElement("a");
                        navLabel.Add(XHelper.CreateAttribute("href", item.Name));
                        navLabel.Add(item.Value);
                        itemEl.Add(navLabel);
                        XHelper.First(XHelper.First(element5.Elements("body")).Elements("ul")).Add(itemEl);
                    }
                    num9++;
                }
            }
            File.WriteAllText(_tempDir + @"\book.css", _defaultCSS);
            packElement.Save(_tempDir + @"\" + bookName + ".opf");
            packElement.RemoveAll();
            ncxElement.Save(_tempDir + @"\toc.ncx");
            ncxElement.RemoveAll();
            if (_currentSettings.ntoc != "True")
            {
                element5.Save(_tempDir + @"\toc.html");
                element5.RemoveAll();
            }
            var parentPath = Path.GetDirectoryName(bookPath);
            if (string.IsNullOrEmpty(parentPath))
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
                Console.WriteLine("Ошибка очистки временной папки: " + ex.Message);
            }
            return result;
        }

        private int CreateChapters(DefaultOptions currentSettings, string bodyStr, XElement element2, XElement element3, List<DataItem> titles, XElement ncxElement, XElement element5, ref int index)
        {
            Console.Write("Разбиваем на главы...");
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
                        if (currentSettings.DelZeroTitle == "True" && (bookNum == 0))
                        {
                            flag18 = true;
                        }
                        else if (!XElement.Parse(bodyContent).Elements("p").Any())
                        {
                            if (currentSettings.ntitle0 == "True")
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
                            if (currentSettings.nh != "True")
                            {
                                bodyContent = Common.GipherHTML(bodyContent);
                            }
                            Common.SaveElementToFile(element2.ToString(), bodyContent, noBookFlag, _tempDir, bookNum);
                            var itemEl = new XElement("item");
                            itemEl.Add(XHelper.CreateAttribute("id", "text" + bookNum));
                            itemEl.Add(new XAttribute("media-type", "text/x-oeb1-document"));
                            itemEl.Add(XHelper.CreateAttribute("href", "book" + bookNum + ".html"));
                            itemEl.Add("");
                            XHelper.First(element3.Elements("manifest")).Add(itemEl);
                            itemEl = new XElement("itemref");
                            itemEl.Add(XHelper.CreateAttribute("idref", "text" + bookNum));
                            XHelper.First(element3.Elements("spine")).Add(itemEl);
                            index = 0;
                            while (index < titles.Count)
                            {
                                var str34 = titles[index].Value;
                                var str35 = titles[index].Name;
                                if (bodyContent.IndexOf("id=\"" + str35 + "\"") != -1)
                                {
                                    itemEl = new XElement("navPoint");
                                    itemEl.Add(XHelper.CreateAttribute("id", "navpoint-" + num9));
                                    itemEl.Add(XHelper.CreateAttribute("playOrder", num9));
                                    var element14 = new XElement("navLabel");
                                    var element13 = new XElement("text");
                                    element13.Add(str34);
                                    element14.Add(element13);
                                    itemEl.Add(element14);
                                    element14 = new XElement("content");
                                    element14.Add(XHelper.CreateAttribute("src", "book" + bookNum + ".html#" + str35));
                                    itemEl.Add(element14);
                                    XHelper.First(ncxElement.Elements("navMap")).Add(itemEl);
                                    if (currentSettings.ntoc != "True")
                                    {
                                        itemEl = new XElement("li");
                                        element14 = new XElement("a");
                                        element14.Add(XHelper.CreateAttribute("href", "book" + bookNum + ".html#" + str35));
                                        element14.Add(str34);
                                        itemEl.Add(element14);
                                        XHelper.First(XHelper.First(element5.Elements("body")).Elements("ul")).Add(itemEl);
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
                        if (currentSettings.DelZeroTitle == "True" && (bookNum == 0))
                        {
                            flag18 = true;
                        }
                        else if (!XElement.Parse(bodyContent).Elements("p").Any())
                        {
                            if (currentSettings.ntitle0 == "True")
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
                            if (currentSettings.nh != "True")
                            {
                                bodyContent = Common.GipherHTML(bodyContent);
                            }
                            Common.SaveElementToFile(element2.ToString(), bodyContent, noBookFlag, _tempDir, bookNum);

                            var itemEl = new XElement("item");
                            itemEl.Add(XHelper.CreateAttribute("id", "text" + bookNum));
                            itemEl.Add(new XAttribute("media-type", "text/x-oeb1-document"));
                            itemEl.Add(XHelper.CreateAttribute("href", "book" + bookNum + ".html"));
                            itemEl.Add("");
                            XHelper.First(element3.Elements("manifest")).Add(itemEl);
                            itemEl = new XElement("itemref");
                            itemEl.Add(XHelper.CreateAttribute("idref", "text" + bookNum));
                            XHelper.First(element3.Elements("spine")).Add(itemEl);
                            for (index = 0; index < titles.Count; index++)
                            {
                                var str34 = titles[index].Value;
                                var str35 = titles[index].Name;
                                if (bodyContent.IndexOf("id=\"" + str35 + "\"") != -1)
                                {
                                    itemEl = new XElement("navPoint");
                                    itemEl.Add(XHelper.CreateAttribute("id", "navpoint-" + num9));
                                    itemEl.Add(XHelper.CreateAttribute("playOrder", num9));
                                    var navLabel = new XElement("navLabel");
                                    var textEl = new XElement("text");
                                    textEl.Add(str34);
                                    navLabel.Add(textEl);
                                    itemEl.Add(navLabel);
                                    navLabel = new XElement("content");
                                    navLabel.Add(XHelper.CreateAttribute("src", "book" + bookNum + ".html#" + str35));
                                    itemEl.Add(navLabel);
                                    XHelper.First(ncxElement.Elements("navMap")).Add(itemEl);

                                    if (currentSettings.ntoc != "True")
                                    {
                                        itemEl = new XElement("li");
                                        navLabel = new XElement("a");
                                        navLabel.Add(XHelper.CreateAttribute("href", "book" + bookNum + ".html#" + str35));
                                        navLabel.Add(str34);
                                        itemEl.Add(navLabel);
                                        XHelper.First(XHelper.First(element5.Elements("body")).Elements("ul")).Add(itemEl);
                                    }
                                    num9++;
                                }
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
            referenceEl.Add(XHelper.CreateAttribute("href", "book0.html"));
            XHelper.First(element3.Elements("guide")).Add(referenceEl);
            Console.WriteLine("(Ok)");
            return num9;
        }

        public XElement ConvertToHtml(string bookPath, DefaultOptions currentSettings, string fData, bool imagesPrepared, out int num9, out XElement hemlElement, out XElement packElement, out List<DataItem> notesList2, out string bodyStr, out bool flag13, out XElement ncxElement, out List<DataItem> titles, out int index)
        {
            var allText = File.ReadAllText(bookPath, fData.ToUpper().IndexOf("UTF-8") > 0 ? Encoding.UTF8 : Encoding.Default);
            Console.Write("FB2 to HTML...");
            allText = allText.Replace("xmlns=\"http://www.gribuser.ru/xml/fictionbook/2.0\"", "").
                              Replace("<title-info", "<t-i").Replace("</title-info>", "</t-i>");
            var startIndex = allText.IndexOf("<image");
            string oldValue;
            var num12 = allText.Length - 1;
            char ch;
            while (startIndex > 0)
            {
                var imgSrc = "";
                oldValue = "<image";
                var num42 = num12;
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
                if (!imagesPrepared)
                {
                    newValue = " ";
                }
                else
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
                allText = allText.Replace(oldValue, newValue);
                startIndex = allText.IndexOf("<image", startIndex);
            }

            var element = XElement.Parse(allText);
            var element5 = element;
            const string str = "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧЩШЬЪЫЭЮЯQWERTYUIOPASDFGHJKLZXCVBNM";

            num9 = 0;
            var num = element.Elements("body").Count();
            XElement content;
            XElement headEl;
            XElement linkEl;
            if (!currentSettings.nstitleb)
            {
                content = new XElement("html");
                //content = new XElement("html");
                headEl = new XElement("head");
                linkEl = new XElement("link");
                linkEl.Add(new XAttribute("type", "text/css"));
                linkEl.Add(new XAttribute("href", "book.css"));
                linkEl.Add(new XAttribute("rel", "Stylesheet"));
                headEl.Add(linkEl);
                content.Add(headEl);
                headEl = new XElement("body");
                linkEl = new XElement("div");
                linkEl.Add(new XAttribute("class", "supertitle"));
                linkEl.Add(new XAttribute("align", "center"));
                linkEl.Add(new XAttribute("id", "booktitle"));
                linkEl.Add(element.Elements("description").Elements("t-i").Elements("author").Select(AddAuthorInfo));
                var pEl = new XElement("p");
                pEl.Add(XHelper.AttributeValue(element.Elements("description").Elements("t-i").Elements("sequence"), "name") + " " + XHelper.AttributeValue(element.Elements("description").Elements("t-i").Elements("sequence"), "number"));
                linkEl.Add(pEl);
                pEl = new XElement("br");
                linkEl.Add(pEl);
                pEl = new XElement("p");
                pEl.Add(new XAttribute("class", "text-name"));
                pEl.Add(XHelper.Value(element.Elements("description").Elements("t-i").Elements("book-title")));
                linkEl.Add(pEl);
                pEl = new XElement("br");
                linkEl.Add(pEl);
                pEl = new XElement("p");
                pEl.Add(XHelper.Value(element.Elements("description").Elements("publish-info").Elements("publisher")));
                linkEl.Add(pEl);
                pEl = new XElement("p");
                pEl.Add(XHelper.Value(element.Elements("description").Elements("publish-info").Elements("city")));
                linkEl.Add(pEl);
                pEl = new XElement("p");
                pEl.Add(XHelper.Value(element.Elements("description").Elements("publish-info").Elements("year")));
                linkEl.Add(pEl);
                pEl = new XElement("br");
                linkEl.Add(pEl);
                headEl.Add(linkEl);
                content.Add(headEl);
                content.Save(_tempDir + @"\booktitle.html");
            }
            hemlElement = new XElement("html");
            linkEl = new XElement("link");
            linkEl.Add(new XAttribute("type", "text/css"));
            linkEl.Add(new XAttribute("href", "book.css"));
            linkEl.Add(new XAttribute("rel", "Stylesheet"));
            hemlElement.Add(new XElement("head", linkEl));
            hemlElement.Add(new XElement("body", ""));
            var str20 = XHelper.Value(XHelper.First(XHelper.First(element.Elements("description")).Elements("t-i")).Elements("lang"));
            if (string.IsNullOrEmpty(str20))
                str20 = "ru";
            var packEl = new XElement("package");
            linkEl = new XElement("meta");
            linkEl.Add(new XAttribute("name", "zero-gutter"));
            linkEl.Add(new XAttribute("content", "true"));
            headEl = new XElement("metadata");
            headEl.Add(linkEl);
            linkEl = new XElement("meta");
            linkEl.Add(new XAttribute("name", "zero-margin"));
            linkEl.Add(new XAttribute("content", "true"));
            headEl.Add(linkEl);
            linkEl = new XElement("dc-metadata");
            linkEl.Add(new XAttribute(XNamespace.Get("http://www.w3.org/2000/xmlns/").GetName("dc"), "http://"));

            var nsHttp = XNamespace.Get("http://");
            content = new XElement(nsHttp.GetName("Title"));
            content.Add(XHelper.Value(element.Elements("description").Elements("t-i").Elements("book-title")));
            linkEl.Add(content);
            content = new XElement(nsHttp.GetName("Language"));
            content.Add(str20);
            linkEl.Add(content);
            content = new XElement(nsHttp.GetName("Creator"));
            content.Add(XHelper.Value(XHelper.First(element.Elements("description").Elements("t-i").Elements("author")).Elements("last-name")) + " " + XHelper.Value(XHelper.First(element.Elements("description").Elements("t-i").Elements("author")).Elements("first-name")) + " " + XHelper.Value(XHelper.First(element.Elements("description").Elements("t-i").Elements("author")).Elements("middle-name")));
            linkEl.Add(content);
            content = new XElement(nsHttp.GetName("Publisher"));
            content.Add(XHelper.Value(element.Elements("description").Elements("publish-info").Elements("publisher")));
            linkEl.Add(content);
            content = new XElement(nsHttp.GetName("date"));
            content.Add(XHelper.Value(element.Elements("description").Elements("publish-info").Elements("year")));
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
            if (!currentSettings.nstitleb)
            {
                packEl = new XElement("item");
                packEl.Add(new XAttribute("id", "booktitle"));
                packEl.Add(new XAttribute("media-type", "text/x-oeb1-document"));
                packEl.Add(new XAttribute("href", "booktitle.html"));
                packEl.Add("");
                XHelper.First(packElement.Elements("manifest")).Add(packEl);
                packEl = new XElement("itemref");
                packEl.Add(new XAttribute("idref", "booktitle"));
                XHelper.First(packElement.Elements("spine")).Add(packEl);
                packEl = new XElement("reference");
                packEl.Add(new XAttribute("type", "start"));
                packEl.Add(new XAttribute("title", "Book"));
                packEl.Add(new XAttribute("href", "booktitle.html"));
                XHelper.First(packElement.Elements("guide")).Add(packEl);
            }
            if ((currentSettings.ntoc != "True") && (currentSettings.ContentOf != "True"))
            {
                packEl = new XElement("item");
                packEl.Add(new XAttribute("id", "content"));
                packEl.Add(new XAttribute("media-type", "text/x-oeb1-document"));
                packEl.Add(new XAttribute("href", "toc.html"));
                packEl.Add("");
                XHelper.First(packElement.Elements("manifest")).Add(packEl);
                packEl = new XElement("itemref");
                packEl.Add(new XAttribute("idref", "content"));
                XHelper.First(packElement.Elements("spine")).Add(packEl);
                packEl = new XElement("reference");
                packEl.Add(new XAttribute("type", "toc"));
                packEl.Add(new XAttribute("title", "toc"));
                packEl.Add(new XAttribute("href", "toc.html"));
                XHelper.First(packElement.Elements("guide")).Add(packEl);
            }
            var str3 = XHelper.AttributeValue(element.Elements("description").Elements("t-i").Elements("coverpage").Elements("div").Elements("img"), "src");
            if (str3 != "")
            {
                packEl = new XElement("EmbeddedCover");
                packEl.Add(str3);
                XHelper.First(XHelper.First(XHelper.First(packElement.Elements("metadata")).Elements("dc-metadata")).Elements("x-metadata")).Add(packEl);
            }
            var notesList = new List<DataItem>();
            notesList2 = new List<DataItem>();
            bodyStr = XHelper.First(element.Elements("body")).ToString();
            flag13 = false;
            if (num > 1)
            {
                for (var i = 1; i < num; i++)
                {
                    var str21 = XHelper.AttributeValue(element.Elements("body").ElementAtOrDefault(i), "name");
                    if (str21 == "") continue;
                    notesList2.Add(new DataItem(str21 + ".html", str21));
                    var list = element.Elements("body").ElementAtOrDefault(i).Descendants("section").ToList();
                    if (list.Count > 0)
                    {
                        for (var idx = 0; idx < list.Count; idx++)
                        {
                            var di = new DataItem();
                            if (currentSettings.NoteBoxb)
                            {
                                var list2 = list[idx].Descendants("p").ToList();
                                var str36 = "<b>";
                                for (var idx2 = 0; idx2 < list2.Count; idx2++)
                                {
                                    if (idx2 == 0)
                                    {
                                        str36 = str36 + list2[idx2].Value + "</b> ";
                                    }
                                    else
                                    {
                                        var str37 = list2[idx2].Value.Replace('<', '[').Replace('>', ']');
                                        str36 = str36 + " " + str37;
                                    }
                                }
                                str36 = str36.Replace("&", "");
                                di.Name = str36;
                            }
                            else
                            {
                                di.Name = str21 + ".html";
                            }
                            di.Value = XHelper.AttributeValue(list[idx], "id");
                            notesList.Add(di);
                        }
                    }
                    if (currentSettings.NoteBoxb) continue;
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
                    var element9 = packEl;
                    var htmltxt = Common.FormatToHTML(element9.ToString());
                    if (currentSettings.nh != "True")
                    {
                        htmltxt = Common.GipherHTML(htmltxt);
                    }
                    htmltxt = Common.AddEncodingToXml(htmltxt);
                    File.WriteAllText(_tempDir + @"\" + str21 + ".html", htmltxt);
                    flag13 = true;
                }
            }
            bodyStr = bodyStr.Replace("<a ", "<A ").Replace("</a>", "</A> ");
            var str23 = "";
            startIndex = bodyStr.IndexOf("<A ");
            var num15 = bodyStr.IndexOf("</A>");
            var num18 = 1;
            while (startIndex > -1)
            {
                num12 = bodyStr.Length - 1;
                bodyStr = bodyStr.Insert(startIndex + 1, "!");
                oldValue = "<!A ";
                var num47 = num12;
                for (var num30 = startIndex + 4; num30 <= num47; num30++)
                {
                    ch = bodyStr[num30];
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
                        ch = oldValue[num31];
                        if (ch == '\"')
                            break;
                        str22 = str22 + ch;
                    }
                    str23 = "";
                    for (var i = 0; i < notesList.Count; i++)
                    {
                        if (str22 == notesList[i].Value)
                        {
                            if (currentSettings.NoteBoxb)
                                str23 = Common.FormatToHTML(notesList[i].Name);
                            else
                                str23 = notesList[i].Name + "#" + str22;
                            break;
                        }
                    }
                }
                bodyStr = currentSettings.NoteBoxb
                              ? bodyStr.Insert(num15 + 5, "<span class=\"note\">" + str23 + "</span>").Replace(oldValue, "<sup>")
                              : bodyStr.Replace(oldValue, "<a href = \"" + str23 + "\"><sup>");
                num18++;
                startIndex = bodyStr.IndexOf("<A ", startIndex);
                if (startIndex != -1)
                    num15 = bodyStr.IndexOf("</A>", startIndex);
            }
            bodyStr = bodyStr.Replace("</A>", currentSettings.NoteBoxb ? "</sup>" : "</sup></a>");
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
                {
                    number = 9;
                }
                if (number < 0)
                {
                    number = 0;
                }
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
            if (!currentSettings.nstitleb)
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
                XHelper.First(ncxElement.Elements("navMap")).Add(packEl);
            }
            if (currentSettings.ntoc != "True" && currentSettings.ContentOf != "True")
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
                XHelper.First(ncxElement.Elements("navMap")).Add(packEl);
            }
            var num6 = 1;
            if (currentSettings.ntoc != "True")
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
            string str34;
            string str35;
            var prevTag = "";
            var flag11 = false;
            var flag15 = true;
            titles = new List<DataItem>();
            index = bodyStr.IndexOf("<");
            while (index > -1)
            {
                num9 = index;
                ch = bodyStr[index];
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
                    if (currentSettings.nb != "True")
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
                    num6++;
                    bodyStr = bodyStr.Insert(num9 + 6, " id=\"title" + num6 + "\"");
                    index = bodyStr.IndexOf(">", index);
                    num16 = bodyStr.IndexOf("</titl", index);
                    str35 = bodyStr.Substring(index + 1, (num16 - index) - 1);
                    str34 = "";
                    var str33 = "";
                    var num51 = str35.Length - 1;
                    for (var num34 = 0; num34 <= num51; num34++)
                    {
                        ch = str35[num34];
                        switch (ch)
                        {
                            case '<':
                                flag15 = false;
                                str33 = "";
                                break;
                            case '>':
                                if (str33 == "/p")
                                    str34 = str34 + " ";
                                flag15 = true;
                                break;
                            default:
                                if (flag15)
                                {
                                    str34 = str34 + ch;
                                }
                                else
                                {
                                    str33 = str33 + ch;
                                }
                                break;
                        }
                    }
                    titles.Add(new DataItem("title" + num6, str34));
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
            Console.WriteLine("(Ok)");
            return element5;
        }

        public static XElement AddAuthorInfo(XElement avtorbook)
        {
            var element2 = new XElement("h2");
            element2.Add(XHelper.Value(avtorbook.Elements("last-name")));
            element2.Add(new XElement("br"));
            element2.Add(XHelper.Value(avtorbook.Elements("first-name")));
            element2.Add(new XElement("br"));
            element2.Add(XHelper.Value(avtorbook.Elements("middle-name")));
            element2.Add(new XElement("br"));
            return element2;
        }
    }
}
