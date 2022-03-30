using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace HelperLand.ViewModels
{
    public class ContactViewModel
    {
        [Required(ErrorMessage = "Please enter first name")]
        [StringLength(25, ErrorMessage = "Firstname length cannot be more than 25 characters")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Please enter last name")]
        [StringLength(25, ErrorMessage = "Lastname length cannot be more than 25 characters")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Please enter email address")]
        [StringLength(100, ErrorMessage = "Email length cannot be more than 100 characters")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Please enter mobile number")]
        [RegularExpression(@"^[6789]\d{9}$", ErrorMessage = "Invalid Mobile Number")]
        public string Mobile { get; set; }

        [Required]
        [StringLength(500)]
        public string Type { get; set; }

        [Required(ErrorMessage = "Enter your query")]
        public string Message { get; set; }

        public IFormFile FilePath { get; set; }
    }
}


