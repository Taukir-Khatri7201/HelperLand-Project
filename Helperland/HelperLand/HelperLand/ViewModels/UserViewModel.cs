using System;
using System.ComponentModel.DataAnnotations;

namespace HelperLand.ViewModels
{
    public class UserViewModel
    {
        [Required(ErrorMessage = "Enter First Name")]
        [DataType(DataType.Text)]
        [StringLength(100, ErrorMessage = "Firstname length cannot be more than 100 characters")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Enter Last Name")]
        [DataType(DataType.Text)]
        [StringLength(100, ErrorMessage = "Lastname length cannot be more than 100 characters")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Enter Email Address")]
        [RegularExpression(@"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$", ErrorMessage = "Invalid Email")]
        [StringLength(100, ErrorMessage = "Email length cannot be more than 100 characters")]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [Required(ErrorMessage = "Enter Password")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@#$%^&+=]).{6,14}$", ErrorMessage = "Password must contain atleast 6 to 14 characters, 1 Alphabet, 1 Number, 1 Special Character")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Enter Confirm Password")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Password do not match")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Enter Mobile Number")]
        [RegularExpression(@"^[6789]\d{9}$", ErrorMessage = "Invalid Mobile Number")]
        public string Mobile { get; set; }
    }
}
