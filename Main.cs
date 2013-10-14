using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Windows.Forms;
using System.Collections.Generic;

namespace Fb2Kindle
{
    class Module1
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Common.ShowMainInfo();
            if (args.Length == 0)
            {
                Common.ShowHelpText();
                return;
            }

            var executingPath = Path.GetDirectoryName(Application.ExecutablePath);
            var currentSettings = XmlSerializerHelper.ReadObjectFromFile<DefaultOptions>(executingPath + @"\fb2kf8.set") ?? new DefaultOptions();
            var defaultCSS = Common.GetScriptFromResource("defstyles.css"); 
            if (File.Exists(currentSettings.defaultCSS))
                defaultCSS = File.ReadAllText(currentSettings.defaultCSS, Encoding.UTF8);

            var bookPath = string.Empty;
            for (var j = 0; j < args.Length; j++)
            {
                switch (args[j].ToLower().Trim())
                {
                    case "-css":
                        if (args.Length > (j + 1))
                        {
                            if (File.Exists(args[j + 1]))
                                defaultCSS = File.ReadAllText(args[j + 1], Encoding.UTF8);
                            else
                            {
                                Console.Write("(Err) Не найден файл стилей: " + args[j + 1]);
                                Console.WriteLine();
                            }
                            j++;
                        }
                        break;
                    case "-d":
                        currentSettings.d = "True";
                        break;
                    case "-nb":
                        currentSettings.nb = "True";
                        break;
                    case "-nch":
                        currentSettings.nc = "True";
                        break;
                    case "-nh":
                        currentSettings.nh = "True";
                        break;
                    default:
                        if (j == 0)
                            bookPath = args[j];
                        break;
                }
            }
            if (string.IsNullOrEmpty(defaultCSS))
            {
                Console.Write("Пустой файл стилей: " + currentSettings.defaultCSS);
                Console.WriteLine();
                return;
            }
            if (string.IsNullOrEmpty(bookPath) || !File.Exists(bookPath))
            {
                Console.Write("Файл не найден: " + bookPath);
                Console.WriteLine();
                return;
            }

            var bookName = Path.GetFileNameWithoutExtension(bookPath);
            var htmlFile = bookName + ".html";
            var parentPath = Path.GetDirectoryName(bookPath);
            if (string.IsNullOrEmpty(parentPath))
            {
                bookPath = Path.Combine(executingPath, bookPath);
                parentPath = executingPath;
            }
            Console.WriteLine("Processing: " + bookName);

