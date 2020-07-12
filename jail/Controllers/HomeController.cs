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

namespace jail.Controllers
{
    [OutputCache(NoStore = true, Duration = 0, VaryByParam = "None")]
    [ActionLogger, SessionRestore]
    public class HomeController : Controller
    {

        #region Overrides

        protected override void HandleUnknownAction(string actionName)
        {
            Logger.WriteWarning($"HandleUnknownAction - {actionName}", CommonHelper.GetClientAddress());
            base.HandleUnknownAction(actionName);
        }

        protected override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            base.OnActionExecuted(filterContext);
        }

        protected override void EndExecute(IAsyncResult asyncResult)
        {
            var appPath = Request.ApplicationPath.TrimEnd('/');
            var localPath = Request.Url.PathAndQuery;
            if (!string.IsNullOrWhiteSpace(appPath))
                localPath = localPath.Substring(appPath.Length);
            ViewBag.Url = AppBaseUrl + localPath.TrimStart('/'); ;
            base.EndExecute(asyncResult);
        }

        #endregion Overrides

        public new RedirectToRouteResult RedirectToAction(string action, string controller)
        {
            return base.RedirectToAction(action, controller);
        }

        #region Logging

        protected override void OnException(ExceptionContext filterContext)
        {
            //if (filterContext.Exception != null)
            //{
            //    if (filterContext.Exception is TaskCanceledException ||
            //        filterContext.Exception is OperationCanceledException)
            //        return;
            //}
            var name = CommonHelper.GetActionLogName(filterContext.HttpContext.Request);
            Logger.WriteError(filterContext.Exception,
                $"{name} - {(filterContext.Exception != null ? filterContext.Exception.Message : null)}", CommonHelper.GetClientAddress());
            base.OnException(filterContext);
        }

        //protected override IAsyncResult BeginExecute(RequestContext requestContext, AsyncCallback callback, object state)
        //{
        //    Logger.WriteTrace(CommonHelper.GetActionLogName(requestContext.HttpContext.Request), CommonHelper.GetClientAddress(), CommonHelper.CurrentIdentityName);
        //    return base.BeginExecute(requestContext, callback, state);
        //}

