using jail.Classes;
using jail.Classes.Attributes;
using jail.Models;
using Simpl.Extensions.Encryption;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace jail.Controllers {
  [OutputCache(NoStore = true, Duration = 0, VaryByParam = "None")]
  [ActionLogger, SessionRestore]
  public class HomeController : Controller {
    
    #region Overrides

    protected override void HandleUnknownAction(string actionName) {
      Logger.WriteWarning($"HandleUnknownAction - {actionName}", CommonHelper.GetClientAddress(Request));
      base.HandleUnknownAction(actionName);
    }

    protected override void EndExecute(IAsyncResult asyncResult) {
      var appPath = Request.ApplicationPath.TrimEnd('/');
      var localPath = Request.Url.PathAndQuery;
      if (!string.IsNullOrWhiteSpace(appPath))
        localPath = localPath.Substring(appPath.Length);
      ViewBag.Url = AppBaseUrl + localPath.TrimStart('/');
      base.EndExecute(asyncResult);
    }

    #endregion Overrides

    #region Common methods

    protected override void OnException(ExceptionContext filterContext) {
      //if (filterContext.Exception != null)
      //{
      //    if (filterContext.Exception is TaskCanceledException ||
      //        filterContext.Exception is OperationCanceledException)
      //        return;
      //}
      var name = CommonHelper.GetActionLogName(filterContext.HttpContext.Request);
      Logger.WriteError(filterContext.Exception,
        $"{name} - {filterContext.Exception?.Message}", CommonHelper.GetClientAddress(Request));
      base.OnException(filterContext);
    }

    //protected override IAsyncResult BeginExecute(RequestContext requestContext, AsyncCallback callback, object state)
    //{
    //    Logger.WriteTrace(CommonHelper.GetActionLogName(requestContext.HttpContext.Request), CommonHelper.GetClientAddress(), CommonHelper.CurrentIdentityName);
    //    return base.BeginExecute(requestContext, callback, state);
    //}

    public UserProfile CurrentUser {
      get =>
        Request.IsAuthenticated
          ? (UserProfile) ControllerContext.HttpContext.Session["User"]
          : null;
      set => ControllerContext.HttpContext.Session["User"] = value;
    }

    private string AppBaseUrl {
      get {
        if (Request.UserHostAddress.Equals("::1"))
          return Url.Content("~/");
        return SettingsHelper.SiteRemotePath + '/';
        //if (Request == null || Request.Url == null || Request.ApplicationPath == null)
        //    return null;
        //return string.Format("{0}://{1}:{2}{3}/", Request.Url.Scheme, Request.Url.Host, Request.Url.Port, Request.ApplicationPath.TrimEnd('/'));
      }
    }

    private string GetLinkToFile(string fileName) {
      return Url.Action("GetFile", "Home", new {fileName = fileName.Replace('\\', '/').TrimStart('/')});
    }

    private string GetBookFilePath(string bookFileName) {
      // var sourceFileName = Server.MapPath($"~/b/{bookFileName}");
      var sourceFileName = $"{SettingsHelper.TempDataFolder}\\{bookFileName}";
      return sourceFileName;
    }

    private string GetFileContentType(string fileExtension) {
      switch (fileExtension) {
        case ".jpg":
        case ".jpeg":
          return System.Net.Mime.MediaTypeNames.Image.Jpeg; //"image/jpeg";
        default:
          return System.Net.Mime.MediaTypeNames.Application.Octet;
      }
    }

    [Route("file")]
    public ActionResult GetFile(string fileName) {
      var fileExt = Path.GetExtension(fileName.ToLower());
      return File(Path.Combine(SettingsHelper.TempDataFolder, fileName), GetFileContentType(fileExt),
        BookHelper.GetCorrectedFileName(fileName));
    }

    #endregion

    #region basic methods

    public static string GetVersionString() {
      return $"Version: {Assembly.GetExecutingAssembly().GetName().Version}";
    }

    [Route("about")]
    public ActionResult About() {
      return View();
    }

    [Route("contact")]
    public ActionResult Contact() {
      return View();
    }

    #endregion

    #region Login-logout

    [Route("login")]
    public ActionResult Login(string returnUrl) {
      var authCookie = HttpContext.Request.Cookies.Get(FormsAuthentication.FormsCookieName);
      if (authCookie != null && !string.IsNullOrEmpty(authCookie.Value)) {
        var ticket = FormsAuthentication.Decrypt(authCookie.Value);
        if (ticket != null && !ticket.Expired && !string.IsNullOrWhiteSpace(ticket.Name)) {
          //restore from cookie
          var user = UserRepository.GetUser(ticket.Name);
          if (user != null) {
            CurrentUser = user;
            ControllerContext.HttpContext.Session.Timeout = 24 * 60;
            FormsAuthentication.SetAuthCookie(ticket.Name, true);
            Logger.WriteInfo($"{user.UserType} session restored", CommonHelper.GetClientAddress(Request), user.Email);
            return !string.IsNullOrWhiteSpace(returnUrl)
              ? (ActionResult) Redirect(returnUrl)
              : RedirectToAction(user.UserType == UserType.Administrator ? "Log" : "Index", "Home");
          }
        }
      }

      return View(new LogOnModel {RedirectUrl = returnUrl});
    }

    [Route("login")]
    [HttpPost]
    public ActionResult Login(LogOnModel model) {
      if (ModelState.IsValid) {
        var user = UserRepository.GetUser(model.UserName, model.Password);
        if (user == null) {
          ModelState.AddModelError("", "The user name or password provided is incorrect.");
        }
        //else if (user.UserType != UserType.Administrator && user.Id != 0)
        //{
        //    ModelState.AddModelError("", string.Format("'{0}' user type is not allowed to login", user.UserType));
        //}
        else {
          Logger.WriteInfo($"{user.UserType} logged in", CommonHelper.GetClientAddress(Request), model.UserName);
          CurrentUser = user;
          ControllerContext.HttpContext.Session.Timeout = 24 * 60;
          //if (model.RememberMe)
          //{
          var ticket = new FormsAuthenticationTicket(1, model.UserName,
            DateTime.Now, DateTime.Now.AddDays(7), false,
            $"{user.Id},{user.UserType}",
            FormsAuthentication.FormsCookiePath);
          var strEncryptedTicket = FormsAuthentication.Encrypt(ticket);
          var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, strEncryptedTicket) {
            Expires = DateTime.Now.AddDays(60)
          };
          HttpContext.Response.Cookies.Add(cookie);
          FormsAuthentication.SetAuthCookie(model.UserName, true);
          if (!string.IsNullOrWhiteSpace(model.RedirectUrl))
            return Redirect(model.RedirectUrl);
          //}
          //else
          //{
          //    //FormsAuthentication.SetAuthCookie(model.UserName, false);
          //    FormsAuthentication.RedirectFromLoginPage(model.UserName, false);
          //}
          return RedirectToAction(user.UserType == UserType.Administrator ? "Log" : "Index", "Home");
        }
      }

      return View(model);
    }

    [Route("logout")]
    public ActionResult Logout() {
      if (CurrentUser != null && CurrentUser.UserType != UserType.Administrator)
        Logger.WriteInfo("Logout", CommonHelper.GetClientAddress(Request), User.Identity.Name);
      HttpContext.Session["User"] = null;
      Request.Cookies.Remove(FormsAuthentication.FormsCookieName);
      FormsAuthentication.SignOut();
      return RedirectToAction("Index");
    }

    #endregion Login-logout

    #region Logging

    [Route("log"), SkipLogging]
    [UserTypeFilter(Roles = new[] {UserType.Administrator})]
    public ActionResult Log(string k = null, SystemLog.LogItemType t = SystemLog.LogItemType.All) {
      ViewBag.DebugMode = Debugger.IsAttached;
      ViewBag.Key = k;
      ViewBag.SearchType = t;
      return View(SystemRepository.GetErrorLogData(SettingsHelper.MaxRecordsToShowAtOnce, k, t));
    }

    [Route("logp"), SkipLogging]
    [HttpGet, UserTypeFilter(Roles = new[] {UserType.Administrator})]
    public ActionResult LogPartial(string key, SystemLog.LogItemType searchType) {
      ViewBag.DebugMode = Debugger.IsAttached;
      ViewBag.Key = key;
      ViewBag.SearchType = searchType;
      return PartialView(SystemRepository.GetErrorLogData(SettingsHelper.MaxRecordsToShowAtOnce, key, searchType));
    }

    [Route("clrlog"), SkipLogging]
    [HttpPost, UserTypeFilter(Roles = new[] {UserType.Administrator})]
    public string ClearSelectedLog(string selection) {
      try {
        selection = selection.Trim();
        if (string.IsNullOrWhiteSpace(selection))
          return "Empty selection!";
        var removed = SystemRepository.ClearByMessagePart(selection);
        return $"Removed: {removed} records by text: '{selection}'";
      }
      catch (Exception ex) {
        return ex.Message;
      }
    }

    [Route("calclog"), SkipLogging]
    [HttpPost, UserTypeFilter(Roles = new[] {UserType.Administrator})]
    public string CalcSelectedLog(string selection) {
      try {
        selection = selection.Trim();
        if (string.IsNullOrWhiteSpace(selection))
          return "Empty selection!";
        var found = SystemRepository.CalcByMessagePart(selection);
        return $"Found {found} records by text: '{selection}'";
      }
      catch (Exception ex) {
        return ex.Message;
      }
    }

    #endregion Logging

    #region Search

    public async Task<ActionResult> Index(string k = null, string l = "ru", int r = 0) {
      ViewBag.Key = k;
      ViewBag.Lang = l;
      if (r > 0)
        return View(await DataRepository.GetRandomData(r, CurrentUser?.Id, l));
      return View(string.IsNullOrWhiteSpace(k) ? new List<BookInfo>() : 
        await DataRepository.GetSearchData(k, l, CurrentUser?.Id));
    }

    [ValidateInput(false)]
    [Route("search")]
    public async Task<ActionResult> SearchResults(string k = null, string l = "ru", int r = 0) {
      if (r > 0)
        return PartialView(await DataRepository.GetRandomData(r, CurrentUser?.Id, l));

      return PartialView(string.IsNullOrWhiteSpace(k) ? new List<BookInfo>() : 
        await DataRepository.GetSearchData(k, l, CurrentUser?.Id));
    }

    [Route("history")]
    public async Task<ActionResult> History(string sortBy = null, bool sortAsc = false) {
      ViewBag.SortAsc = sortAsc;
      ViewBag.SortBy = sortBy;

      var path = SettingsHelper.TempDataFolder;
      var info = new DirectoryInfo(path);

      try {
        var total = Directory.GetFiles(path, "*", SearchOption.AllDirectories).Sum(t => new FileInfo(t).Length);
        ViewBag.TotalSize = Simpl.Extensions.StringHelper.FileSizeStr(total);
      }
      catch (Exception ex) {
        Logger.WriteError(ex, "Error calculating allocated files total size");
      }

      var files = info.GetFiles().Where(f => f.Extension.Equals(".fb2", StringComparison.OrdinalIgnoreCase))
        .OrderBy(p => p.CreationTime).ToList();
      //remove too old files from drive
      //            var oldItems = files.Where(f => f.CreationTime < DateTime.Now.AddYears(-1));
      //            foreach (var oldItem in oldItems)
      //            {
      //                System.IO.File.Delete(oldItem.FullName);
      //remove work folder & all generated content
      //            }

      //fill files data to books info
      var books = new List<BookHistoryInfo>();
      foreach (var fi in files) {
        int.TryParse(Path.GetFileNameWithoutExtension(fi.Name), out var bookId);
        books.Add(new BookHistoryInfo
          {Id = bookId, GeneratedTime = fi.CreationTime, FileName = fi.Name, Title = fi.Name});
      }

      //leave only first N in list
      return View(await DataRepository.GetHistory(books, sortBy, sortAsc, SettingsHelper.MaxRecordsToShowAtOnce));
    }

    private void TryToDelete(string path, bool isFile) {
      try {
        if (isFile) {
          if (System.IO.File.Exists(path))
            System.IO.File.Delete(path);
        }
        else {
          if (Directory.Exists(path))
            Directory.Delete(path, true);
        }
      }
      catch (Exception ex) {
        Logger.WriteError(ex, $"Error deleting history item '{path}': {ex.Message}", CommonHelper.GetClientAddress(Request));
      }
    }

    [Route("h")]
    [HttpDelete, UserTypeFilter(Roles = new[] { UserType.Administrator })]
    public ActionResult HistoryCleanup() {
      try {
        var di = new DirectoryInfo(SettingsHelper.TempDataFolder);
        foreach (FileInfo file in di.GetFiles())
          TryToDelete(file.FullName, true); //file.Delete();
        foreach (DirectoryInfo dir in di.GetDirectories())
          TryToDelete(dir.FullName, false); //dir.Delete(true);
        Logger.WriteWarning($"History was cleaned by user '{CurrentUser.Email}'",
          CommonHelper.GetClientAddress(Request));
        return new HttpStatusCodeResult(HttpStatusCode.OK, "Done");
      }
      catch (Exception ex) {
        Logger.WriteError(ex, $"Error cleaning history: {ex.Message}",
          CommonHelper.GetClientAddress(Request));
        throw;
      }
    }

    [Route("history")]
    [HttpDelete, UserTypeFilter(Roles = new[] {UserType.Administrator})]
    public ActionResult HistoryDelete(long id, string fileName) {
      try {
        fileName = Server.UrlDecode(fileName);
        fileName = Path.GetFileNameWithoutExtension(fileName);
        if (id > 0) {
          fileName = id.ToString();
        }

        var workingPath = SettingsHelper.TempDataFolder;
        TryToDelete(Path.Combine(workingPath, $"{fileName}.fb2"), true);
        TryToDelete(Path.Combine(workingPath, $"{fileName}.mobi"), true);
        TryToDelete(Path.Combine(workingPath, $"{fileName}"), false);
        Logger.WriteWarning($"History item '{fileName}' was deleted by user '{CurrentUser.Email}'", CommonHelper.GetClientAddress(Request));
        return new HttpStatusCodeResult(HttpStatusCode.OK, "Done");
      }
      catch (Exception ex) {
        Logger.WriteError(ex, $"Error deleting history item {id}/{fileName}: {ex.Message}", CommonHelper.GetClientAddress(Request));
        throw;
      }
    }

    #endregion

    #region book

    [Route("f/{id:long}")]
    public FileResult Download(long id) {
      var book = DataRepository.GetBook(id);
      if (book == null)
        throw new FileNotFoundException("Book not found in db");

      var sourceFileName = GetBookFilePath(book.FileName);
      if (!System.IO.File.Exists(sourceFileName)) {
        var archPath = Path.Combine(SettingsHelper.ArchivesPath, book.ArchiveFileName);
        if (!System.IO.File.Exists(archPath))
          throw new FileNotFoundException("Book archive not found");
        BookHelper.ExtractZipFile(archPath, book.FileName, sourceFileName);
      }

      Logger.WriteDebug($"Downloading fb2 for {book.Id} - '{book.Title}'", CommonHelper.GetClientAddress(Request));
      var fileData = System.IO.File.ReadAllBytes(sourceFileName);
      return File(fileData, System.Net.Mime.MediaTypeNames.Application.Octet,
        BookHelper.GetBookDownloadFileName(book));
    }

    [Route("m/{id:long}")]
    public FileResult Mobi(long id) {
      var book = DataRepository.GetBook(id);
      if (book == null)
        throw new FileNotFoundException("Book not found in db");
      var sourceFileName = GetBookFilePath(book.FileName);
      var resultFile = Path.ChangeExtension(sourceFileName, ".mobi");
      if (!System.IO.File.Exists(resultFile))
        throw new FileNotFoundException("File not found", resultFile);
      var fileBytes = System.IO.File.ReadAllBytes(resultFile);
      Logger.WriteDebug($"Downloading mobi for {book.Id} - '{book.Title}'", CommonHelper.GetClientAddress(Request));
      return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet,
        BookHelper.GetBookDownloadFileName(book, ".mobi"));
    }

    [Route("deliver/{id:long}")]
    public async Task<ActionResult> Deliver(long id) {
      var book = DataRepository.GetBook(id);
      if (book == null)
        throw new FileNotFoundException("Book not found in db");
      var sourceFileName = GetBookFilePath(book.FileName);
      var resultFile = Path.ChangeExtension(sourceFileName, ".mobi");
      if (!System.IO.File.Exists(resultFile)) {
        Response.StatusCode = (int) HttpStatusCode.NotFound;
        return Json("File not found", JsonRequestBehavior.AllowGet);
        //return new HttpStatusCodeResult(HttpStatusCode.NotFound);
      }

      try {
        var email = CurrentUser.Email;
        if (email.ToLower().GetHash().Equals(CommonHelper.AdminLoginHash))
          email = SettingsHelper.AdminDefaultEmail;

        Logger.WriteDebug($"Sending to {email}...");
        await CommonHelper.SendBookByMail(book.Title, resultFile, email);
        Logger.WriteInfo($"Sent {book.Title} to {email}...");
        return Json($"Done! Please check '{email}' for message with book attached.", JsonRequestBehavior.AllowGet);
      }
      catch (Exception ex) {
        Logger.WriteError(ex, $"Mail delivery failed:  {ex.Message}", CommonHelper.GetClientAddress(Request));
        Response.StatusCode = 500; // Replace .AddHeader
        return Json(ex.Message, JsonRequestBehavior.AllowGet);
        //return new HttpStatusCodeResult(HttpStatusCode.BadRequest, ex.Message);
      }
    }

    [Route("generate/{id:long}")]
    public ActionResult Generate(long id) {
      var book = DataRepository.GetBook(id);
      if (book == null)
        throw new FileNotFoundException("Book not found in db");
      var sourceFileName = GetBookFilePath(book.FileName);
      var resultFile = Path.ChangeExtension(sourceFileName, ".mobi");
      if (System.IO.File.Exists(resultFile))
        return Json(new {message = "Done"}, JsonRequestBehavior.AllowGet);

      var archPath = Path.Combine(SettingsHelper.ArchivesPath, book.ArchiveFileName);
      if (!System.IO.File.Exists(archPath))
        throw new FileNotFoundException("Book archive not found");

      if (!System.IO.File.Exists(sourceFileName))
        BookHelper.ExtractZipFile(archPath, book.FileName, sourceFileName);
      var taskId = ConvertQueue.ConvertBookNoWait(sourceFileName);
      return Json(new {
        message = "Please wait a bit and refresh the page",
        taskId
      }, JsonRequestBehavior.AllowGet);
    }

    [Route("generate/status")]
    public ActionResult GetConvertStatus(Guid taskId) {
      var task = ConvertQueue.GetTaskStatus(taskId);
      return Json(new {result = task?.Result, output = string.Join("<br>", task?.Output)}, JsonRequestBehavior.AllowGet);
    }

    [Route("{bookId:long}")]
    public ActionResult Read(long bookId) {
      return RedirectPermanent($"{bookId}/toc.html");
    }

    [Route("{bookId:long}/Images/{imageName}")]
    public ActionResult BookImage(long bookId, string imageName) {
      return Book(bookId, $"Images/{imageName}");
    }

    [Route("{bookId:long}/{fileName}")]
    public ActionResult Book(long bookId, string fileName) {
      var filePath = Path.Combine(SettingsHelper.TempDataFolder, $"{bookId}\\{fileName}");
      if (!System.IO.File.Exists(filePath)) {
        if ("cover.jpg".Equals(fileName, StringComparison.OrdinalIgnoreCase) && SettingsHelper.GenerateBookDetails) {
          var book = DataRepository.GetBook(bookId);
          var task = new Task(() => PrepareBookDetails(book, true));
          BackgroundTasks.EnqueueAction(task);
          task.Wait(SettingsHelper.GenerateBookTimeout * 1000);
          if (System.IO.File.Exists(filePath)) {
            return File(filePath, System.Net.Mime.MediaTypeNames.Image.Jpeg);
          }
        }
        return new RedirectResult("~/Images/NoImage.jpg", true);
        // return new HttpNotFoundResult("File not found.");
      }

      var fileExt = Path.GetExtension(fileName.ToLower());
      switch (fileExt) {
        case ".jpg":
        case ".jpeg":
        case ".png":
          return File(filePath, System.Net.Mime.MediaTypeNames.Image.Jpeg);
        default:
          return File(filePath, System.Net.Mime.MediaTypeNames.Text.Html);
      }
    }

    private string PrepareBookDetails(BookDetailedInfo book, bool previewMode) {
      if (book == null)
        throw new FileNotFoundException("Book not found in db");

      var sourceFileName = GetBookFilePath(book.FileName);
      var detailsFolder = $"{Path.GetDirectoryName(sourceFileName)}/{Path.GetFileNameWithoutExtension(sourceFileName)}";
      if (string.IsNullOrWhiteSpace(detailsFolder))
        throw new DirectoryNotFoundException("Invalid Details folder name");
      var folderExists = Directory.Exists(detailsFolder);
      if (previewMode && folderExists)
        return sourceFileName;
      Directory.CreateDirectory(detailsFolder);

      try {
        if (!System.IO.File.Exists(sourceFileName)) {
          var archPath = Path.Combine(SettingsHelper.ArchivesPath, book.ArchiveFileName);
          if (!System.IO.File.Exists(archPath))
            throw new FileNotFoundException("Book archive not found");
          BookHelper.ExtractZipFile(archPath, book.FileName, sourceFileName);
        }

        var coverImagePath = Path.Combine(detailsFolder, "cover.jpg");
        if (!System.IO.File.Exists(coverImagePath))
          BookHelper.SaveCover(sourceFileName, coverImagePath);

        var annotationsPath = Path.Combine(detailsFolder, "annotation.txt");
        if (string.IsNullOrWhiteSpace(book.Description))
          book.Description = BookHelper.GetAnnotation(sourceFileName, annotationsPath);
        ViewBag.Title = book.Title;

        return sourceFileName;
      }
      finally {
        if (previewMode && !string.IsNullOrEmpty(sourceFileName) && System.IO.File.Exists(sourceFileName)) {
          try {
            System.IO.File.Delete(sourceFileName);
          }
          catch {
            // ignored
          }
        }
      }
    }

    [Route("d/{id:long}")]
    public ActionResult Details(long id) {
      var book = DataRepository.GetBook(id);
      var sourceFileName = PrepareBookDetails(book, false);
      var mobiFile = Path.ChangeExtension(sourceFileName, ".mobi");
      if (System.IO.File.Exists(mobiFile)) {
        ViewBag.MobiFileFound = true;
        ViewBag.MobiFileSize = Simpl.Extensions.StringHelper.FileSizeStr(new FileInfo(mobiFile).Length);
      }
      else
        ViewBag.MobiFileFound = false;

      Logger.WriteDebug($"Details for {book.Id} - '{book.Title}'", CommonHelper.GetClientAddress(Request));
      return View(book);
    }

    [Route("books"), HttpDelete, UserTypeFilter(Roles = new[] {UserType.Administrator})]
    public async Task<ActionResult> Delete(long id) {
      try {
        var deletedRows = await DataRepository.DeleteBookById(id);
        if (deletedRows == 0)
          throw new Exception($"Book with Id {id} not found.");
        Logger.WriteWarning($"Book with Id '{id}' was deleted by user '{CurrentUser.Email}'", CommonHelper.GetClientAddress(Request));
        return new HttpStatusCodeResult(HttpStatusCode.OK, "Done");
      }
      catch (Exception ex) {
        Logger.WriteError(ex, $"Error deleting book with Id {id}: {ex.Message}", CommonHelper.GetClientAddress(Request));
        throw;
      }
    }
    
    #endregion

    #region sequence

    [Route("s/{id:long}")]
    public ActionResult Sequence(long id) {
      var data = DataRepository.GetSequenceData(id, CurrentUser?.Id);
      ViewBag.Title = data.Value;
      ViewBag.SequenceMode = true;
      return View(data);
    }

    #endregion

    #region author

    [Route("a/{id:long}")]
    public ActionResult Author(long id) {
      var data = DataRepository.GetAuthorData(id, CurrentUser?.Id);
      ViewBag.Title = data.FullName;
      ViewBag.AuthorMode = true;
      return View(data);
    }

    #endregion

    #region upload/convert

    [Route("c")]
    public ActionResult UploadFile() {
      ViewBag.maxRequestLength = SettingsHelper.MaxRequestLength;
      return View();
    }

    // [HttpPost]
    // public ActionResult HandleFileUpload() {
    //     var originFileName = Server.UrlDecode(Request.Headers["X-File-Name"]);
    //     if (string.IsNullOrEmpty(originFileName))
    //         return Json(new { success = false });
    //     var originRealPath = $"{SettingsHelper.TempDataFolder}\\{BookHelper.GetCorrectedFileName(originFileName)}";
    //     if (string.IsNullOrEmpty(originRealPath))
    //         return Json(new { success = false });
    //     var mobiDisplayName = Path.ChangeExtension(originFileName, ".mobi");
    //     var mobiRelativePath = GetLinkToFile(mobiDisplayName);
    //     if (System.IO.File.Exists(originRealPath)) {
    //         var mobiRealPath = Path.ChangeExtension(originRealPath, ".mobi");
    //         if (System.IO.File.Exists(mobiRealPath))
    //             return Json(new { success = true, link = mobiRelativePath, fileName = mobiDisplayName });
    //         System.IO.File.Delete(originRealPath); //delete old uploaded file to re-convert new one
    //     }
    //     using (var fileStream = new FileStream(originRealPath, FileMode.OpenOrCreate))
    //         Request.InputStream.CopyTo(fileStream);
    //
    //     if (!BookHelper.ConvertBook(originRealPath))
    //         throw new ArgumentException("Error converting book for kindle");
    //
    //     return Json(new { success = true, link = mobiRelativePath, fileName = mobiDisplayName });
    // }

    [HttpPost]
    public ActionResult HandleMultipleFileUpload() {
      var names = new List<string>();
      var links = new List<string>();
      try {
        foreach (string fileName in Request.Files) {
          var file = Request.Files[fileName];
          if (file == null || file.ContentLength <= 0 || string.IsNullOrEmpty(file.FileName))
            continue;
          var originRealPath = $"{SettingsHelper.TempDataFolder}\\{BookHelper.GetCorrectedFileName(file.FileName)}";
          var mobiDisplayName = Path.ChangeExtension(BookHelper.GetCorrectedFileName(file.FileName), ".mobi");
          var mobiRelativePath = GetLinkToFile(mobiDisplayName);
          if (System.IO.File.Exists(originRealPath)) {
            var mobiRealPath = Path.ChangeExtension(originRealPath, ".mobi");
            if (System.IO.File.Exists(mobiRealPath)) {
              System.IO.File.Delete(mobiRealPath); //delete old converted file
              // names.Add(mobiDisplayName);
              // links.Add(mobiRelativePath);
              // continue;
            }

            System.IO.File.Delete(originRealPath); //delete old uploaded file to re-convert new one
          }

          file.SaveAs(originRealPath);

          if (!ConvertQueue.ConvertBook(originRealPath))
            throw new ArgumentException("Error converting book for kindle");

          names.Add(mobiDisplayName);
          links.Add(mobiRelativePath);
        }

        if (names.Count == 0 || links.Count == 0) {
          return Json(new {Message = "No files processed."});
        }

        return Json(new {Message = "OK", names, links});
      }
      catch (Exception ex) {
        return Json(new {Message = "Error saving file."});
      }
    }

    public FileResult GetConverter() {
      var name = Path.GetFileName(SettingsHelper.ConverterPath);
      return File(SettingsHelper.ConverterPath, System.Net.Mime.MediaTypeNames.Application.Octet, name);
    }

    #endregion

    #region PasswordChange

    [Route("passwd")]
    [HttpGet, UserTypeFilter(Roles = new[] {UserType.Administrator, UserType.User})]
    public ActionResult PasswordChange(long id) {
      var user = UserRepository.GetUserById(id) ?? CurrentUser;
      var item = new ChangePasswordModel {Id = user.Id, Username = user.Email, HasPassword = user.HasPassword};
      return PartialView("_PasswordChange", item);
    }

    [Route("passwd")]
    [HttpPost, UserTypeFilter(Roles = new[] {UserType.Administrator, UserType.User})]
    public ActionResult PasswordChange(ChangePasswordModel model) {
      if (ModelState.IsValid) {
        if (UserRepository.SetUserPassword(model.Id, model.OldPassword, model.NewPassword)) {
          //_notificationManager.NotifyPasswordChanged(model.Id, 0);
          var res = $"Password of user '{model.Username}' was modified by '{CurrentUser.Email}'";
          Logger.WriteInfo(res, CommonHelper.GetClientAddress(Request));
          ModelState.Clear();
          return Json(new {message = res, id = model.Id});
        }

        ModelState.AddModelError("", "The current password is incorrect or the new password is invalid.");
      }

      return PartialView("_PasswordChange", model);
    }

    #endregion PasswordChange

    #region Users

    [Route("users")]
    [HttpGet, UserTypeFilter(Roles = new[] {UserType.Administrator})]
    public ActionResult Users() {
      return View(UserRepository.GetUsers(string.Empty));
    }

    [Route("usersp")]
    [HttpGet, UserTypeFilter(Roles = new[] {UserType.Administrator})]
    public ActionResult UsersSearch(string key) {
      return PartialView("UsersPartial", UserRepository.GetUsers(key));
    }

    [Route("userdel")]
    [HttpGet, UserTypeFilter(Roles = new[] {UserType.Administrator})]
    public ActionResult UserDelete(long id) {
      try {
        var user = UserRepository.GetUserById(id);
        if (user == null)
          return new HttpStatusCodeResult(HttpStatusCode.NotFound, "User was not found");
        UserRepository.DeleteUser(user.Id);
        Logger.WriteWarning($"User '{user.Email}' was deleted by user '{CurrentUser.Email}'", CommonHelper.GetClientAddress(Request));
        return new HttpStatusCodeResult(HttpStatusCode.OK, "Done");
      }
      catch (Exception ex) {
        Logger.WriteError(ex, $"Error deleting user {id}: {ex.Message}", CommonHelper.GetClientAddress(Request));
        throw;
      }
    }

    [Route("useradd")]
    [HttpGet, UserTypeFilter(Roles = new[] {UserType.Administrator})]
    public ActionResult UserEdit(long id = 0) {
      var user = UserRepository.GetUserById(id);
      ViewBag.TimeTrackUsers = TimeTrackRepository.GetAllUsers();
      return user != null
        ? PartialView(user)
        : PartialView(new UserProfile {
          UserType = UserType.User,
        });
    }

    [Route("useradd")]
    [HttpPost, UserTypeFilter(Roles = new[] {UserType.Administrator})]
    public ActionResult UserEdit(UserProfile model) {
      if (ModelState.IsValid) {
        //var old = UserRepository.GetUserById(model.Id);
        //if (UserRepository.IsUserContactsUnique(model.Contacts, model.Id))
        {
          Logger.WriteInfo(string.Format("User '{0}' was {2} by '{1}'", model.Email,
            CurrentUser.Email, model.Id > 0 ? "modified" : "created"), CommonHelper.GetClientAddress(Request));
          UserRepository.SaveUser(model);

          if (model.Id == CurrentUser.Id) {
            CurrentUser.MergeWith(model);
          }

          ModelState.Clear();
          return Json(new {message = "User successfully saved.", itemId = model.Id});
        }
        //ModelState.AddModelError("", "User email or phone already registered. Please enter another one.");
      }

      ViewBag.TimeTrackUsers = TimeTrackRepository.GetAllUsers();
      return PartialView(model);
    }

    [Route("passwdreset")]
    [HttpGet, UserTypeFilter(Roles = new[] {UserType.Administrator})]
    public string ResetUserPassword(long id) {
      try {
        if (UserRepository.SetUserPassword(id, null))
          return "Password was cleaned for user";
        return "User not found by Id";
      }
      catch (Exception ex) {
        return ex.Message;
      }
    }

    #endregion Users

    #region TimeTrack

    [Route("t")]
    [HttpGet, UserTypeFilter(Roles = new[] {UserType.Administrator, UserType.User})]
    public ActionResult Time(long id = 0) {
      ViewBag.Id = id;
      return View(CheckItem.FromUserCheckData(TimeTrackRepository.GetLastCheckInOut(id > 0 ? id : CurrentUser.TimeTrackId)));
    }

    [Route("tp")]
    [HttpGet, UserTypeFilter(Roles = new[] {UserType.Administrator, UserType.User})]
    public ActionResult TimePartial(long id = 0) {
      ViewBag.Id = id;
      return PartialView("TimePartial", CheckItem.FromUserCheckData(TimeTrackRepository.GetLastCheckInOut(id > 0 ? id : CurrentUser.TimeTrackId)));
    }

    [Route("in")]
    [HttpGet, UserTypeFilter(Roles = new[] {UserType.Administrator, UserType.User})]
    public ActionResult CheckIn(long id = 0) {
      TimeTrackRepository.CheckIn(id > 0 ? id : CurrentUser.TimeTrackId);
      Logger.WriteInfo("User checked IN", CommonHelper.GetClientAddress(Request));
      return new HttpStatusCodeResult(HttpStatusCode.OK, "Done");
    }

    [Route("out")]
    [HttpGet, UserTypeFilter(Roles = new[] {UserType.Administrator, UserType.User})]
    public ActionResult CheckOut(long id = 0) {
      TimeTrackRepository.CheckOut(id > 0 ? id : CurrentUser.TimeTrackId);
      Logger.WriteInfo("User checked OUT", CommonHelper.GetClientAddress(Request));
      return new HttpStatusCodeResult(HttpStatusCode.OK, "Done");
    }

    #endregion TimeTrack

    #region flibusta recomendations

    [Route("fav")]
    [HttpGet, UserTypeFilter(Roles = new[] {UserType.Administrator, UserType.User})]
    public async Task<ActionResult> Favorites(long id = 0, int pageNum = 0, string k = null) {
      if ((id == 0 || id != CurrentUser.Id) && CurrentUser.UserType != UserType.Administrator) {
        throw new ArgumentException("You passed wrong UserId.");
      }

      ViewBag.Id = id;
      ViewBag.Key = k;
      var books = await DataRepository.GetFavorites(id, pageNum, SettingsHelper.MaxRecordsToShowAtOnce, k);
      return View(books);
    }

    [Route("favupdate")]
    [HttpGet, UserTypeFilter(Roles = new[] {UserType.Administrator, UserType.User})]
    public async Task<string> UpdateFavorites(int userId) {
      try {
        if (userId <= 0)
          throw new ArgumentOutOfRangeException(nameof(userId));

        int flibustaId;
        var userName = CurrentUser.Email;
        if (userId == CurrentUser.Id)
          flibustaId = CurrentUser.FlibustaId;
        else {
          if (CurrentUser.UserType != UserType.Administrator)
            throw new Exception("You are not allowed to fetch other users data.");

          var user = UserRepository.GetUserById(userId);
          if (user == null)
            throw new Exception($"User not found by id: {userId}");

          flibustaId = user.FlibustaId;
          userName = user.Email;
        }

        if (flibustaId == 0)
          throw new Exception("User don't have FlibustaId assigned.");

        var culture = CultureInfo.GetCultureInfo("en-US");
        var booksFetched = 0;
        var pageNum = 0;
        var fetchMore = true;
        using (var client = new WebClient()) {
          while (fetchMore) {
            var url = $"{SettingsHelper.FlibustaLink}/rec?page={pageNum++}&view=recs&user={flibustaId}&udata=id";
            var pageData = await client.DownloadStringTaskAsync(url);
            var bytes = Encoding.Default.GetBytes(pageData);
            pageData = Encoding.UTF8.GetString(bytes);
            var regex = new Regex(@"<tr>[\s\S]*?<td><a href=\""\/b\/([\d]+)\"">(.+)<\/a>[\s\S]*?user\/([\d]+)[\s\S]*?<td>(.+)<\/td>[\s\S]*?\/tr>");
            var matches = regex.Matches(pageData);
            foreach (Match m in matches) {
              if (!m.Success || m.Groups.Count != 5)
                throw new InvalidDataException("Error parsing page data");

              var bookId = long.Parse(m.Groups[1].Value);
              // var bookTitle = m.Groups[2].Value;
              var flibustaUserId = long.Parse(m.Groups[3].Value);
              if (flibustaUserId != flibustaId)
                throw new InvalidDataException("Wrong UserId value received.");
              var dateAdded = DateTime.Parse(m.Groups[4].Value, culture);

              var bookFound = await DataRepository.GetFavoriteId(bookId, userId, dateAdded);
              if (bookFound != 0) {
                fetchMore = false;
                break;
              }

              await DataRepository.SaveFavorite(bookId, userId, dateAdded);
              booksFetched++;
            }
          }
        }

        return $"Fetched {booksFetched} books from {pageNum} processed page(s) for user '{userName}' ({userId}/{flibustaId}).";
      }
      catch (Exception ex) {
        Logger.WriteError(ex, $"Error fetching favorites for user {userId}: {ex.Message}", CommonHelper.GetClientAddress(Request));
        return ex.Message;
      }
    }

    [HttpGet, Route("favtoggle"), UserTypeFilter(Roles = new[] {UserType.Administrator, UserType.User})]
    public async Task<ActionResult> FavoriteToggle(long bookId) {
      try {
        var favId = await DataRepository.GetFavoriteId(bookId, CurrentUser.Id, null);
        if (favId == 0) {
          //add fav
          favId = await DataRepository.SaveFavorite(bookId, CurrentUser.Id, DateTime.Now);
          Logger.WriteWarning($"Favorite item '{favId}' was added by user '{CurrentUser.Email}'", CommonHelper.GetClientAddress(Request));
        }
        else {
          //delete fav
          return await FavoriteDelete(favId);
        }

        return new HttpStatusCodeResult(HttpStatusCode.OK);
      }
      catch (Exception ex) {
        Logger.WriteError(ex, $"Error adding favorite item: {ex.Message}", CommonHelper.GetClientAddress(Request));
        return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, ex.Message);
      }
    }

    [HttpDelete, Route("favdelete"), UserTypeFilter(Roles = new[] {UserType.Administrator})]
    public async Task<ActionResult> FavoriteDelete(long id) {
      try {
        var count = await DataRepository.DeleteFavorite(id);
        if (count != 0) {
          Logger.WriteWarning($"Favorite item '{id}' was deleted by user '{CurrentUser.Email}'", CommonHelper.GetClientAddress(Request));
          return new HttpStatusCodeResult(HttpStatusCode.OK, "Done");
        }

        return new HttpStatusCodeResult(HttpStatusCode.NotFound, $"Favorite with id {id} not found.");
      }
      catch (Exception ex) {
        Logger.WriteError(ex, $"Error deleting favorite item {id}: {ex.Message}", CommonHelper.GetClientAddress(Request));
        return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, ex.Message);
      }
    }

    #endregion flibusta recomendations
  }
}