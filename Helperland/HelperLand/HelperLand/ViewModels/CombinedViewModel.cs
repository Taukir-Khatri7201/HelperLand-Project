using HelperLand.Models;
using System;
using System.Collections.Generic;

namespace HelperLand.ViewModels
{
    public class CombinedViewModel
    {
        public LoginViewModel LoginModel { get; set; }
        public ForgotPasswordViewModel forgotPassModel { get; set; }
        public ServiceRequestsWrapperViewModel serviceRequests { get; set; }
        public RescheduleServiceViewModel rescheduleService { get; set; }
        public CancelCompleteRequestViewModel cancelRequest { get; set; }
        public Rating spRating { get; set; }
        public AddressViewModel address { get; set; }
        public UserDetailsUpdateViewModel details { get; set; }
        public List<AddressViewModel> userAddresses { get; set; }
        public ChangePasswordViewModel changePassword { get; set; }
        public List<SpecialUserViewModel> specialUsers { get; set; }
        public AdminRescheduleServiceViewModel adminRescheduleService { get; set; }
        public List<UserDetailsViewModel> allUsers { get; set; }
    }
}