        public UserProfile CurrentUser {
            get {
                return Request.IsAuthenticated
                    ? (UserProfile)ControllerContext.HttpContext.Session["User"]
                    : null;
            }
            set {
                ControllerContext.HttpContext.Session["User"] = value;
            }
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

        private string GetLinkToFile(string fileName)
        {
            return AppBaseUrl + fileName.Replace(Server.MapPath("~"), "").Replace('\\', '/').TrimStart('/');
        }

        #endregion

        #region basic methods

        public static string GetVersionBuildTime()
        {
            var ver = Assembly.GetExecutingAssembly().GetName().Version;
            var buildTime = new DateTime(2000, 1, 1).AddDays(ver.Build).AddSeconds(ver.Revision * 2);
            //            if (TimeZone.IsDaylightSavingTime(DateTime.Now, TimeZone.CurrentTimeZone.GetDaylightChanges(DateTime.Now.Year)))
            //                buildTime = buildTime.AddHours(1);
            return $"{buildTime:yyyy-MM-dd HH:mm}";
        }

        public static string GetVersionString()
        {
            return $"Version: {Assembly.GetExecutingAssembly().GetName().Version}";
        }

        [Route("about")]
        public ActionResult About()
        {
            return View();
        }

        [Route("contact")]
        public ActionResult Contact()
        {
            return View();
        }

        #endregion

        #region Login-logout

        [Route("login")]
        public ActionResult Login(string returnUrl)
        {
            HttpCookie authCookie = HttpContext.Request.Cookies.Get(FormsAuthentication.FormsCookieName);
            if (authCookie != null && !string.IsNullOrEmpty(authCookie.Value))
            {
                var ticket = FormsAuthentication.Decrypt(authCookie.Value);
                if (ticket != null && !ticket.Expired && !string.IsNullOrWhiteSpace(ticket.Name))
                {
                    //restore from cookie
                    var user = UserRepository.GetUser(ticket.Name);
                    if (user != null)
                    {
                        CurrentUser = user;
                        ControllerContext.HttpContext.Session.Timeout = 24 * 60;
                        FormsAuthentication.SetAuthCookie(ticket.Name, true);
                        Logger.WriteInfo($"{user.UserType} session restored", CommonHelper.GetClientAddress(), user.Email);
                        return !string.IsNullOrWhiteSpace(returnUrl)
                            ? (ActionResult)Redirect(returnUrl)
                            : RedirectToAction(user.UserType == UserType.Administrator ? "Log" : "Index", "Home");
                    }
                }
            }
            return View(new LogOnModel { RedirectUrl = returnUrl });
        }

        [Route("login")]
        [HttpPost]
        public ActionResult Login(LogOnModel model)
        {
            if (ModelState.IsValid)
            {
                var user = UserRepository.GetUser(model.UserName, model.Password);
                if (user == null)
                {
                    ModelState.AddModelError("", "The user name or password provided is incorrect.");
                }
                //else if (user.UserType != UserType.Administrator && user.Id != 0)
                //{
                //    ModelState.AddModelError("", string.Format("'{0}' user type is not allowed to login", user.UserType));
                //}
                else
                {
                    Logger.WriteInfo($"{user.UserType} logged in", CommonHelper.GetClientAddress(), model.UserName);
                    CurrentUser = user;
                    ControllerContext.HttpContext.Session.Timeout = 24 * 60;
                    //if (model.RememberMe)
                    //{
                    var ticket = new FormsAuthenticationTicket(1, model.UserName,
                        DateTime.Now, DateTime.Now.AddDays(7), false,
                        $"{user.Id},{user.UserType}",
                        FormsAuthentication.FormsCookiePath);
                    var strEncryptedTicket = FormsAuthentication.Encrypt(ticket);
                    var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, strEncryptedTicket)
                    {
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
        public ActionResult Logout()
        {
            if (CurrentUser != null && CurrentUser.UserType != UserType.Administrator)
                Logger.WriteInfo("Logout", CommonHelper.GetClientAddress(), User.Identity.Name);
            HttpContext.Session["User"] = null;
            Request.Cookies.Remove(FormsAuthentication.FormsCookieName);
            FormsAuthentication.SignOut();
            return RedirectToAction("Index");
        }

        #endregion Login-logout

        #region Logging

        [Route("log"), SkipLogging]
        [UserTypeFilter(Roles = new[] { UserType.Administrator })]
        public ActionResult Log(string k = null, SystemLog.LogItemType t = SystemLog.LogItemType.All)
        {
            ViewBag.DebugMode = Debugger.IsAttached;
            ViewBag.Key = k;
            ViewBag.SearchType = t;
            return View(SystemRepository.GetErrorLogData(SettingsHelper.MaxRecordsToShowAtOnce, k, t));
        }

        [Route("logp"), SkipLogging]
        [HttpGet, UserTypeFilter(Roles = new[] { UserType.Administrator })]
        public ActionResult LogPartial(string key, SystemLog.LogItemType searchType)
        {
            ViewBag.DebugMode = Debugger.IsAttached;
            ViewBag.Key = key;
            ViewBag.SearchType = searchType;
            return PartialView(SystemRepository.GetErrorLogData(SettingsHelper.MaxRecordsToShowAtOnce, key, searchType));
        }

        [Route("clrlog"), SkipLogging]
        [HttpPost, UserTypeFilter(Roles = new[] { UserType.Administrator })]
        public string ClearSelectedLog(string selection)
        {
            try
            {
                selection = selection.Trim();
                if (string.IsNullOrWhiteSpace(selection))
                    return "Empty selection!";
                var removed = SystemRepository.ClearByMessagePart(selection);
                return $"Removed: {removed} records by text: '{selection}'";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        [Route("calclog"), SkipLogging]
        [HttpPost, UserTypeFilter(Roles = new[] { UserType.Administrator })]
        public string CalcSelectedLog(string selection)
        {
            try
            {
                selection = selection.Trim();
                if (string.IsNullOrWhiteSpace(selection))
                    return "Empty selection!";
                var found = SystemRepository.CalcByMessagePart(selection);
                return $"Found {found} records by text: '{selection}'";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        #endregion Logging

        #region Search

        public async Task<ActionResult> Index(string k = null, string l = "ru")
        {
            ViewBag.Key = k;
            ViewBag.Lang = l;
            return View(string.IsNullOrWhiteSpace(k) ? new List<BookInfo>() :
                await DataRepository.GetSearchDataAsync(k, l));
        }

        [ValidateInput(false)]
        [Route("search")]
        public async Task<ActionResult> SearchResults(string k = null, string l = "ru")
        {
            return PartialView(string.IsNullOrWhiteSpace(k) ? new List<BookInfo>() :
                await DataRepository.GetSearchDataAsync(k, l));
        }

        [Route("history")]
        public async Task<ActionResult> History()
        {
            var path = Server.MapPath("~/b");
            var info = new DirectoryInfo(path);

            try
            {
                long total = Directory.GetFiles(path, "*", SearchOption.AllDirectories).Sum(t => new FileInfo(t).Length);
                ViewBag.TotalSize = Simpl.Extensions.StringHelper.FileSizeStr(total);
            }
            catch (Exception ex)
            {
                Logger.WriteError(ex, "Error calculating allocated files total size");
            }
            var files = info.GetFiles().Where(f => f.Extension.Equals(".fb2", StringComparison.OrdinalIgnoreCase)).OrderBy(p => p.CreationTime).ToList();
            //remove too old files from drive
            //            var oldItems = files.Where(f => f.CreationTime < DateTime.Now.AddYears(-1));
            //            foreach (var oldItem in oldItems)
            //            {
            //                System.IO.File.Delete(oldItem.FullName);
            //remove work folder & all generated content
            //            }

            //fill files data to books info
            var books = new List<BookHistoryInfo>();
            foreach (var fi in files)
            {
                int.TryParse(Path.GetFileNameWithoutExtension(fi.Name), out var bookId);
                books.Add(new BookHistoryInfo { Id = bookId, GeneratedTime = fi.CreationTime, FileName = fi.Name, Title = fi.Name });
            }

            //leave only first N in list
            return View(await DataRepository.GetHistoryAsync(books.Take(SettingsHelper.MaxRecordsToShowAtOnce)));
        }

        private static void TryToDelete(string path, bool isFile)
        {
            try
            {
                if (isFile)
                {
                    if (System.IO.File.Exists(path))
                        System.IO.File.Delete(path);
                }
                else
                {
                    if (Directory.Exists(path))
                        Directory.Delete(path, true);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteError(ex, $"Error deleting history item '{path}': {ex.Message}", CommonHelper.GetClientAddress());
            }
        }

        [Route("history")]
        [HttpDelete, UserTypeFilter(Roles = new[] { UserType.Administrator })]
        public ActionResult HistoryDelete(long id, string fileName)
        {
            try
            {
                fileName = Server.UrlDecode(fileName);
                fileName = Path.GetFileNameWithoutExtension(fileName);
                if (id > 0)
                {
                    fileName = id.ToString();
                }
                var workingPath = Server.MapPath("~/b");
                TryToDelete(Path.Combine(workingPath, $"{fileName}.fb2"), true);
                TryToDelete(Path.Combine(workingPath, $"{fileName}.mobi"), true);
                TryToDelete(Path.Combine(workingPath, $"{fileName}"), false);
                Logger.WriteWarning($"History item '{fileName}' was deleted by user '{CurrentUser.Email}'", CommonHelper.GetClientAddress());
                return new HttpStatusCodeResult(HttpStatusCode.OK, "Done");
            }
            catch (Exception ex)
            {
                Logger.WriteError(ex, $"Error deleting history item {id}/{fileName}: {ex.Message}", CommonHelper.GetClientAddress());
                throw;
            }
        }

        #endregion

        #region book

        [Route("f/{id:long}")]
        public FileResult Download(long id)
        {
            var book = DataRepository.GetBook(id);
            if (book == null)
                throw new FileNotFoundException("Book not found in db");

            var sourceFileName = Server.MapPath($"~/b/{book.FileName}");
            if (!System.IO.File.Exists(sourceFileName))
            {
                var archPath = Path.Combine(SettingsHelper.ArchivesPath, book.ArchiveFileName);
                if (!System.IO.File.Exists(archPath))
                    throw new FileNotFoundException("Book archive not found");
                BookHelper.ExtractZipFile(archPath, book.FileName, sourceFileName);
            }
            Logger.WriteDebug($"Downloading fb2 for {book.Id} - '{book.Title}'", CommonHelper.GetClientAddress());
            var fileData = System.IO.File.ReadAllBytes(sourceFileName);
            return File(fileData, System.Net.Mime.MediaTypeNames.Application.Octet,
                BookHelper.GetBookDownloadFileName(book));
        }

        [Route("m/{id:long}")]
        public FileResult Mobi(long id)
        {
            var book = DataRepository.GetBook(id);
            if (book == null)
                throw new FileNotFoundException("Book not found in db");
            var sourceFileName = Server.MapPath($"~/b/{book.FileName}");
            var resultFile = Path.ChangeExtension(sourceFileName, ".mobi");
            if (!System.IO.File.Exists(resultFile))
                throw new FileNotFoundException("File not found", resultFile);
            var fileBytes = System.IO.File.ReadAllBytes(resultFile);
            Logger.WriteDebug($"Downloading mobi for {book.Id} - '{book.Title}'", CommonHelper.GetClientAddress());
            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet,
                BookHelper.GetBookDownloadFileName(book, ".mobi"));
        }

        [Route("deliver/{id:long}")]
        public async Task<ActionResult> Deliver(long id)
        {
            var book = DataRepository.GetBook(id);
            if (book == null)
                throw new FileNotFoundException("Book not found in db");
            var sourceFileName = Server.MapPath($"~/b/{book.FileName}");
            var resultFile = Path.ChangeExtension(sourceFileName, ".mobi");
            if (!System.IO.File.Exists(resultFile))
            {
                Response.StatusCode = (int)HttpStatusCode.NotFound;
                return Json("File not found", JsonRequestBehavior.AllowGet);
                //return new HttpStatusCodeResult(HttpStatusCode.NotFound);
                //throw new FileNotFoundException("File not found", resultFile);
            }
            try
            {
                var email = CurrentUser.Email;
                if (email.ToLower().GetHash().Equals(CommonHelper.AdminLoginHash))
                    email = SettingsHelper.AdminDefaultEmail;

                await CommonHelper.SendBookByMail(book.Title, resultFile, email);
                return Json("Please check your Kindle for new doc", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500; // Replace .AddHeader
                return Json(ex.Message, JsonRequestBehavior.AllowGet);
                //                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, ex.Message);
            }
        }

        [Route("generate/{id:long}")]
        public ActionResult Generate(long id)
        {
            var book = DataRepository.GetBook(id);
            if (book == null)
                throw new FileNotFoundException("Book not found in db");
            var sourceFileName = Server.MapPath($"~/b/{book.FileName}");
            var resultFile = Path.ChangeExtension(sourceFileName, ".mobi");
            if (System.IO.File.Exists(resultFile))
                return Json("Done", JsonRequestBehavior.AllowGet);

            var archPath = Path.Combine(SettingsHelper.ArchivesPath, book.ArchiveFileName);
            if (!System.IO.File.Exists(archPath))
                throw new FileNotFoundException("Book archive not found");

            if (!System.IO.File.Exists(sourceFileName))
                BookHelper.ExtractZipFile(archPath, book.FileName, sourceFileName);
            BookHelper.ConvertBookNoWait(sourceFileName);
            return Json("Please wait a bit and refresh the page", JsonRequestBehavior.AllowGet);
        }

        [Route("r/{id:long}")]
        public ActionResult Read(long id)
        {
            var book = DataRepository.GetBook(id);
            if (book == null)
                throw new FileNotFoundException("Book not found in db");

            var sourceFileName = Server.MapPath($"~/b/{book.FileName}");
            if (!System.IO.File.Exists(sourceFileName))
            {
                var archPath = Path.Combine(SettingsHelper.ArchivesPath, book.ArchiveFileName);
                if (!System.IO.File.Exists(archPath))
                    throw new FileNotFoundException("Book archive not found");
                BookHelper.ExtractZipFile(archPath, book.FileName, sourceFileName);
            }

            var detailsFolder = Path.Combine(Path.GetDirectoryName(sourceFileName), Path.GetFileNameWithoutExtension(sourceFileName));
            if (string.IsNullOrWhiteSpace(detailsFolder))
                throw new DirectoryNotFoundException("Details folder is empty");
            Directory.CreateDirectory(detailsFolder);
            var readingPath = Path.Combine(detailsFolder, "toc.html");
            if (!System.IO.File.Exists(readingPath))
            {
                throw new FileNotFoundException("Book not found, please prepare it first");
                //BookHelper.Transform(tempFile, readingPath, Server.MapPath("~/xhtml.xsl"));
                //if (!BookHelper.ConvertBook(sourceFileName))
                //    throw new ArgumentException("Error converting book for kindle");
            }
            ViewBag.Title = book.Title;
            Logger.WriteDebug($"Reading book {book.Id} - '{book.Title}'", CommonHelper.GetClientAddress());
            return new RedirectResult(Path.Combine(@"../" + readingPath.Replace(Server.MapPath("~"), "").Replace('\\', '/')));
            //return new FilePathResult(GetLinkToFile(readingPath), "text/html");
            //ViewBag.BookContent = GetLinkToFile(readingPath);//Path.Combine(@"../" + readingPath.Replace(Server.MapPath("~"), "").Replace('\\', '/'));
            //return View(book);
        }

        [Route("d/{id:long}")]
        public ActionResult Details(long id)
        {
            var book = DataRepository.GetBook(id);
            if (book == null)
                throw new FileNotFoundException("Book not found in db");

            var archPath = Path.Combine(SettingsHelper.ArchivesPath, book.ArchiveFileName);
            if (!System.IO.File.Exists(archPath))
                throw new FileNotFoundException("Book archive not found");

            var sourceFileName = Server.MapPath($"~/b/{book.FileName}");
            if (!System.IO.File.Exists(sourceFileName))
                BookHelper.ExtractZipFile(archPath, book.FileName, sourceFileName);
            var detailsFolder = Path.Combine(Path.GetDirectoryName(sourceFileName), Path.GetFileNameWithoutExtension(sourceFileName));
            if (string.IsNullOrWhiteSpace(detailsFolder))
                throw new DirectoryNotFoundException("Details folder is empty");
            Directory.CreateDirectory(detailsFolder);
            var coverImagePath = Path.Combine(detailsFolder, "cover.jpg");
            var annotationsPath = Path.Combine(detailsFolder, "annotation.txt");
            if (!System.IO.File.Exists(coverImagePath))
                BookHelper.SaveCover(sourceFileName, coverImagePath);
            if (string.IsNullOrWhiteSpace(book.Description))
                book.Description = BookHelper.GetAnnotation(sourceFileName, annotationsPath);
            ViewBag.Title = book.Title;
            if (System.IO.File.Exists(coverImagePath))
                ViewBag.Image = GetLinkToFile(coverImagePath);

            var mobiFile = Path.ChangeExtension(sourceFileName, ".mobi");
            ViewBag.MobiFileFound = System.IO.File.Exists(mobiFile);

            Logger.WriteDebug($"Details for {book.Id} - '{book.Title}'", CommonHelper.GetClientAddress());
            return View(book);
        }

        #endregion

        #region sequence

        [Route("s/{id:long}")]
        public ActionResult Sequence(long id)
        {
            var data = DataRepository.GetSequenceData(id);
            ViewBag.Title = data.Value;
            ViewBag.SequenceMode = true;
            return View(data);
        }

        #endregion

        #region author

        [Route("a/{id:long}")]
        public ActionResult Author(long id)
        {
            var data = DataRepository.GetAuthorData(id);
            ViewBag.Title = data.FullName;
            ViewBag.AuthorMode = true;
            return View(data);
        }

        #endregion

        #region upload/convert

        [Route("c")]
        public ActionResult UploadFile()
        {
            ViewBag.maxRequestLength = SettingsHelper.MaxRequestLength;
            return View();
        }

        [HttpPost]
        public ActionResult HandleFileUpload()
        {
            var originFileName = Server.UrlDecode(Request.Headers["X-File-Name"]);
            if (string.IsNullOrEmpty(originFileName))
                return Json(new { success = false });
            var originRealPath = Server.MapPath($"~/b/{BookHelper.GetCorrectedFileName(originFileName)}");
            if (string.IsNullOrEmpty(originRealPath))
                return Json(new { success = false });
            var mobiDisplayName = Path.ChangeExtension(originFileName, ".mobi");
            var mobiRealPath = Path.ChangeExtension(originRealPath, ".mobi");
            var mobiRelativePath = GetLinkToFile(mobiRealPath);
            if (System.IO.File.Exists(originRealPath))
            {
                if (System.IO.File.Exists(mobiRealPath))
                    return Json(new { success = true, link = mobiRelativePath, fileName = mobiDisplayName });
                System.IO.File.Delete(originRealPath); //delete old uploaded file to re-convert new one
            }
            using (var fileStream = new FileStream(originRealPath, FileMode.OpenOrCreate))
                Request.InputStream.CopyTo(fileStream);

            if (!BookHelper.ConvertBook(originRealPath))
                throw new ArgumentException("Error converting book for kindle");

            return Json(new { success = true, link = mobiRelativePath, fileName = mobiDisplayName });
        }

        public FileResult GetConverter()
        {
            var name = Path.GetFileName(SettingsHelper.ConverterPath);
            return File(SettingsHelper.ConverterPath,
                System.Net.Mime.MediaTypeNames.Application.Octet, name);
        }

        #endregion

        #region PasswordChange

        [Route("passwd")]
        [HttpGet, UserTypeFilter(Roles = new[] { UserType.Administrator, UserType.User })]
        public ActionResult PasswordChange(long id)
        {
            var user = UserRepository.GetUserById(id) ?? CurrentUser;
            var item = new ChangePasswordModel { Id = user.Id, Username = user.Email, HasPassword = user.HasPassword };
            return PartialView("_PasswordChange", item);
        }

        [Route("passwd")]
        [HttpPost, UserTypeFilter(Roles = new[] { UserType.Administrator, UserType.User })]
        public ActionResult PasswordChange(ChangePasswordModel model)
        {
            if (ModelState.IsValid)
            {
                if (UserRepository.SetUserPassword(model.Id, model.OldPassword, model.NewPassword))
                {
                    //_notificationManager.NotifyPasswordChanged(model.Id, 0);
                    var res = $"Password of user '{model.Username}' was modified by '{CurrentUser.Email}'";
                    Logger.WriteInfo(res, CommonHelper.GetClientAddress());
                    ModelState.Clear();
                    return Json(new { message = res, id = model.Id });
                }
                ModelState.AddModelError("", "The current password is incorrect or the new password is invalid.");
            }
            return PartialView("_PasswordChange", model);
        }

        #endregion PasswordChange

        #region Users

        [Route("users")]
        [HttpGet, UserTypeFilter(Roles = new[] { UserType.Administrator })]
        public ActionResult Users()
        {
            return View(UserRepository.GetUsers(string.Empty));
        }

        [Route("usersp")]
        [HttpGet, UserTypeFilter(Roles = new[] { UserType.Administrator })]
        public ActionResult UsersSearch(string key)
        {
            return PartialView("UsersPartial", UserRepository.GetUsers(key));
        }

        [Route("userdel")]
        [HttpGet, UserTypeFilter(Roles = new[] { UserType.Administrator })]
        public ActionResult UserDelete(long id)
        {
            try
            {
                var user = UserRepository.GetUserById(id);
                if (user == null)
                    return new HttpStatusCodeResult(HttpStatusCode.NotFound, "User was not found");
                UserRepository.DeleteUser(user.Id);
                Logger.WriteWarning($"User '{user.Email}' was deleted by user '{CurrentUser.Email}'", CommonHelper.GetClientAddress());
                return new HttpStatusCodeResult(HttpStatusCode.OK, "Done");
            }
            catch (Exception ex)
            {
                Logger.WriteError(ex, $"Error deleting user {id}: {ex.Message}", CommonHelper.GetClientAddress());
                throw;
            }
        }

        [Route("useradd")]
        [HttpGet, UserTypeFilter(Roles = new[] { UserType.Administrator })]
        public ActionResult UserEdit(long id = 0)
        {
            var user = UserRepository.GetUserById(id);
            ViewBag.TimeTrackUsers = TimeTrackRepository.GetAllUsers();
            return user != null
                ? PartialView(user)
                : PartialView(new UserProfile
                {
                    UserType = UserType.User,
                });
        }

        [Route("useradd")]
        [HttpPost, UserTypeFilter(Roles = new[] { UserType.Administrator })]
        public ActionResult UserEdit(UserProfile model)
        {
            if (ModelState.IsValid)
            {
                //var old = UserRepository.GetUserById(model.Id);
                //if (UserRepository.IsUserContactsUnique(model.Contacts, model.Id))
                {
                    Logger.WriteInfo(string.Format("User '{0}' was {2} by '{1}'", model.Email,
                        CurrentUser.Email, model.Id > 0 ? "modified" : "created"), CommonHelper.GetClientAddress());
                    UserRepository.SaveUser(model);

                    if (model.Id == CurrentUser.Id)
                    {
                        CurrentUser.MergeWith(model);
                    }
                    ModelState.Clear();
                    return Json(new { message = "User successfully saved.", itemId = model.Id });
                }
                //ModelState.AddModelError("", "User email or phone already registered. Please enter another one.");
            }
            ViewBag.TimeTrackUsers = TimeTrackRepository.GetAllUsers();
            return PartialView(model);
        }

        [Route("passwdreset")]
        [HttpGet, UserTypeFilter(Roles = new[] { UserType.Administrator })]
        public string ResetUserPassword(long id)
        {
            try
            {
                if (UserRepository.SetUserPassword(id, null))
                    return "Password was cleaned for user";
                return "User not found by Id";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        #endregion Users

        #region TimeTrack

        [Route("t")]
        [HttpGet, UserTypeFilter(Roles = new[] { UserType.Administrator, UserType.User })]
        public ActionResult Time(long id = 0)
        {
            ViewBag.Id = id;
            return View(CheckItem.FromUserCheckData(TimeTrackRepository.GetLastCheckInOut(id > 0 ? id : CurrentUser.TimeTrackId)));
        }

        [Route("tp")]
        [HttpGet, UserTypeFilter(Roles = new[] { UserType.Administrator, UserType.User })]
        public ActionResult TimePartial(long id = 0)
        {
            ViewBag.Id = id;
            return PartialView("TimePartial", CheckItem.FromUserCheckData(TimeTrackRepository.GetLastCheckInOut(id > 0 ? id : CurrentUser.TimeTrackId)));
        }

        [Route("in")]
        [HttpGet, UserTypeFilter(Roles = new[] { UserType.Administrator, UserType.User })]
        public ActionResult CheckIn(long id = 0)
        {
            TimeTrackRepository.CheckIn(id > 0 ? id : CurrentUser.TimeTrackId);
            Logger.WriteInfo("User checked IN", CommonHelper.GetClientAddress());
            return new HttpStatusCodeResult(HttpStatusCode.OK, "Done");
        }

        [Route("out")]
        [HttpGet, UserTypeFilter(Roles = new[] { UserType.Administrator, UserType.User })]
        public ActionResult CheckOut(long id = 0)
        {
            TimeTrackRepository.CheckOut(id > 0 ? id : CurrentUser.TimeTrackId);
            Logger.WriteInfo("User checked OUT", CommonHelper.GetClientAddress());
            return new HttpStatusCodeResult(HttpStatusCode.OK, "Done");
        }

        #endregion TimeTrack

        #region flibusta recomendations

        [Route("fav")]
        [HttpGet, UserTypeFilter(Roles = new[] { UserType.Administrator, UserType.User })]
        public async Task<ActionResult> Favorites(long id = 0, int pageNum = 0)
        {
            if (id == 0 && CurrentUser.UserType != UserType.Administrator)
            {
                if (CurrentUser.FlibustaId <= 0)
                    throw new ArgumentException("FlibustaId should be passed as parameter.");
                id = CurrentUser.FlibustaId;
            }
            ViewBag.Id = id;
            var books = await DataRepository.GetFavorites(id, pageNum, SettingsHelper.MaxRecordsToShowAtOnce);
            return View(books);
        }

        [Route("favupdate")]
        [HttpGet, UserTypeFilter(Roles = new[] { UserType.Administrator })]
        public async Task<string> UpdateFavorites(long id)
        {
            try
            {
                var culture = CultureInfo.GetCultureInfo("en-US");
                var booksFetched = 0;
                var pageNum = 0;
                var fetchMore = true;
                using (var client = new WebClient())
                {
                    while (fetchMore)
                    {
                        var pageData = await client.DownloadStringTaskAsync($"https://flibusta.is/rec?view=recs&adata=name&bdata=id&udata=id&user={id}&page={pageNum++}");
                        byte[] bytes = Encoding.Default.GetBytes(pageData);
                        pageData = Encoding.UTF8.GetString(bytes);
                        var regex = new Regex(@"<tr>[\s\S]*?<td><a href=\""\/b\/([\d]+)\"">(.+)<\/a>[\s\S]*?user\/([\d]+)[\s\S]*?<td>(.+)<\/td>[\s\S]*?\/tr>");
                        var matches = regex.Matches(pageData);
                        foreach (Match m in matches)
                        {
                            if (!m.Success || m.Groups.Count != 5)
                                throw new InvalidDataException("Error parsing page data");

                            var bookId = long.Parse(m.Groups[1].Value);
                            // var bookTitle = m.Groups[2].Value;
                            var userId = long.Parse(m.Groups[3].Value);
                            var dateAdded = DateTime.Parse(m.Groups[4].Value, culture);

                            if (userId != id)
                                throw new InvalidDataException("Wrong UserId value received.");

                            var bookFound = await DataRepository.GetFavorite(bookId, userId, dateAdded);
                            if (bookFound != 0)
                            {
                                fetchMore = false;
                                break;
                            }
                            await DataRepository.SaveFavorite(bookId, userId, dateAdded);
                            booksFetched++;
                        }
                    }
                }
                return $"Successfully fetched {booksFetched} books for user '{id}' from {pageNum} processed page(s).";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        #endregion flibusta recomendations
    }
}