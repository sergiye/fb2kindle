using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Ionic.Zip;
using Ionic.Zlib;

namespace Fb2Kindle {

  internal class Convertor {

    internal class TocItem {
      internal string Name { get; }
      internal string Href { get; }
      internal List<TocItem> SubItems { get; }

      public bool IsLastCollapsibleLevel => SubItems.Count > 0 && SubItems.All(si => si.SubItems.Count == 0);

      internal TocItem(string name, string href) {
        Name = name;
        Href = href;
        SubItems = new List<TocItem>();
      }

      internal TocItem Add(string name, string href) {
        var subItem = new TocItem(name, href);
        SubItems.Add(subItem);
        return subItem;
      }
    }

    private const string DropCap = "АБВГДЕЖЗИКЛМНОПРСТУФХЦЧЩШЭЮЯ"; //"АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧЩШЬЪЫЭЮЯQWERTYUIOPASDFGHJKLZXCVBNM";
    private const string NoAuthorText = "без автора";
    private XElement opfFile;
    private readonly AppOptions options;

    #region public
    
    internal Convertor(AppOptions options) {
      this.options = options;
      if (!string.IsNullOrEmpty(options.Css)) return;
      var defStylesFile = Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, ".css");
      if (File.Exists(defStylesFile)) {
        options.Css = File.ReadAllText(defStylesFile);
      }
      if (!string.IsNullOrEmpty(options.Css)) return;
      options.Css = Util.GetScriptFromResource("Fb2Kindle.css");
    }

