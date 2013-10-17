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
            try
            {
                var bookName = Path.GetFileNameWithoutExtension(bookPath);
                var book = Common.LoadBookWithoutNs(bookPath, bookName);
                if (book == null) return false;
                //create temp working folder
                _tempDir = Common.PrepareTempFolder(bookName, Common.ImagesFolderName, _workingFolder);
                if (_customFontsUsed && Directory.Exists(_workingFolder + @"\fonts"))
                {
                    Directory.CreateDirectory(_tempDir + @"\fonts");
                    Common.CopyDirectory(_workingFolder + @"\fonts", _tempDir + @"\fonts", true);
                }
                imagesPrepared = !_currentSettings.noImages && Common.ExtractImages(book, _tempDir, Common.ImagesFolderName);

                List<DataItem> notesList2;
                List<DataItem> titles;
                string bodyStr;
                bool notesCreated;
                int playOrder;

                var packElement = Common.GetEmptyPackage(book);
                Common.AddTitleToPackage(packElement);
                if (!_currentSettings.ntoc)
                    Common.AddDefaultToc(packElement);

                Common.UpdateImages(book, imagesPrepared);

                var imgSrc = Common.AttributeValue(book.Elements("description").Elements("title-info").Elements("coverpage").Elements("div").Elements("img"), "src");
                Common.AddCoverImage(packElement, imgSrc);

                ConvertToHtml(book, out playOrder, out notesList2, out bodyStr, out notesCreated, out titles);

                var ncxElement = new XElement("ncx");
                ncxElement.Add(new XElement("head", ""));
                ncxElement.Add(new XElement("docTitle", new XElement("text", "KF8")));
                ncxElement.Add(new XElement("navMap", ""));
                //if (!_currentSettings.nstitle)
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
                }


                var tocEl = Common.CreateEmptyToc();
                if (!_currentSettings.ntoc)
                    ncxElement.Elements("navMap").First().Add(tocEl);

                var htmlElement = new XElement("html");
                var linkEl = new XElement("link");
                linkEl.Add(new XAttribute("type", "text/css"));
                linkEl.Add(new XAttribute("href", "book.css"));
                linkEl.Add(new XAttribute("rel", "Stylesheet"));
                htmlElement.Add(new XElement("head", linkEl));
                htmlElement.Add(new XElement("body", ""));

                if (_currentSettings.nch)
                {
                    CreateSingleBook(bookName + ".html", titles, ncxElement, tocEl, packElement, bodyStr, htmlElement);
                }
                else
                {
                    playOrder = CreateChapters(bodyStr, htmlElement, packElement, titles, ncxElement, tocEl);
                }
                if (!_currentSettings.ntoc)
                {
                    var navPoint = new XElement("navPoint");
                    navPoint.Add(new XAttribute("id", "navpoint-" + (titles.Count + 1).ToString()));
                    navPoint.Add(new XAttribute("playOrder", (titles.Count + 1).ToString()));
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
                    navPoint.Add(new XAttribute("id", "content2"));
                    navPoint.Add(new XAttribute("media-type", "text/x-oeb1-document"));
                    navPoint.Add(new XAttribute("href", "toc.html"));
                    navPoint.Add("");
                    packElement.Elements("manifest").First().Add(navPoint);
                    navPoint = new XElement("itemref");
                    navPoint.Add(new XAttribute("idref", "content"));
                    packElement.Elements("spine").First().Add(navPoint);
                    navPoint = new XElement("reference");
                    navPoint.Add(new XAttribute("type", "toc"));
                    navPoint.Add(new XAttribute("title", "Содержание"));
                    navPoint.Add(new XAttribute("href", "toc.html"));
                    packElement.Elements("guide").First().Add(navPoint);
                }
                if (notesCreated)
                {
                    foreach (var item in notesList2)
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

                        itemEl = new XElement("navPoint");
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

                        if (!_currentSettings.ntoc)
                        {
                            itemEl = new XElement("li");
                            navLabel = new XElement("a");
                            navLabel.Add(new XAttribute("href", item.Name));
                            navLabel.Add(item.Value);
                            itemEl.Add(navLabel);
                            tocEl.Elements("body").First().Elements("ul").First().Add(itemEl);
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
                    tocEl.Save(_tempDir + @"\toc.html");
                    tocEl.RemoveAll();
                }
                var result = Common.CreateMobi(_tempDir, bookName, bookPath);
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
                ClearTempFolder();
            }
        }

        public void CreateSingleBook(string htmlFile, List<DataItem> titles, XElement ncxElement, XElement tocEl, XElement packElement, string bodyStr, XElement htmlElement)
        {
            var i = 0;
            while (i < titles.Count)
            {
                var navPoint = new XElement("navPoint");
                navPoint.Add(new XAttribute("id", "navpoint-" + (i + 2).ToString()));
                navPoint.Add(new XAttribute("playOrder", (i + 2).ToString()));
                var navLabel = new XElement("navLabel");
                var textEl = new XElement("text");
                textEl.Add(titles[i].Value);
                navLabel.Add(textEl);
                navPoint.Add(navLabel);
                navLabel = new XElement("content");
                navLabel.Add(new XAttribute("src", htmlFile + "#" + titles[i].Name));
                navPoint.Add(navLabel);
                ncxElement.Elements("navMap").First().Add(navPoint);
                if (!_currentSettings.ntoc)
                {
                    navPoint = new XElement("li");
                    navLabel = new XElement("a");
                    navLabel.Add(new XAttribute("href", htmlFile + "#" + titles[i].Name));
                    navLabel.Add(titles[i].Value);
                    navPoint.Add(navLabel);
                    tocEl.Elements("body").First().Elements("ul").First().Add(navPoint);
                }
                i++;
            }
            var itemEl = new XElement("item");
            itemEl.Add(new XAttribute("id", "text"));
            itemEl.Add(new XAttribute("media-type", "text/x-oeb1-document"));
            itemEl.Add(new XAttribute("href", htmlFile));
            itemEl.Add("");
            packElement.Elements("manifest").First().Add(itemEl);
            itemEl = new XElement("itemref");
            itemEl.Add(new XAttribute("idref", "text"));
            packElement.Elements("spine").First().Add(itemEl);
            itemEl = new XElement("reference");
            itemEl.Add(new XAttribute("type", "text"));
            itemEl.Add(new XAttribute("title", "Начало"));
            itemEl.Add(new XAttribute("href", htmlFile));
            packElement.Elements("guide").First().Add(itemEl);
            bodyStr = Common.TabRep(bodyStr);
            var htmlContent = htmlElement.ToString();
            htmlContent = htmlContent.Insert(htmlContent.IndexOf("<body>") + 6, bodyStr).Replace("<sectio1", "<div class=\"book\"").Replace("</sectio1>", "</div>");
            Common.SaveWithEncoding(_tempDir + @"\" + htmlFile, htmlContent);
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

        private int CreateChapters(string bodyStr, XElement element2, XElement packElement, List<DataItem> titles, XElement ncxElement, XElement tocEl)
        {
            Console.Write("Chapters creation...");
            var bookNum = 0;
            var startIdx = bodyStr.IndexOf("<sectio1", 2);
            var endIdx = bodyStr.IndexOf("</sectio1>");
            var start = 0;
            var str40 = "";
            var flag17 = false;
            var num9 = 2;
            while (endIdx != -1)
            {
                string bodyContent;
                bool noBookFlag;
                if ((startIdx < endIdx) & (startIdx != -1))
                {
                    if (!flag17)
                    {
                        bodyContent = bodyStr.Substring(start, startIdx - start) + "</sectio1>";
                        noBookFlag = !XElement.Parse(bodyContent).Elements("p").Any();
                        bodyContent = str40 + bodyContent;
                        str40 = "";
                        bodyContent = Common.TabRep(bodyContent);
                        Common.SaveElementToFile(element2.ToString(), bodyContent, noBookFlag, _tempDir, bookNum);
                        var itemEl = new XElement("item");
                        itemEl.Add(new XAttribute("id", "text" + bookNum));
                        itemEl.Add(new XAttribute("media-type", "text/x-oeb1-document"));
                        itemEl.Add(new XAttribute("href", "book" + bookNum + ".html"));
                        itemEl.Add("");
                        packElement.Elements("manifest").First().Add(itemEl);
                        itemEl = new XElement("itemref");
                        itemEl.Add(new XAttribute("idref", "text" + bookNum));
                        packElement.Elements("spine").First().Add(itemEl);
                        var i = 0;
                        while (i < titles.Count)
                        {
                            if (bodyContent.IndexOf(string.Format("id=\"{0}\"", titles[i].Name)) != -1)
                            {
                                itemEl = new XElement("navPoint");
                                itemEl.Add(new XAttribute("id", "navpoint-" + num9));
                                itemEl.Add(new XAttribute("playOrder", num9));
                                var element14 = new XElement("navLabel");
                                var element13 = new XElement("text");
                                element13.Add(titles[i].Value);
                                element14.Add(element13);
                                itemEl.Add(element14);
                                element14 = new XElement("content");
                                element14.Add(new XAttribute("src", string.Format("book{0}.html#{1}", bookNum, titles[i].Name)));
                                itemEl.Add(element14);
                                ncxElement.Elements("navMap").First().Add(itemEl);
                                if (!_currentSettings.ntoc)
                                {
                                    itemEl = new XElement("li");
                                    element14 = new XElement("a");
                                    element14.Add(new XAttribute("href", string.Format("book{0}.html#{1}", bookNum, titles[i].Name)));
                                    element14.Add(titles[i].Value);
                                    itemEl.Add(element14);
                                    tocEl.Elements("body").First().Elements("ul").First().Add(itemEl);
                                }
                                num9++;
                            }
                            i++;
                        }
                    }
                    start = startIdx;
                    startIdx = bodyStr.IndexOf("<sectio1", (startIdx + 1));
                    flag17 = false;
                }
                else
                {
                    if (!flag17)
                    {
                        bodyContent = bodyStr.Substring(start, (endIdx - start) + 11);
                        noBookFlag = !XElement.Parse(bodyContent).Elements("p").Any();
                        bodyContent = str40 + bodyContent;
                        str40 = "";
                        bodyContent = Common.TabRep(bodyContent);
                        Common.SaveElementToFile(element2.ToString(), bodyContent, noBookFlag, _tempDir, bookNum);

                        var itemEl = new XElement("item");
                        itemEl.Add(new XAttribute("id", "text" + bookNum));
                        itemEl.Add(new XAttribute("media-type", "text/x-oeb1-document"));
                        itemEl.Add(new XAttribute("href", "book" + bookNum + ".html"));
                        itemEl.Add("");
                        packElement.Elements("manifest").First().Add(itemEl);
                        itemEl = new XElement("itemref");
                        itemEl.Add(new XAttribute("idref", "text" + bookNum));
                        packElement.Elements("spine").First().Add(itemEl);
                        for (var i = 0; i < titles.Count; i++)
                        {
                            var str34 = titles[i].Value;
                            var str35 = titles[i].Name;
                            if (bodyContent.IndexOf("id=\"" + str35 + "\"") == -1) continue;
                            itemEl = new XElement("navPoint");
                            itemEl.Add(new XAttribute("id", "navpoint-" + num9));
                            itemEl.Add(new XAttribute("playOrder", num9));
                            var navLabel = new XElement("navLabel");
                            var textEl = new XElement("text");
                            textEl.Add(str34);
                            navLabel.Add(textEl);
                            itemEl.Add(navLabel);
                            navLabel = new XElement("content");
                            navLabel.Add(new XAttribute("src", "book" + bookNum + ".html#" + str35));
                            itemEl.Add(navLabel);
                            ncxElement.Elements("navMap").First().Add(itemEl);

                            if (!_currentSettings.ntoc)
                            {
                                itemEl = new XElement("li");
                                navLabel = new XElement("a");
                                navLabel.Add(new XAttribute("href", "book" + bookNum + ".html#" + str35));
                                navLabel.Add(str34);
                                itemEl.Add(navLabel);
                                tocEl.Elements("body").First().Elements("ul").First().Add(itemEl);
                            }
                            num9++;
                        }
                    }
                    flag17 = true;
                    start = endIdx;
                    endIdx = bodyStr.IndexOf("</sectio1>", (endIdx + 1));
                }
                if (!flag17)
                {
                    bookNum++;
                }
            }
            var referenceEl = new XElement("reference");
            referenceEl.Add(new XAttribute("type", "text"));
            referenceEl.Add(new XAttribute("title", "Начало"));
            referenceEl.Add(new XAttribute("href", "book0.html"));
            packElement.Elements("guide").First().Add(referenceEl);
            Console.WriteLine("(OK)");
            return num9;
        }

        public void ConvertToHtml(XElement book, out int playOrder, out List<DataItem> notesList2, out string bodyStr, out bool notesCreated, out List<DataItem> titles)
        {
            Console.Write("FB2 to HTML...");

            const string str = "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧЩШЬЪЫЭЮЯQWERTYUIOPASDFGHJKLZXCVBNM";

            playOrder = 0;
            Common.CreateTitlePage(book, _tempDir);

            var notesList = new List<DataItem>();
            notesList2 = new List<DataItem>();
            notesCreated = false;
            var bodies = new List<XElement>();
            bodies.AddRange(book.Elements("body"));
            if (bodies.Count() > 1)
            {
                for (var i = 1; i < bodies.Count; i++)
                {
                    var bodyName = (string)bodies[i].Attribute("name");
                    if (string.IsNullOrEmpty(bodyName)) continue;
                    notesList2.Add(new DataItem(bodyName + ".html", bodyName));
                    var list = bodies[i].Descendants("section").ToList();
                    if (list.Count > 0)
                        foreach (var t in list)
                            notesList.Add(new DataItem(bodyName + ".html", "#" + (string)t.Attribute("id")));
                    Common.CreateNoteBox(book, i, bodyName, _tempDir);
                    notesCreated = true;
                }
            }
            bodyStr = Common.UpdateATags(bodies[0], notesList);

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

            var titleIdx = 1;
            var prevTag = "";
            var specTag = false;
            var tagClosed = true;
            titles = new List<DataItem>();
            var index = bodyStr.IndexOf("<");
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
        }
    }
}
