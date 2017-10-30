using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Web.Configuration;
using System.Web.Mvc;
using jail.Classes;

namespace jail.Controllers
{
    public class HomeController : Controller
    {
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
            return PartialView(DataRepository.GetSearchData(key));
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

        public ActionResult File()
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