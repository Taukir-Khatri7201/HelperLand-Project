using System.ComponentModel.DataAnnotations;

namespace HelperLand.ViewModels
{
    public class CancelCompleteRequestViewModel
    {
        public int ServiceId { get; set; }
        public int ServiceRequestId { get; set; }
        public int ServiceProviderId { get; set; }

        [Required(ErrorMessage = "Please enter a reason")]
        public string Reason { get; set; }
    }
}
