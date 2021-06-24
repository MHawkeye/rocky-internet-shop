using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rocky_Models
{
    public class ApplicationUser:IdentityUser
    {
        public string FullName { get; set; }
        public string City { get; set; }
        public string StreetAddress { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
    }
}
