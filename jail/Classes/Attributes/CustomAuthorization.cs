using System.Linq;
using System.Web;
using System.Web.Mvc;
using jail.Models;

namespace jail.Classes.Attributes
{
    public class CustomAuthorization : AuthorizeAttribute
    {
        public new UserType[] Roles { get; set; }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            var user = httpContext.Session["User"] as UserProfile;
            if (user == null) return false;

            if (Roles == null || Roles.Length <= 0) return false;
            var userType = user.UserType;
            return userType == UserType.Administrator || Roles.Any(role => role == userType);
        }
    }
}