    internal bool ConvertBookSequence(List<string> books) {
      try {
        if (options.UseSourceAsTempFolder)
          options.TempFolder = Path.Combine(Path.GetDirectoryName(books[0]), Path.GetFileNameWithoutExtension(books[0]));

        //create temp working folder
        if (string.IsNullOrWhiteSpace(options.TempFolder))
          options.TempFolder = $"{Path.GetTempPath()}\\{Guid.NewGuid()}";

        // tempDir = GetVersionedPath(tempDir);
        if (!Directory.Exists(options.TempFolder))
          Directory.CreateDirectory(options.TempFolder);

        if (options.Css.Contains("src: url(\"fonts/") && Directory.Exists(options.AppPath + @"\fonts")) {
          Directory.CreateDirectory(options.TempFolder + @"\fonts");
          Util.CopyDirectory(options.AppPath + @"\fonts", options.TempFolder + @"\fonts", true);
        }
        File.WriteAllText(options.TempFolder + @"\book.css", options.Css);

        var coverDone = false;
        TocItem rootToc = null;
        for (var idx = 0; idx < books.Count; idx++) {
          var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(books[idx]);
          if (fileNameWithoutExtension != null) {
            var fileName = fileNameWithoutExtension.Trim();
            Util.WriteLine("Processing: " + fileName);
            var book = LoadBookWithoutNs(books[idx]);
            if (book == null) return false;

            if (idx == 0) {
              options.TargetName = fileName;
              //create instances
              opfFile = GetEmptyPackage(book, books.Count > 1);
              AddPackItem("ncx", "toc.ncx", "application/x-dtbncx+xml", false);
            }

            var bookPostfix = idx == 0 ? "" : $"_{idx}";

            //update images (extract and rewrite refs)
            Directory.CreateDirectory($"{options.TempFolder}\\Images");
            if (ProcessImages(book, $"Images\\{bookPostfix}", coverDone)) {
              var imgSrc = Util.AttributeValue(book.Elements("description").Elements("title-info").Elements("coverpage").Elements("div").Elements("img"), "src");
              if (!string.IsNullOrEmpty(imgSrc)) {
                ImagesHelper.AutoScaleImage(Path.Combine(options.TempFolder, imgSrc));
                if (!coverDone) {
                  opfFile.Elements("metadata").First().Elements("x-metadata").First().Add(new XElement("EmbeddedCover", imgSrc));
                  AddGuideItem("Cover", imgSrc, "other.ms-coverimage-standard");
                  AddPackItem("cover", imgSrc, System.Net.Mime.MediaTypeNames.Image.Jpeg, false);
                  coverDone = true;
                }
                else {
                  AddGuideItem($"Cover{bookPostfix}", imgSrc);
                  AddPackItem($"Cover{bookPostfix}", imgSrc, System.Net.Mime.MediaTypeNames.Image.Jpeg);
                }
              }
            }

            //book root element to contain all the sections
            var bookFileName = $"book{bookPostfix}.html";
            var bookRoot = new XElement("div");
            //add title
            bookRoot.Add(CreateTitlePage(book));
            if (idx == 0)
              AddGuideItem("Title", bookFileName, "start");
            AddPackItem("it" + bookPostfix, bookFileName);
            var bookTitle = GetTitle(book);
            //add to TOC
            if (rootToc == null)
              rootToc = new TocItem(bookTitle, null);
            var tocItem = rootToc.Add(bookTitle, bookFileName);
            ProcessAllData(book, bookRoot, bookPostfix, tocItem, bookFileName);
            ConvertTagsToHtml(bookRoot, true);
            SaveAsHtmlBook(bookRoot, $"{options.TempFolder}\\{bookFileName}", bookTitle);
          }
        }

        CreateNcxFile(rootToc);

        if (rootToc != null && !options.Config.SkipToc) {
          GenerateTocFile(rootToc);
          AddPackItem("content", "toc.html");
          AddGuideItem("toc", "toc.html", "toc");
        }

        SaveXmlToFile(opfFile, $@"{options.TempFolder}\content.opf");
        var tmpBookPath = options.Epub 
          ? CreateEpub() 
          : CreateMobi();
        opfFile.RemoveAll();

        var result = tmpBookPath != null;
        if (result) {
          if (!string.IsNullOrWhiteSpace(options.MailTo) && SendBookByMail(tmpBookPath))
            File.Delete(tmpBookPath);
          else {
            var targetFilePath = GetVersionedPath(Path.GetDirectoryName(books[0]), options.TargetName,
              Path.GetExtension(tmpBookPath)); 
            File.Move(tmpBookPath,targetFilePath);
            Util.Write("Created: ");
            Util.WriteLine(targetFilePath, ConsoleColor.DarkCyan);
          }
        }
    
        if (result && options.Config.DeleteOriginal) {
          foreach (var book in books)
            File.Delete(book);
        }
        return result;
      }
      catch (Exception ex) {
        Util.WriteLine("Unknown error: " + ex.Message, ConsoleColor.Red);
        return false;
      }
      finally {
        try {
          if (!string.IsNullOrWhiteSpace(options.TempFolder)) {
            switch (options.CleanupMode) {
              case ConverterCleanupMode.Full:
                Directory.Delete(options.TempFolder, true);
                break;
              case ConverterCleanupMode.Partial:
                //File.Delete(Path.Combine(tempDir, Path.GetFileNameWithoutExtension(inputFile) + ".opf"));
                File.Delete(Path.Combine(options.TempFolder, "kindlegen.exe"));

                //for Partial mode UseSourceAsTempFolder is always true 
                // var destFolder = GetVersionedPath(Path.GetDirectoryName(bookPath) +"\\" + bookName);
                // if (!tempDir.Equals(destFolder, StringComparison.OrdinalIgnoreCase))
                //   Directory.Move(tempDir, destFolder);
                break;
            }
          }
        }
        catch (Exception ex) {
          Util.WriteLine("Error clearing temp folder: " + ex.Message, ConsoleColor.Red);
          Util.WriteLine();
        }
      }
    }

    internal bool ConvertBook(string bookPath) {
      return ConvertBookSequence(new List<string> { bookPath });
    }

    #endregion public

    #region ncx

    private static XElement AddNcxItem(XElement parent, int playOrder, string label, string href) {
      var navPoint = new XElement("navPoint");
      navPoint.Add(new XAttribute("id", $"p{playOrder}"));
      navPoint.Add(new XAttribute("playOrder", playOrder.ToString()));
      navPoint.Add(new XElement("navLabel", new XElement("text", label)));
      navPoint.Add(new XElement("content", new XAttribute("src", href)));
      parent.Add(navPoint);
      return navPoint;
    }

