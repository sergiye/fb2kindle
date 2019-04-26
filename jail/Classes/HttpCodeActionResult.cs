using System.Net;
using System.Web.Mvc;

namespace jail.Classes
{
    internal class HttpCodeActionResult : ActionResult
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _status;

        public HttpCodeActionResult(HttpStatusCode statusCode, string status)
        {
            _statusCode = statusCode;
            _status = status;
        }

        public override void ExecuteResult(ControllerContext context)
        {
            // Set the response code to 403.
            context.HttpContext.Response.StatusCode = (int) _statusCode;
            context.HttpContext.Response.StatusDescription = _status;
        }
    }
}