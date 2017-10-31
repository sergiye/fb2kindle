using System.Web.Mvc;

namespace jail.Classes.Attributes
{
    /// <summary>
    /// Action Logger attribute
    /// </summary>
    public class ActionLoggerAttribute : ActionFilterAttribute
    {
        /// <inheritdoc />
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            Log.WriteDebug(string.Format("{0} - {1}", CommonHelper.GetActionLogName(actionContext.Request), "requested"), 
                CommonHelper.CurrentIdentityName, CommonHelper.GetClientAddress());
        }
    }
}