using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Web.Mvc;
using Simpl.Extensions;

namespace jail.Classes.Attributes
{
    internal class ApiHttpsAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            if (Debugger.IsAttached)
                return;
            if (string.Equals(actionContext.Request.RequestUri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
                return;
            actionContext.Response = new HttpResponseMessage(HttpStatusCode.BadRequest)
                                     {
                                         Content = new StringContent(new { message = "HTTPS Required" }.ToJson())
                                     };
        }
    }
}