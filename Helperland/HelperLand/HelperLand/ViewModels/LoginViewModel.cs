using System.ComponentModel.DataAnnotations;

namespace HelperLand.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Enter registered email address")]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [DataType(DataType.Password)]
        [Required(ErrorMessage = "Enter Password")]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }
}
