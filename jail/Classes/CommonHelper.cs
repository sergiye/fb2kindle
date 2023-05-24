using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Web;
using jail.Models;

namespace jail.Classes {
    /// <summary>
    /// Common methods to make life easier
    /// </summary>
    public static class CommonHelper {
        /// <summary>
        /// Get client IP from HTTP request
        /// </summary>
        /// <returns></returns>
        public static string GetClientAddress(HttpRequestBase request) {
            // var request = HttpContext.Current.Request;
            var clientAddress = String.Empty;
            if (request.ServerVariables["HTTP_X_FORWARDED_FOR"] != null) {
                // Using X-Forwarded-For last address
                clientAddress = request.ServerVariables["HTTP_X_FORWARDED_FOR"].Split(',').Last().Trim();
            }
            else if (request.ServerVariables["X_FORWARDED_FOR"] != null)
                clientAddress = request.ServerVariables["X_FORWARDED_FOR"];
            else if (request.ServerVariables["REMOTE_ADDR"] != null)
                clientAddress = request.ServerVariables["REMOTE_ADDR"];
            else if (!String.IsNullOrWhiteSpace(request.UserHostAddress))
                clientAddress = request.UserHostAddress;

            return clientAddress;
        }

        /// <summary>
        /// Get api action name from request
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static string GetActionLogName(HttpRequestMessage request) {
            return $"{request.Method} {request.RequestUri.AbsolutePath}{request.RequestUri.Query}";
        }

        /// <summary>
        /// Get api action name from request
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static string GetActionLogName(HttpRequestBase request) {
            var queryString = HttpUtility.UrlDecode(request.QueryString.ToString());
            if (!string.IsNullOrWhiteSpace(queryString))
                queryString = $"?{queryString}";
            return $"{request.HttpMethod} {request.Path}{queryString}";
        }

        /// <summary>
        /// Get current user identity name
        /// </summary>
        public static string CurrentIdentityName {
            get {
                var name = "Anonymous";
                if (HttpContext.Current != null && HttpContext.Current.User.Identity.IsAuthenticated) {
                    name = HttpContext.Current.User.Identity.Name;
                }

                return name;
            }
        }

        public static UserType? CurrentUserType {
            get =>
                (HttpContext.Current != null && HttpContext.Current.User.Identity.IsAuthenticated)
                    ? (HttpContext.Current.Session["User"] as UserProfile)?.UserType
                    : null;
            set => HttpContext.Current.Session["User"] = value;
        }

        public static string AdminLoginHash => "dae7a3d670e30f7278ea90344c768af1";
        public static string AdminPasswordHash => "e3bbe98ee127683efc57b077e19cfa43";

        internal static async Task SendBookByMail(string bookName, string tmpBookPath, string mailTo) {
            if (string.IsNullOrWhiteSpace(SettingsHelper.SmtpServer) || SettingsHelper.SmtpPort <= 0)
                throw new Exception("Mail delivery failed: smtp not configured");

            using (var smtp = new SmtpClient(SettingsHelper.SmtpServer, SettingsHelper.SmtpPort) {
                       UseDefaultCredentials = false,
                       Credentials = new NetworkCredential(SettingsHelper.SmtpLogin, SettingsHelper.SmtpPassword),
                       EnableSsl = true
                   }) {
                using (var message = new MailMessage(new MailAddress(SettingsHelper.SmtpLogin, "Simpl's converter"),
                           new MailAddress(mailTo)) {
                           Subject = bookName,
                           Body = "Hello! Please, check book(s) attached"
                       }) {
                    using (var att = new Attachment(tmpBookPath)) {
                        message.Attachments.Add(att);
                        await smtp.SendMailAsync(message);
                    }
                }
            }
        }
    }
}