    private void CreateNcxFile(TocItem tocItem) {
      var ncx = new XElement("ncx");
      var head = new XElement("head", "");
      head.Add(new XElement("meta", new XAttribute("name", "dtb:uid"), new XAttribute("content", "BookId")));
      head.Add(new XElement("meta", new XAttribute("name", "dtb:depth"), new XAttribute("content", "3")));
      head.Add(new XElement("meta", new XAttribute("name", "dtb:totalPageCount"), new XAttribute("content", "0")));
      head.Add(new XElement("meta", new XAttribute("name", "dtb:maxPageNumber"), new XAttribute("content", "0")));
      ncx.Add(head);
      ncx.Add(new XElement("docTitle", new XElement("text", options.TargetName)));
      ncx.Add(new XElement("docAuthor", new XElement("text", "fb2Kindle")));
      var navMap = new XElement("navMap", "");
      AddNcxItem(navMap, 0, "Описание", "book.html#it");
      var playOrder = 2;
      AddTocListItems(tocItem, navMap, ref playOrder, 1);
      if (!options.Config.SkipToc)
        AddNcxItem(navMap, 1, "Содержание", "toc.html#toc");
      ncx.Add(navMap);
      SaveXmlToFile(ncx, $"{options.TempFolder}\\toc.ncx");
      ncx.RemoveAll();
    }

    private void AddTocListItems(TocItem tocItem, XElement navMap, ref int playOrder, int depth) {
      foreach (var subItem in tocItem.SubItems) {
        var navPoint = navMap;
        if (depth > 2 && subItem.IsLastCollapsibleLevel)
          navPoint = AddNcxItem(navMap, playOrder++, subItem.Name, subItem.Href);
        else
          AddNcxItem(navMap, playOrder++, subItem.Name, subItem.Href);
        AddTocListItems(subItem, navPoint, ref playOrder, depth + 1);
      }
    }

    #endregion ncx

    private void UpdateLinksInBook(XElement book, string filename) {
      var links = new Dictionary<string, string>();
      //store new link targets in dictionary
      foreach (var idEl in book.DescendantsAndSelf().Where(el => el.Name != "div" && el.Attribute("id") != null)) {
        links[$"#{(string)idEl.Attribute("id")}"] = filename;
      }
      //update found links hrefs
      foreach (var a in book.Descendants("a")) {
        var href = a.Attribute("href")?.Value;
        if (string.IsNullOrEmpty(href) || !links.ContainsKey(href)) continue;
        var value = a.Value;
        a.RemoveAll();
        a.SetAttributeValue("href", links[href] + href);
        a.Add(new XElement("sup", value));
      }
    }

    private static int SaveSubSections(XElement section, int bookNum, TocItem parent, string postfix, string bookFileName) {
      var bookId = "i" + bookNum + postfix;
      var t = section.Elements("title").FirstOrDefault(el => !string.IsNullOrWhiteSpace(el.Value));
      //var t = section.Descendants("title").FirstOrDefault(el => !string.IsNullOrWhiteSpace(el.Value));
      // if (t == null || string.IsNullOrEmpty(t.Value)) {
      //   t = section.Elements("p").FirstOrDefault();
      // }

      if (t != null && !string.IsNullOrEmpty(t.Value)) {
        Util.RenameTag(t, "div", "title");
        var inner = new XElement("div");
        inner.SetAttributeValue("class", bookNum == 0 ? "title0" : "title1");
        inner.SetAttributeValue("id", bookId);
        inner.Add(t.Nodes());
        t.RemoveNodes();
        t.Add(inner);
        //t.SetAttributeValue("id", string.Format("title{0}", bookNum + 2));
        parent = parent.Add(t.Value.Trim(), $"{bookFileName}#{bookId}");
      }
      bookNum++;
      foreach (var subSection in section.Descendants("section")) {
        bookNum = SaveSubSections(subSection, bookNum, parent, postfix, bookFileName);
      }
      return bookNum;
    }

