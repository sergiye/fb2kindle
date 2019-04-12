using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using jail.Models;

namespace jail.Classes.Attributes
{
    public class CustomAuthorization : AuthorizeAttribute
    {
        public new UserType[] Roles { get; set; }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (httpContext.Session["User"] == null)
            {
                var authCookie = httpContext.Request.Cookies.Get(FormsAuthentication.FormsCookieName);
                if (authCookie == null) return false;

                var ticket = FormsAuthentication.Decrypt(authCookie.Value);
                if (ticket == null) return false;
                var userData = ticket.UserData.Split(',');
                if (userData.Length != 2) return false;

                var user = UserRepository.GetUserById(int.Parse(userData[0]));
                if (user == null) return false;
                httpContext.Session["User"] = user;
                Logger.WriteInfo(string.Format("{0} is back", user.UserType), CommonHelper.GetClientAddress(), user.Email);
            }

            if (Roles == null || Roles.Length <= 0) return false;
            
            var sessionUser = (UserProfile) httpContext.Session["User"];
            var userType = sessionUser.UserType;
            return userType == UserType.Administrator || Roles.Any(role => role == userType);
        }
    }
}