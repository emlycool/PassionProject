using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PassionProject.Models
{
    public class HomeViewModel
    {
        public List<PropertyListingDto> PropertyListings { get; set; }
        public FilterPropertyDto FilterPropertyDto { get; set; }
    }
}