using System;
using System.Linq;
using System.Net.Http;
using System.Web;

namespace jail.Classes
{
    /// <summary>
    /// Common methods to make life easier
    /// </summary>
    public static class CommonHelper
    {
        /// <summary>
        /// Get client IP from HTTP request
        /// </summary>
        /// <returns></returns>
        public static string GetClientAddress()
        {
            var request = HttpContext.Current.Request;
            var clientAddress = String.Empty;
            if (request.ServerVariables["HTTP_X_FORWARDED_FOR"] != null)
            {
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
        public static string GetActionLogName(HttpRequestMessage request)
        {
            return String.Format("{0} {1}{2}", request.Method,
                request.RequestUri.AbsolutePath, request.RequestUri.Query);
        }

        /// <summary>
        /// Get api action name from request
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static string GetActionLogName(HttpRequestBase request)
        {
            return string.Format("{0} {1}", request.HttpMethod, request.RawUrl);
        }

        /// <summary>
        /// Get current user identity name
        /// </summary>
        public static string CurrentIdentityName
        {
            get
            {
                var name = "Anonymous";
                if (HttpContext.Current.User.Identity.IsAuthenticated)
                {
                    name = HttpContext.Current.User.Identity.Name;
                }
                return name;
            }
        }

        public static string Translit(this string str)
        {
            string[] lat_up = { "A", "B", "V", "G", "D", "E", "Yo", "Zh", "Z", "I", "Y", "K", "L", "M", "N", "O", "P", "R", "S", "T", "U", "F", "Kh", "Ts", "Ch", "Sh", "Shch", "\"", "Y", "'", "E", "Yu", "Ya" };
            string[] lat_low = { "a", "b", "v", "g", "d", "e", "yo", "zh", "z", "i", "y", "k", "l", "m", "n", "o", "p", "r", "s", "t", "u", "f", "kh", "ts", "ch", "sh", "shch", "\"", "y", "'", "e", "yu", "ya" };
            string[] rus_up = { "А", "Б", "В", "Г", "Д", "Е", "Ё", "Ж", "З", "И", "Й", "К", "Л", "М", "Н", "О", "П", "Р", "С", "Т", "У", "Ф", "Х", "Ц", "Ч", "Ш", "Щ", "Ъ", "Ы", "Ь", "Э", "Ю", "Я" };
            string[] rus_low = { "а", "б", "в", "г", "д", "е", "ё", "ж", "з", "и", "й", "к", "л", "м", "н", "о", "п", "р", "с", "т", "у", "ф", "х", "ц", "ч", "ш", "щ", "ъ", "ы", "ь", "э", "ю", "я" };
            for (int i = 0; i <= 32; i++)
            {
                str = str.Replace(rus_up[i], lat_up[i]);
                str = str.Replace(rus_low[i], lat_low[i]);
            }
            return str;
        }
    }
}