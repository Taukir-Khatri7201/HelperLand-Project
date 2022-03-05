using System;
using System.ComponentModel.DataAnnotations;

namespace HelperLand.ViewModels
{
    public class RescheduleServiceViewModel
    {
        public int ServiceId { get; set; }

        [Required(ErrorMessage = "Please select service date")]
        [DataType(DataType.Date, ErrorMessage = "Invalid format")]
        public DateTime ServiceStartDate { get; set; }

        [Required(ErrorMessage = "Please select service start time")]
        public float StartTime { get; set; }
    }
}
