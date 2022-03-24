using System;
using System.Collections.Generic;

namespace HelperLand.ViewModels
{
    public class ServiceRequestsViewModel
    {
        public string CustomerName { get; set; }
        public float Distance { get; set; }
        public bool IsConflicting { get; set; }
        public int ServiceRequestId { get; set; }
        public int ServiceId { get; set; }
        public string ServiceDate { get; set; }
        public float StartTimeFloat { get; set; }
        public string ServiceStartTime { get; set; }
        public string ServiceEndTime { get; set; }
        public float TotalAmount { get; set; }
        public int SPUserId { get; set; }
        public string SPName { get; set; }
        public string SPAvtar { get; set; }
        public float SPRating { get; set; }
        public float Duration { get; set; }
        public string ExtraServices { get; set; }
        public string Street { get; set; }
        public string HouseNumber { get; set; }
        public string City { get; set; }
        public string PostalCode { get; set; }
        public string ServiceAddress { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
        public string Comments { get; set; }
        public bool HasPets { get; set; }
        public int? Status { get; set; } 
        public Guid RecordVersion { get; set; }
        public string ZipCode { get; set; }
        public bool IsCompleted { get; set; }
    }
}
