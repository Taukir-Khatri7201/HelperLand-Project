using System.ComponentModel.DataAnnotations;

namespace HelperLand.ViewModels
{
    public class AddressViewModel
    {
        public int AddressId { get; set; }
        [Required(ErrorMessage = "Enter Street Name")]
        public string Street { get; set; }
        [Required(ErrorMessage = "Enter House Number")]
        [RegularExpression(@"\d+", ErrorMessage = "Enter House Number in form of digits")]
        public string HouseNumber { get; set; }
        [Required(ErrorMessage = "Enter Postal Code")]
        public string PostalCode { get; set; }
        [Required(ErrorMessage = "Enter City")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "City name cannot be more than 50 characters")]
        public string City { get; set; }
        [Required(ErrorMessage = "Enter Mobile Number")]
        [RegularExpression(@"^[6789]\d{9}$", ErrorMessage = "Invalid Mobile Number")]
        public string Mobile { get; set; }
        public bool IsDefault { get; set; }
    }
}
