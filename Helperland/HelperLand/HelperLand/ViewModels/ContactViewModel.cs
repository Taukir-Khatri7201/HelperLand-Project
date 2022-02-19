using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace HelperLand.ViewModels
{
    public class ContactViewModel
    {
        [Required(ErrorMessage = "Please enter first name")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Please enter last name")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Please enter email address")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Please enter mobile number")]
        public string Mobile { get; set; }

        [Required]
        public string Type { get; set; }

        [Required(ErrorMessage = "Enter your query")]
        public string Message { get; set; }

        public IFormFile FilePath { get; set; }
    }

    //public class AcceptPrivacy : ValidationAttribute
    //{
    //    protected override ValidationResult IsValid(object value, ValidationContext context)
    //    {
    //        if (context.ObjectInstance == null)
    //        {
    //            return new ValidationResult("Please accept privacy policy");
    //        }
    //        var val = (ContactViewModel)context.ObjectInstance;
    //        return (val.Privacy == true) ? ValidationResult.Success : new ValidationResult("Please accept privacy policy");
    //    }
    //}
}