    private void ProcessAllData(XElement book, XElement bookRoot, string postfix, TocItem parent, string bookFileName) {

      Util.Write("FB2 to HTML...", ConsoleColor.White);
      UpdateLinksInBook(book, bookFileName);
      var bodies = book.Elements("body").ToArray();
      //process other "bodies" (notes)
      var additionalParts = new List<KeyValuePair<string, XElement>>();
      for (var i = 1; i < bodies.Length; i++) {
        Util.RenameTag(bodies[i], "section");
        // if (i < bodies.Length - 1) {
        //   //all but last -> merge into first body
        //   if (bodies[i].Parent != null)
        //     bodies[i].Remove();
        //   bodies[0].Add(bodies[i]);
        //   continue;
        // }
        additionalParts.Add(new KeyValuePair<string, XElement>($"body{i}", bodies[i]));
      }

      bodies[0].Name = "section";
      if (options.Config.DropCaps && options.Css.Contains("span.dc{"))
        SetBigFirstLetters(bodies[0]);

      if (options.Config.NoChapters) {
        var i = 0;
        var ts = bodies[0].Descendants("title");
        foreach (var t in ts) {
          if (!string.IsNullOrEmpty(t.Value))
            parent.Add(t.Value.Trim(), $"{bookFileName}#title{i + 2}");
          Util.RenameTag(t, "div", "title");
          var inner = new XElement("div");
          inner.SetAttributeValue("class", i == 0 ? "title0" : "title1");
          inner.SetAttributeValue("id", $"title{i + 2}");
          inner.Add(t.Nodes());
          t.RemoveNodes();
          t.Add(inner);
          //t.SetAttributeValue("id", string.Format("title{0}", i + 2));
          i++;
        }
      }
      else {
        SaveSubSections(bodies[0], 0, parent, postfix, bookFileName);
      }
      bookRoot.Add(bodies[0]);

      foreach (var part in additionalParts) {
        var item = part.Value;
        if (string.IsNullOrWhiteSpace((string)item.Attribute("id")))
          item.Add(new XAttribute("id", part.Key));
        string bodyName = null;
        var titleEl = item.Descendants("title").FirstOrDefault(el => !string.IsNullOrWhiteSpace(el.Value));
        if (titleEl != null)
          bodyName = titleEl.Value.Trim();
        if (string.IsNullOrEmpty(bodyName)) {
          bodyName = (string)item.Attribute("name");
        }
        bookRoot.Add(item);
        parent.Add(bodyName, $"{bookFileName}#{part.Key}");
      }

      Util.WriteLine("(OK)", ConsoleColor.Green);
    }

