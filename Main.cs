using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Windows.Forms;
using Microsoft.VisualBasic.CompilerServices;

namespace Fb2Kindle
{
    class Module1
    {
        [STAThread]
        public static void Main(string[] args)
        {
            ConverterHelper.ShowMainInfo();
            if (args.Length == 0)
            {
                ConverterHelper.ShowHelpText();
                return;
            }

            var executingPath = Path.GetDirectoryName(Application.ExecutablePath);
            var currentSettings = XmlSerializerHelper.ReadObjectFromFile<DefaultOptions>(executingPath + @"\fb2kf8.set") ?? new DefaultOptions();
            var defaultCSS = ConverterHelper.GetScriptFromResource("defstyles.css"); 
            if (File.Exists(currentSettings.defaultCSS))
                defaultCSS = File.ReadAllText(currentSettings.defaultCSS, Encoding.UTF8);

            var bookPath = string.Empty;
            const string str = "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧЩШЬЪЫЭЮЯQWERTYUIOPASDFGHJKLZXCVBNM";
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
                                return;
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
                Console.Write("Файл стилей не найден: " + currentSettings.defaultCSS);
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
            //create temp working folder
            const string images = "images";
            var tempDir = Path.Combine(Path.GetTempPath(), bookName);
            if (!Directory.Exists(tempDir))
                Directory.CreateDirectory(tempDir);
            if (!Directory.Exists(tempDir + @"\" + images))
                Directory.CreateDirectory(tempDir + @"\" + images);
            if (Directory.Exists(executingPath + @"\" + images))
                ConverterHelper.CopyDirectory(executingPath + @"\" + images, tempDir + @"\" + images, true);

            Console.WriteLine("Processing: " + bookName);
            //extract images
            var processingAppFound = true;
            var startInfo = new ProcessStartInfo();
            if (File.Exists(executingPath + @"\fb2bin.exe"))
            {
                Console.WriteLine("Извлекаем картинки...");
                startInfo.FileName = executingPath + @"\fb2bin.exe";
                startInfo.Arguments = "-x -q -q -d \"" + tempDir + @"\" + images + "\" \"" + bookPath + "\"";
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                var process = Process.Start(startInfo);
                process.WaitForExit();
                switch (process.ExitCode)
                {
                    case 0:
                        Console.WriteLine("(Ok)");
                        break;
                    case 1:
                        Console.WriteLine("(Картинки извлечены, но могут быть ошибки!)");
                        break;
                    case 2:
                        Console.WriteLine("(Невалидный исходный файл - выполнение невозможно!)");
                        break;
                    case 3:
                        Console.WriteLine("(Приключилась фатальная ошибка!)");
                        break;
                    case 4:
                        Console.WriteLine("(Ошибка в параметрах командной строки!)");
                        break;
                }
            }
            else
            {
                processingAppFound = false;
                Console.Write("Невозможно извлечь картинки");
                Console.WriteLine();
            }

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
                if (processingAppFound)
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

            var tempClass = new TempClass();
            var namespace2 = XNamespace.Get("");
            tempClass.h2 = namespace2.GetName("h2");
            tempClass.lastName = namespace2.GetName("last-name");
            tempClass.br = namespace2.GetName("br");
            tempClass.firstName = namespace2.GetName("first-name");
            tempClass.middleName = namespace2.GetName("middle-name");
            var name28 = namespace2.GetName("p");
            var name29 = namespace2.GetName("sequence");
            var name30 = namespace2.GetName("name");
            var name31 = namespace2.GetName("number");
            var name32 = namespace2.GetName("book-title");
            var name33 = namespace2.GetName("publish-info");
            var name34 = namespace2.GetName("publisher");
            var name35 = namespace2.GetName("city");
            var name36 = namespace2.GetName("year");
            var name37 = namespace2.GetName("lang");
            var name38 = namespace2.GetName("package");
            var name39 = namespace2.GetName("metadata");
            var name40 = namespace2.GetName("meta");
            var name41 = namespace2.GetName("content");
            var name42 = namespace2.GetName("dc-metadata");
            var name43 = XNamespace.Get("http://www.w3.org/2000/xmlns/").GetName("dc");
            var namespace4 = XNamespace.Get("http://");
            var name44 = namespace4.GetName("Title");
            var name45 = namespace4.GetName("Language");
            var name46 = namespace4.GetName("Creator");
            var name47 = namespace4.GetName("Publisher");
            var name48 = namespace4.GetName("date");
            var name49 = namespace2.GetName("x-metadata");
            var name50 = namespace2.GetName("manifest");
            var name51 = namespace2.GetName("item");
            var name52 = namespace2.GetName("media-type");
            var name53 = namespace2.GetName("spine");
            var name54 = namespace2.GetName("toc");
            var name55 = namespace2.GetName("guide");
            var name56 = namespace2.GetName("itemref");
            var name57 = namespace2.GetName("idref");
            var name58 = namespace2.GetName("reference");
            var name59 = namespace2.GetName("title");
            var name60 = namespace2.GetName("coverpage");
            var name61 = namespace2.GetName("img");
            var name62 = namespace2.GetName("src");
            var name63 = namespace2.GetName("EmbeddedCover");
            var name64 = namespace2.GetName("section");
            var name65 = namespace2.GetName("ncx");
            var name66 = namespace2.GetName("docTitle");
            var name67 = namespace2.GetName("text");
            var name68 = namespace2.GetName("navMap");
            var name69 = namespace2.GetName("navPoint");
            var name70 = namespace2.GetName("playOrder");
            var name71 = namespace2.GetName("navLabel");
            var name72 = namespace2.GetName("ul");
            var name73 = namespace2.GetName("li");
            var name74 = namespace2.GetName("a");

            var content = new XElement(InternalXmlHelper.GetXName("TabReplace"));
            content.Add("");
            var tabRepX = content;

            var num9 = 0;
            XElement element12;
            XElement element13;
            XElement element14;
            XElement element15;
            
            var num = element.Elements(InternalXmlHelper.GetXName("body")).Count();
            if (!currentSettings.nstitleb)
            {
                content = new XElement(InternalXmlHelper.GetXName("html"));
                element12 = new XElement(InternalXmlHelper.GetXName("head"));
                element13 = new XElement(InternalXmlHelper.GetXName("link"));
                element13.Add(new XAttribute(InternalXmlHelper.GetXName("type"), "text/css"));
                element13.Add(new XAttribute(InternalXmlHelper.GetXName("href"), "book.css"));
                element13.Add(new XAttribute(InternalXmlHelper.GetXName("rel"), "Stylesheet"));
                element12.Add(element13);
                content.Add(element12);
                element12 = new XElement(InternalXmlHelper.GetXName("body"));
                element13 = new XElement(InternalXmlHelper.GetXName("div"));
                element13.Add(new XAttribute(InternalXmlHelper.GetXName("class"), "supertitle"));
                element13.Add(new XAttribute(InternalXmlHelper.GetXName("align"), "center"));
                element13.Add(new XAttribute(InternalXmlHelper.GetXName("id"), "booktitle"));
                element13.Add(element.Elements(InternalXmlHelper.GetXName("description")).Elements(InternalXmlHelper.GetXName("t-i")).Elements(InternalXmlHelper.GetXName("author")).Select(tempClass.Process));
                element14 = new XElement(name28);
                element14.Add(InternalXmlHelper.get_AttributeValue(element.Elements(InternalXmlHelper.GetXName("description")).Elements(InternalXmlHelper.GetXName("t-i")).Elements(name29), name30) + " " + InternalXmlHelper.get_AttributeValue(element.Elements(InternalXmlHelper.GetXName("description")).Elements(InternalXmlHelper.GetXName("t-i")).Elements(name29), name31));
                element13.Add(element14);
                element14 = new XElement(tempClass.br);
                element13.Add(element14);
                element14 = new XElement(name28);
                element14.Add(new XAttribute(InternalXmlHelper.GetXName("class"), "text-name"));
                element14.Add(InternalXmlHelper.get_Value(element.Elements(InternalXmlHelper.GetXName("description")).Elements(InternalXmlHelper.GetXName("t-i")).Elements(name32)));
                element13.Add(element14);
                element14 = new XElement(tempClass.br);
                element13.Add(element14);
                element14 = new XElement(name28);
                element14.Add(InternalXmlHelper.get_Value(element.Elements(InternalXmlHelper.GetXName("description")).Elements(name33).Elements(name34)));
                element13.Add(element14);
                element14 = new XElement(name28);
                element14.Add(InternalXmlHelper.get_Value(element.Elements(InternalXmlHelper.GetXName("description")).Elements(name33).Elements(name35)));
                element13.Add(element14);
                element14 = new XElement(name28);
                element14.Add(InternalXmlHelper.get_Value(element.Elements(InternalXmlHelper.GetXName("description")).Elements(name33).Elements(name36)));
                element13.Add(element14);
                element14 = new XElement(tempClass.br);
                element13.Add(element14);
                element12.Add(element13);
                content.Add(element12);
                content.Save(tempDir + @"\booktitle.html");
            }
            var element2 = new XElement(InternalXmlHelper.GetXName("html"));
            element13 = new XElement(InternalXmlHelper.GetXName("head"));
            element12 = new XElement(InternalXmlHelper.GetXName("link"));
            element12.Add(new XAttribute(InternalXmlHelper.GetXName("type"), "text/css"));
            element12.Add(new XAttribute(InternalXmlHelper.GetXName("href"), "book.css"));
            element12.Add(new XAttribute(InternalXmlHelper.GetXName("rel"), "Stylesheet"));
            element13.Add(element12);
            element2.Add(element13);
            element13 = new XElement(InternalXmlHelper.GetXName("body"));
            element13.Add("");
            element2.Add(element13);
            var str20 = InternalXmlHelper.get_Value(element.Elements(InternalXmlHelper.GetXName("description")).ElementAtOrDefault(0).Elements(InternalXmlHelper.GetXName("t-i")).ElementAtOrDefault(0).Elements(name37));
            if (str20 == "")
            {
                str20 = "ru";
            }
            element14 = new XElement(name38);
            element13 = new XElement(name39);
            element12 = new XElement(name40);
            element12.Add(new XAttribute(name30, "zero-gutter"));
            element12.Add(new XAttribute(name41, "true"));
            element13.Add(element12);
            element12 = new XElement(name40);
            element12.Add(new XAttribute(name30, "zero-margin"));
            element12.Add(new XAttribute(name41, "true"));
            element13.Add(element12);
            element12 = new XElement(name42);
            element12.Add(new XAttribute(name43, "http://"));
            content = new XElement(name44);
            content.Add(InternalXmlHelper.get_Value(element.Elements(InternalXmlHelper.GetXName("description")).Elements(InternalXmlHelper.GetXName("t-i")).Elements(name32)));
            element12.Add(content);
            content = new XElement(name45);
            content.Add(str20);
            element12.Add(content);
            content = new XElement(name46);
            content.Add(InternalXmlHelper.get_Value(element.Elements(InternalXmlHelper.GetXName("description")).Elements(InternalXmlHelper.GetXName("t-i")).Elements(InternalXmlHelper.GetXName("author")).ElementAtOrDefault(0).Elements(tempClass.lastName)) + " " + InternalXmlHelper.get_Value(element.Elements(InternalXmlHelper.GetXName("description")).Elements(InternalXmlHelper.GetXName("t-i")).Elements(InternalXmlHelper.GetXName("author")).ElementAtOrDefault(0).Elements(tempClass.firstName)) + " " + InternalXmlHelper.get_Value(element.Elements(InternalXmlHelper.GetXName("description")).Elements(InternalXmlHelper.GetXName("t-i")).Elements(InternalXmlHelper.GetXName("author")).ElementAtOrDefault(0).Elements(tempClass.middleName)));
            element12.Add(content);
            content = new XElement(name47);
            content.Add(InternalXmlHelper.get_Value(element.Elements(InternalXmlHelper.GetXName("description")).Elements(name33).Elements(name34)));
            element12.Add(content);
            content = new XElement(name48);
            content.Add(InternalXmlHelper.get_Value(element.Elements(InternalXmlHelper.GetXName("description")).Elements(name33).Elements(name36)));
            element12.Add(content);
            content = new XElement(name49);
            content.Add("");
            element12.Add(content);
            element13.Add(element12);
            element14.Add(element13);
            element13 = new XElement(name50);
            element12 = new XElement(name51);
            element12.Add(new XAttribute(InternalXmlHelper.GetXName("id"), "ncx"));
            element12.Add(new XAttribute(name52, "application/x-dtbncx+xml"));
            element12.Add(new XAttribute(InternalXmlHelper.GetXName("href"), "toc.ncx"));
            element13.Add(element12);
            element14.Add(element13);
            element13 = new XElement(name53);
            element13.Add(new XAttribute(name54, "ncx"));
            element13.Add("");
            element14.Add(element13);
            element13 = new XElement(name55);
            element13.Add("");
            element14.Add(element13);
            var element3 = element14;
            if (!currentSettings.nstitleb)
            {
                element14 = new XElement(name51);
                element14.Add(new XAttribute(InternalXmlHelper.GetXName("id"), "booktitle"));
                element14.Add(new XAttribute(name52, "text/x-oeb1-document"));
                element14.Add(new XAttribute(InternalXmlHelper.GetXName("href"), "booktitle.html"));
                element14.Add("");
                element3.Elements(name50).ElementAtOrDefault(0).Add(element14);
                element14 = new XElement(name56);
                element14.Add(new XAttribute(name57, "booktitle"));
                element3.Elements(name53).ElementAtOrDefault(0).Add(element14);
                element14 = new XElement(name58);
                element14.Add(new XAttribute(InternalXmlHelper.GetXName("type"), "start"));
                element14.Add(new XAttribute(name59, "Book"));
                element14.Add(new XAttribute(InternalXmlHelper.GetXName("href"), "booktitle.html"));
                element3.Elements(name55).ElementAtOrDefault(0).Add(element14);
            }
            if ((currentSettings.ntoc != "True") && (currentSettings.ContentOf != "True"))
            {
                element14 = new XElement(name51);
                element14.Add(new XAttribute(InternalXmlHelper.GetXName("id"), "content"));
                element14.Add(new XAttribute(name52, "text/x-oeb1-document"));
                element14.Add(new XAttribute(InternalXmlHelper.GetXName("href"), "toc.html"));
                element14.Add("");
                element3.Elements(name50).ElementAtOrDefault(0).Add(element14);
                element14 = new XElement(name56);
                element14.Add(new XAttribute(name57, "content"));
                element3.Elements(name53).ElementAtOrDefault(0).Add(element14);
                element14 = new XElement(name58);
                element14.Add(new XAttribute(InternalXmlHelper.GetXName("type"), "toc"));
                element14.Add(new XAttribute(name59, "toc"));
                element14.Add(new XAttribute(InternalXmlHelper.GetXName("href"), "toc.html"));
                element3.Elements(name55).ElementAtOrDefault(0).Add(element14);
            }
            var str3 = InternalXmlHelper.get_AttributeValue(element.Elements(InternalXmlHelper.GetXName("description")).Elements(InternalXmlHelper.GetXName("t-i")).Elements(name60).Elements(InternalXmlHelper.GetXName("div")).Elements(name61), name62);
            if (str3 != "")
            {
                element14 = new XElement(name63);
                element14.Add(str3);
                element3.Elements(name39).ElementAtOrDefault(0).Elements(name42).ElementAtOrDefault(0).Elements(name49).ElementAtOrDefault(0).Add(element14);
            }
            var strArray = new string[2,1];
            var strArray2 = new string[2,1];
            var str18 = element.Elements(InternalXmlHelper.GetXName("body")).ElementAtOrDefault(0).ToString();
            var flag13 = false;
            var num5 = 0;
            var num7 = 0;
            if (num > 1)
            {
                var num44 = num - 1;
                for (var n = 1; n <= num44; n++)
                {
                    var str21 = InternalXmlHelper.get_AttributeValue(element.Elements(InternalXmlHelper.GetXName("body")).ElementAtOrDefault(n), name30);
                    if (str21 == "") continue;
                    num5++;
                    strArray2 = (string[,]) Utils.CopyArray(strArray2, new string[2,num5 + 1]);
                    strArray2[0, num5] = str21 + ".html";
                    strArray2[1, num5] = str21;
                    var list = element.Elements(InternalXmlHelper.GetXName("body")).ElementAtOrDefault(n).Descendants(name64).ToList();
                    if (list.Count > 0)
                    {
                        var num45 = list.Count - 1;
                        for (var num27 = 0; num27 <= num45; num27++)
                        {
                            num7++;
                            strArray = (string[,]) Utils.CopyArray(strArray, new string[2,num7 + 1]);
                            if (currentSettings.NoteBoxb)
                            {
                                var list2 = list[num27].Descendants(name28).ToList();
                                var str36 = "<b>";
                                var num46 = list2.Count - 1;
                                for (var num28 = 0; num28 <= num46; num28++)
                                {
                                    if (num28 == 0)
                                    {
                                        str36 = str36 + list2[num28].Value + "</b> ";
                                    }
                                    else
                                    {
                                        var str37 = list2[num28].Value.Replace('<', '[').Replace('>', ']');
                                        str36 = str36 + " " + str37;
                                    }
                                }
                                str36 = str36.Replace("&", "");
                                strArray[0, num7] = str36;
                            }
                            else
                            {
                                strArray[0, num7] = str21 + ".html";
                            }
                            strArray[1, num7] = InternalXmlHelper.get_AttributeValue(list[num27], InternalXmlHelper.GetXName("id"));
                        }
                    }
                    if (!currentSettings.NoteBoxb)
                    {
                        element14 = new XElement(InternalXmlHelper.GetXName("html"));
                        element13 = new XElement(InternalXmlHelper.GetXName("head"));
                        element12 = new XElement(InternalXmlHelper.GetXName("link"));
                        element12.Add(new XAttribute(InternalXmlHelper.GetXName("type"), "text/css"));
                        element12.Add(new XAttribute(InternalXmlHelper.GetXName("href"), "book.css"));
                        element12.Add(new XAttribute(InternalXmlHelper.GetXName("rel"), "Stylesheet"));
                        element13.Add(element12);
                        element14.Add(element13);
                        element13 = new XElement(InternalXmlHelper.GetXName("body"));
                        element13.Add(element.Elements(InternalXmlHelper.GetXName("body")).ElementAtOrDefault(n).Nodes());
                        element14.Add(element13);
                        var element9 = element14;
                        var htmltxt = ConverterHelper.FormatToHTML(element9.ToString());
                        if (currentSettings.nh != "True")
                        {
                            htmltxt = ConverterHelper.GipherHTML(htmltxt);
                        }
                        htmltxt = ConverterHelper.AddEncodingToXml(htmltxt);
                        File.WriteAllText(tempDir + @"\" + str21 + ".html", htmltxt);
                        flag13 = true;
                    }
                }
            }
            str18 = str18.Replace("<a ", "<A ").Replace("</a>", "</A> ");
            var str23 = "";
            startIndex = str18.IndexOf("<A ");
            var num15 = str18.IndexOf("</A>");
            var num18 = 1;
            while (startIndex > -1)
            {
                num12 = str18.Length - 1;
                str18 = str18.Insert(startIndex + 1, "!");
                oldValue = "<!A ";
                var num47 = num12;
                for (var num30 = startIndex + 4; num30 <= num47; num30++)
                {
                    ch = str18[num30];
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
                    var num49 = num7;
                    for (var num32 = 1; num32 <= num49; num32++)
                    {
                        if (str22 == strArray[1, num32])
                        {
                            if (currentSettings.NoteBoxb)
                            {
                                str23 = ConverterHelper.FormatToHTML(strArray[0, num32]);
                            }
                            else
                            {
                                str23 = strArray[0, num32] + "#" + str22;
                            }
                            break;
                        }
                    }
                }
                if (currentSettings.NoteBoxb)
                {
                    str18 = str18.Insert(num15 + 5, "<span class=\"note\">" + str23 + "</span>").Replace(oldValue, "<sup>");
                }
                else
                {
                    str18 = str18.Replace(oldValue, "<a href = \"" + str23 + "\"><sup>");
                }
                num18++;
                startIndex = str18.IndexOf("<A ", startIndex);
                if (startIndex != -1)
                {
                    num15 = str18.IndexOf("</A>", startIndex);
                }
            }
            str18 = str18.Replace("</A>", currentSettings.NoteBoxb ? "</sup>" : "</sup></a>");
            var num2 = str18.IndexOf("<body>");
            var number = 1;
            var numArray = new int[4,1];
            var num10 = 1;
            var num16 = str18.IndexOf("<section");
            var num17 = str18.IndexOf("</section>");
//                    var num4 = num2;
            while (num17 > 0)
            {
                numArray = (int[,]) Utils.CopyArray(numArray, new int[4,num10 + 1]);
                if ((num16 < num17) & (num16 != -1))
                {
                    numArray[1, num10] = number;
                    numArray[2, num10] = num16;
                    numArray[3, num10] = 1;
                    str18 = str18.Remove(num16, 8).Insert(num16, "<sectio1");
                    number++;
                }
                else
                {
                    number--;
                    numArray[1, num10] = number;
                    numArray[2, num10] = num17;
                    numArray[3, num10] = -1;
                    str18 = str18.Remove(num17, 10).Insert(num17, "</sectio1>");
                }
                if (num16 != -1)
                {
                    num16 = str18.IndexOf("<section", num16);
                }
                num17 = str18.IndexOf("</section>", num17);
                num10++;
            }
            num16 = str18.IndexOf("<title");
            num17 = str18.IndexOf("</title>");
            while (num16 > 0)
            {
                number = 0;
                var num50 = num10 - 2;
                for (var num33 = 1; num33 <= num50; num33++)
                {
                    if ((num16 > numArray[2, num33]) & (num16 < numArray[2, num33 + 1]))
                    {
                        number = numArray[1, num33];
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
                str18 = str18.Remove(num16, 6).Insert(num16, "<titl" + number).Remove(num17, 8).Insert(num17, "</titl" + number + ">");
                num16 = str18.IndexOf("<title", num17);
                num17 = str18.IndexOf("</title>", num17);
            }
            element14 = new XElement(name65);
            element13 = new XElement(InternalXmlHelper.GetXName("head"));
            element13.Add("");
            element14.Add(element13);
            element13 = new XElement(name66);
            element12 = new XElement(name67);
            element12.Add("KF8");
            element13.Add(element12);
            element14.Add(element13);
            element13 = new XElement(name68);
            element13.Add("");
            element14.Add(element13);
            var element6 = element14;
            if (!currentSettings.nstitleb)
            {
                element14 = new XElement(name69);
                element14.Add(new XAttribute(InternalXmlHelper.GetXName("id"), "navpoint-0"));
                element14.Add(new XAttribute(name70, "0"));
                element13 = new XElement(name71);
                element12 = new XElement(name67);
                element12.Add("Обложка");
                element13.Add(element12);
                element14.Add(element13);
                element13 = new XElement(name41);
                element13.Add(new XAttribute(name62, "booktitle.html#booktitle"));
                element14.Add(element13);
                element6.Elements(name68).ElementAtOrDefault(0).Add(element14);
            }
            if (currentSettings.ntoc != "True" && currentSettings.ContentOf != "True")
            {
                element14 = new XElement(name69);
                element14.Add(new XAttribute(InternalXmlHelper.GetXName("id"), "navpoint-1"));
                element14.Add(new XAttribute(name70, "1"));
                element13 = new XElement(name71);
                element12 = new XElement(name67);
                element12.Add("Содержание");
                element13.Add(element12);
                element14.Add(element13);
                element13 = new XElement(name41);
                element13.Add(new XAttribute(name62, "toc.html#toc"));
                element14.Add(element13);
                element6.Elements(name68).ElementAtOrDefault(0).Add(element14);
            }
            var num6 = 1;
            if (currentSettings.ntoc != "True")
            {
                element14 = new XElement(InternalXmlHelper.GetXName("html"));
                element13 = new XElement(InternalXmlHelper.GetXName("head"));
                element12 = new XElement(name59);
                element12.Add("Содержание");
                element13.Add(element12);
                element12 = new XElement(InternalXmlHelper.GetXName("link"));
                element12.Add(new XAttribute(InternalXmlHelper.GetXName("type"), "text/css"));
                element12.Add(new XAttribute(InternalXmlHelper.GetXName("href"), "book.css"));
                element12.Add(new XAttribute(InternalXmlHelper.GetXName("rel"), "Stylesheet"));
                element13.Add(element12);
                element14.Add(element13);
                element13 = new XElement(InternalXmlHelper.GetXName("body"));
                element12 = new XElement(InternalXmlHelper.GetXName("div"));
                element12.Add(new XAttribute(InternalXmlHelper.GetXName("class"), "title"));
                content = new XElement(InternalXmlHelper.GetXName("div"));
                content.Add(new XAttribute(InternalXmlHelper.GetXName("class"), "title1"));
                content.Add(new XAttribute(InternalXmlHelper.GetXName("id"), "toc"));
                element15 = new XElement(name28);
                element15.Add("Содержание");
                content.Add(element15);
                element12.Add(content);
                element13.Add(element12);
                element12 = new XElement(name72);
                element12.Add("");
                element13.Add(element12);
                element14.Add(element13);
                element5 = element14;
            }
            string str34;
            string str35;
            var str26 = "";
            var flag11 = false;
            var flag15 = true;
            var num11 = 0;
            var strArray3 = new string[2,1];
            var num8 = str18.IndexOf("<");
            while (num8 > -1)
            {
                num9 = num8;
                ch = str18[num8];
                var str32 = "";
                while (ch != '>')
                {
                    ch = str18[num8];
                    num8++;
                    str32 = str32 + ch;
                }
                if (str32.Contains("<titl") || str32.Contains("<epigraph") || str32.Contains("<subtitle") || str32.Contains("<div") || 
                    str32.Contains("<poem") || str32.Contains("<cite"))
                {
                    flag11 = false;
                }
                if ((str32.IndexOf("<p ") != -1) | (str32.IndexOf("<p>") != -1))
                {
                    if (currentSettings.nb != "True")
                    {
                        if (str26.Equals("</titl0>") || str26.Equals("</titl1>") || str26.Equals("</titl2>") || str26.Equals("</titl3>") || 
                            str26.Equals("</titl4>") || str26.Equals("</titl5>") || str26.Equals("</titl6>") || str26.Equals("</titl7>") || 
                            str26.Equals("</titl8>") || str26.Equals("</titl9>") || str26.Equals("</titl1>") || str26.Equals("</subtitle>") || str26.Equals("</epigraph>"))
                        {
                            flag11 = true;
                            while (ch != '<')
                            {
                                ch = str18[num8];
                                if (ch != ' ')
                                {
                                    if (str.IndexOf(ch) != -1)
                                    {
                                        str18 = str18.Remove(num8, 1).Insert(num8, "<span class=\"dropcaps\">" + ch + "</span>").Insert(num9 + 2, " style=\"text-indent:0px;\"");
                                    }
                                    break;
                                }
                                num8++;
                            }
                        }
                        else if (flag11)
                        {
                            while (ch != '<')
                            {
                                ch = str18[num8];
                                if (ch != ' ')
                                {
                                    if (str.IndexOf(ch) != -1)
                                    {
                                        str18 = str18.Remove(num8, 1).Insert(num8, "<span class=\"dropcaps2\">" + ch + "</span>");
                                    }
                                    break;
                                }
                                num8++;
                            }
                        }
                    }
                }
                else if (str32.IndexOf("<titl") != -1)
                {
                    num6++;
                    str18 = str18.Insert(num9 + 6, " id=\"title" + num6 + "\"");
                    num8 = str18.IndexOf(">", num8);
                    num16 = str18.IndexOf("</titl", num8);
                    str35 = str18.Substring(num8 + 1, (num16 - num8) - 1);
                    str34 = "";
                    var str33 = "";
                    var num51 = str35.Length - 1;
                    for (var num34 = 0; num34 <= num51; num34++)
                    {
                        ch = str35[num34];
                        if (ch == '<')
                        {
                            flag15 = false;
                            str33 = "";
                        }
                        else if (ch == '>')
                        {
                            if (str33 == "/p")
                            {
                                str34 = str34 + " ";
                            }
                            flag15 = true;
                        }
                        else if (flag15)
                        {
                            str34 = str34 + ch;
                        }
                        else
                        {
                            str33 = str33 + ch;
                        }
                    }
                    strArray3 = (string[,]) Utils.CopyArray(strArray3, new string[2,num11 + 1]);
                    strArray3[0, num11] = "title" + num6;
                    strArray3[1, num11] = str34;
                    num11++;
                }
                if (((str32 == "</div>") | (str32 == "</cite>")) | (str32 == "</poem>"))
                {
                    flag11 = true;
                }
                if ((str32 != "<empty-line/>") & (str32 != "<empty-line />"))
                {
                    str26 = str32;
                }
                num8 = str18.IndexOf("<", num8);
            }
            str18 = str18.Replace("<text-author>", "<p class=\"text-author\">").Replace("</text-author>", "</p>").
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
                num16 = str18.IndexOf("<sectio1", 2);
                num17 = str18.IndexOf("</sectio1>");
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
                            bodyContent = str18.Substring(start, num16 - start) + "</sectio1>";
                            if (currentSettings.DelZeroTitle == "True" && (bookNum == 0))
                            {
                                flag18 = true;
                            }
                            else if (!XElement.Parse(bodyContent).Elements(name28).Any())
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
                                bodyContent = TabRep(bodyContent, tabRepX);
                                if (currentSettings.nh != "True")
                                {
                                    bodyContent = ConverterHelper.GipherHTML(bodyContent);
                                }
                                ConverterHelper.SaveElementToFile(element2, bodyContent, noBookFlag, tempDir, bookNum);
                                element15 = new XElement(name51);
                                element15.Add(InternalXmlHelper.CreateAttribute(InternalXmlHelper.GetXName("id"), "text" + bookNum));
                                element15.Add(new XAttribute(name52, "text/x-oeb1-document"));
                                element15.Add(InternalXmlHelper.CreateAttribute(InternalXmlHelper.GetXName("href"), "book" + bookNum + ".html"));
                                element15.Add("");
                                element3.Elements(name50).ElementAtOrDefault(0).Add(element15);
                                element15 = new XElement(name56);
                                element15.Add(InternalXmlHelper.CreateAttribute(name57, "text" + bookNum));
                                element3.Elements(name53).ElementAtOrDefault(0).Add(element15);
                                var num52 = num11 - 1;
                                num8 = 0;
                                while (num8 <= num52)
                                {
                                    str34 = strArray3[1, num8];
                                    str35 = strArray3[0, num8];
                                    if (bodyContent.IndexOf("id=\"" + str35 + "\"") != -1)
                                    {
                                        element15 = new XElement(name69);
                                        element15.Add(InternalXmlHelper.CreateAttribute(InternalXmlHelper.GetXName("id"), "navpoint-" + num9));
                                        element15.Add(InternalXmlHelper.CreateAttribute(name70, num9));
                                        element14 = new XElement(name71);
                                        element13 = new XElement(name67);
                                        element13.Add(str34);
                                        element14.Add(element13);
                                        element15.Add(element14);
                                        element14 = new XElement(name41);
                                        element14.Add(InternalXmlHelper.CreateAttribute(name62, "book" + bookNum + ".html#" + str35));
                                        element15.Add(element14);
                                        element6.Elements(name68).ElementAtOrDefault(0).Add(element15);
                                        if (currentSettings.ntoc != "True")
                                        {
                                            element15 = new XElement(name73);
                                            element14 = new XElement(name74);
                                            element14.Add(InternalXmlHelper.CreateAttribute(InternalXmlHelper.GetXName("href"), "book" + bookNum + ".html#" + str35));
                                            element14.Add(str34);
                                            element15.Add(element14);
                                            element5.Elements(InternalXmlHelper.GetXName("body")).ElementAtOrDefault(0).Elements(name72).ElementAtOrDefault(0).Add(element15);
                                        }
                                        num9++;
                                    }
                                    num8++;
                                }
                            }
                        }
                        start = num16;
                        num16 = str18.IndexOf("<sectio1", (num16 + 1));
                        flag17 = false;
                    }
                    else
                    {
                        if (!flag17)
                        {
                            bodyContent = str18.Substring(start, (num17 - start) + 11);
                            if (currentSettings.DelZeroTitle == "True" && (bookNum == 0))
                            {
                                flag18 = true;
                            }
                            else if (!XElement.Parse(bodyContent).Elements(name28).Any())
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
                                bodyContent = TabRep(bodyContent, tabRepX);
                                if (currentSettings.nh != "True")
                                {
                                    bodyContent = ConverterHelper.GipherHTML(bodyContent);
                                }

                                ConverterHelper.SaveElementToFile(element2, bodyContent, noBookFlag, tempDir, bookNum);

                                element15 = new XElement(name51);
                                element15.Add(InternalXmlHelper.CreateAttribute(InternalXmlHelper.GetXName("id"), "text" + bookNum));
                                element15.Add(new XAttribute(name52, "text/x-oeb1-document"));
                                element15.Add(InternalXmlHelper.CreateAttribute(InternalXmlHelper.GetXName("href"), "book" + bookNum + ".html"));
                                element15.Add("");
                                element3.Elements(name50).ElementAtOrDefault(0).Add(element15);
                                element15 = new XElement(name56);
                                element15.Add(InternalXmlHelper.CreateAttribute(name57, "text" + bookNum));
                                element3.Elements(name53).ElementAtOrDefault(0).Add(element15);
                                var num53 = num11 - 1;
                                for (num8 = 0; num8 <= num53; num8++)
                                {
                                    str34 = strArray3[1, num8];
                                    str35 = strArray3[0, num8];
                                    if (bodyContent.IndexOf("id=\"" + str35 + "\"") != -1)
                                    {
                                        element15 = new XElement(name69);
                                        element15.Add(InternalXmlHelper.CreateAttribute(InternalXmlHelper.GetXName("id"), "navpoint-" + num9));
                                        element15.Add(InternalXmlHelper.CreateAttribute(name70, num9));
                                        element14 = new XElement(name71);
                                        element13 = new XElement(name67);
                                        element13.Add(str34);
                                        element14.Add(element13);
                                        element15.Add(element14);
                                        element14 = new XElement(name41);
                                        element14.Add(InternalXmlHelper.CreateAttribute(name62, "book" + bookNum + ".html#" + str35));
                                        element15.Add(element14);
                                        element6.Elements(name68).ElementAtOrDefault(0).Add(element15);
                                        if (currentSettings.ntoc != "True")
                                        {
                                            element15 = new XElement(name73);
                                            element14 = new XElement(name74);
                                            element14.Add(InternalXmlHelper.CreateAttribute(InternalXmlHelper.GetXName("href"), "book" + bookNum + ".html#" + str35));
                                            element14.Add(str34);
                                            element15.Add(element14);
                                            element5.Elements(InternalXmlHelper.GetXName("body")).ElementAtOrDefault(0).Elements(name72).ElementAtOrDefault(0).Add(element15);
                                        }
                                        num9++;
                                    }
                                }
                            }
                        }
                        flag17 = true;
                        start = num17;
                        num17 = str18.IndexOf("</sectio1>", (num17 + 1));
                    }
                    if (!flag17 & !flag18)
                    {
                        bookNum++;
                    }
                    flag18 = false;
                }
                element15 = new XElement(name58);
                element15.Add(new XAttribute(InternalXmlHelper.GetXName("type"), "text"));
                element15.Add(new XAttribute(name59, "Book"));
                element15.Add(InternalXmlHelper.CreateAttribute(InternalXmlHelper.GetXName("href"), "book0.html"));
                element3.Elements(name55).ElementAtOrDefault(0).Add(element15);
                Console.Write("(Ok)");
                Console.WriteLine();
            }
            else
            {
                var num54 = num11 - 1;
                num8 = 0;
                while (num8 <= num54)
                {
                    str34 = strArray3[1, num8];
                    str35 = strArray3[0, num8];
                    element15 = new XElement(name69);
                    element15.Add(InternalXmlHelper.CreateAttribute(InternalXmlHelper.GetXName("id"), "navpoint-" + (num8 + 2).ToString()));
                    element15.Add(InternalXmlHelper.CreateAttribute(name70, (num8 + 2).ToString()));
                    element14 = new XElement(name71);
                    element13 = new XElement(name67);
                    element13.Add(str34);
                    element14.Add(element13);
                    element15.Add(element14);
                    element14 = new XElement(name41);
                    element14.Add(InternalXmlHelper.CreateAttribute(name62, htmlFile + "#" + str35));
                    element15.Add(element14);
                    element6.Elements(name68).ElementAtOrDefault(0).Add(element15);
                    if (currentSettings.ntoc != "True")
                    {
                        element15 = new XElement(name73);
                        element14 = new XElement(name74);
                        element14.Add(InternalXmlHelper.CreateAttribute(InternalXmlHelper.GetXName("href"), htmlFile + "#" + str35));
                        element14.Add(str34);
                        element15.Add(element14);
                        element5.Elements(InternalXmlHelper.GetXName("body")).ElementAtOrDefault(0).Elements(name72).ElementAtOrDefault(0).Add(element15);
                    }
                    num8++;
                }
                element15 = new XElement(name51);
                element15.Add(new XAttribute(InternalXmlHelper.GetXName("id"), "text"));
                element15.Add(new XAttribute(name52, "text/x-oeb1-document"));
                element15.Add(InternalXmlHelper.CreateAttribute(InternalXmlHelper.GetXName("href"), htmlFile));
                element15.Add("");
                element3.Elements(name50).ElementAtOrDefault(0).Add(element15);
                element15 = new XElement(name56);
                element15.Add(new XAttribute(name57, "text"));
                element3.Elements(name53).ElementAtOrDefault(0).Add(element15);
                element15 = new XElement(name58);
                element15.Add(new XAttribute(InternalXmlHelper.GetXName("type"), "text"));
                element15.Add(new XAttribute(name59, "Book"));
                element15.Add(InternalXmlHelper.CreateAttribute(InternalXmlHelper.GetXName("href"), htmlFile));
                element3.Elements(name55).ElementAtOrDefault(0).Add(element15);
                str18 = TabRep(str18, tabRepX);
                if (currentSettings.nh != "True")
                {
                    str18 = ConverterHelper.GipherHTML(str18);
                }
                str17 = str17.Insert(str17.IndexOf("<body>") + 6, str18).Replace("<sectio1", "<div class=\"book\"").Replace("</sectio1>", "</div>");
                File.WriteAllText(tempDir + @"\" + htmlFile, str17);
            }
            if (currentSettings.ntoc != "True" && currentSettings.ContentOf == "True")
            {
                element15 = new XElement(name69);
                element15.Add(InternalXmlHelper.CreateAttribute(InternalXmlHelper.GetXName("id"), "navpoint-" + (num8 + 2).ToString()));
                element15.Add(InternalXmlHelper.CreateAttribute(name70, (num8 + 2).ToString()));
                element14 = new XElement(name71);
                element13 = new XElement(name67);
                element13.Add("Contents");
                element14.Add(element13);
                element15.Add(element14);
                element14 = new XElement(name41);
                element14.Add(new XAttribute(name62, "toc.html#toc"));
                element15.Add(element14);
                element6.Elements(name68).ElementAtOrDefault(0).Add(element15);
                element15 = new XElement(name51);
                element15.Add(new XAttribute(InternalXmlHelper.GetXName("id"), "content"));
                element15.Add(new XAttribute(name52, "text/x-oeb1-document"));
                element15.Add(new XAttribute(InternalXmlHelper.GetXName("href"), "toc.html"));
                element15.Add("");
                element3.Elements(name50).ElementAtOrDefault(0).Add(element15);
                element15 = new XElement(name56);
                element15.Add(new XAttribute(name57, "content"));
                element3.Elements(name53).ElementAtOrDefault(0).Add(element15);
                element15 = new XElement(name58);
                element15.Add(new XAttribute(InternalXmlHelper.GetXName("type"), "toc"));
                element15.Add(new XAttribute(name59, "toc"));
                element15.Add(new XAttribute(InternalXmlHelper.GetXName("href"), "toc.html"));
                element3.Elements(name55).ElementAtOrDefault(0).Add(element15);
            }
            if (flag13)
            {
                var num55 = num5;
                for (var num37 = 1; num37 <= num55; num37++)
                {
                    element15 = new XElement(name51);
                    element15.Add(InternalXmlHelper.CreateAttribute(InternalXmlHelper.GetXName("id"), strArray2[1, num37]));
                    element15.Add(new XAttribute(name52, "text/x-oeb1-document"));
                    element15.Add(InternalXmlHelper.CreateAttribute(InternalXmlHelper.GetXName("href"), strArray2[0, num37]));
                    element15.Add("");
                    element3.Elements(name50).ElementAtOrDefault(0).Add(element15);
                    element15 = new XElement(name56);
                    element15.Add(InternalXmlHelper.CreateAttribute(name57, strArray2[1, num37]));
                    element3.Elements(name53).ElementAtOrDefault(0).Add(element15);
                    element15 = new XElement(name69);
                    element15.Add(InternalXmlHelper.CreateAttribute(InternalXmlHelper.GetXName("id"), "navpoint-" + strArray2[1, num37]));
                    element15.Add(InternalXmlHelper.CreateAttribute(name70, num9));
                    element14 = new XElement(name71);
                    element13 = new XElement(name67);
                    element13.Add(strArray2[1, num37]);
                    element14.Add(element13);
                    element15.Add(element14);
                    element14 = new XElement(name41);
                    element14.Add(InternalXmlHelper.CreateAttribute(name62, strArray2[0, num37]));
                    element15.Add(element14);
                    element6.Elements(name68).ElementAtOrDefault(0).Add(element15);
                    if (currentSettings.ntoc != "True")
                    {
                        element15 = new XElement(name73);
                        element14 = new XElement(name74);
                        element14.Add(InternalXmlHelper.CreateAttribute(InternalXmlHelper.GetXName("href"), strArray2[0, num37]));
                        element14.Add(strArray2[1, num37]);
                        element15.Add(element14);
                        element5.Elements(InternalXmlHelper.GetXName("body")).ElementAtOrDefault(0).Elements(name72).ElementAtOrDefault(0).Add(element15);
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
                ConverterHelper.CopyDirectory(executingPath + @"\fonts", tempDir + @"\fonts", true);
            }

            ConverterHelper.CreateMobi(executingPath, tempDir, bookName, parentPath, currentSettings.deleteOrigin, bookPath);
        }

        public static string TabRep(string Str, XElement TabRepX)
        {
            var namespace2 = XNamespace.Get("");
            var name = namespace2.GetName("rep");
            var name2 = namespace2.GetName("fl");
            var name3 = namespace2.GetName("str1");
            var name4 = namespace2.GetName("str2");
            Str = Str.Replace(Convert.ToChar(160).ToString(), "&nbsp;");
            Str = Str.Replace(Convert.ToChar(0xad).ToString(), "&shy;");
            var num10 = TabRepX.Elements(name).Count() - 1;
            for (var i = 0; i <= num10; i++)
            {
                var strArray = new string[3,1];
                var el = TabRepX.Elements(name).ElementAtOrDefault(i);
                if (InternalXmlHelper.get_AttributeValue(el, name2) != "true") continue;
                var str = ConverterHelper.DeCodStr(InternalXmlHelper.get_Value(el.Elements(name3)));
                var str2 = ConverterHelper.DeCodStr(InternalXmlHelper.get_Value(el.Elements(name4)));
                var num = 0;
                var num2 = 0;
                var strArray2 = ConverterHelper.ReturnMasStr(str, ref num, ref num2);
                int num3;
                if (strArray2[0] != "")
                {
                    num3 = strArray2.GetLength(0) - 1;
                    strArray = (string[,]) Utils.CopyArray(strArray, new string[3,num3 + 1]);
                    var num11 = num3;
                    for (var k = 0; k <= num11; k++)
                    {
                        strArray[0, k] = str.Substring(0, num) + strArray2[k] + str.Substring(num2 + 1, (str.Length - num2) - 1);
                        strArray[1, k] = strArray2[k];
                    }
                }
                else
                {
                    strArray[0, 0] = str;
                }
                num = 0;
                num2 = 0;
                var strArray3 = ConverterHelper.ReturnMasStr(str2, ref num, ref num2);
                if (strArray3[0] != "")
                {
                    num3 = strArray.GetLength(1) - 1;
                    var index = strArray3.GetLength(0) - 1;
                    var num12 = num3;
                    for (var m = 0; m <= num12; m++)
                    {
                        var str3 = m >= index ? strArray3[index] : strArray3[m];
                        strArray[2, m] = ConverterHelper.RandomMas(str2.Substring(0, num) + str3 + str2.Substring(num2 + 1, (str2.Length - num2) - 1));
                    }
                }
                else
                {
                    var num13 = strArray.GetLength(1) - 1;
                    for (var n = 0; n <= num13; n++)
                    {
                        strArray[2, n] = ConverterHelper.RandomMas(str2);
                    }
                }
                num3 = strArray.GetLength(1) - 1;
                var num14 = num3;
                for (var j = 0; j <= num14; j++)
                {
                    if (Str != null) 
                        Str = Str.Replace(strArray[0, j], strArray[2, j].Replace("%NAME%", strArray[1, j]));
                }
            }
            return Str;
        }
    }
}