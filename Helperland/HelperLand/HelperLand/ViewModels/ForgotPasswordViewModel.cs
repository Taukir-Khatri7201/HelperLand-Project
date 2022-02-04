using System.ComponentModel.DataAnnotations;

namespace HelperLand.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Enter registered email address")]
        public string Email { get; set; }
    }
}
