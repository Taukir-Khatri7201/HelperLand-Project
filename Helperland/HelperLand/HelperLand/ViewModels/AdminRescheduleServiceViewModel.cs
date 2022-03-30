using System.ComponentModel.DataAnnotations;

namespace HelperLand.ViewModels
{
    public class AdminRescheduleServiceViewModel
    {
        public RescheduleServiceViewModel serviceDetails { get; set; }
        public AddressViewModel address { get; set; }
        public AddressViewModel invoiceAddress { get; set; }

        [StringLength(500, ErrorMessage = "Reason length cannot be more than 500 characters")]
        public string reason { get; set; }
        public string callCenterNotes { get; set; }
    }
}
