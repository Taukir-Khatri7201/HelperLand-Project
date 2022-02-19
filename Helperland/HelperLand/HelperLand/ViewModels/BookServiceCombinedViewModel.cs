namespace HelperLand.ViewModels
{
    public class BookServiceCombinedViewModel
    {
        public PostalCodeViewModel postalcode { get; set; }

        public ScheduleAndPlanViewModel scheduleAndPlan { get; set; }
        public AddressViewModel address { get; set; }

        public int startTime { get; set; }
        public int totalamount { get; set; }
        public int totalservicetime { get; set; }
        public int extraservicetime { get; set; }
        public int basichrs { get; set; }
    }
}
