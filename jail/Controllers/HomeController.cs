using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using System.Web.Routing;
using Fb2Kindle;
using Ionic.Zip;
using jail.Classes;

namespace jail.Controllers
{
    [OutputCache(NoStore = true, Duration = 0, VaryByParam = "None")]
    public class HomeController : Controller
    {
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

        public static string GetVersionString()
        {
            var ver = Assembly.GetExecutingAssembly().GetName().Version;
            var buildTime = new DateTime(2000, 1, 1).AddDays(ver.Build).AddSeconds(ver.Revision * 2);
            if (TimeZone.IsDaylightSavingTime(DateTime.Now, TimeZone.CurrentTimeZone.GetDaylightChanges(DateTime.Now.Year)))
                buildTime = buildTime.AddHours(1);
            return string.Format("Version: {0}; Updated: {1:yy/MM/dd HH:mm}",
                Assembly.GetExecutingAssembly().GetName().Version, buildTime);
        }

        public ActionResult Index()
        {
            return View(DataRepository.GetSearchData(null, null));
        }

        public ActionResult SearchResults(string key, string searchLang)
        {
            return PartialView(DataRepository.GetSearchData(key.ToLower(), searchLang));
        }

        [HttpGet, Route("download")]
        public FileResult Download(long id)
        {
            var book = DataRepository.GetBook(id);
            if (book == null)
            {
                throw new FileNotFoundException("Book not found in db");
            }

            var archPath = Path.Combine(DataRepository.ArchivesPath, book.ArchiveFileName);
            if (!System.IO.File.Exists(archPath))
            {
                throw new FileNotFoundException("Book archive not found");
            }
            using (var zip = new ZipFile(archPath))
            {
                var zipEntry = zip.Entries.FirstOrDefault(e => e.FileName.Equals(book.FileName));
                if (zipEntry == null)
                {
                    throw new FileNotFoundException("Book file not found in archive");
                }

                var ms = new MemoryStream();
                {
                    zipEntry.Extract(ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    return File(ms.ToArray(), System.Net.Mime.MediaTypeNames.Application.Octet, 
                        CommonHelper.GetBookDownloadFileName(book));
                }
            }
        }

        [HttpGet, Route("mobi")]
        public FileResult Mobi(long id)
        {
            var book = DataRepository.GetBook(id);
            if (book == null)
                throw new FileNotFoundException("Book not found in db");
            Directory.CreateDirectory(SettingsHelper.ConvertedBooksPath);
            var resultFile = Path.Combine(SettingsHelper.ConvertedBooksPath, Path.ChangeExtension(book.FileName, ".mobi"));
            if (!System.IO.File.Exists(resultFile))
            {
                var tempFile = Path.Combine(Path.GetTempPath(), book.FileName);
                var archPath = Path.Combine(DataRepository.ArchivesPath, book.ArchiveFileName);
                if (!System.IO.File.Exists(archPath))
                {
                    throw new FileNotFoundException("Book archive not found");
                }
                using (var zip = new ZipFile(archPath))
                {
                    var zipEntry = zip.Entries.FirstOrDefault(e => e.FileName.Equals(book.FileName));
                    if (zipEntry == null)
                        throw new FileNotFoundException("Book file not found in archive");
                    using (var fs = System.IO.File.Create(tempFile))
                        zipEntry.Extract(fs);
                }

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

        [HttpGet, Route("read")]
        public ActionResult Read(long id)
        {
            var book = DataRepository.GetBook(id);
            if (book == null)
                throw new FileNotFoundException("Book not found in db");

            var archPath = Path.Combine(DataRepository.ArchivesPath, book.ArchiveFileName);
            if (!System.IO.File.Exists(archPath))
                throw new FileNotFoundException("Book archive not found");
            using (var zip = new ZipFile(archPath))
            {
                var zipEntry = zip.Entries.FirstOrDefault(e => e.FileName.Equals(book.FileName));
                if (zipEntry == null)
                    throw new FileNotFoundException("Book file not found in archive");

                var tempFile = Server.MapPath(string.Format("~/Uploads/{0}", book.FileName));
                using (var fs = System.IO.File.Create(tempFile))
                    zipEntry.Extract(fs);

                var sp = new MobiConverter(tempFile);
                if (sp.InitializationError)
                    throw new ArgumentException("Error preparing file to read (initialization)");
                sp.saveImages();
                var generatedFile = sp.transform(Server.MapPath("~/xhtml.xsl"), "index.html");
                book.BookContent = System.IO.File.ReadAllText(generatedFile);
//                using (var ms = new MemoryStream())
//                {
//                    zipEntry.Extract(ms);
//                    ms.Seek(0, SeekOrigin.Begin);
//                    book.BookContent = System.Text.Encoding.UTF8.GetString(ms.ToArray());
//                }
            }
            ViewBag.Title = book.Title;
            
            return View(book);
        }

        public ActionResult Details(long id)
        {
            var book = DataRepository.GetBook(id);
            if (book == null)
            {
                throw new FileNotFoundException("Book not found in db");
            }

            var archPath = Path.Combine(DataRepository.ArchivesPath, book.ArchiveFileName);
            if (!System.IO.File.Exists(archPath))
            {
                throw new FileNotFoundException("Book archive not found");
            }
//            using (var zip = new ZipFile(archPath))
//            {
//                var zipEntry = zip.Entries.FirstOrDefault(e => e.FileName.Equals(book.FileName));
//                if (zipEntry == null)
//                {
//                    throw new FileNotFoundException("Book file not found in archive");
//                }
//
//                var ms = new MemoryStream();
//                {
//                    zipEntry.Extract(ms);
//                    ms.Seek(0, SeekOrigin.Begin);
//
//                }
//            }
            ViewBag.Title = book.Title;
            ViewBag.Image = string.Format("/Uploads/{0}/cover.jpg", Path.GetFileNameWithoutExtension(book.FileName));

            return View(book);
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
            var mobiRelativePath = Path.Combine(@"..\" + mobiRealPath.Replace(Server.MapPath("~"), ""));
            if (System.IO.File.Exists(originRealPath))
            {
                if (System.IO.File.Exists(mobiRealPath))
                {
                    return Json(new { success = true, link = mobiRelativePath, fileName = mobiDisplayName });
                }
                System.IO.File.Delete(originRealPath); //delete old uploaded file to re-convert new one
            }
            using (var fileStream = new FileStream(originRealPath, FileMode.OpenOrCreate))
                Request.InputStream.CopyTo(fileStream);

            var conv = new Convertor(new DefaultOptions(), SettingsHelper.ConverterCss, SettingsHelper.ConverterDetailedOutput);
            if (!conv.ConvertBook(originRealPath, false))
                throw new ArgumentException("Error converting book for kindle");

            return Json(new { success = true, link = mobiRelativePath, fileName = mobiDisplayName });
        }
    }
}