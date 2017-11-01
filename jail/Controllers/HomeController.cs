using System;
using System.IO;
using System.Reflection;
using System.Web.Mvc;
using System.Web.Routing;
using Fb2Kindle;
using jail.Classes;

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
            Log.WriteError(filterContext.Exception, string.Format("{0} - {1}", name,
                filterContext.Exception != null ? filterContext.Exception.Message : null), CommonHelper.GetClientAddress(), CommonHelper.CurrentIdentityName);
            base.OnException(filterContext);
        }

        protected override IAsyncResult BeginExecute(RequestContext requestContext, AsyncCallback callback, object state)
        {
            Log.WriteDebug(CommonHelper.GetActionLogName(requestContext.HttpContext.Request), CommonHelper.GetClientAddress(), CommonHelper.CurrentIdentityName);
            return base.BeginExecute(requestContext, callback, state);
        }

        #endregion

        #region basic methods

        private string AppBaseUrl
        {
            get
            {
                return Url.Content("~/");
            }
        }

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
            ViewBag.Message = "Contacts page.";
            return View();
        }

        #endregion

        #region Search

        public ActionResult Index()
        {
            return View(DataRepository.GetSearchData(null, null));
        }

        public ActionResult SearchResults(string key, string searchLang)
        {
            return PartialView(DataRepository.GetSearchData(key.ToLower(), searchLang));
        }

        #endregion

        #region book

        [Route("d/{id}")]
        public FileResult Download(long id)
        {
            var book = DataRepository.GetBook(id);
            if (book == null)
                throw new FileNotFoundException("Book not found in db");

            var archPath = Path.Combine(DataRepository.ArchivesPath, book.ArchiveFileName);
            if (!System.IO.File.Exists(archPath))
                throw new FileNotFoundException("Book archive not found");
            var fileData = CommonHelper.ExtractZipFile(archPath, book.FileName);
            return File(fileData, System.Net.Mime.MediaTypeNames.Application.Octet, 
                CommonHelper.GetBookDownloadFileName(book));
        }

        [Route("m/{id}")]
        public FileResult Mobi(long id)
        {
            var book = DataRepository.GetBook(id);
            if (book == null)
                throw new FileNotFoundException("Book not found in db");
            Directory.CreateDirectory(SettingsHelper.ConvertedBooksPath);
            var resultFile = Path.Combine(SettingsHelper.ConvertedBooksPath, Path.ChangeExtension(book.FileName, ".mobi"));
            if (!System.IO.File.Exists(resultFile))
            {
                var archPath = Path.Combine(DataRepository.ArchivesPath, book.ArchiveFileName);
                if (!System.IO.File.Exists(archPath))
                    throw new FileNotFoundException("Book archive not found");

                var tempFile = Path.Combine(Path.GetTempPath(), book.FileName);
                if (!System.IO.File.Exists(tempFile))
                    CommonHelper.ExtractZipFile(archPath, book.FileName, tempFile);

                var conv = new Convertor(SettingsHelper.ConverterSettings, SettingsHelper.ConverterCss, SettingsHelper.ConverterDetailedOutput);
                if (!conv.ConvertBook(tempFile, false))
                    throw new ArgumentException("Error converting book for kindle");

                tempFile = Path.ChangeExtension(tempFile, ".mobi");
                System.IO.File.Move(tempFile, resultFile);
            }

            var fileBytes = System.IO.File.ReadAllBytes(resultFile);
            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, 
                CommonHelper.GetBookDownloadFileName(book, ".mobi"));
        }

        [Route("r/{id}")]
        public ActionResult Read(long id)
        {
            var book = DataRepository.GetBook(id);
            if (book == null)
                throw new FileNotFoundException("Book not found in db");

            var archPath = Path.Combine(DataRepository.ArchivesPath, book.ArchiveFileName);
            if (!System.IO.File.Exists(archPath))
                throw new FileNotFoundException("Book archive not found");

            var tempFile = Server.MapPath(string.Format("~/Uploads/{0}", book.FileName));
            if (!System.IO.File.Exists(tempFile))
                CommonHelper.ExtractZipFile(archPath, book.FileName, tempFile);

            var detailsFolder = Path.Combine(Path.GetDirectoryName(tempFile), Path.GetFileNameWithoutExtension(tempFile));
            if (string.IsNullOrWhiteSpace(detailsFolder))
                throw new DirectoryNotFoundException("Details folder is empty");
            Directory.CreateDirectory(detailsFolder);
            var readingPath = Path.Combine(detailsFolder, "index.html");
            if (!System.IO.File.Exists(readingPath))
                MobiConverter.Transform(tempFile, readingPath, Server.MapPath("~/xhtml.xsl"));
            ViewBag.Title = book.Title;
            ViewBag.BookContent = GetLinkToFile(readingPath);//Path.Combine(@"../" + readingPath.Replace(Server.MapPath("~"), "").Replace('\\', '/'));
            return View(book);
        }

        [Route("b/{id}")]
        public ActionResult Details(long id)
        {
            var book = DataRepository.GetBook(id);
            if (book == null)
                throw new FileNotFoundException("Book not found in db");

            var archPath = Path.Combine(DataRepository.ArchivesPath, book.ArchiveFileName);
            if (!System.IO.File.Exists(archPath))
                throw new FileNotFoundException("Book archive not found");

            var tempFile = Server.MapPath(string.Format("~/Uploads/{0}", book.FileName));
            if (!System.IO.File.Exists(tempFile))
                CommonHelper.ExtractZipFile(archPath, book.FileName, tempFile);
            var detailsFolder = Path.Combine(Path.GetDirectoryName(tempFile), Path.GetFileNameWithoutExtension(tempFile));
            if (string.IsNullOrWhiteSpace(detailsFolder))
                throw new DirectoryNotFoundException("Details folder is empty");
            Directory.CreateDirectory(detailsFolder);
            var coverImagePath = Path.Combine(detailsFolder, "cover.jpg");
            var annotationsPath = Path.Combine(detailsFolder, "annotation.txt");
            if (!System.IO.File.Exists(coverImagePath))
                MobiConverter.SaveCover(tempFile, coverImagePath);
            if (string.IsNullOrWhiteSpace(book.Description))
            {
                if (!System.IO.File.Exists(annotationsPath))
                    MobiConverter.SaveAnnotation(tempFile, annotationsPath);
                book.Description = System.IO.File.ReadAllText(annotationsPath);
            }
            ViewBag.Title = book.Title;
            ViewBag.Image = GetLinkToFile(coverImagePath); //Path.Combine(@"..\" + coverImagePath.Replace(Server.MapPath("~"), ""));

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
            var originRealPath = Server.MapPath(string.Format("~/Uploads/{0}", 
                CommonHelper.GetCorrectedFileName(originFileName)));
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

            var conv = new Convertor(new DefaultOptions(), SettingsHelper.ConverterCss, SettingsHelper.ConverterDetailedOutput);
            if (!conv.ConvertBook(originRealPath, false))
                throw new ArgumentException("Error converting book for kindle");

            return Json(new { success = true, link = mobiRelativePath, fileName = mobiDisplayName });
        }

        #endregion
    }
}