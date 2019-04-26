using System;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using jail.Models;

namespace jail.Classes.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class UserTypeFilterAttribute : ActionFilterAttribute
    {
        public UserType[] Roles { get; set; }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext.RequestContext.HttpContext.Session["User"] is UserProfile user)
            {
                if (Roles != null && Roles.Length > 0)
                {
                    var userType = user.UserType;
                    if (userType == UserType.Administrator || Roles.Any(role => role == userType))
                    {
                        return; //all is ok
                    }
                }
            }
            filterContext.Result = new HttpCodeActionResult(HttpStatusCode.MethodNotAllowed, "This user type is not allowed");
        }
    }
}