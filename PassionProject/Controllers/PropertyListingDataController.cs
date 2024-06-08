using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Microsoft.AspNet.Identity;
using PassionProject.Helpers;
using PassionProject.Models;
using PassionProject.Services;

namespace PassionProject.Controllers
{
    public class PropertyListingDataController : ApiController
    {
        private readonly PropertyListingService _propertyListingService;

        public PropertyListingDataController()
        {
            var db = new ApplicationDbContext();
            _propertyListingService = new PropertyListingService(db);
        }

        /// <summary>
        /// Retrieves all property listings.
        /// </summary>
        /// <returns>An HTTP response message containing the list of property listings.</returns>
        [HttpGet]
        [Route("api/property-listings")]
        public IHttpActionResult PropertyListings()
        {
            var propertyListings = _propertyListingService.GetAllWithDetails();

            List<PropertyListingDto> propertyListingDtos = propertyListings
                .Select(propertyListing => propertyListing.ToPropertyListingDto(Url))
                .ToList();

            return ResponseHelper.JsonResponse("Property listings retrieved successfully", HttpStatusCode.OK, true, data: propertyListingDtos);
        }

        /// <summary>
        /// Retrieves property listings for the authenticated user.
        /// </summary>
        /// <returns>An HTTP response message containing the list of property listings.</returns>
        [HttpGet]
        [Authorize]
        [Route("api/user/property-listings")]
        public IHttpActionResult PropertyListingsForAuthUser()
        {
            string userId = User.Identity.GetUserId();
            var propertyListings = _propertyListingService.GetByUserId(userId);

            List<PropertyListingDto> propertyListingDtos = propertyListings
                .Select(propertyListing => propertyListing.ToPropertyListingDto(Url))
                .ToList();


            return ResponseHelper.JsonResponse("Property listings retrieved successfully", HttpStatusCode.OK, true, data: propertyListingDtos);
        }

        /// <summary>
        /// Stores a new property listing.
        /// </summary>
        /// <returns>An HTTP response message containing the result of the operation.</returns>
        [HttpPost]
        [Authorize]
        [Route("api/property-listings")]
        public async Task<IHttpActionResult> StorePropertyListing()
        {
            if (!Request.Content.IsMimeMultipartContent())
            {
                return ResponseHelper.JsonResponse("Unsupported media type", HttpStatusCode.UnsupportedMediaType, false);
            }

            var provider = new MultipartMemoryStreamProvider();
            await Request.Content.ReadAsMultipartAsync(provider);

            PropertyListingDto propertyListingDto = new PropertyListingDto();
            string uploadDirectory = HttpContext.Current.Server.MapPath("~/Uploads");
            List<Media> mediaFiles = await _propertyListingService.HandleFileUpload(provider.Contents, uploadDirectory);

            propertyListingDto.Name = HttpContext.Current.Request.Form["Name"];
            propertyListingDto.Price = Convert.ToDecimal(HttpContext.Current.Request.Form["Price"]);
            propertyListingDto.NoBedRooms = Convert.ToInt32(HttpContext.Current.Request.Form["NoBedRooms"]);
            propertyListingDto.NoBathRooms = Convert.ToInt32(HttpContext.Current.Request.Form["NoBathRooms"]);
            propertyListingDto.SquareFootage = Convert.ToDecimal(HttpContext.Current.Request.Form["SquareFootage"]);
            propertyListingDto.Description = HttpContext.Current.Request.Form["Description"];
            propertyListingDto.Status = HttpContext.Current.Request.Form["Status"];
            propertyListingDto.Type = HttpContext.Current.Request.Form["Type"];
            propertyListingDto.Features = HttpContext.Current.Request.Form.GetValues("Features[]");

            if (propertyListingDto == null)
            {
                return ResponseHelper.JsonResponse("Property listing data is null", HttpStatusCode.BadRequest, false);
            }

            if (!_propertyListingService.ValidatePropertyListing(propertyListingDto, out var validationResults))
            {
                var errors = validationResults.ToDictionary(
                    vr => vr.MemberNames.FirstOrDefault() ?? string.Empty,
                    vr => vr.ErrorMessage
                );  

                return ResponseHelper.JsonResponse("Request is invalid", HttpStatusCode.BadRequest, false, errors: errors);
            }

            var existingListing = await _propertyListingService.GetByNameAsync(propertyListingDto.Name);

            if (existingListing != null)
            {
                return ResponseHelper.JsonResponse("Property name has been taken", HttpStatusCode.BadRequest, false, errors: new
                {
                    Name = "Name has been taken"
                });
            }

            var propertyListing = propertyListingDto.HydrateModel();
            propertyListing.UserId = User.Identity.GetUserId();

            await _propertyListingService.AddAsync(propertyListing);
            await _propertyListingService.SaveMediaFilesAsync(mediaFiles, propertyListing);

            _propertyListingService.LoadMediaItems(propertyListing);
            _propertyListingService.LoadUser(propertyListing);

            propertyListingDto = propertyListing.ToPropertyListingDto(Url);

            return ResponseHelper.JsonResponse("Property listing created successfully", HttpStatusCode.Created, true, propertyListingDto);
        }

