using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HelperLand.ViewModels
{
    public class ScheduleAndPlanViewModel
    {
        public string Code { get; set; }

        [Required(ErrorMessage = "Please select service date")]
        [DataType(DataType.Date, ErrorMessage = "Invalid format")]
        public DateTime ServiceStartDate { get; set; }

        [Required(ErrorMessage = "Please select service start time")]
        public float StartTime { get; set; }

        [Required(ErrorMessage = "Please select total time")]
        public float ServiceHrs { get; set; }

        public Dictionary<string, bool> ExtraServices { get; set; }

        public string Comments { get; set; }

        public bool HavePets { get; set; }

        public float Total { get; set; }
    }
}
