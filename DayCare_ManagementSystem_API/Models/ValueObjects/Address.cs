using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace DayCare_ManagementSystem_API.Models.ValueObjects
{
    public class Address
    {
        public string? StreetNumber { get; set; }

        [Required(ErrorMessage = "Street Name is required")]
        public string StreetName { get; set; }

        [Required(ErrorMessage = "Suburb is required")]
        public string Suburb { get; set; }

        [Required(ErrorMessage = "City is required")]
        public string City { get; set; }

        [Required(ErrorMessage = "Suburb is required")]
        public string Province { get; set; }

        [Required(ErrorMessage = "PostalCode is required")]
        public string PostalCode { get; set; }
        public string Country { get; set; } = "South Africa";

        public string? FullAdress { get; set; }

    }


}
