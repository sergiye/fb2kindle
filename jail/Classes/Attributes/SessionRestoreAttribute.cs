using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using jail.Models;

namespace jail.Classes.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SessionRestoreAttribute : AuthorizeAttribute
    {
        protected UserProfile TryToRestoreSession(HttpContextBase httpContext)
        {
            if (httpContext.Session == null) return null;

            if (httpContext.Session["User"] != null)
                return httpContext.Session["User"] as UserProfile;

            var authCookie = httpContext.Request.Cookies.Get(FormsAuthentication.FormsCookieName);
            if (authCookie == null) return null;

            var ticket = FormsAuthentication.Decrypt(authCookie.Value);
            if (ticket == null || ticket.Expired || string.IsNullOrWhiteSpace(ticket.Name))
                return null;

            //restore from cookie
            var user = UserRepository.GetUser(ticket.Name);
            if (user == null)
                return null;
            httpContext.Session["User"] = user;
            httpContext.Session.Timeout = 24 * 60;
            FormsAuthentication.SetAuthCookie(ticket.Name, true);
            Logger.WriteInfo(string.Format("{0} session restored", user.UserType),
                CommonHelper.GetClientAddress(httpContext.Request), user.Email);
            return user;
        }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (TryToRestoreSession(httpContext) == null) 
              return false;
            return true; //no access restriction here!
    }
    }
}