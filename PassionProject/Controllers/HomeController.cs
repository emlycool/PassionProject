using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using Newtonsoft.Json;
using PassionProject.Helpers;
using PassionProject.Models;

namespace PassionProject.Controllers
{
    public class HomeController : Controller
    {
        private static HttpClient client;
        private JavaScriptSerializer jss = new JavaScriptSerializer();
        public HomeController()
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


        public async Task<ActionResult> Index(FilterPropertyDto filter)
        {
            GetApplicationCookie();
            string endpoint = "property-listings";

            FilterPropertyDto filterProperty = new FilterPropertyDto();
            // Construct query string based on filter
            var queryParameters = new List<string>();
            if (filter != null)
            {
                if (filter.MinPrice.HasValue)
                {
                    queryParameters.Add($"MinPrice={filter.MinPrice.Value}");
                    filterProperty.MinPrice = filter.MinPrice.Value;
                }
                if (filter.MaxPrice.HasValue)
                {
                    queryParameters.Add($"MaxPrice={filter.MaxPrice.Value}");
                    filterProperty.MaxPrice = filter.MaxPrice.Value;
                }
                if (filter.MinBedrooms.HasValue)
                {
                    queryParameters.Add($"MinBedrooms={filter.MinBedrooms.Value}");
                    filterProperty.MinBedrooms = filter.MinBedrooms.Value;
                }
                if (filter.MaxBedrooms.HasValue)
                {
                    queryParameters.Add($"MaxBedrooms={filter.MaxBedrooms.Value}");
                    filterProperty.MaxBedrooms = filter.MaxBedrooms.Value;
                }
            }

            if (queryParameters.Any())
            {
                endpoint += "?" + string.Join("&", queryParameters);
            }

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

            HomeViewModel model = new HomeViewModel();
            model.PropertyListings = list;
            model.FilterPropertyDto = filterProperty;

            return View(model);
        }

        public async Task<ActionResult> Details(string slug)
        {
            if (slug == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.NotFound, "Listing not found");
            }
            string endpoint = "property-listings/" + slug.ToString();

            HttpResponseMessage response = await client.GetAsync(endpoint);

            if (!response.IsSuccessStatusCode)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Failed to load listing");
            }

            string responseContent = await response.Content.ReadAsStringAsync();

            // Deserialize the response content
            var apiResponse = JsonConvert.DeserializeObject<ApiResponse<PropertyListingDto>>(responseContent);

            if (apiResponse == null || !apiResponse.Success)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Failed to load listing");
            }

            // Extract the list of property listing
            PropertyListingDto propertyDto = apiResponse.Data;


            return View(propertyDto);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}