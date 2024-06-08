using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Policy;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Http.Routing;
using Newtonsoft.Json;

namespace PassionProject.Models
{
    public class PropertyListing
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public decimal Price { get; set; }
        public int NoBedRooms { get; set; }
        public int NoBathRooms { get; set; }
        public decimal SquareFootage { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
        public string Features { get; set; }

        [StringLength(128)]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        public DateTime? PublishedAt { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public ICollection<Media> MediaItems { get; set; }

        public PropertyListingDto ToPropertyListingDto(UrlHelper urlhelper)
        {
            return new PropertyListingDto()
            {
                Id = Id,
                Name = Name,
                Slug = Slug,
                Price = Price,
                NoBedRooms = NoBedRooms,
                NoBathRooms = NoBathRooms,
                Description = Description,
                Status = Status,
                Type = Type,
                Features = Features == null ? null : JsonConvert.DeserializeObject<string[]>(Features),
                SquareFootage = SquareFootage,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt,
                PublishedAt = PublishedAt,
                Agent = User == null ? null : new AgentDto()
                {
                    FirstName = User.FirstName,
                    LastName = User.LastName,
                    BusinessName = User.BusinessName,
                    Email = User.Email,
                    Phone = User.PhoneNumber
                },
                MediaItems = User == null ? null : MediaItems.Select(media => new MediaDto
                {
                    Id = media.Id,
                    Tag = media.Tag,
                    FileName = media.FileName,
                    Extension = media.Extension,
                    Url = urlhelper.Content("~/Uploads/" + media.FileName)
                }).ToList()
            };
        }

        public void UpdateFromDto(PropertyListingDto dto)
        {
            // Update properties from DTO
            Name = dto.Name;
            Price = dto.Price;
            NoBedRooms = dto.NoBedRooms;
            NoBathRooms = dto.NoBathRooms;
            SquareFootage = dto.SquareFootage;
            Description = dto.Description;
            Status = dto.Status;
            Type = dto.Type;
            Features = dto.SerializeFeaturesToJson(dto.Features);
            Slug = dto.Slugify(dto.Name);
            

            // TODO: I may add other related entities here or keep seperate e.g Media, Agent/User details
        }

    }

    public class PropertyListingDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; }
        public string Slug { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be a positive number")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Number of bedrooms is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Number of bedrooms must be at least 1")]
        public int NoBedRooms { get; set; }

        [Required(ErrorMessage = "Number of bathrooms is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Number of bathrooms must be at least 1")]
        public int NoBathRooms { get; set; }

        [Required(ErrorMessage = "Square footage is required")]
        [Range(1, double.MaxValue, ErrorMessage = "Square footage must be a positive number")]
        public decimal SquareFootage { get; set; }


        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Status is required")]
        public string Status { get; set; }

        [Required(ErrorMessage = "Type is required")]
        public string Type { get; set; }

        public string[] Features { get; set; }

        public int UserId { get; set; }
        public DateTime? PublishedAt { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public AgentDto Agent { get; set; }

        public ICollection<MediaDto> MediaItems { get; set; } = new List<MediaDto>();

        public string Slugify(string name)
        {
            // Convert to lowercase
            string slug = name.ToLowerInvariant();

            // Replace spaces with hyphens
            slug = Regex.Replace(slug, @"\s+", "-");

            // Remove non-alphanumeric characters
            slug = Regex.Replace(slug, @"[^a-z0-9\-_]", "");

            // Trim hyphens from the beginning and end
            slug = slug.Trim('-');

            return slug;
        }

        public string SerializeFeaturesToJson(string[] features)
        {
            if (features == null)
            {
                return null; // Handle null input (optional)
            }

            return JsonConvert.SerializeObject(features);
        }

        public PropertyListing HydrateModel()
        {
            return new PropertyListing()
            {
               Name = this.Name,
               Description = this.Description,  
               Status = this.Status,        
               Type = this.Type,
               Price = this.Price,
               NoBathRooms = this.NoBathRooms,
               NoBedRooms = this.NoBedRooms,
               Features = this.SerializeFeaturesToJson(this.Features),
               Slug = this.Slugify(this.Name),
               SquareFootage = this.SquareFootage
            };
        }
    }

    public class AgentDto
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string BusinessName { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; } 
    }
}