namespace HelperLand.ViewModels
{
    public class BookServiceCombinedViewModel
    {
        public PostalCodeViewModel postalcode { get; set; }

        public ScheduleAndPlanViewModel scheduleAndPlan { get; set; }
        public AddressViewModel address { get; set; }

        public float startTime { get; set; }
        public float totalamount { get; set; }
        public float totalservicetime { get; set; }
        public float extraservicetime { get; set; }
        public float basichrs { get; set; }
    }
}
