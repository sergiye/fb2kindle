using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using jail.Classes;
using jail.Classes.Attributes;
using jail.Models;
using Simpl.Extensions.Encryption;

namespace jail.Controllers
{
    [OutputCache(NoStore = true, Duration = 0, VaryByParam = "None")]
    public class HomeController : Controller
    {
        #region Logging

        protected override void OnException(ExceptionContext filterContext)
        {
//            if (filterContext.Exception != null)
//            {
//                if (filterContext.Exception is TaskCanceledException ||
//                    filterContext.Exception is OperationCanceledException)
//                    return;
//            }
            var name = CommonHelper.GetActionLogName(filterContext.HttpContext.Request);
            Logger.WriteError(filterContext.Exception, string.Format("{0} - {1}", name,
                filterContext.Exception != null ? filterContext.Exception.Message : null), CommonHelper.GetClientAddress(), CommonHelper.CurrentIdentityName);
            base.OnException(filterContext);
        }

        protected override IAsyncResult BeginExecute(RequestContext requestContext, AsyncCallback callback, object state)
        {
            if (!CommonHelper.CurrentIdentityName.GetHash().Equals(CommonHelper.AdminLoginHash))
                Logger.WriteDebug(CommonHelper.GetActionLogName(requestContext.HttpContext.Request), CommonHelper.GetClientAddress(), CommonHelper.CurrentIdentityName);
            return base.BeginExecute(requestContext, callback, state);
        }

        public UserProfile CurrentUser
        {
            get
            {
                return Request.IsAuthenticated
                    ? (UserProfile)ControllerContext.HttpContext.Session["User"]
                    : null;
            }
        }

//        public bool IsAdmin()
//        {
//            var user = User as UserProfile;
//            if (user == null)
//                return false;
//            return user.UserType == UserType.Administrator;
//        }

        private string AppBaseUrl { get { return Url.Content("~/"); } }

        private string AppBaseUrl2
        {
            get
            {
                if (Request == null || Request.Url == null || Request.ApplicationPath == null)
                    return null;
                return string.Format("{0}://{1}:{2}{3}/", Request.Url.Scheme, Request.Url.Host, Request.Url.Port,
                    Request.ApplicationPath.TrimEnd('/'));
            }
        }

        private string GetLinkToFile(string fileName)
        {
            return AppBaseUrl + fileName.Replace(Server.MapPath("~"), "").Replace('\\', '/');
        }

        #endregion

        #region basic methods

        public static string GetVersionString()
        {
            var ver = Assembly.GetExecutingAssembly().GetName().Version;
            var buildTime = new DateTime(2000, 1, 1).AddDays(ver.Build).AddSeconds(ver.Revision * 2);
            if (TimeZone.IsDaylightSavingTime(DateTime.Now, TimeZone.CurrentTimeZone.GetDaylightChanges(DateTime.Now.Year)))
                buildTime = buildTime.AddHours(1);
            return string.Format("Version: {0}; Updated: {1:yy/MM/dd HH:mm}",
                Assembly.GetExecutingAssembly().GetName().Version, buildTime);
        }

        public ActionResult About()
        {
            return View();
        }

        public ActionResult Contact()
        {
            return View();
        }

        #endregion

        #region Login-logout

        public ActionResult LogOn()
        {
            return View(new LogOnModel());
        }

