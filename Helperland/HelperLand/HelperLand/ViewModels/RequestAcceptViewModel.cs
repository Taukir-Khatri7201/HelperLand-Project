using System;

namespace HelperLand.ViewModels
{
    public class RequestAcceptViewModel
    {
        public int ServiceRequestId { get; set; }
        public string ServiceId { get; set; }
        public int ServiceProviderId { get; set; }
        public Guid OldRecordVersion { get; set; }
        public string ZipCode { get; set; }
    }
}
