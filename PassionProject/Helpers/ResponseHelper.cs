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
    public class ApiResponse<T>
    {
        public string Message { get; set; }
        public bool Success { get; set; }
        public T Data { get; set; }
        public object Errors { get; set; }
    }

    public static class ResponseHelper
    {
        public static IHttpActionResult JsonResponse(string message, HttpStatusCode statusCode, bool success, object data = null, object errors = null)
        {
            ApiResponse<object> responseData = new ApiResponse<object>
            {
                Message = message,
                Success = success,
                Data = data,
                Errors = errors
            };

            var response = new HttpResponseMessage(statusCode)
            {
                Content = new ObjectContent<ApiResponse<object>>(responseData, new JsonMediaTypeFormatter())
            };

            return new ResponseMessageResult(response);
        }
    }
}