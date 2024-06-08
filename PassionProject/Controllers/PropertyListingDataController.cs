using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Xml.Linq;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using PassionProject.Helpers;
using PassionProject.Models;

namespace PassionProject.Controllers
{

    public class PropertyListingDataController : ApiController
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        [HttpGet]
        [Route("api/property-listings")]
        public IHttpActionResult PropertyListings()
        {
            List<PropertyListing> propertyListings = db.PropertyListings
            .Include(p => p.MediaItems)
            .Include(p => p.User)
            .ToList();

            List<PropertyListingDto> propertyListingDtos = new List<PropertyListingDto>();

            propertyListings.ForEach(propertyListing =>
            {
                propertyListingDtos.Add(propertyListing.ToPropertyListingDto(Url));
            });


            return ResponseHelper.JsonResponse("Property listings retrieved successfully", HttpStatusCode.OK, true, data: propertyListingDtos);
        }

        [HttpPost]
        [Authorize]
        [Route("api/property-listings")]
        public async Task<IHttpActionResult> StorePropertyListing()
        {
            // Check if the request contains multipart/form-data.
            if (!Request.Content.IsMimeMultipartContent())
            {
                return ResponseHelper.JsonResponse("Unsupported media type", HttpStatusCode.UnsupportedMediaType, false);
            }

            var provider = new MultipartMemoryStreamProvider();
            await Request.Content.ReadAsMultipartAsync(provider);

            // Extract the JSON part of the request
            PropertyListingDto propertyListingDto = new PropertyListingDto();
            List<Media> mediaFiles = new List<Media>();

            foreach (var content in provider.Contents)
            {
                if (content.Headers.ContentDisposition.Name.Trim('"').Equals("media"))
                {
                    // Process the file part
                    var fileData = await content.ReadAsByteArrayAsync();
                    var fileName = content.Headers.ContentDisposition.FileName.Trim('"');

                    // Get the directory path where you want to save the files
                    string uploadDirectory = HttpContext.Current.Server.MapPath("~/Uploads");

                    // Check if the directory exists, and create it if it doesn't
                    if (!Directory.Exists(uploadDirectory))
                    {
                        Directory.CreateDirectory(uploadDirectory);
                    }

                    // Combine the directory path with the file name to get the full file path
                    string filePath = Path.Combine(uploadDirectory, fileName);

                    // Save the file to the specified path
                    File.WriteAllBytes(filePath, fileData);

                    // Add media
                    mediaFiles.Add(new Media()
                    {
                        Disk = "local", // for only handling storing locally
                        Tag = "property-image",
                        FileName = fileName,
                        Extension = Path.GetExtension(fileName),
                        FileSize = fileData.Length.ToString(), // File size in bytes
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            // Extract form data
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

            // Manually validate the model
            var validationContext = new ValidationContext(propertyListingDto, null, null)
            {
                Items = { { typeof(ApplicationDbContext), db } } // Pass the DbContext as a service to the validation context
            };
            var validationResults = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(propertyListingDto, validationContext, validationResults, true);

            if (!isValid)
            {
                // Collect all validation errors
                var errors = validationResults.ToDictionary(
                    vr => vr.MemberNames.FirstOrDefault() ?? string.Empty,
                    vr => vr.ErrorMessage
                );

                return ResponseHelper.JsonResponse("Request is invalid", HttpStatusCode.BadRequest, false, errors: errors);
            }

            // check if name is taken
            // Check if a property listing with the same name already exists
            PropertyListing existingListing = db.PropertyListings.FirstOrDefault(pl => pl.Name == propertyListingDto.Name);

            if (existingListing != null)
            {
                return ResponseHelper.JsonResponse("Property name has been taken", HttpStatusCode.BadRequest, false, errors: new
                {
                    Name = "Name has been taken"
                });
            }


            // Create PropertyListing from DTO
            PropertyListing propertyListing = propertyListingDto.HydrateModel();
            propertyListing.UserId = User.Identity.GetUserId();

            // Save PropertyListing to database
            db.PropertyListings.Add(propertyListing);
            await db.SaveChangesAsync();

            // Load user associated with the property listing
            db.Entry(propertyListing).Reference(p => p.User).Load();

            // Save media files to database
            foreach (var media in mediaFiles)
            {
                media.PropertyListingId = propertyListing.Id;
                db.Media.Add(media);
            }

            // Save all changes to the database
            await db.SaveChangesAsync();

            // Load media associated with the property listing
            db.Entry(propertyListing).Collection(p => p.MediaItems).Load();

            propertyListingDto = propertyListing.ToPropertyListingDto(Url);

            return ResponseHelper.JsonResponse("Property listing created successfully", HttpStatusCode.Created, true, propertyListingDto);
        }
    }   
}