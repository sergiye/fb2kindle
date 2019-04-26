using System;
using System.Web.Mvc;

namespace jail.Classes.Attributes
{
    /// <summary>
    /// Action Logger attribute
    /// </summary>
    public class ActionLoggerAttribute : ActionFilterAttribute
    {
        /// <inheritdoc />
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var request = filterContext.HttpContext.Request;
            if (request.HttpMethod == "GET"
                && (request.Path.Equals("/log", StringComparison.OrdinalIgnoreCase) || request.Path.Equals("/logp", StringComparison.OrdinalIgnoreCase)))
                return;
            Logger.WriteTrace(CommonHelper.GetActionLogName(request), 
                CommonHelper.GetClientAddress(), CommonHelper.CurrentIdentityName);
        }
    }
}