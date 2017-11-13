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
            if (httpContext.Session != null && httpContext.Session["User"] == null)
            {
                var authenCookie = httpContext.Request.Cookies.Get(FormsAuthentication.FormsCookieName);
                if (authenCookie == null) return false;

                var ticket = FormsAuthentication.Decrypt(authenCookie.Value);
                var userData = ticket.UserData.Split(',');
                if (userData.Length != 2)
                    return false;

                var user = UserRepository.GetUserById(int.Parse(userData[0]));
                if (user != null)
                {
                    httpContext.Session["User"] = user;
                    if (user.UserType != UserType.Administrator)
                        Logger.WriteInfo(string.Format("{0} re-entered admin zone", user.UserType), user.Email, CommonHelper.GetClientAddress());
                }
                else
                    return false;
            }

            if (Roles != null && Roles.Length > 0)
            {
                var user = (UserProfile)httpContext.Session["User"];
                var userType = user.UserType;
                return userType == UserType.Administrator || Roles.Any(role => role == userType);
            }
            return false;
        }
    }
}