using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using PassionProject.Models;
using System.Web.Script.Serialization;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Net;
using PassionProject.Helpers;

namespace PassionProject.Controllers
{
    public class PropertyListingController : Controller
    {
        private static HttpClient client;
        private JavaScriptSerializer jss = new JavaScriptSerializer();
        public PropertyListingController()
        {
            HttpClientHandler handler = new HttpClientHandler()
            {
                AllowAutoRedirect = false,
                //cookies are manually set in RequestHeader
                UseCookies = false
            };
            client = new HttpClient(handler);
            client.BaseAddress = new Uri("https://localhost:44343/api/");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <summary>
        /// Grabs the authentication cookie sent to this controller.
        /// For proper WebAPI authentication, you can send a post request with login credentials to the WebAPI and log the access token from the response. The controller already knows this token, so we're just passing it up the chain.
        /// 
        /// Here is a descriptive article which walks through the process of setting up authorization/authentication directly.
        /// https://docs.microsoft.com/en-us/aspnet/web-api/overview/security/individual-accounts-in-web-api
        /// </summary>
        private void GetApplicationCookie()
        {
            string token = "";
            //HTTP client is set up to be reused, otherwise it will exhaust server resources.
            //This is a bit dangerous because a previously authenticated cookie could be cached for
            //a follow-up request from someone else. Reset cookies in HTTP client before grabbing a new one.
            client.DefaultRequestHeaders.Remove("Cookie");
            if (!User.Identity.IsAuthenticated) return;

            HttpCookie cookie = System.Web.HttpContext.Current.Request.Cookies.Get(".AspNet.ApplicationCookie");
            if (cookie != null) token = cookie.Value;

            //collect token as it is submitted to the controller
            //use it to pass along to the WebAPI.
            Debug.WriteLine("Token Submitted is : " + token);
            if (token != "") client.DefaultRequestHeaders.Add("Cookie", ".AspNet.ApplicationCookie=" + token);

            return;
        }


        // GET: PropertyListing
        [HttpGet]
        [Authorize]
        [Route("agent/property-listing")]
        public async Task<ActionResult> Index()
        {
            GetApplicationCookie();

            string endpoint = "user/property-listings";

            HttpResponseMessage response = await client.GetAsync(endpoint);

            if (!response.IsSuccessStatusCode)
            {
                // Log error response
                Debug.WriteLine($"Received error response: {response.StatusCode}");

                // Log error content if available
                if (response.Content != null)
                {
                    Debug.WriteLine($"Error content: {await response.Content.ReadAsStringAsync()}");
                }

                return RedirectToAction("Error");
            }

            string responseContent = await response.Content.ReadAsStringAsync();
            Debug.WriteLine($"Response content: {responseContent}");

            // Deserialize the response content
            var apiResponse = JsonConvert.DeserializeObject<ApiResponse<List<PropertyListingDto>>>(responseContent);

            if (apiResponse == null || !apiResponse.Success)
            {
                // Log error message
                Debug.WriteLine($"API response error: {apiResponse?.Message}");
                return RedirectToAction("Error");
            }

            // Extract the list of property listings
            List<PropertyListingDto> list = apiResponse.Data;

            return View(list);
        }


        [HttpGet]
        [Route("agent/property-listing/create")]
        [Authorize]
        public ActionResult Create()
        {
            PropertyListingDto property = new PropertyListingDto();
            return View(property);
        }


        [HttpPost]
        [Route("agent/property-listing/create")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(PropertyListingDto model, IEnumerable<HttpPostedFileBase> media)
        {
            // Debugging statement to check if files are received
            Debug.WriteLine($"Number of files received: {media?.Count()}");

            // Retrieve Features from form data manually
            string[] features = HttpContext.Request.Form.GetValues("Features[]");
            model.Features = features;

            if (ModelState.IsValid)
            {
                GetApplicationCookie();
                string endpoint = "property-listings";

                var multipartContent = new MultipartFormDataContent();

                // Add each property of the model as a separate form value
                foreach (var property in model.GetType().GetProperties())
                {
                    if (property.PropertyType.IsArray && property.PropertyType.GetElementType() == typeof(string))
                    {
                        // Handle array properties separately
                        var arrayValue = (string[])property.GetValue(model);
                        if (arrayValue != null)
                        {
                            foreach (var item in arrayValue)
                            {
                                multipartContent.Add(new StringContent(item), property.Name + "[]");
                            }
                        }
                    }
                    else
                    {
                        // Handle non-array properties
                        var value = property.GetValue(model)?.ToString() ?? string.Empty;
                        multipartContent.Add(new StringContent(value), property.Name);
                    }
                }

                // Add each file content to the multipart content
                if (media != null)
                {
                    foreach (var file in media)
                    {
                        if (file != null && file.ContentLength > 0)
                        {
                            var fileContent = new StreamContent(file.InputStream);
                            fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                            {
                                Name = "media",
                                FileName = file.FileName
                            };
                            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                            multipartContent.Add(fileContent);
                        }
                    }
                }

                Debug.WriteLine($"Request content: {await multipartContent.ReadAsStringAsync()}");

                // Log the cookies being sent with the request
                if (client.DefaultRequestHeaders.Contains("Cookie"))
                {
                    foreach (var cookie in client.DefaultRequestHeaders.GetValues("Cookie"))
                    {
                        Debug.WriteLine($"Request cookie: {cookie}");
                    }
                }

                // Send the multipart request to the API
                HttpResponseMessage response = await client.PostAsync(endpoint, multipartContent);

                if (response.IsSuccessStatusCode)
                {
                    // Log successful response
                    Debug.WriteLine($"Received successful response: {response.StatusCode}");
                    Debug.WriteLine($"Received successful response: {await response.Content.ReadAsStringAsync()}");

                    // Redirect to Index action
                    return RedirectToAction("Index");
                }
                else
                {
                    // Log error response
                    Debug.WriteLine($"Received error response: {response.StatusCode}");

                    // Log error content if available
                    if (response.Content != null)
                    {
                        Debug.WriteLine($"Error content: {await response.Content.ReadAsStringAsync()}");
                    }

                    // Redirect
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Failed to create listing");
                }
            }

            return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Failed to create listing");
        }

        // GET: /PropertyListing/Update/1
        [HttpGet]
        [Route("agent/property-listing/update/{slug}")]
        [Authorize]
        public async Task<ActionResult> Update(string slug)
        {
            string endpoint = "property-listings/" + slug.ToString();

            HttpResponseMessage response = await client.GetAsync(endpoint);

            if (!response.IsSuccessStatusCode)
            {
                // Log error response
                Debug.WriteLine($"Received error response: {response.StatusCode}");

                // Log error content if available
                if (response.Content != null)
                {
                    Debug.WriteLine($"Error content: {await response.Content.ReadAsStringAsync()}");
                }

                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Failed to load listing");
            }

            string responseContent = await response.Content.ReadAsStringAsync();

            // Deserialize the response content
            var apiResponse = JsonConvert.DeserializeObject<ApiResponse<PropertyListingDto>>(responseContent);

            if (apiResponse == null || !apiResponse.Success)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Failed to load listing");
            }

            // Extract the list of property listings
            PropertyListingDto propertyDto = apiResponse.Data;


            return View(propertyDto);
        }


        // POST: /PropertyListing/Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        [Route("agent/property-listing/update")]
        public async Task<ActionResult> Update(int id, PropertyListingDto model, IEnumerable<HttpPostedFileBase> media)
        {
            // Retrieve Features from form data manually
            string[] features = HttpContext.Request.Form.GetValues("Features[]");
            model.Features = features;

            // Debugging statement to check if files are received
            Debug.WriteLine($"Number of files received: {media?.Count()}");
            if (ModelState.IsValid)
            {
                GetApplicationCookie();
                string endpoint = "property-listings/" + id.ToString();
                Debug.WriteLine($"endpoint is  {endpoint}");

                var multipartContent = new MultipartFormDataContent();

                // Add each property of the model as a separate form value
                foreach (var property in model.GetType().GetProperties())
                {
                    if (property.PropertyType.IsArray && property.PropertyType.GetElementType() == typeof(string))
                    {
                        // Handle array properties separately
                        var arrayValue = (string[])property.GetValue(model);
                        if (arrayValue != null)
                        {
                            foreach (var item in arrayValue)
                            {
                                multipartContent.Add(new StringContent(item), property.Name + "[]");
                            }
                        }
                    }
                    else
                    {
                        // Handle non-array properties
                        var value = property.GetValue(model)?.ToString() ?? string.Empty;
                        multipartContent.Add(new StringContent(value), property.Name);
                    }
                }

                // Add each file content to the multipart content
                if (media != null)
                {
                    foreach (var file in media)
                    {
                        if (file != null && file.ContentLength > 0)
                        {
                            var fileContent = new StreamContent(file.InputStream);
                            fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                            {
                                Name = "media",
                                FileName = file.FileName
                            };
                            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                            multipartContent.Add(fileContent);
                        }
                    }
                }


                // Send the multipart request to the API
                HttpResponseMessage response = await client.PutAsync(endpoint, multipartContent);

                if (response.IsSuccessStatusCode)
                {
                    // Log successful response
                    Debug.WriteLine($"Received successful response: {response.StatusCode}");
                    Debug.WriteLine($"Received successful response: {await response.Content.ReadAsStringAsync()}");

                    // Redirect to Index action
                    return RedirectToAction("Index");
                }
                else
                {
                    // Log error response
                    Debug.WriteLine($"Received error response: {response.StatusCode}");

                    // Log error content if available
                    if (response.Content != null)
                    {
                        Debug.WriteLine($"Error content: {await response.Content.ReadAsStringAsync()}");
                    }

                    // Redirect to Error action
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Failed to update listing");
                }
            }

            return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Failed to update listing");
        }

        //Delete is integrated directly on the blade
    }
}