    private static void SetBigFirstLetters(XElement body) {
      var regex = new Regex(@"^<p>(\w{1})([\s\w]+.+?)</p>$");
      var sections = body.Descendants("section");
      foreach (var sec in sections) {
        var newPart = true;
        foreach (var t in sec.Elements()) {
          switch (t.Name.ToString()) {
            case "title":
            case "subtitle":
              newPart = true;
              break;
            case "p":
              if (t.IsEmpty || t.HasAttributes) continue;
              var pVal = t.ToString().Trim().Replace("\r", "").Replace("\n", "");
              var matches = regex.Matches(pVal);
              if (matches.Count <= 0 || matches[0].Groups.Count != 3) {
                newPart = false;
                continue;
              }
              var firstSymbol = matches[0].Groups[1].Value;
              if (!DropCap.Contains(firstSymbol)) {
                newPart = false;
                continue;
              }
              t.RemoveAll();
              var newEl = XElement.Parse("<p>" + matches[0].Groups[2].Value + "</p>");
              var span = new XElement("span", firstSymbol);
              if (newPart) {
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

    private string CreateEpub() {
      
      Util.WriteLine("Creating epub...", ConsoleColor.White);

      var epubDir = Directory.CreateDirectory($"{Path.GetTempPath()}\\{Guid.NewGuid()}");
      var opsDir = epubDir.CreateSubdirectory("OPS");
      Util.CopyDirectory(options.TempFolder, $"{opsDir.FullName}", true);
      
      epubDir.CreateSubdirectory("META-INF");
      File.WriteAllText($"{epubDir.FullName}/META-INF/container.xml", @"<?xml version=""1.0"" encoding=""UTF-8""?><container xmlns=""urn:oasis:names:tc:opendocument:xmlns:container"" version=""1.0""><rootfiles><rootfile full-path=""OPS/content.opf"" media-type=""application/oebps-package+xml""/></rootfiles></container>");
      File.WriteAllText($"{epubDir.FullName}/mimetype", "application/epub+zip");

      var tmpBookPath = GetVersionedPath(options.TempFolder, Util.GetValidFileName(options.DocumentTitle), ".epub");
      // var tmpBookPath = GetVersionedPath(options.TempFolder, options.TargetName, ".epub");
      using (var zip = new ZipFile(tmpBookPath)) {
        zip.CompressionLevel = options.Config.CompressionLevel switch {
          1 => CompressionLevel.Default,
          2 => CompressionLevel.BestCompression,
          _ => CompressionLevel.BestSpeed
        };
        zip.AddDirectory(epubDir.FullName);
        zip.Save();
      }
      epubDir.Delete(true);

      return tmpBookPath;
    }

    private string CreateMobi() {

      Util.WriteLine("Creating mobi (KF8)...", ConsoleColor.White);
      var kindleGenPath = $"{options.AppPath}\\kindlegen.exe";
      if (!File.Exists(kindleGenPath)) {
        kindleGenPath = $"{options.TempFolder}\\kindlegen.exe";
        if (!Util.GetFileFromResource("kindlegen.exe", kindleGenPath)) {
          Util.WriteLine("kindlegen.exe not found", ConsoleColor.Red);
          return null;
        }
      }

      var outputFileName = Util.GetValidFileName(options.DocumentTitle); //options.TargetName
      var args = $"\"{options.TempFolder}\\content.opf\" -c{options.Config.CompressionLevel} -o \"{outputFileName}.mobi\"";
      var res = Util.StartProcess(kindleGenPath, args, options.DetailedOutput);
      if (res == 2) {
        Util.WriteLine("Error converting to mobi", ConsoleColor.Red);
        return null;
      }
      
      return $"{options.TempFolder}\\{outputFileName}.mobi";
    }

    private static string GetVersionedPath(string filePath, string fileName = null, string fileExtension = null) {
      var versionNumber = 1;
      if (string.IsNullOrWhiteSpace(fileName)) {
        var result = filePath;
        while (Directory.Exists(result))
          result = $"{filePath}(v{versionNumber++})";
        return result;
      }
      else {
        var result = $"{filePath}\\{fileName}{fileExtension}";
        while (File.Exists(result))
          result = $"{filePath}\\{fileName}(v{versionNumber++}){fileExtension}";
        return result;
      }
    }
    
    private bool SendBookByMail(string tmpBookPath) {
      try {
        if (string.IsNullOrWhiteSpace(options.Config.SmtpServer) || options.Config.SmtpPort <= 0) {
          Util.WriteLine("Mail delivery failed: smtp not configured", ConsoleColor.Red);
          return false;
        }
        // Util.WriteLine($"SMTP: {_currentSettings.SmtpLogin} / {_currentSettings.SmtpServer}:{_currentSettings.SmtpPort}", ConsoleColor.White);
        Util.Write($"Sending to {options.MailTo}...", ConsoleColor.White);
        using (var smtp = new SmtpClient(options.Config.SmtpServer, options.Config.SmtpPort)) {
          smtp.UseDefaultCredentials = false;
          smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
          smtp.Timeout = options.Config.SmtpTimeout;
          smtp.Credentials = new NetworkCredential(options.Config.SmtpLogin, options.Config.SmtpPassword);
          smtp.EnableSsl = true;
          using (var message = new MailMessage(new MailAddress(options.Config.SmtpLogin, "Simpl's converter"),
                   new MailAddress(options.MailTo))) {
            // message.BodyEncoding = message.SubjectEncoding = Encoding.UTF8;
            message.IsBodyHtml = false;
            message.Subject = options.DocumentTitle; //options.TargetName;
            var fileName = Path.GetFileName(tmpBookPath); //options.TargetName
            message.Body = $"Hello! Please, check '{fileName}' file with '{options.DocumentTitle}' book attached";

            using (var att = new Attachment(tmpBookPath)) {
              message.Attachments.Add(att);
              smtp.Send(message);
              //await smtp.SendMailAsync(message);
            }
          }
        }
        Util.WriteLine("OK", ConsoleColor.Green);
        return true;
      }
      catch (Exception ex) {
        Util.WriteLine($"Error: {ex.Message}", ConsoleColor.Red);
      }
      return false;
    }

    private static IEnumerable<string> GetAuthors(IEnumerable<XElement> bookAuthors, int maxCount = int.MaxValue) {
      var returned = 0;
      foreach (var ai in bookAuthors) {
        var author = $"{Util.Value(ai.Elements("last-name"))} {Util.Value(ai.Elements("first-name"))} {Util.Value(ai.Elements("middle-name"))}";
        if (!string.IsNullOrWhiteSpace(author)) {
          yield return author.Trim();
          returned++;
        }
        if (returned >= maxCount)
          yield break;
      }
      if (returned == 0)
        yield return NoAuthorText;
    }

    private static XElement CreateTitlePage(XElement book) {
      var root = new XElement("div", new XAttribute("id", "it"));
      root.Add(new XAttribute("class", "supertitle"));
      root.Add(new XAttribute("align", "center"));

      //author(s)
      var authorsInfo = new XElement("div");
      authorsInfo.Add(new XAttribute("class", "text-author"));
      var authors = GetAuthors(book.Elements("description").Elements("title-info").Elements("author"));
      authorsInfo.Add(new XElement("div", string.Join(", ", authors)));
      root.Add(authorsInfo, new XElement("br"));

      //title
      var title = new XElement("p");
      title.Add(new XAttribute("class", "text-name"));
      title.Add(Util.Value(book.Elements("description").Elements("title-info").Elements("book-title"), ""));
      root.Add(title, new XElement("br"));

      //sequence
      root.Add(new XElement("p", $"{Util.AttributeValue(book.Elements("description").Elements("title-info").Elements("sequence"), "name")} {Util.AttributeValue(book.Elements("description").Elements("title-info").Elements("sequence"), "number")}"));
      root.Add(new XElement("br"));

      //annotation
      var annotation = book.Elements("description").Elements("title-info").Elements("annotation").FirstOrDefault();
      if (annotation != null) {
        annotation.Name = "p";
        root.Add(annotation);
      }
      //root.Add(new XElement("p", Util.Value(book.Elements("description").Elements("title-info").Elements("annotation"))));
      root.Add(new XElement("br"), new XElement("br"));
      root.Add(new XElement("p", Util.Value(book.Elements("description").Elements("publish-info").Elements("publisher"))));
      root.Add(new XElement("p", Util.Value(book.Elements("description").Elements("publish-info").Elements("city"))));
      root.Add(new XElement("p", Util.Value(book.Elements("description").Elements("publish-info").Elements("year"))));

      //footer
      root.Add(new XElement("br"), new XElement("br"), new XElement("br"));
      root.Add(new XElement("p", $"Converted by © Fb2Kindle {Assembly.GetExecutingAssembly().GetName().Version.ToString(3)}"));
      root.Add(new XElement("p", "(Egoshin.Sergey@gmail.com)"));
      return root;
    }

    #region helper methods

    private static void ConvertTagsToHtml(XElement book, bool full = false) {
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

    // private static XDocument ReadXDocumentWithInvalidCharacters(string filename) {
    //   XDocument xDocument;
    //   var xmlReaderSettings = new XmlReaderSettings { CheckCharacters = false };
    //   using (var xmlReader = XmlReader.Create(filename, xmlReaderSettings)) {
    //     // Load our XDocument
    //     xmlReader.MoveToContent();
    //     xDocument = XDocument.Load(xmlReader);
    //   }
    //   return xDocument;
    // }
    //
    // private static Stream GenerateStreamFromString(string s) {
    //   var stream = new MemoryStream();
    //   var writer = new StreamWriter(stream);
    //   writer.Write(s);
    //   writer.Flush();
    //   stream.Position = 0;
    //   return stream;
    // }

    private static XElement LoadBookWithoutNs(string fileName) {
      try {
        XElement book;
        //book = ReadXDocumentWithInvalidCharacters(fileName).Root;
        using (Stream file = File.OpenRead(fileName)) {
          book = XElement.Load(file, LoadOptions.PreserveWhitespace);
        }
        XNamespace ns = "";
        foreach (var el in book.DescendantsAndSelf()) {
          el.Name = ns.GetName(el.Name.LocalName);
          var atList = el.Attributes().ToList();
          el.Attributes().Remove();
          foreach (var at in atList)
            el.Add(new XAttribute(ns.GetName(at.Name.LocalName), at.Value));
        }
        book = new XElement("book", book.Elements("description"), book.Elements("body"), book.Elements("binary"));
        return book;
      }
      catch (Exception ex) {
        Util.WriteLine("Unknown file format: " + ex.Message, ConsoleColor.Red);
        return null;
      }
    }

    private static void SaveXmlToFile(XNode xml, string file) {
      //xml.Save(file, Debugger.IsAttached ? SaveOptions.None : SaveOptions.DisableFormatting);
      var doc = XDocument.Load(xml.CreateReader());
      doc.Declaration = new XDeclaration("1.0", "utf-8", null);
      var writer = new XmlEncodeWriter(Encoding.UTF8);
      doc.Save(writer, SaveOptions.None);
      File.WriteAllText(file, writer.ToString());
      //File.WriteAllText(file, doc.ToString());
    }

    private static void SaveAsHtmlBook(XElement bodyEl, string fileName, string title) {
      var doc = new XElement("html");

      var head = new XElement("head", "");
      head.Add(new XElement("meta", new XAttribute("charset", "utf-8")));
      head.Add(new XElement("title", $"{title}"),
        new XElement("link", new XAttribute("type", "text/css"), new XAttribute("href", "book.css"), new XAttribute("rel", "Stylesheet")));
      doc.Add(head);

      doc.Add(new XElement("body", bodyEl));
      Util.RenameTags(doc, "section", "div", "book");
      Util.RenameTags(doc, "annotation", "em");
      SaveXmlToFile(doc, fileName);
      doc.RemoveAll();
    }

    private static string GetTitle(XElement book) {
      return Util.Value(book.Elements("description").Elements("title-info").Elements("book-title"), "Книга").Trim();
    }

    private XElement GetEmptyPackage(XElement book, bool useSequenceNameOnly = false) {
      var package = new XElement("package");
      package.Add(new XAttribute("unique-identifier", "DOI"));
      package.Add(new XAttribute(XNamespace.Get("http://www.w3.org/2000/xmlns/").GetName("fo"), "http://www.w3.org/1999/XSL/Format"));
      package.Add(new XAttribute(XNamespace.Get("http://www.w3.org/2000/xmlns/").GetName("fb"), "http://www.gribuser.ru/xml/fictionbook/2.0"));
      package.Add(new XAttribute(XNamespace.Get("http://www.w3.org/2000/xmlns/").GetName("xlink"), "http://www.w3.org/1999/xlink"));
      var linkEl = new XElement("meta", new XAttribute("name", "zero-gutter"), new XAttribute("content", "true"));
      var headEl = new XElement("metadata", linkEl);
      linkEl = new XElement("meta", new XAttribute("name", "zero-margin"), new XAttribute("content", "true"));
      headEl.Add(linkEl);
      headEl.Add(new XElement("meta", new XAttribute("name", "cover"), new XAttribute("content", "cover")));
      linkEl = new XElement("dc-metadata");
      XNamespace dc = "http://purl.org/metadata/dublin_core";
      linkEl.Add(new XAttribute(XNamespace.Xmlns + "dc", dc));
      linkEl.Add(new XAttribute(XNamespace.Xmlns + "oebpackage", "http://openebook.org/namespaces/oeb-package/1.0/"));

      var content = new XElement(dc + "Title");

      var bookTitle = GetTitle(book);
      var seqName = Util.AttributeValue(book.Elements("description").Elements("title-info").Elements("sequence"), "name");
      if (useSequenceNameOnly) {
        bookTitle = string.IsNullOrEmpty(seqName) ? bookTitle : seqName;
      }
      else {
        if (options.Config.AddSequenceInfo)
          bookTitle = $"{seqName} {Util.AttributeValue(book.Elements("description").Elements("title-info").Elements("sequence"), "number")} {bookTitle}";
      }
      content.Add(bookTitle);
      
      options.DocumentTitle = bookTitle;
      Util.Write("Target document title: ");
      Util.WriteLine(bookTitle, ConsoleColor.Green);

      linkEl.Add(content);
      content = new XElement(dc + "Creator");
      var authors = GetAuthors(book.Elements("description").Elements("title-info").Elements("author"), 5);
      content.Add(string.Join(", ", authors));
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
      package.Add(headEl);

      package.Add(new XElement("manifest"));
      package.Add(new XElement("spine", new XAttribute("toc", "ncx")));
      return package;
    }

    private void GenerateTocFile(TocItem rootToc) {
      var toc = new XElement("html", new XAttribute("type", "toc"));
      var head = new XElement("head", "");
      head.Add(new XElement("meta", new XAttribute("charset", "utf-8")));
      head.Add(new XElement("title", $"{rootToc.Name} - Содержание"),
          new XElement("link", new XAttribute("type", "text/css"), new XAttribute("href", "book.css"), new XAttribute("rel", "Stylesheet")));
      toc.Add(head);
      var ul = new XElement("ul", "");
      toc.Add(new XElement("body", new XElement("div", new XAttribute("class", "title"),
          new XAttribute("id", "toc"), "Содержание"), ul));
      AddTocSubItems(rootToc, ul);
      SaveXmlToFile(toc, $@"{options.TempFolder}\toc.html");
    }

    private static void AddTocSubItems(TocItem tocItem, XElement tocEl) {
      foreach (var subItem in tocItem.SubItems) {
        var li = new XElement("li", new XElement("a", new XAttribute("href", subItem.Href), subItem.Name));
        tocEl.Add(li);
        if (!subItem.SubItems.Any()) continue;
        var ul = new XElement("ul", "");
        li.Add(ul);
        AddTocSubItems(subItem, ul);
      }
    } 

    private void AddPackItem(string id, string href, string mediaType = "text/x-oeb1-document", bool addSpine = true) {
      var packEl = new XElement("item");
      packEl.Add(new XAttribute("id", id));
      packEl.Add(new XAttribute("href", href.Replace("\\", "/")));
      packEl.Add(new XAttribute("media-type", mediaType));
      opfFile.Elements("manifest").First().Add(packEl);
      if (addSpine)
        opfFile.Elements("spine").First().Add(new XElement("itemref", new XAttribute("idref", id)));
    }

    private void AddGuideItem(string id, string href, string guideType = "text") {
      if (string.IsNullOrEmpty(guideType)) return;
      // if (guideType.Equals("text")) return;
      var itemEl = new XElement("reference");
      itemEl.Add(new XAttribute("type", guideType)); //"text"
      itemEl.Add(new XAttribute("title", id));
      itemEl.Add(new XAttribute("href", href.Replace("\\", "/")));
      var guide = opfFile.Elements("guide").FirstOrDefault();
      if (guide == null) {
        guide = new XElement("guide", "");
        opfFile.Add(guide);
      }
      guide.Add(itemEl);
    }

    #endregion helper methods

    #region Images

    private bool ProcessImages(XElement book, string imagesPrefix, bool coverDone) {
      var imagesCreated = (!coverDone || !options.Config.NoImages) && ExtractImages(book, options.TempFolder, imagesPrefix);
      var list = Util.RenameTags(book, "image", "div", "image");
      foreach (var element in list) {
        if (!imagesCreated)
          element.Remove();
        else {
          if (options.Config.NoImages &&
              (element.Parent == null || !element.Parent.Name.LocalName.Equals("coverpage", StringComparison.OrdinalIgnoreCase))) {
            //keep coverpage only
            element.Remove();
            continue;
          }

          var src = element.Attribute("href")?.Value;
          element.RemoveAll();
          element.SetAttributeValue("class", "image");
          if (string.IsNullOrEmpty(src)) continue;
          src = src.Replace("#", "");
          var imgEl = new XElement("img");
          imgEl.SetAttributeValue("src", GetImageNameWithExt($"{imagesPrefix}{src}"));
          element.Add(imgEl);
        }
      }
      return imagesCreated;
    }

    private string GetImageNameWithExt(string original) {
      var ext = Path.GetExtension(original);
      if (string.IsNullOrWhiteSpace(ext))
        return original + ".jpg";
      return original;
    }

    private bool ExtractImages(XElement book, string workFolder, string imagesPrefix) {
      if (book == null) return true;
      Util.Write("Extracting images...", ConsoleColor.White);
      foreach (var binEl in book.Elements("binary")) {
        try {
          var file = GetImageNameWithExt($"{workFolder}\\{imagesPrefix}{binEl.Attribute("id")?.Value}");
          var format = ImagesHelper.GetImageFormatFromMimeType(binEl.Attribute("content-type")?.Value, options.Config.Jpeg ? ImageFormat.Jpeg : ImageFormat.Png);
          //todo: we can get format from img.RawFormat
          var fileBytes = Convert.FromBase64String(binEl.Value);
          try {
            using (Stream str = new MemoryStream(fileBytes)) {
              using (var img = Image.FromStream(str)) {
                // var pngCodec = Util.GetEncoderInfo(ImageFormat.Png);
                // if (pngCodec != null) {
                //   var parameters = new EncoderParameters(1) {
                //     Param = {
                //       [0] = new EncoderParameter(Encoder.ColorDepth, 24)
                //     }
                //   };
                //   img.Save(file, pngCodec, parameters);
                // }
                // else
                img.Save(file, format);
              }
            }
            if (options.Config.Grayscaled) {
              Image gsImage;
              using (var img = Image.FromFile(file)) {
                gsImage = ImagesHelper.GrayScale(img, true, format);
              }
              gsImage.Save(file, format);
            }
          }
          catch (Exception ex) {
            Util.WriteLine("Error compressing image: " + ex.Message, ConsoleColor.Red);
            File.WriteAllBytes(file, fileBytes);
          }
        }
        catch (Exception ex) {
          Util.WriteLine(ex.Message, ConsoleColor.Red);
        }
      }
      Util.WriteLine("(OK)", ConsoleColor.Green);
      return true;
    }

    #endregion Images
  }
}
