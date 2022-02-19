using System.ComponentModel.DataAnnotations;

namespace HelperLand.ViewModels
{
    public class PostalCodeViewModel
    {
        [Required(ErrorMessage = "Please enter postal code")]
        [RegularExpression(@"^\d{5}$", ErrorMessage = "Invalid postal code")]
        public string code { get; set; }
    }
}
