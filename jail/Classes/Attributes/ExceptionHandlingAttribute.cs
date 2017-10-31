using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Filters;
using jail.Classes;
using Simpl.Extensions;

namespace jail.Classes.Attributes
{
    /// <summary>
    /// Attribute for handling exceptions in API
    /// </summary>
    internal class ExceptionHandlingAttribute : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext context)
        {
            if (context.Exception != null)
            {
                if (context.Exception is TaskCanceledException || 
                    context.Exception is OperationCanceledException)
                    return;
            }

            var login = CommonHelper.CurrentIdentityName;
            var name = string.Format("{0} {1}{2}", context.Request.Method, context.Request.RequestUri.AbsolutePath, context.Request.RequestUri.Query);
            //var name = string.Format("{0}/{1}", context.ActionContext.ActionDescriptor.ControllerDescriptor.ControllerName, context.ActionContext.ActionDescriptor.ActionName);
            Log.WriteError(context.Exception, string.Format("{0} - {1}", name, context.Exception.Message), login, CommonHelper.GetClientAddress());
            throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                ReasonPhrase = "API execution error",
                Content = new StringContent(new SwifticErrorContent(context.Exception.Message).ToJson()),
            });
        }
    }
}