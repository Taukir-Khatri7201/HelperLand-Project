using System.ComponentModel.DataAnnotations;

namespace HelperLand.ViewModels
{
    public class AddressViewModel
    {
        [Required(ErrorMessage = "Enter Street Name")]
        public string Street { get; set; }
        [Required(ErrorMessage = "Enter House Number")]
        [RegularExpression(@"\d+", ErrorMessage = "Enter House Number in form of digits")]
        public string HouseNumber { get; set; }
        [Required(ErrorMessage = "Enter Postal Code")]
        public string PostalCode { get; set; }
        [Required(ErrorMessage = "Enter City")]
        public string City { get; set; }
        [Required(ErrorMessage = "Enter Mobile Number")]
        public string Mobile { get; set; }
    }
}
