using System;
using System.Web.Mvc;

namespace jail.Classes.Attributes
{
    /// <summary>
    /// Attribute to mark methods to be skipped for logging
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class SkipLoggingAttribute : Attribute
    {
    }

    /// <summary>
    /// Action Logger attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class ActionLoggerAttribute : ActionFilterAttribute
    {
        /// <inheritdoc />
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var request = filterContext.HttpContext.Request;
            foreach (Attribute a in filterContext.ActionDescriptor.GetCustomAttributes(false))
            {
                if (a.GetType() == typeof(SkipLoggingAttribute))
                    return;
            }

            var actionName = CommonHelper.GetActionLogName(request);
            if (!actionName.Equals("GET /", StringComparison.OrdinalIgnoreCase))
                Logger.WriteTrace(actionName, CommonHelper.GetClientAddress(request), CommonHelper.CurrentIdentityName);
        }
    }
}