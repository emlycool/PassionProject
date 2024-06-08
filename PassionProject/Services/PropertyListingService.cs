using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI.WebControls;
using PassionProject.Models;

namespace PassionProject.Services
{
    public class PropertyListingService : BaseService<PropertyListing>
    {
        public PropertyListingService(ApplicationDbContext context) : base(context)
        {
        }

        public IEnumerable<PropertyListing> GetAllWithDetails()
        {
            return this.EagerLoadData(_context.PropertyListings);
        }

        public IEnumerable<PropertyListing> GetByUserId(string userId)
        {
            return this.EagerLoadData(_context.PropertyListings.Where(propertyListing => propertyListing.UserId == userId));
        }

        public IEnumerable<PropertyListing> EagerLoadData(IQueryable<PropertyListing> query)
        {
            return query.Include(p => p.MediaItems)
                        .Include(p => p.User)
                        .ToList();
        }


        public async Task<PropertyListing> GetByNameAsync(string name)
        {
            return await _context.PropertyListings.FirstOrDefaultAsync(pl => pl.Name == name);
        }

        public async Task SaveMediaFilesAsync(List<Media> mediaFiles, PropertyListing propertyListing)
        {
            foreach (var media in mediaFiles)
            {
                media.PropertyListingId = propertyListing.Id;
                _context.Media.Add(media);
            }
            await _context.SaveChangesAsync();
        }

        public async Task<List<Media>> HandleFileUpload(Collection<HttpContent> contents, string uploadDirectory)
        {
            var mediaFiles = new List<Media>();

            foreach (var file in contents)
            {
                if (file.Headers.ContentDisposition.Name.Trim('"').Equals("media"))
                {
                    // Process the file part
                    var fileData = await file.ReadAsByteArrayAsync();
                    var fileName = file.Headers.ContentDisposition.FileName.Trim('"');

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
                    mediaFiles.Add(new Media
                    {
                        Disk = "local",
                        Tag = "property-image",
                        FileName = fileName,
                        Extension = Path.GetExtension(fileName),
                        FileSize = fileData.Length.ToString(),
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
            return mediaFiles;
        }

        public bool ValidatePropertyListing(PropertyListingDto propertyListingDto, out List<ValidationResult> validationResults)
        {
            var validationContext = new ValidationContext(propertyListingDto, null, null);
            validationResults = new List<ValidationResult>();
            return Validator.TryValidateObject(propertyListingDto, validationContext, validationResults, true);
        }

        public void LoadMediaItems(PropertyListing propertyListing)
        {
            _context.Entry(propertyListing).Collection(p => p.MediaItems).Load();
        }

        public void LoadUser(PropertyListing propertyListing)
        {
            _context.Entry(propertyListing).Reference(p => p.User).Load();
        }

        public PropertyListing GetBySlug(string slug)
        {
            return _context.PropertyListings
                .Include(p => p.MediaItems)
                .Include(p => p.User)
                .FirstOrDefault(p => p.Slug == slug);
        }

        public PropertyListing GetById(int id)
        {
            return _context.PropertyListings
                .Include(p => p.MediaItems)
                .Include(p => p.User)
                .FirstOrDefault(p => p.Id == id);
        }

        public async Task UpdateAsync(PropertyListing propertyListing)
        {
            _context.Entry(propertyListing).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(PropertyListing propertyListing)
        {
            _context.PropertyListings.Remove(propertyListing);
            await _context.SaveChangesAsync();
        }
    }
}
