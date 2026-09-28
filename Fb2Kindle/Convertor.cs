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
using System.Xml;
using System.Xml.Linq;
using System.IO.Compression;
using sergiye.Common;

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
    private const string KindleGenName = "kindlegen.exe";
    private static readonly XNamespace NcxNs = "http://www.daisy.org/z3986/2005/ncx/";
    private static readonly XNamespace XhtmlNs = "http://www.w3.org/1999/xhtml";
    private static readonly XNamespace OpfNs = "http://www.idpf.org/2007/opf";
    private static readonly XNamespace DcNs = "http://purl.org/dc/elements/1.1/";
    private static readonly string[] BlockImageParents = ["section", "body", "coverpage"];
    private XElement opfFile;
    private string bookId;
    private string kindleGenPath;
    private readonly HashSet<string> referencedImages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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
        options.TempFolder = options.UseSourceAsTempFolder
          ? Path.Combine(Path.GetDirectoryName(books[0]), Path.GetFileNameWithoutExtension(books[0]))
          : Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // tempDir = GetVersionedPath(tempDir);
        if (!Directory.Exists(options.TempFolder))
          Directory.CreateDirectory(options.TempFolder);

        if (Regex.IsMatch(options.Css, @"url\(\s*[""']?fonts/", RegexOptions.IgnoreCase) && Directory.Exists(options.AppPath + @"\fonts")) {
          Directory.CreateDirectory(options.TempFolder + @"\fonts");
          Util.CopyDirectory(options.AppPath + @"\fonts", options.TempFolder + @"\fonts", true);
        }
        File.WriteAllText(options.TempFolder + @"\book.css", options.Css);

        TaskbarProgressHelper.SetState(TaskbarProgressHelper.TaskbarStates.Normal);
        TaskbarProgressHelper.SetValue(0, books.Count);

        referencedImages.Clear();
        var origins = new Dictionary<string, string>();
        var sources = ExtractArchives(books, origins);
        var convertedOrigins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var coverDone = false;
        TocItem rootToc = null;
        var sequenceIndex = 0;
        for (var idx = 0; idx < sources.Count; idx++) {
          var fileName = Path.GetFileNameWithoutExtension(sources[idx]).Trim();
          Util.WriteLine("Processing: " + fileName);
          TaskbarProgressHelper.SetState(TaskbarProgressHelper.TaskbarStates.Normal);
          TaskbarProgressHelper.SetValue(idx, sources.Count);

          if (options.OptimizeSource) {
            try {
              XElement bookRaw;
              using (Stream file = File.OpenRead(sources[idx])) {
                bookRaw = XElement.Load(file, LoadOptions.PreserveWhitespace);
              }
              if (bookRaw != null && OptimizeImages(bookRaw)) {
                 bookRaw.Save(sources[idx], SaveOptions.DisableFormatting);
              }
            }
            catch (Exception ex) {
              Util.WriteLine("Unable to optimize source: " + ex.Message, Util.ErrorColor);
            }
            continue;
          }

          var book = LoadBookWithoutNs(sources[idx]);
          if (book == null) continue;

          if (sequenceIndex == 0) {
            options.TargetName = fileName;
            //create instances
            opfFile = GetEmptyPackage(book, sources.Count > 1);
            AddPackItem("ncx", "toc.ncx", "application/x-dtbncx+xml", false);
          }

          var bookPostfix = sequenceIndex == 0 ? "" : $"_{sequenceIndex}";
          string sequenceCover = null;

          //update images (extract and rewrite refs)
          Directory.CreateDirectory($"{options.TempFolder}\\Images");
          if (ProcessImages(book, $"Images/{bookPostfix}", coverDone)) {
            var imgSrc = Util.AttributeValue(TitleInfo(book).Elements("coverpage").Elements("div").Elements("img"), "src");
            if (!string.IsNullOrEmpty(imgSrc) && PrepareCover(imgSrc)) {
              if (!coverDone) {
                opfFile.Element(OpfNs + "metadata").Add(new XElement(OpfNs + "meta", new XAttribute("name", "cover"), new XAttribute("content", "cover")));
                AddPackItem("cover", imgSrc, GetMediaType(imgSrc), false);
                coverDone = true;
              }
              else {
                sequenceCover = imgSrc;
              }
            }
          }

          //book root element to contain all the sections
          var bookFileName = $"book{bookPostfix}.html";
          var bookRoot = new XElement("div");
          if (sequenceCover != null)
            bookRoot.Add(new XElement("div", new XAttribute("class", "image"), new XElement("img", new XAttribute("src", sequenceCover))));
          //add title
          bookRoot.Add(CreateTitlePage(book));
          if (sequenceIndex == 0)
            AddGuideItem("Title", bookFileName, "text");
          AddPackItem("it" + bookPostfix, bookFileName);
          var bookTitle = GetTitle(book);
          //add to TOC
          rootToc ??= new TocItem(bookTitle, null);
          var tocItem = rootToc.Add(bookTitle, bookFileName);
          ProcessAllData(book, bookRoot, bookPostfix, tocItem, bookFileName);
          ConvertTagsToHtml(bookRoot, true);
          SaveAsHtmlBook(bookRoot, $"{options.TempFolder}\\{bookFileName}", bookTitle);
          convertedOrigins.Add(origins[sources[idx]]);
          sequenceIndex++;
        }

        TaskbarProgressHelper.SetState(TaskbarProgressHelper.TaskbarStates.NoProgress);

        if (options.OptimizeSource)
          return sources.Count > 0;
        if (sequenceIndex == 0)
           return false;
        CreateNcxFile(rootToc);

        if (!options.Config.SkipToc) {
          GenerateTocFile(rootToc);
          AddPackItem("content", "toc.html");
          AddGuideItem("toc", "toc.html", "toc");
        }

        AddResourceItems();
        SaveXmlToFile(opfFile, $@"{options.TempFolder}\content.opf");

        if (options.Test)
          return true;

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
            Util.WriteLine(targetFilePath, Util.StatusColor);
          }
        }

        if (result && options.Config.DeleteOriginal) {
          foreach (var book in convertedOrigins)
            File.Delete(book);
        }
        return result;
      }
      catch (Exception ex) {
        Util.WriteLine("Unknown error: " + ex.Message, Util.ErrorColor);
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
                File.Delete(Path.Combine(options.TempFolder, KindleGenName));

                //for Partial mode UseSourceAsTempFolder is always true
                // var destFolder = GetVersionedPath(Path.GetDirectoryName(bookPath) +"\\" + bookName);
                // if (!tempDir.Equals(destFolder, StringComparison.OrdinalIgnoreCase))
                //   Directory.Move(tempDir, destFolder);
                break;
            }
          }
        }
        catch (Exception ex) {
          Util.WriteLine("Error clearing temp folder: " + ex.Message, Util.ErrorColor);
          Util.WriteLine();
        }
      }
    }

    internal bool ConvertBook(string bookPath) {
      return ConvertBookSequence([bookPath]);
    }

    #endregion public

    private List<string> ExtractArchives(List<string> books, Dictionary<string, string> origins) {
      var result = new List<string>();
      foreach (var bookPath in books) {
        var fileExtension = Path.GetExtension(bookPath);
        switch (fileExtension.ToLower()) {
          case ".fb2":
            result.Add(bookPath);
            origins[bookPath] = bookPath;
            break;
          case ".zip":
            var fileName = Path.GetFileNameWithoutExtension(bookPath).Trim();
            var zipFileIndex = 0;
            try {
              using (var zip = ZipFile.OpenRead(bookPath)) {
                foreach (var zipEntry in zip.Entries) {
                  var zipEntryFileExtension = Path.GetExtension(zipEntry.Name)?.ToLower();
                  if (!".fb2".Equals(zipEntryFileExtension))
                    continue;
                  var unzippedFileName = zipFileIndex == 0
                    ? Util.GetValidFileName($"{fileName}{zipEntryFileExtension}")
                    : Util.GetValidFileName($"{fileName}_{zipFileIndex}{zipEntryFileExtension}");
                  var unzippedPath = Path.Combine(options.TempFolder, unzippedFileName);
                  zipEntry.ExtractToFile(unzippedPath, true);
                  result.Add(unzippedPath);
                  origins[unzippedPath] = bookPath;
                  zipFileIndex++;
                }
              }
            }
            catch (Exception ex) when (ex is InvalidDataException || ex is IOException || ex is UnauthorizedAccessException) {
              Util.WriteLine($"Unable to read archive '{bookPath}': {ex.Message}", Util.ErrorColor);
            }
            break;
          default:
            Util.WriteLine("Not supported file format: " + fileExtension, Util.ErrorColor);
            break;
        }
      }
      return result;
    }

    #region ncx

    private static XElement AddNcxItem(XElement parent, int playOrder, string label, string href) {
      var navPoint = new XElement(NcxNs + "navPoint");
      navPoint.Add(new XAttribute("id", $"p{playOrder}"));
      navPoint.Add(new XAttribute("playOrder", playOrder.ToString()));
      navPoint.Add(new XElement(NcxNs + "navLabel", new XElement(NcxNs + "text", label)));
      navPoint.Add(new XElement(NcxNs + "content", new XAttribute("src", href)));
      parent.Add(navPoint);
      return navPoint;
    }

    private void CreateNcxFile(TocItem tocItem) {
      var ncx = new XElement(NcxNs + "ncx", new XAttribute("version", "2005-1"));
      var head = new XElement(NcxNs + "head", "");
      head.Add(new XElement(NcxNs + "meta", new XAttribute("name", "dtb:uid"), new XAttribute("content", bookId)));
      head.Add(new XElement(NcxNs + "meta", new XAttribute("name", "dtb:depth"), new XAttribute("content", "3")));
      head.Add(new XElement(NcxNs + "meta", new XAttribute("name", "dtb:totalPageCount"), new XAttribute("content", "0")));
      head.Add(new XElement(NcxNs + "meta", new XAttribute("name", "dtb:maxPageNumber"), new XAttribute("content", "0")));
      ncx.Add(head);
      ncx.Add(new XElement(NcxNs + "docTitle", new XElement(NcxNs + "text", options.TargetName)));
      ncx.Add(new XElement(NcxNs + "docAuthor", new XElement(NcxNs + "text", "fb2Kindle")));
      var navMap = new XElement(NcxNs + "navMap", "");
      var playOrder = 1;
      AddNcxItem(navMap, playOrder++, "Описание", "book.html#it");
      AddTocListItems(tocItem, navMap, ref playOrder, 1);
      if (!options.Config.SkipToc)
        AddNcxItem(navMap, playOrder, "Содержание", "toc.html#toc");
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
      foreach (var subSection in section.Elements("section")) {
        bookNum = SaveSubSections(subSection, bookNum, parent, postfix, bookFileName);
      }
      return bookNum;
    }

    private void ProcessAllData(XElement book, XElement bookRoot, string postfix, TocItem parent, string bookFileName) {

      Util.Write("FB2 to HTML...", Util.InfoColor);
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
      if (options.Config.DropCaps && Regex.IsMatch(options.Css, @"span\.dc\s*\{"))
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
        item.Attribute("name")?.Remove();
        bookRoot.Add(item);
        parent.Add(bodyName, $"{bookFileName}#{part.Key}");
      }

      Util.WriteLine("(OK)", Util.MessageColor);
    }

    private static void SetBigFirstLetters(XElement body) {
      var regex = new Regex(@"^<p>(\w{1})([\s\w]+.+?)</p>$");
      foreach (var sec in body.Descendants("section")) {
        var t = sec.Elements("p").FirstOrDefault(p => !p.IsEmpty && !p.HasAttributes);
        if (t == null) continue;
        var pVal = t.ToString().Trim().Replace("\r", "").Replace("\n", "");
        var matches = regex.Matches(pVal);
        if (matches.Count <= 0 || matches[0].Groups.Count != 3) continue;
        var firstSymbol = matches[0].Groups[1].Value;
        if (!DropCap.Contains(firstSymbol)) continue;
        var newEl = XElement.Parse("<p>" + matches[0].Groups[2].Value + "</p>");
        newEl.SetAttributeValue("style", "text-indent:0px;");
        newEl.AddFirst(new XElement("span", new XAttribute("class", "dc"), firstSymbol));
        t.ReplaceWith(newEl);
      }
    }

    private string CreateEpub() {

      Util.WriteLine("Creating epub...", Util.InfoColor);

      var tmpBookPath = GetVersionedPath(options.TempFolder, Util.GetValidFileName(options.DocumentTitle), ".epub");
      // var tmpBookPath = GetVersionedPath(options.TempFolder, options.TargetName, ".epub");
      using (var epub = new EpubArchive(tmpBookPath)) {
        epub.AddEntry("mimetype", "application/epub+zip", false);
        epub.AddEntry("META-INF/container.xml", @"<?xml version=""1.0"" encoding=""UTF-8""?><container xmlns=""urn:oasis:names:tc:opendocument:xmlns:container"" version=""1.0""><rootfiles><rootfile full-path=""OPS/content.opf"" media-type=""application/oebps-package+xml""/></rootfiles></container>");
        epub.AddFile("OPS/content.opf", Path.Combine(options.TempFolder, "content.opf"));
        foreach (var href in GetManifestFiles()) {
          var file = Path.Combine(options.TempFolder, href);
          if (File.Exists(file))
            epub.AddFile($"OPS/{href}", file);
        }
      }

      return tmpBookPath;
    }

    private string CreateMobi() {

      Util.WriteLine("Creating mobi (KF8)...", Util.InfoColor);
      var kindleGen = GetKindleGenPath();
      if (kindleGen == null) {
        Util.WriteLine($"{KindleGenName} not found", Util.ErrorColor);
        return null;
      }

      var outputFileName = Util.GetValidFileName(options.DocumentTitle); //options.TargetName
      var args = $"\"{options.TempFolder}\\content.opf\" -c{options.Config.CompressionLevel} -o \"{outputFileName}.mobi\"";
      var res = Util.StartProcess(kindleGen, args, options.DetailedOutput);
      var mobiPath = $"{options.TempFolder}\\{outputFileName}.mobi";
      if (res == 2 || !File.Exists(mobiPath)) {
        Util.WriteLine($"Error converting to mobi (kindlegen exit code {res})", Util.ErrorColor);
        return null;
      }

      return mobiPath;
    }

    private string GetKindleGenPath() {
      if (kindleGenPath != null && File.Exists(kindleGenPath))
        return kindleGenPath;
      var appKindleGen = Path.Combine(options.AppPath, KindleGenName);
      if (File.Exists(appKindleGen))
        return kindleGenPath = appKindleGen;

      var cacheFolder = Path.Combine(Path.GetTempPath(), "Fb2Kindle", Assembly.GetExecutingAssembly().GetName().Version.ToString());
      var cachedPath = Path.Combine(cacheFolder, KindleGenName);
      if (!File.Exists(cachedPath)) {
        Directory.CreateDirectory(cacheFolder);
        var tmpPath = $"{cachedPath}.{Guid.NewGuid()}.tmp";
        if (!Util.GetFileFromResource(KindleGenName, tmpPath))
          return null;
        try {
          File.Move(tmpPath, cachedPath);
        }
        catch (IOException) {
          //another instance has extracted it in the meantime
          File.Delete(tmpPath);
        }
      }
      return kindleGenPath = cachedPath;
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
        if (string.IsNullOrWhiteSpace(options.Config.SmtpServer) || options.Config.SmtpPort <= 0 ||
            string.IsNullOrWhiteSpace(options.Config.SmtpLogin) || string.IsNullOrEmpty(options.Config.SmtpPassword)) {
          Util.WriteLine("Mail delivery failed: smtp not configured", Util.ErrorColor);
          return false;
        }
        // Util.WriteLine($"SMTP: {_currentSettings.SmtpLogin} / {_currentSettings.SmtpServer}:{_currentSettings.SmtpPort}", Util.InfoColor);
        Util.Write($"Sending to {options.MailTo}...", Util.InfoColor);
        using (var smtp = new SmtpClient(options.Config.SmtpServer, options.Config.SmtpPort)) {
          smtp.UseDefaultCredentials = false;
          smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
          smtp.Timeout = options.Config.SmtpTimeout;
          smtp.Credentials = new NetworkCredential(options.Config.SmtpLogin, options.Config.GetSmtpPassword());
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
        Util.WriteLine("OK", Util.MessageColor);
        return true;
      }
      catch (Exception ex) {
        Util.WriteLine($"Error: {ex.Message}", Util.ErrorColor);
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

      //author(s)
      var authorsInfo = new XElement("div");
      authorsInfo.Add(new XAttribute("class", "text-author"));
      var authors = GetAuthors(TitleInfo(book).Elements("author"));
      authorsInfo.Add(new XElement("div", string.Join(", ", authors)));
      root.Add(authorsInfo, new XElement("br"));

      //title
      var title = new XElement("p");
      title.Add(new XAttribute("class", "text-name"));
      title.Add(Util.Value(TitleInfo(book).Elements("book-title"), ""));
      root.Add(title, new XElement("br"));

      //sequence
      var sequence = GetSequenceText(book);
      if (sequence != null)
        root.Add(new XElement("p", sequence), new XElement("br"));

      //annotation
      var annotation = TitleInfo(book).Elements("annotation").FirstOrDefault();
      if (annotation != null) {
        annotation.Name = "div";
        root.Add(annotation);
      }
      //root.Add(new XElement("p", Util.Value(book.Elements("description").Elements("title-info").Elements("annotation"))));
      root.Add(new XElement("br"), new XElement("br"));
      root.Add(new XElement("p", Util.Value(PublishInfo(book).Elements("publisher"))));
      root.Add(new XElement("p", Util.Value(PublishInfo(book).Elements("city"))));
      root.Add(new XElement("p", Util.Value(PublishInfo(book).Elements("year"))));

      //footer
      root.Add(new XElement("br"), new XElement("br"), new XElement("br"));
      root.Add(new XElement("p", $"Converted by © Fb2Kindle {Assembly.GetExecutingAssembly().GetName().Version.ToString(3)}"));
      root.Add(new XElement("p", "(Egoshin.Sergey@gmail.com)"));
      return root;
    }

    #region helper methods

    private static void ConvertTagsToHtml(XElement book, bool full = false) {
      Util.RenameTags(book, "text-author", "p", "text-author");
      Util.RenameTags(book, "empty-line", "br");
      Util.RenameTags(book, "epigraph", "div", "epigraph");
      Util.RenameTags(book, "subtitle", "div", "subtitle");
      Util.RenameTags(book, "cite", "div", "cite");
      Util.RenameTags(book, "emphasis", "i");
      Util.RenameTags(book, "strong", "b");
      Util.RenameTags(book, "strikethrough", "del");
      foreach (var style in Util.RenameTags(book, "style", "span"))
        style.Attribute("name")?.Remove();
      Util.RenameTags(book, "date", "p", "date");
      Util.RenameTags(book, "poem", "div", "poem");
      Util.RenameTags(book, "v", "p");
      Util.RenameTags(book, "stanza", "div", "stanza");
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
        if (!book.Elements("body").Any()) {
          Util.WriteLine("Unknown file format: no book body found", Util.ErrorColor);
          return null;
        }
        return book;
      }
      catch (Exception ex) {
        Util.WriteLine("Unknown file format: " + ex.Message, Util.ErrorColor);
        return null;
      }
    }

    private static void SaveXmlToFile(XNode xml, string file) {
      //xml.Save(file, Debugger.IsAttached ? SaveOptions.None : SaveOptions.DisableFormatting);
      var writer = new XmlEncodeWriter(Encoding.UTF8);
      using (var xmlWriter = XmlWriter.Create(writer)) {
        xmlWriter.WriteStartDocument();
        xml.WriteTo(xmlWriter);
      }
      File.WriteAllText(file, writer.ToString());
      //File.WriteAllText(file, doc.ToString());
    }

    private static void SaveAsHtmlBook(XElement bodyEl, string fileName, string title) {
      var doc = new XElement("html");

      var head = new XElement("head", "");
      head.Add(CreateContentTypeMeta());
      head.Add(new XElement("title", $"{title}"),
        new XElement("link", new XAttribute("type", "text/css"), new XAttribute("href", "book.css"), new XAttribute("rel", "Stylesheet")));
      doc.Add(head);

      doc.Add(new XElement("body", bodyEl));
      Util.RenameTags(doc, "section", "div", "book");
      Util.RenameTags(doc, "annotation", "div", "annotation");
      SaveAsXhtml(doc, fileName);
      doc.RemoveAll();
    }

    private static XElement CreateContentTypeMeta() {
      return new XElement("meta", new XAttribute("http-equiv", "Content-Type"), new XAttribute("content", "text/html; charset=utf-8"));
    }

    private static void SaveAsXhtml(XElement html, string fileName) {
      foreach (var el in html.DescendantsAndSelf())
        el.Name = XhtmlNs + el.Name.LocalName;
      SaveXmlToFile(html, fileName);
    }

    private static IEnumerable<XElement> TitleInfo(XElement book) {
      return book.Elements("description").Elements("title-info");
    }

    private static IEnumerable<XElement> PublishInfo(XElement book) {
      return book.Elements("description").Elements("publish-info");
    }

    private static string GetSequenceText(XElement book) {
      var sequence = TitleInfo(book).Elements("sequence");
      var name = Util.AttributeValue(sequence, "name");
      if (string.IsNullOrEmpty(name)) return null;
      var number = Util.AttributeValue(sequence, "number");
      return string.IsNullOrEmpty(number) ? name : $"{name} {number}";
    }

    private static string GetTitle(XElement book) {
      return Util.Value(TitleInfo(book).Elements("book-title"), "Книга").Trim();
    }

    private XElement GetEmptyPackage(XElement book, bool useSequenceNameOnly = false) {
      var package = new XElement(OpfNs + "package", new XAttribute("version", "2.0"), new XAttribute("unique-identifier", "BookId"));
      var linkEl = new XElement(OpfNs + "metadata", new XAttribute(XNamespace.Xmlns + "dc", DcNs));

      var content = new XElement(DcNs + "title");

      var bookTitle = GetTitle(book);
      var seqName = Util.AttributeValue(TitleInfo(book).Elements("sequence"), "name");
      if (useSequenceNameOnly) {
        bookTitle = string.IsNullOrEmpty(seqName) ? bookTitle : seqName;
      }
      else {
        var sequence = GetSequenceText(book);
        if (options.Config.AddSequenceInfo && sequence != null)
          bookTitle = $"{sequence} {bookTitle}";
      }
      content.Add(bookTitle);

      options.DocumentTitle = bookTitle;
      Util.Write("Target document title: ");
      Util.WriteLine(bookTitle, Util.MessageColor);

      linkEl.Add(content);
      content = new XElement(DcNs + "creator");
      var authors = GetAuthors(TitleInfo(book).Elements("author"), 5);
      content.Add(string.Join(", ", authors));
      linkEl.Add(content);
      var publisher = Util.Value(PublishInfo(book).Elements("publisher"));
      if (!string.IsNullOrEmpty(publisher))
        linkEl.Add(new XElement(DcNs + "publisher", publisher));
      //content.Add(Util.Value(book.Elements("description").Elements("publish-info").Elements("year")));
      linkEl.Add(new XElement(DcNs + "date", DateTime.Today.ToString("yyyy-MM-dd")));
      bookId = $"urn:uuid:{Guid.NewGuid()}";
      linkEl.Add(new XElement(DcNs + "identifier", new XAttribute("id", "BookId"), bookId));
      content = new XElement(DcNs + "language");
      var bookLang = Util.Value(TitleInfo(book).Elements("lang"));
      if (string.IsNullOrEmpty(bookLang))
        bookLang = "ru";
      content.Add(bookLang);
      linkEl.Add(content);
      var description = Util.Value(TitleInfo(book).Elements("annotation"));
      if (!string.IsNullOrEmpty(description))
        linkEl.Add(new XElement(DcNs + "description", description));
      linkEl.Add(new XElement(OpfNs + "meta", new XAttribute("name", "zero-gutter"), new XAttribute("content", "true")));
      linkEl.Add(new XElement(OpfNs + "meta", new XAttribute("name", "zero-margin"), new XAttribute("content", "true")));
      package.Add(linkEl);

      package.Add(new XElement(OpfNs + "manifest"));
      package.Add(new XElement(OpfNs + "spine", new XAttribute("toc", "ncx")));
      return package;
    }

    private void GenerateTocFile(TocItem rootToc) {
      var toc = new XElement("html");
      var head = new XElement("head", "");
      head.Add(CreateContentTypeMeta());
      head.Add(new XElement("title", $"{rootToc.Name} - Содержание"),
          new XElement("link", new XAttribute("type", "text/css"), new XAttribute("href", "book.css"), new XAttribute("rel", "Stylesheet")));
      toc.Add(head);
      var ul = new XElement("ul", "");
      toc.Add(new XElement("body", new XElement("div", new XAttribute("class", "title"),
          new XAttribute("id", "toc"), "Содержание"), ul));
      AddTocSubItems(rootToc, ul);
      SaveAsXhtml(toc, $@"{options.TempFolder}\toc.html");
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

    private void AddPackItem(string id, string href, string mediaType = "application/xhtml+xml", bool addSpine = true) {
      var packEl = new XElement(OpfNs + "item");
      packEl.Add(new XAttribute("id", id));
      packEl.Add(new XAttribute("href", href.Replace("\\", "/")));
      packEl.Add(new XAttribute("media-type", mediaType));
      opfFile.Element(OpfNs + "manifest").Add(packEl);
      if (addSpine)
        opfFile.Element(OpfNs + "spine").Add(new XElement(OpfNs + "itemref", new XAttribute("idref", id)));
    }

    private IEnumerable<string> GetManifestFiles() {
      return opfFile.Element(OpfNs + "manifest").Elements(OpfNs + "item").Select(item => (string)item.Attribute("href"));
    }

    private void AddResourceItems() {
      var hrefs = new HashSet<string>(GetManifestFiles(), StringComparer.OrdinalIgnoreCase);
      var index = 0;
      foreach (var file in Directory.GetFiles(options.TempFolder, "*", SearchOption.AllDirectories)) {
        var mediaType = GetMediaType(file);
        if (mediaType == null) continue;
        var href = file.Substring(options.TempFolder.Length).TrimStart('\\', '/').Replace("\\", "/");
        if (hrefs.Contains(href)) continue;
        if (href.StartsWith("Images/", StringComparison.OrdinalIgnoreCase) && !referencedImages.Contains(href)) continue;
        AddPackItem($"res{index++}", href, mediaType, false);
      }
    }

    private void AddGuideItem(string id, string href, string guideType = "text") {
      if (string.IsNullOrEmpty(guideType)) return;
      // if (guideType.Equals("text")) return;
      var itemEl = new XElement(OpfNs + "reference");
      itemEl.Add(new XAttribute("type", guideType)); //"text"
      itemEl.Add(new XAttribute("title", id));
      itemEl.Add(new XAttribute("href", href.Replace("\\", "/")));
      var guide = opfFile.Element(OpfNs + "guide");
      if (guide == null) {
        guide = new XElement(OpfNs + "guide", "");
        opfFile.Add(guide);
      }
      guide.Add(itemEl);
    }

    #endregion helper methods

    #region Images

    private bool ProcessImages(XElement book, string imagesPrefix, bool coverDone) {
      var imageFiles = new Dictionary<string, string>();
      var imagesCreated = (!coverDone || !options.Config.NoImages) && ExtractImages(book, options.TempFolder, imagesPrefix, imageFiles);
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

          var src = element.Attribute("href")?.Value?.Replace("#", "");
          if (string.IsNullOrEmpty(src) || !imageFiles.TryGetValue(src, out var imageFile)) {
            element.Remove();
            continue;
          }
          element.RemoveAll();
          element.SetAttributeValue("class", "image");
          if (element.Parent != null && !BlockImageParents.Contains(element.Parent.Name.LocalName))
            element.Name = "span";
          var imgEl = new XElement("img");
          imgEl.SetAttributeValue("src", imageFile);
          referencedImages.Add(imageFile);
          element.Add(imgEl);
        }
      }
      return imagesCreated;
    }

    private bool PrepareCover(string imgSrc) {
      try {
        ImageExtensions.AutoScaleImage(Path.Combine(options.TempFolder, imgSrc), true, options.Config.OptimizeImagesWidth, options.Config.OptimizeImagesHeight);
        return true;
      }
      catch (Exception ex) {
        Util.WriteLine($"Cover image '{imgSrc}' skipped: {ex.Message}", Util.ErrorColor);
        return false;
      }
    }

    private static string GetImageFileName(string imagesPrefix, string imageId, ImageFormat format) {
      return GetImageNameWithExt(imagesPrefix + Util.GetValidFileName(imageId), format);
    }

    private ImageFormat GetTargetFormat(ImageFormat rawFormat) {
      if (rawFormat.Equals(ImageFormat.Jpeg) || rawFormat.Equals(ImageFormat.Png) || rawFormat.Equals(ImageFormat.Gif))
        return rawFormat;
      return options.Config.Jpeg ? ImageFormat.Jpeg : ImageFormat.Png;
    }

    private static string GetImageNameWithExt(string original, ImageFormat format) {
      var formatExt = format.Equals(ImageFormat.Png) ? ".png" : format.Equals(ImageFormat.Gif) ? ".gif" : ".jpg";
      var ext = Path.GetExtension(original);
      if (ext.Equals(formatExt, StringComparison.OrdinalIgnoreCase) ||
          formatExt == ".jpg" && ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
        return original;
      return original + formatExt;
    }

    private static string GetMediaType(string fileName) {
      switch (Path.GetExtension(fileName).ToLower()) {
        case ".png":
          return "image/png";
        case ".gif":
          return "image/gif";
        case ".bmp":
          return "image/bmp";
        case ".jpg":
        case ".jpeg":
          return System.Net.Mime.MediaTypeNames.Image.Jpeg;
        case ".svg":
          return "image/svg+xml";
        case ".css":
          return "text/css";
        case ".ttf":
          return "application/x-font-ttf";
        case ".otf":
          return "application/vnd.ms-opentype";
        case ".woff":
          return "application/font-woff";
        default:
          return null;
      }
    }

    private bool ExtractImages(XElement book, string workFolder, string imagesPrefix, Dictionary<string, string> imageFiles) {
      if (book == null) return true;
      Util.Write("Extracting images...", Util.InfoColor);
      foreach (var binEl in book.Elements("binary")) {
        try {
          var imageId = binEl.Attribute("id")?.Value;
          var fileBytes = Convert.FromBase64String(binEl.Value);
          var decoded = false;
          ImageFormat format = null;
          string imageFile = null;
          string file = null;
          try {
            using (Stream str = new MemoryStream(fileBytes)) {
              using (var img = Image.FromStream(str)) {
                format = GetTargetFormat(img.RawFormat);
                imageFile = GetImageFileName(imagesPrefix, imageId, format);
                file = Path.Combine(workFolder, imageFile);
                decoded = true;
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
                if (img.RawFormat.Equals(format))
                  File.WriteAllBytes(file, fileBytes);
                else
                  img.Save(file, format);
              }
            }

            if (options.Config.OptimizeImages)
              ImageExtensions.AutoScaleImage(file, magnify: false, options.Config.OptimizeImagesWidth, options.Config.OptimizeImagesHeight);

            if (options.Config.Grayscaled) {
              Image gsImage;
              using (var img = Image.FromFile(file)) {
                gsImage = img.Grayscale(true, format);
              }
              using (gsImage)
                gsImage.Save(file, format);
            }
          }
          catch (Exception ex) when (decoded) {
            Util.WriteLine("Error compressing image: " + ex.Message, Util.ErrorColor);
            File.WriteAllBytes(file, fileBytes);
          }
          imageFiles[imageId] = imageFile;
        }
        catch (Exception ex) {
          Util.WriteLine($"Image '{binEl.Attribute("id")?.Value}' skipped: {ex.Message}", Util.ErrorColor);
        }
      }
      Util.WriteLine("(OK)", Util.MessageColor);
      return true;
    }

    private bool OptimizeImages(XElement book) {
      if (book == null) return false;
      Util.Write("Optimizing source images...", Util.InfoColor);
      var hasChanges = false;
      foreach (var binEl in book.Descendants().Where(e => e.Name.LocalName == "binary")) {
        var imgId = binEl.Attribute("id")?.Value;
        try {
          var format = (binEl.Attribute("content-type")?.Value).GetImageFormatFromMimeType(options.Config.Jpeg ? ImageFormat.Jpeg : ImageFormat.Png);
          //todo: we can get format from img.RawFormat
          var imageBytes = Convert.FromBase64String(binEl.Value);
          if (!ImageExtensions.AutoScaleImage(imageBytes, format, false,
                options.Config.OptimizeImagesWidth, options.Config.OptimizeImagesHeight,
                out var scaledBytes)) continue;
          if (imageBytes.Length <= scaledBytes.Length)
            continue;
          binEl.Value = Convert.ToBase64String(scaledBytes);
          hasChanges = true;
        }
        catch (Exception ex) {
          Util.WriteLine($"Error processing image '{imgId}': " + ex.Message, Util.ErrorColor);
        }
      }
      Util.WriteLine("(OK)", Util.MessageColor);
      return hasChanges;
    }

    #endregion Images
  }
}