        [HttpPost]
        public ActionResult LogOn(LogOnModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                var user = model.UserName.GetHash().Equals(CommonHelper.AdminLoginHash) &&
                           model.Password.GetHash().Equals(CommonHelper.AdminPasswordHash)
                    ? UserRepository.GetUserById(0)
                    : UserRepository.GetUser(model.UserName, model.Password);
                if (user == null)
                {
                    ModelState.AddModelError("", "The user name or password provided is incorrect.");
                }
//                else if (user.UserType != UserType.Administrator && user.Id != 0)
//                {
//                    ModelState.AddModelError("", string.Format("'{0}' user type is not allowed to login", user.UserType));
//                }
                else
                {
                    if (user.UserType != UserType.Administrator)
                        Logger.WriteInfo(string.Format("{0} logged into admin", user.UserType), CommonHelper.GetClientAddress(), model.UserName);
                    if (ControllerContext.HttpContext.Session != null)
                    {
                        ControllerContext.HttpContext.Session["User"] = user;
                        ControllerContext.HttpContext.Session.Timeout = 20;
                    }
//                    if (model.RememberMe)
//                    {
                        var ticket = new FormsAuthenticationTicket(1, model.UserName,
                            DateTime.Now, DateTime.Now.AddDays(7), false,
                            string.Format("{0},{1}", user.Id, user.UserType));
                        var strEncryptedTicket = FormsAuthentication.Encrypt(ticket);
                        var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, strEncryptedTicket)
                                     {
                                         Expires = DateTime.MaxValue
                                     };
                        HttpContext.Response.Cookies.Add(cookie);
//                    }
//                    else
//                    {
//                        //FormsAuthentication.SetAuthCookie(model.UserName, false);
//                        FormsAuthentication.RedirectFromLoginPage(model.UserName, false);
//                    }
                    //                    if (Url.IsLocalUrl(returnUrl) && returnUrl.Length > 1 && returnUrl.StartsWith("/")
                    //                        && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\"))
                    //                        return Redirect(returnUrl);
                    return RedirectToAction(user.UserType == UserType.Administrator ? "Log" : "Index", "Home");
                }
            }
            return View(model);
        }

        public ActionResult LogOff()
        {
            if (CurrentUser != null && CurrentUser.UserType != UserType.Administrator)
                Logger.WriteInfo("Logout from admin zone", CommonHelper.GetClientAddress(), User.Identity.Name);
            if (HttpContext.Session != null)
                HttpContext.Session["User"] = null;
            Request.Cookies.Remove(FormsAuthentication.FormsCookieName);
            FormsAuthentication.SignOut();
            return RedirectToAction("Index");
        }

        #endregion Login-logout

        #region Logging

        [CustomAuthorization(Roles = new[] { UserType.Administrator })]
        public ActionResult Log()
        {
            ViewBag.DebugMode = Debugger.IsAttached;
            return View(SystemRepository.GetErrorLogData(SettingsHelper.MaxRecordsToShowAtOnce));
        }

        [HttpGet, CustomAuthorization(Roles = new[] { UserType.Administrator })]
        public ActionResult LogPartial(string key, SystemLog.LogItemType searchType)
        {
            ViewBag.DebugMode = Debugger.IsAttached;
            return PartialView(SystemRepository.GetErrorLogData(SettingsHelper.MaxRecordsToShowAtOnce, key, searchType));
        }

        [HttpPost, CustomAuthorization(Roles = new[] { UserType.Administrator })]
        public string ClearSelectedLog(string selection)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(selection))
                    return "Empty selection!";
                var removed = SystemRepository.ClearByMessagePart(selection);
                return string.Format("Removed: {0} records by text: '{1}'", removed, selection);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        [HttpPost, CustomAuthorization(Roles = new[] { UserType.Administrator })]
        public string CalcSelectedLog(string selection)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(selection))
                    return "Empty selection!";
                var found = SystemRepository.CalcByMessagePart(selection);
                return string.Format("Found {0} records by text: '{1}'", found, selection);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        #endregion Logging

        #region Search

        public ActionResult Index(string k = null, string l = "ru")
        {
            ViewBag.Key = k;
            ViewBag.Lang = l;
            return View(string.IsNullOrWhiteSpace(k) ? new List<BookInfo>() :
                DataRepository.GetSearchData(k, l));
        }

        [ValidateInput(false)]
        [Route("search")]
        public ActionResult SearchResults(string k = null, string l = "ru")
        {
            return PartialView(string.IsNullOrWhiteSpace(k) ? new List<BookInfo>() : 
                DataRepository.GetSearchData(k, l));
        }

        #endregion

        #region book

        [Route("f/{id}")]
        public FileResult Download(long id)
        {
            var book = DataRepository.GetBook(id);
            if (book == null)
                throw new FileNotFoundException("Book not found in db");

            var sourceFileName = Server.MapPath(string.Format("~/b/{0}", book.FileName));
            if (!System.IO.File.Exists(sourceFileName))
            {
                var archPath = Path.Combine(SettingsHelper.ArchivesPath, book.ArchiveFileName);
                if (!System.IO.File.Exists(archPath))
                    throw new FileNotFoundException("Book archive not found");
                BookHelper.ExtractZipFile(archPath, book.FileName, sourceFileName);
            }
            var fileData = System.IO.File.ReadAllBytes(sourceFileName);
            return File(fileData, System.Net.Mime.MediaTypeNames.Application.Octet, 
                BookHelper.GetBookDownloadFileName(book));
        }

        [Route("m/{id}")]
        public FileResult Mobi(long id)
        {
            var book = DataRepository.GetBook(id);
            if (book == null)
                throw new FileNotFoundException("Book not found in db");
            var sourceFileName = Server.MapPath(string.Format("~/b/{0}", book.FileName));
            var resultFile = Path.ChangeExtension(sourceFileName, ".mobi");
            if (!System.IO.File.Exists(resultFile))
            {
                var archPath = Path.Combine(SettingsHelper.ArchivesPath, book.ArchiveFileName);
                if (!System.IO.File.Exists(archPath))
                    throw new FileNotFoundException("Book archive not found");

                if (!System.IO.File.Exists(sourceFileName))
                    BookHelper.ExtractZipFile(archPath, book.FileName, sourceFileName);

                if (!BookHelper.ConvertBook(sourceFileName, false))
                    throw new ArgumentException("Error converting book for kindle");
            }
            var fileBytes = System.IO.File.ReadAllBytes(resultFile);
            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, 
                BookHelper.GetBookDownloadFileName(book, ".mobi"));
        }

        [Route("r/{id}")]
        public ActionResult Read(long id)
        {
            var book = DataRepository.GetBook(id);
            if (book == null)
                throw new FileNotFoundException("Book not found in db");

            var tempFile = Server.MapPath(string.Format("~/b/{0}", book.FileName));
            if (!System.IO.File.Exists(tempFile))
            {
                var archPath = Path.Combine(SettingsHelper.ArchivesPath, book.ArchiveFileName);
                if (!System.IO.File.Exists(archPath))
                    throw new FileNotFoundException("Book archive not found");
                BookHelper.ExtractZipFile(archPath, book.FileName, tempFile);
            }

            var detailsFolder = Path.Combine(Path.GetDirectoryName(tempFile), Path.GetFileNameWithoutExtension(tempFile));
            if (string.IsNullOrWhiteSpace(detailsFolder))
                throw new DirectoryNotFoundException("Details folder is empty");
            Directory.CreateDirectory(detailsFolder);
            var readingPath = Path.Combine(detailsFolder, "index.html");
            if (!System.IO.File.Exists(readingPath))
                BookHelper.Transform(tempFile, readingPath, Server.MapPath("~/xhtml.xsl"));
            ViewBag.Title = book.Title;
            return new FilePathResult(GetLinkToFile(readingPath), "text/html");
            //ViewBag.BookContent = GetLinkToFile(readingPath);//Path.Combine(@"../" + readingPath.Replace(Server.MapPath("~"), "").Replace('\\', '/'));
            //return View(book);
        }

        [Route("d/{id}")]
        public ActionResult Details(long id)
        {
            var book = DataRepository.GetBook(id);
            if (book == null)
                throw new FileNotFoundException("Book not found in db");

            var archPath = Path.Combine(SettingsHelper.ArchivesPath, book.ArchiveFileName);
            if (!System.IO.File.Exists(archPath))
                throw new FileNotFoundException("Book archive not found");

            var tempFile = Server.MapPath(string.Format("~/b/{0}", book.FileName));
            if (!System.IO.File.Exists(tempFile))
                BookHelper.ExtractZipFile(archPath, book.FileName, tempFile);
            var detailsFolder = Path.Combine(Path.GetDirectoryName(tempFile), Path.GetFileNameWithoutExtension(tempFile));
            if (string.IsNullOrWhiteSpace(detailsFolder))
                throw new DirectoryNotFoundException("Details folder is empty");
            Directory.CreateDirectory(detailsFolder);
            var coverImagePath = Path.Combine(detailsFolder, "cover.jpg");
            var annotationsPath = Path.Combine(detailsFolder, "annotation.txt");
            if (!System.IO.File.Exists(coverImagePath))
                BookHelper.SaveCover(tempFile, coverImagePath);
            if (string.IsNullOrWhiteSpace(book.Description))
                book.Description = BookHelper.GetAnnotation(tempFile, annotationsPath);
            ViewBag.Title = book.Title;
            if (System.IO.File.Exists(coverImagePath))
                ViewBag.Image = GetLinkToFile(coverImagePath);
            return View(book);
        }

        #endregion

        #region sequence

        [Route("s/{id}")]
        public ActionResult Sequence(long id)
        {
            var data = DataRepository.GetSequenceData(id);
            ViewBag.Title = data.Value;
            ViewBag.SequenceMode = true;
            return View(data);
        }

        #endregion

        #region author

        [Route("a/{id}")]
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
            var originFileName = Request.Headers["X-File-Name"];
            if (string.IsNullOrEmpty(originFileName))
                return Json(new { success = false });
            var originRealPath = Server.MapPath(string.Format("~/b/{0}", 
                BookHelper.GetCorrectedFileName(originFileName)));
            if (string.IsNullOrEmpty(originRealPath))
                return Json(new { success = false });
            var mobiDisplayName = Path.ChangeExtension(originFileName, ".mobi");
            var mobiRealPath = Path.ChangeExtension(originRealPath, ".mobi");
            var mobiRelativePath = GetLinkToFile(mobiRealPath);
            if (System.IO.File.Exists(originRealPath))
            {
                if (System.IO.File.Exists(mobiRealPath))
                    return Json(new {success = true, link = mobiRelativePath, fileName = mobiDisplayName});
                System.IO.File.Delete(originRealPath); //delete old uploaded file to re-convert new one
            }
            using (var fileStream = new FileStream(originRealPath, FileMode.OpenOrCreate))
                Request.InputStream.CopyTo(fileStream);

            if (!BookHelper.ConvertBook(originRealPath, false))
                throw new ArgumentException("Error converting book for kindle");

            return Json(new { success = true, link = mobiRelativePath, fileName = mobiDisplayName });
        }

        #endregion
    }
}