        /// <summary>
        /// Retrieves a property listing by its slug.
        /// </summary>
        /// <param name="slug">The slug of the property listing.</param>
        /// <returns>An HTTP response message containing the property listing.</returns>
        [HttpGet]
        [Route("api/property-listings/{slug}")]
        public IHttpActionResult GetPropertyListingBySlug(string slug)
        {
            var propertyListing = _propertyListingService.GetBySlug(slug);

            if (propertyListing == null)
            {
                return ResponseHelper.JsonResponse("Property listing not found", HttpStatusCode.NotFound, false);
            }

            _propertyListingService.LoadMediaItems(propertyListing);
            _propertyListingService.LoadUser(propertyListing);

            var propertyListingDto = propertyListing.ToPropertyListingDto(Url);
            return ResponseHelper.JsonResponse("Property listing retrieved successfully", HttpStatusCode.OK, true, propertyListingDto);
        }

        /// <summary>
        /// Updates an existing property listing.
        /// </summary>
        /// <param name="id">The ID of the property listing to update.</param>
        /// <param name="updatedDto">The updated details of the property listing.</param>
        /// <returns>An HTTP response message containing the result of the operation.</returns>
        [HttpPut]
        [Authorize]
        [Route("api/property-listings/{id}")]
        public async Task<IHttpActionResult> UpdatePropertyListing(int id, PropertyListingDto updatedDto)
        {
            // Check if the user is authorized to update this property listing
            var userId = User.Identity.GetUserId();
            var propertyListing = _propertyListingService.GetById(id);

            if (propertyListing == null)
            {
                return ResponseHelper.JsonResponse("Property listing not found", HttpStatusCode.NotFound, false);
            }

            if (propertyListing.UserId != userId)
            {
                return ResponseHelper.JsonResponse("Unauthorized", HttpStatusCode.Unauthorized, false);
            }

            // Validate the updated DTO
            if (!_propertyListingService.ValidatePropertyListing(updatedDto, out var validationResults))
            {
                var errors = validationResults.ToDictionary(
                    vr => vr.MemberNames.FirstOrDefault() ?? string.Empty,
                    vr => vr.ErrorMessage
                );

                return ResponseHelper.JsonResponse("Request is invalid", HttpStatusCode.BadRequest, false, errors: errors);
            }

            // Update the property listing
            propertyListing.UpdateFromDto(updatedDto);
            await _propertyListingService.UpdateAsync(propertyListing);

            // Load related entities
            _propertyListingService.LoadMediaItems(propertyListing);
            _propertyListingService.LoadUser(propertyListing);

            var propertyListingDto = propertyListing.ToPropertyListingDto(Url);
            return ResponseHelper.JsonResponse("Property listing updated successfully", HttpStatusCode.OK, true, propertyListingDto);
        }

        /// <summary>
        /// Deletes a property listing.
        /// </summary>
        /// <param name="id">The ID of the property listing to delete.</param>
        /// <returns>An HTTP response message containing the result of the operation.</returns>
        [HttpDelete]
        [Authorize]
        [Route("api/property-listings/{id}")]
        public async Task<IHttpActionResult> DeletePropertyListing(int id)
        {
            // Check if the user is authorized to delete this property listing
            var userId = User.Identity.GetUserId();
            var propertyListing = _propertyListingService.GetById(id);

            if (propertyListing == null)
            {
                return ResponseHelper.JsonResponse("Property listing not found", HttpStatusCode.NotFound, false);
            }

            if (propertyListing.UserId != userId)
            {
                return ResponseHelper.JsonResponse("Unauthorized", HttpStatusCode.Unauthorized, false);
            }

            // Delete the property listing
            await _propertyListingService.DeleteAsync(propertyListing);

            return ResponseHelper.JsonResponse("Property listing deleted successfully", HttpStatusCode.OK, true);
        }
    }
}
