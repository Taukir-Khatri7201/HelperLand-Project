using System;
using System.ComponentModel.DataAnnotations;

namespace HelperLand.ViewModels
{
    public class UserDetailsUpdateViewModel
    {
        [Required]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Please enter first name")]
        [StringLength(100, ErrorMessage = "Firstname cannot be more than 100 characters")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Please enter last name")]
        public string LastName { get; set; }

        public string Email { get; set; }

        [Required(ErrorMessage = "Please enter phone number")]
        [RegularExpression(@"^[6789]\d{9}$", ErrorMessage = "Invalid Mobile Number")]
        public string Mobile { get; set; }

        public int Day { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public int Nationality { get; set; }
        public int Language { get; set; }
        public string UserAvatar { get; set; }
        public int Gender { get; set; }
        public AddressViewModel Address { get; set; }
    }
}
