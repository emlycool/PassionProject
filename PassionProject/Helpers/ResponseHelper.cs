using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Formatting;
using System.Net.Http;
using System.Net;
using System.Web;
using System.Web.Http.Results;
using System.Web.Http;

namespace PassionProject.Helpers
{
    public static class ResponseHelper
    {
        public static IHttpActionResult JsonResponse(string message, HttpStatusCode statusCode, bool success, object data = null, object errors = null)
        {
            var responseData = new
            {
                message,
                success,
                data,
                errors
            };
            var response = new HttpResponseMessage(statusCode);
            response.Content = new ObjectContent<object>(responseData, new JsonMediaTypeFormatter());
            return new ResponseMessageResult(response);
        }
    }
}