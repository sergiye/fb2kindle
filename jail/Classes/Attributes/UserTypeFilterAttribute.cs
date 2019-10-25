using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
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
            
            Logger.WriteWarning(string.Format("Access was blocked: {0}", CommonHelper.GetActionLogName(filterContext.HttpContext.Request)), 
                CommonHelper.GetClientAddress(), CommonHelper.CurrentIdentityName);
            //filterContext.Result = new HttpCodeActionResult(HttpStatusCode.MethodNotAllowed, "This user type is not allowed");
            //filterContext.Result = new RedirectResult(Url.Action("Login"));
            //filterContext.Result = new RedirectToRouteResult("Login", null);
            filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Home", action = "Login" }));
            //var controller = (HomeController)filterContext.Controller;
            //filterContext.Result = controller.RedirectToAction("Login", "home");
        }
    }
}