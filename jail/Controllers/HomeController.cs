using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Configuration;
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
            return string.Format("Application version: {0}; Build Time: {1:yyyy/MM/dd HH:mm:ss}",
                Assembly.GetExecutingAssembly().GetName().Version, buildTime);
        }

        public ActionResult Index()
        {
            return View(DataRepository.GetSearchData(""));
        }

        public ActionResult SearchResults(string key)
        {
            return PartialView(DataRepository.GetSearchData(key.ToLower()));
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

            var resultFile = Server.MapPath(string.Format("~/Uploads/{0}", Path.ChangeExtension(book.FileName, ".mobi")));
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
            
                using(var ms = new MemoryStream())
                {
                    zipEntry.Extract(ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    book.BookContent = System.Text.Encoding.UTF8.GetString(ms.ToArray());
                }
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
            ViewBag.maxRequestLength = maxRequestLength;
            return View();
        }

        /// <summary>
        /// The max file size in bytes
        /// </summary>
        protected long maxRequestLength
        {
            get
            {
                var section = ConfigurationManager.GetSection("system.web/httpRuntime") as HttpRuntimeSection;
                return section != null ? (long)section.MaxRequestLength * 1024 : 4096 * 1024;
            }
        }

        /// <summary>
        /// Checks if a file is sent to the server
        /// and saves it to the Uploads folder.
        /// </summary>
        [HttpPost]
        public ActionResult HandleFileUpload()
        {
            if (string.IsNullOrEmpty(Request.Headers["X-File-Name"])) return Json(new {success = false});
            var path = Server.MapPath(string.Format("~/Uploads/{0}_{1}", Guid.NewGuid(), Request.Headers["X-File-Name"]));
            using (var fileStream = new FileStream(path, FileMode.OpenOrCreate))
                Request.InputStream.CopyTo(fileStream);
            return Json(new { success = true });
        }
    }
}