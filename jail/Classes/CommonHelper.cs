﻿using System;
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

        public static string AdminLoginHash { get { return "dae7a3d670e30f7278ea90344c768af1"; } }
        public static string AdminPasswordHash { get { return "e3bbe98ee127683efc57b077e19cfa43"; } }
    }
}