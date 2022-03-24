namespace HelperLand.ViewModels
{
    public class AdminRescheduleServiceViewModel
    {
        public RescheduleServiceViewModel serviceDetails { get; set; }
        public AddressViewModel address { get; set; }
        public AddressViewModel invoiceAddress { get; set; }
        public string reason { get; set; }
        public string callCenterNotes { get; set; }
    }
}