            //create temp working folder
            const string images = "images";
            var tempDir = Common.PrepareTempFolder(bookName, images, executingPath);
            var imagesPrepared = Common.ExtractImages(executingPath, tempDir, images, bookPath);
            var fData = File.ReadAllText(bookPath);
            if (fData.Length == 0)
            {
                Console.Write("Файл " + bookPath + " не может быть обработан!");
                Console.WriteLine();
                return;
            }
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
                var str24 = "";
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
                if (imagesPrepared)
                {
                    var num24 = oldValue.IndexOf("#");
                    if (num24 != -1)
                    {
                        var length = oldValue.Length;
                        for (var m = num24 + 1; m <= length; m++)
                        {
                            ch = oldValue[m];
                            if (ch == '\"')
                            {
                                break;
                            }
                            str24 = str24 + ch;
                        }
                    }
                    newValue = "<div class=\"image\"><img src=\"" + images + "/" + str24 + "\"/></div>";
                }
                else
                {
                    newValue = " ";
                }
                allText = allText.Replace(oldValue, newValue);
                startIndex = allText.IndexOf("<image", startIndex);
            }

            var element = XElement.Parse(allText);
            var element5 = element;
            const string str = "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧЩШЬЪЫЭЮЯQWERTYUIOPASDFGHJKLZXCVBNM";

            var name43 = XNamespace.Get("http://www.w3.org/2000/xmlns/").GetName("dc");
            var namespace4 = XNamespace.Get("http://");
            var name44 = namespace4.GetName("Title");
            var name45 = namespace4.GetName("Language");
            var name46 = namespace4.GetName("Creator");
            var name47 = namespace4.GetName("Publisher");
            var name48 = namespace4.GetName("date");

            var num9 = 0;
            XElement element12;
            XElement element13;
            XElement element14;
            XElement element15;
            XElement content;
            
            var num = element.Elements("body").Count();
            if (!currentSettings.nstitleb)
            {
                content = new XElement("html");
                //content = new XElement("html");
                element12 = new XElement("head");
                element13 = new XElement("link");
                element13.Add(new XAttribute("type", "text/css"));
                element13.Add(new XAttribute("href", "book.css"));
                element13.Add(new XAttribute("rel", "Stylesheet"));
                element12.Add(element13);
                content.Add(element12);
                element12 = new XElement("body");
                element13 = new XElement("div");
                element13.Add(new XAttribute("class", "supertitle"));
                element13.Add(new XAttribute("align", "center"));
                element13.Add(new XAttribute("id", "booktitle"));
                element13.Add(element.Elements("description").Elements("t-i").Elements("author").Select(AddAuthorInfo));
                element14 = new XElement("p");
                element14.Add(XHelper.get_AttributeValue(element.Elements("description").Elements("t-i").Elements("sequence"), "name") + " " + XHelper.get_AttributeValue(element.Elements("description").Elements("t-i").Elements("sequence"), "number"));
                element13.Add(element14);
                element14 = new XElement("br");
                element13.Add(element14);
                element14 = new XElement("p");
                element14.Add(new XAttribute("class", "text-name"));
                element14.Add(XHelper.get_Value(element.Elements("description").Elements("t-i").Elements("book-title")));
                element13.Add(element14);
                element14 = new XElement("br");
                element13.Add(element14);
                element14 = new XElement("p");
                element14.Add(XHelper.get_Value(element.Elements("description").Elements("publish-info").Elements("publisher")));
                element13.Add(element14);
                element14 = new XElement("p");
                element14.Add(XHelper.get_Value(element.Elements("description").Elements("publish-info").Elements("city")));
                element13.Add(element14);
                element14 = new XElement("p");
                element14.Add(XHelper.get_Value(element.Elements("description").Elements("publish-info").Elements("year")));
                element13.Add(element14);
                element14 = new XElement("br");
                element13.Add(element14);
                element12.Add(element13);
                content.Add(element12);
                content.Save(tempDir + @"\booktitle.html");
            }
            var element2 = new XElement("html");
            element13 = new XElement("head");
            element12 = new XElement("link");
            element12.Add(new XAttribute("type", "text/css"));
            element12.Add(new XAttribute("href", "book.css"));
            element12.Add(new XAttribute("rel", "Stylesheet"));
            element13.Add(element12);
            element2.Add(element13);
            element13 = new XElement("body");
            element13.Add("");
            element2.Add(element13);
            var str20 = XHelper.get_Value(element.Elements("description").ElementAtOrDefault(0).Elements("t-i").ElementAtOrDefault(0).Elements("lang"));
            if (str20 == "")
            {
                str20 = "ru";
            }
            element14 = new XElement("package");
            element13 = new XElement("metadata");
            element12 = new XElement("meta");
            element12.Add(new XAttribute("name", "zero-gutter"));
            element12.Add(new XAttribute("content", "true"));
            element13.Add(element12);
            element12 = new XElement("meta");
            element12.Add(new XAttribute("name", "zero-margin"));
            element12.Add(new XAttribute("content", "true"));
            element13.Add(element12);
            element12 = new XElement("dc-metadata");
            element12.Add(new XAttribute(name43, "http://"));
            content = new XElement(name44);
            content.Add(XHelper.get_Value(element.Elements("description").Elements("t-i").Elements("book-title")));
            element12.Add(content);
            content = new XElement(name45);
            content.Add(str20);
            element12.Add(content);
            content = new XElement(name46);
            content.Add(XHelper.get_Value(element.Elements("description").Elements("t-i").Elements("author").ElementAtOrDefault(0).Elements("last-name")) + " " + XHelper.get_Value(element.Elements("description").Elements("t-i").Elements("author").ElementAtOrDefault(0).Elements("first-name")) + " " + XHelper.get_Value(element.Elements("description").Elements("t-i").Elements("author").ElementAtOrDefault(0).Elements("middle-name")));
            element12.Add(content);
            content = new XElement(name47);
            content.Add(XHelper.get_Value(element.Elements("description").Elements("publish-info").Elements("publisher")));
            element12.Add(content);
            content = new XElement(name48);
            content.Add(XHelper.get_Value(element.Elements("description").Elements("publish-info").Elements("year")));
            element12.Add(content);
            content = new XElement("x-metadata");
            content.Add("");
            element12.Add(content);
            element13.Add(element12);
            element14.Add(element13);
            element13 = new XElement("manifest");
            element12 = new XElement("item");
            element12.Add(new XAttribute("id", "ncx"));
            element12.Add(new XAttribute("media-type", "application/x-dtbncx+xml"));
            element12.Add(new XAttribute("href", "toc.ncx"));
            element13.Add(element12);
            element14.Add(element13);
            element13 = new XElement("spine");
            element13.Add(new XAttribute("toc", "ncx"));
            element13.Add("");
            element14.Add(element13);
            element13 = new XElement("guide");
            element13.Add("");
            element14.Add(element13);
            var element3 = element14;
            if (!currentSettings.nstitleb)
            {
                element14 = new XElement("item");
                element14.Add(new XAttribute("id", "booktitle"));
                element14.Add(new XAttribute("media-type", "text/x-oeb1-document"));
                element14.Add(new XAttribute("href", "booktitle.html"));
                element14.Add("");
                element3.Elements("manifest").ElementAtOrDefault(0).Add(element14);
                element14 = new XElement("itemref");
                element14.Add(new XAttribute("idref", "booktitle"));
                element3.Elements("spine").ElementAtOrDefault(0).Add(element14);
                element14 = new XElement("reference");
                element14.Add(new XAttribute("type", "start"));
                element14.Add(new XAttribute("title", "Book"));
                element14.Add(new XAttribute("href", "booktitle.html"));
                element3.Elements("guide").ElementAtOrDefault(0).Add(element14);
            }
            if ((currentSettings.ntoc != "True") && (currentSettings.ContentOf != "True"))
            {
                element14 = new XElement("item");
                element14.Add(new XAttribute("id", "content"));
                element14.Add(new XAttribute("media-type", "text/x-oeb1-document"));
                element14.Add(new XAttribute("href", "toc.html"));
                element14.Add("");
                element3.Elements("manifest").ElementAtOrDefault(0).Add(element14);
                element14 = new XElement("itemref");
                element14.Add(new XAttribute("idref", "content"));
                element3.Elements("spine").ElementAtOrDefault(0).Add(element14);
                element14 = new XElement("reference");
                element14.Add(new XAttribute("type", "toc"));
                element14.Add(new XAttribute("title", "toc"));
                element14.Add(new XAttribute("href", "toc.html"));
                element3.Elements("guide").ElementAtOrDefault(0).Add(element14);
            }
            var str3 = XHelper.get_AttributeValue(element.Elements("description").Elements("t-i").Elements("coverpage").Elements("div").Elements("img"), "src");
            if (str3 != "")
            {
                element14 = new XElement("EmbeddedCover");
                element14.Add(str3);
                element3.Elements("metadata").ElementAtOrDefault(0).Elements("dc-metadata").ElementAtOrDefault(0).Elements("x-metadata").ElementAtOrDefault(0).Add(element14);
            }
            var notesList = new List<DataItem>();
            var notesList2 = new List<DataItem>();
            var bodyStr = element.Elements("body").ElementAtOrDefault(0).ToString();
            var flag13 = false;
            if (num > 1)
            {
                for (var i = 1; i < num; i++)
                {
                    var str21 = XHelper.get_AttributeValue(element.Elements("body").ElementAtOrDefault(i), "name");
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
                            di.Value = XHelper.get_AttributeValue(list[idx], "id");
                            notesList.Add(di);
                        }
                    }
                    if (currentSettings.NoteBoxb) continue;
                    element14 = new XElement("html");
                    element13 = new XElement("head");
                    element12 = new XElement("link");
                    element12.Add(new XAttribute("type", "text/css"));
                    element12.Add(new XAttribute("href", "book.css"));
                    element12.Add(new XAttribute("rel", "Stylesheet"));
                    element13.Add(element12);
                    element14.Add(element13);
                    element13 = new XElement("body");
                    element13.Add(element.Elements("body").ElementAtOrDefault(i).Nodes());
                    element14.Add(element13);
                    var element9 = element14;
                    var htmltxt = Common.FormatToHTML(element9.ToString());
                    if (currentSettings.nh != "True")
                    {
                        htmltxt = Common.GipherHTML(htmltxt);
                    }
                    htmltxt = Common.AddEncodingToXml(htmltxt);
                    File.WriteAllText(tempDir + @"\" + str21 + ".html", htmltxt);
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
            element14 = new XElement("ncx");
            element13 = new XElement("head");
            element13.Add("");
            element14.Add(element13);
            element13 = new XElement("docTitle");
            element12 = new XElement("text");
            element12.Add("KF8");
            element13.Add(element12);
            element14.Add(element13);
            element13 = new XElement("navMap");
            element13.Add("");
            element14.Add(element13);
            var element6 = element14;
            if (!currentSettings.nstitleb)
            {
                element14 = new XElement("navPoint");
                element14.Add(new XAttribute("id", "navpoint-0"));
                element14.Add(new XAttribute("playOrder", "0"));
                element13 = new XElement("navLabel");
                element12 = new XElement("text");
                element12.Add("Обложка");
                element13.Add(element12);
                element14.Add(element13);
                element13 = new XElement("content");
                element13.Add(new XAttribute("src", "booktitle.html#booktitle"));
                element14.Add(element13);
                element6.Elements("navMap").ElementAtOrDefault(0).Add(element14);
            }
            if (currentSettings.ntoc != "True" && currentSettings.ContentOf != "True")
            {
                element14 = new XElement("navPoint");
                element14.Add(new XAttribute("id", "navpoint-1"));
                element14.Add(new XAttribute("playOrder", "1"));
                element13 = new XElement("navLabel");
                element12 = new XElement("text");
                element12.Add("Содержание");
                element13.Add(element12);
                element14.Add(element13);
                element13 = new XElement("content");
                element13.Add(new XAttribute("src", "toc.html#toc"));
                element14.Add(element13);
                element6.Elements("navMap").ElementAtOrDefault(0).Add(element14);
            }
            var num6 = 1;
            if (currentSettings.ntoc != "True")
            {
                element14 = new XElement("html");
                element13 = new XElement("head");
                element12 = new XElement("title");
                element12.Add("Содержание");
                element13.Add(element12);
                element12 = new XElement("link");
                element12.Add(new XAttribute("type", "text/css"));
                element12.Add(new XAttribute("href", "book.css"));
                element12.Add(new XAttribute("rel", "Stylesheet"));
                element13.Add(element12);
                element14.Add(element13);
                element13 = new XElement("body");
                element12 = new XElement("div");
                element12.Add(new XAttribute("class", "title"));
                content = new XElement("div");
                content.Add(new XAttribute("class", "title1"));
                content.Add(new XAttribute("id", "toc"));
                element15 = new XElement("p");
                element15.Add("Содержание");
                content.Add(element15);
                element12.Add(content);
                element13.Add(element12);
                element12 = new XElement("ul");
                element12.Add("");
                element13.Add(element12);
                element14.Add(element13);
                element5 = element14;
            }
            string str34;
            string str35;
            var prevTag = "";
            var flag11 = false;
            var flag15 = true;
            var titles = new List<DataItem>();
            var index = bodyStr.IndexOf("<");
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
            Console.Write("(Ok)");
            Console.WriteLine();
            var str17 = element2.ToString();
            if (currentSettings.nc != "True")
            {
                Console.Write("Разбиваем на главы...");
                var bookNum = 0;
                num16 = bodyStr.IndexOf("<sectio1", 2);
                num17 = bodyStr.IndexOf("</sectio1>");
                var start = 0;
                var str40 = "";
                var flag17 = false;
                var flag18 = false;
                var noBookFlag = false;
                num9 = 2;
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
                                Common.SaveElementToFile(element2.ToString(), bodyContent, noBookFlag, tempDir, bookNum);
                                element15 = new XElement("item");
                                element15.Add(XHelper.CreateAttribute("id", "text" + bookNum));
                                element15.Add(new XAttribute("media-type", "text/x-oeb1-document"));
                                element15.Add(XHelper.CreateAttribute("href", "book" + bookNum + ".html"));
                                element15.Add("");
                                element3.Elements("manifest").ElementAtOrDefault(0).Add(element15);
                                element15 = new XElement("itemref");
                                element15.Add(XHelper.CreateAttribute("idref", "text" + bookNum));
                                element3.Elements("spine").ElementAtOrDefault(0).Add(element15);
                                index = 0;
                                while (index < titles.Count)
                                {
                                    str34 = titles[index].Value;
                                    str35 = titles[index].Name;
                                    if (bodyContent.IndexOf("id=\"" + str35 + "\"") != -1)
                                    {
                                        element15 = new XElement("navPoint");
                                        element15.Add(XHelper.CreateAttribute("id", "navpoint-" + num9));
                                        element15.Add(XHelper.CreateAttribute("playOrder", num9));
                                        element14 = new XElement("navLabel");
                                        element13 = new XElement("text");
                                        element13.Add(str34);
                                        element14.Add(element13);
                                        element15.Add(element14);
                                        element14 = new XElement("content");
                                        element14.Add(XHelper.CreateAttribute("src", "book" + bookNum + ".html#" + str35));
                                        element15.Add(element14);
                                        element6.Elements("navMap").ElementAtOrDefault(0).Add(element15);
                                        if (currentSettings.ntoc != "True")
                                        {
                                            element15 = new XElement("li");
                                            element14 = new XElement("a");
                                            element14.Add(XHelper.CreateAttribute("href", "book" + bookNum + ".html#" + str35));
                                            element14.Add(str34);
                                            element15.Add(element14);
                                            element5.Elements("body").ElementAtOrDefault(0).Elements("ul").ElementAtOrDefault(0).Add(element15);
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
                                Common.SaveElementToFile(element2.ToString(), bodyContent, noBookFlag, tempDir, bookNum);

                                element15 = new XElement("item");
                                element15.Add(XHelper.CreateAttribute("id", "text" + bookNum));
                                element15.Add(new XAttribute("media-type", "text/x-oeb1-document"));
                                element15.Add(XHelper.CreateAttribute("href", "book" + bookNum + ".html"));
                                element15.Add("");
                                element3.Elements("manifest").ElementAtOrDefault(0).Add(element15);
                                element15 = new XElement("itemref");
                                element15.Add(XHelper.CreateAttribute("idref", "text" + bookNum));
                                element3.Elements("spine").ElementAtOrDefault(0).Add(element15);
                                for (index = 0; index < titles.Count; index++)
                                {
                                    str34 = titles[index].Value;
                                    str35 = titles[index].Name;
                                    if (bodyContent.IndexOf("id=\"" + str35 + "\"") != -1)
                                    {
                                        element15 = new XElement("navPoint");
                                        element15.Add(XHelper.CreateAttribute("id", "navpoint-" + num9));
                                        element15.Add(XHelper.CreateAttribute("playOrder", num9));
                                        element14 = new XElement("navLabel");
                                        element13 = new XElement("text");
                                        element13.Add(str34);
                                        element14.Add(element13);
                                        element15.Add(element14);
                                        element14 = new XElement("content");
                                        element14.Add(XHelper.CreateAttribute("src", "book" + bookNum + ".html#" + str35));
                                        element15.Add(element14);
                                        element6.Elements("navMap").ElementAtOrDefault(0).Add(element15);
                                        if (currentSettings.ntoc != "True")
                                        {
                                            element15 = new XElement("li");
                                            element14 = new XElement("a");
                                            element14.Add(XHelper.CreateAttribute("href", "book" + bookNum + ".html#" + str35));
                                            element14.Add(str34);
                                            element15.Add(element14);
                                            element5.Elements("body").ElementAtOrDefault(0).Elements("ul").ElementAtOrDefault(0).Add(element15);
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
                element15 = new XElement("reference");
                element15.Add(new XAttribute("type", "text"));
                element15.Add(new XAttribute("title", "Book"));
                element15.Add(XHelper.CreateAttribute("href", "book0.html"));
                element3.Elements("guide").ElementAtOrDefault(0).Add(element15);
                Console.Write("(Ok)");
                Console.WriteLine();
            }
            else
            {
                index = 0;
                while (index < titles.Count)
                {
                    str34 = titles[index].Value;
                    str35 = titles[index].Name;
                    element15 = new XElement("navPoint");
                    element15.Add(XHelper.CreateAttribute("id", "navpoint-" + (index + 2).ToString()));
                    element15.Add(XHelper.CreateAttribute("playOrder", (index + 2).ToString()));
                    element14 = new XElement("navLabel");
                    element13 = new XElement("text");
                    element13.Add(str34);
                    element14.Add(element13);
                    element15.Add(element14);
                    element14 = new XElement("content");
                    element14.Add(XHelper.CreateAttribute("src", htmlFile + "#" + str35));
                    element15.Add(element14);
                    element6.Elements("navMap").ElementAtOrDefault(0).Add(element15);
                    if (currentSettings.ntoc != "True")
                    {
                        element15 = new XElement("li");
                        element14 = new XElement("a");
                        element14.Add(XHelper.CreateAttribute("href", htmlFile + "#" + str35));
                        element14.Add(str34);
                        element15.Add(element14);
                        element5.Elements("body").ElementAtOrDefault(0).Elements("ul").ElementAtOrDefault(0).Add(element15);
                    }
                    index++;
                }
                element15 = new XElement("item");
                element15.Add(new XAttribute("id", "text"));
                element15.Add(new XAttribute("media-type", "text/x-oeb1-document"));
                element15.Add(XHelper.CreateAttribute("href", htmlFile));
                element15.Add("");
                element3.Elements("manifest").ElementAtOrDefault(0).Add(element15);
                element15 = new XElement("itemref");
                element15.Add(new XAttribute("idref", "text"));
                element3.Elements("spine").ElementAtOrDefault(0).Add(element15);
                element15 = new XElement("reference");
                element15.Add(new XAttribute("type", "text"));
                element15.Add(new XAttribute("title", "Book"));
                element15.Add(XHelper.CreateAttribute("href", htmlFile));
                element3.Elements("guide").ElementAtOrDefault(0).Add(element15);
                bodyStr = Common.TabRep(bodyStr);
                if (currentSettings.nh != "True")
                {
                    bodyStr = Common.GipherHTML(bodyStr);
                }
                str17 = str17.Insert(str17.IndexOf("<body>") + 6, bodyStr).Replace("<sectio1", "<div class=\"book\"").Replace("</sectio1>", "</div>");
                File.WriteAllText(tempDir + @"\" + htmlFile, str17);
            }
            if (currentSettings.ntoc != "True" && currentSettings.ContentOf == "True")
            {
                element15 = new XElement("navPoint");
                element15.Add(XHelper.CreateAttribute("id", "navpoint-" + (index + 2).ToString()));
                element15.Add(XHelper.CreateAttribute("playOrder", (index + 2).ToString()));
                element14 = new XElement("navLabel");
                element13 = new XElement("text");
                element13.Add("Contents");
                element14.Add(element13);
                element15.Add(element14);
                element14 = new XElement("content");
                element14.Add(new XAttribute("src", "toc.html#toc"));
                element15.Add(element14);
                element6.Elements("navMap").ElementAtOrDefault(0).Add(element15);
                element15 = new XElement("item");
                element15.Add(new XAttribute("id", "content"));
                element15.Add(new XAttribute("media-type", "text/x-oeb1-document"));
                element15.Add(new XAttribute("href", "toc.html"));
                element15.Add("");
                element3.Elements("manifest").ElementAtOrDefault(0).Add(element15);
                element15 = new XElement("itemref");
                element15.Add(new XAttribute("idref", "content"));
                element3.Elements("spine").ElementAtOrDefault(0).Add(element15);
                element15 = new XElement("reference");
                element15.Add(new XAttribute("type", "toc"));
                element15.Add(new XAttribute("title", "toc"));
                element15.Add(new XAttribute("href", "toc.html"));
                element3.Elements("guide").ElementAtOrDefault(0).Add(element15);
            }
            if (flag13)
            {
                foreach (var item in notesList2)
                {
                    element15 = new XElement("item");
                    element15.Add(XHelper.CreateAttribute("id", item.Value));
                    element15.Add(new XAttribute("media-type", "text/x-oeb1-document"));
                    element15.Add(XHelper.CreateAttribute("href", item.Name));
                    element15.Add("");
                    element3.Elements("manifest").ElementAtOrDefault(0).Add(element15);
                    element15 = new XElement("itemref");
                    element15.Add(XHelper.CreateAttribute("idref", item.Value));
                    element3.Elements("spine").ElementAtOrDefault(0).Add(element15);
                    element15 = new XElement("navPoint");
                    element15.Add(XHelper.CreateAttribute("id", "navpoint-" + item.Value));
                    element15.Add(XHelper.CreateAttribute("playOrder", num9));
                    element14 = new XElement("navLabel");
                    element13 = new XElement("text");
                    element13.Add(item.Value);
                    element14.Add(element13);
                    element15.Add(element14);
                    element14 = new XElement("content");
                    element14.Add(XHelper.CreateAttribute("src", item.Name));
                    element15.Add(element14);
                    element6.Elements("navMap").ElementAtOrDefault(0).Add(element15);
                    if (currentSettings.ntoc != "True")
                    {
                        element15 = new XElement("li");
                        element14 = new XElement("a");
                        element14.Add(XHelper.CreateAttribute("href", item.Name));
                        element14.Add(item.Value);
                        element15.Add(element14);
                        element5.Elements("body").ElementAtOrDefault(0).Elements("ul").ElementAtOrDefault(0).Add(element15);
                    }
                    num9++;
                }
            }
            File.WriteAllText(tempDir + @"\book.css", defaultCSS);
            element3.Save(tempDir + @"\" + bookName + ".opf");
            element3.RemoveAll();
            element6.Save(tempDir + @"\toc.ncx");
            element6.RemoveAll();
            if (currentSettings.ntoc != "True")
            {
                element5.Save(tempDir + @"\toc.html");
                element5.RemoveAll();
            }
            if (Directory.Exists(executingPath + @"\fonts"))
            {
                Directory.CreateDirectory(tempDir + @"\fonts");
                Common.CopyDirectory(executingPath + @"\fonts", tempDir + @"\fonts", true);
            }

            Common.CreateMobi(executingPath, tempDir, bookName, parentPath, currentSettings.deleteOrigin, bookPath);
            Directory.Delete(tempDir, true);
        }

        public static XElement AddAuthorInfo(XElement avtorbook)
        {
            var element2 = new XElement("h2");
            element2.Add(XHelper.get_Value(avtorbook.Elements("last-name")));
            element2.Add(new XElement("br"));
            element2.Add(XHelper.get_Value(avtorbook.Elements("first-name")));
            element2.Add(new XElement("br"));
            element2.Add(XHelper.get_Value(avtorbook.Elements("middle-name")));
            element2.Add(new XElement("br"));
            return element2;
        }
    }
}