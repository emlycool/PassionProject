using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace PassionProject.Models
{
    public class Media
    {
        [Key]
        public int Id { get; set; }

        public int PropertyListingId { get; set; }
        [ForeignKey("PropertyListingId")]
        public virtual PropertyListing PropertyListing { get; set; }

        public string Disk {  get; set; }

        public string Tag { get; set; }

        public string FileName { get; set; }

        public string Extension { get; set; }

        public string FileSize { get; set; }


        public DateTime? CreatedAt { get; set; }
    }

    public class MediaDto
    {
        public int Id { get; set; }

        public int EntityTypeId { get; set; }

        public string EntityType { get; set; }

        public string Disk { get; set; }

        public string Tag { get; set; }

        public string FileName { get; set; }

        public string Extension { get; set; }

        public string Url { get; set; }
    }
}