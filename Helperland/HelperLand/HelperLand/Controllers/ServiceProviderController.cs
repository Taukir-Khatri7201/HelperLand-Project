using HelperLand.Data;
using HelperLand.Extensions;
using HelperLand.Models;
using HelperLand.Security;
using HelperLand.Utility;
using HelperLand.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace HelperLand.Controllers
{
    [Authorize(Roles = "ServiceProvider")]
    public class ServiceProviderController : BaseController
    {
        private readonly HelperlandDBContext context;
        private readonly IEmailSender emailSender;
        private readonly ICustomDataProtector protector;
        public object ServiceRequestLock = new object();
        public JsonSerializerOptions serializerOptions = new JsonSerializerOptions() {
            PropertyNameCaseInsensitive = false
        };

        public ServiceProviderController(HelperlandDBContext context, IEmailSender emailSender, IHttpContextAccessor httpContextAccessor, ICustomDataProtector protector) : base(httpContextAccessor)
        {
            this.context = context;
            this.emailSender = emailSender;
            this.protector = protector;
        }

        public IActionResult NewServiceRequests()
        {
            CombinedViewModel model = getServiceRequests(1);
            return View(model);
        }
        
        public IActionResult UpcomingServices()
        {
            CombinedViewModel model = getServiceRequests(2);
            return View(model);
        }

        public IActionResult ServiceHistory()
        {
            CombinedViewModel model = getServiceRequests(3);
            return View(model);
        }

        public IActionResult MyRatings()
        {
            CombinedViewModel combinedViewModel = new CombinedViewModel();
            combinedViewModel.serviceRequests = new ServiceRequestsWrapperViewModel();
            combinedViewModel.serviceRequests.Requests = new List<ServiceRequestsViewModel>();
            var available = context.Ratings.Where(m => m.RatingTo == loggedUser.UserId);
            if(available.Any())
            {
                var allRatings = available.ToList();
                foreach(var rating in allRatings)
                {
                    var user = context.Users.Where(u => u.UserId == rating.RatingFrom).First();
                    ServiceRequest request = context.ServiceRequests.Where(r => r.ServiceRequestId == rating.ServiceRequestId).First();
                    TimeSpan StartTimeSpan = request.ServiceStartDate.TimeOfDay;
                    var TotalServiceHrs = (int)request.ServiceHours;
                    var TotalServiceMinutes = (int)request.ServiceHours < request.ServiceHours ? 30 : 0;
                    TimeSpan EndTimeSpan = new TimeSpan(
                                                    StartTimeSpan.Hours + TotalServiceHrs,
                                                    StartTimeSpan.Minutes + TotalServiceMinutes, 0);

                    ServiceRequestsViewModel model = new ServiceRequestsViewModel()
                    {
                        ServiceId = request.ServiceId,
                        ServiceRequestId = rating.ServiceRequestId,
                        CustomerName = user.FirstName + " " + user.LastName,
                        ServiceDate = formatDate(request.ServiceStartDate.Date),
                        ServiceStartTime = convertToString(StartTimeSpan),
                        ServiceEndTime = convertToString(EndTimeSpan),
                        SPRating = (float)rating.Ratings,
                        Comments = rating.Comments,
                    };
                    combinedViewModel.serviceRequests.Requests.Add(model);
                }
            }
            return View(combinedViewModel);
        }

        public IActionResult BlockCustomer()
        {
            var available = context.ServiceRequests.Where(s => s.ServiceProviderId == loggedUser.UserId).Select(s => s.UserId).Distinct().ToList();
            CombinedViewModel model = new CombinedViewModel();
            model.specialUsers = new List<SpecialUserViewModel>();
            foreach(var userId in available)
            {
                var user = context.Users.Where(u => u.UserId == userId).First();
                var userDetails = new SpecialUserViewModel()
                {
                    UserId = userId,
                    UserName = user.FirstName + " " + user.LastName,
                    UserAvatar = user.UserProfilePicture,
                };
                var isBlocked = context.FavoriteAndBlockeds.Where(m => m.UserId == loggedUser.UserId
                                                                       && m.TargetUserId == userId
                                                                       && m.IsBlocked == true).Any();
                userDetails.IsBlocked = isBlocked;

                if(string.IsNullOrEmpty(userDetails.UserAvatar))
                {
                    userDetails.UserAvatar = "/assets/avatar-male.png";
                }
                model.specialUsers.Add(userDetails);
            }
            return View(model);
        }

        public IActionResult my_settings()
        {
            CombinedViewModel finalModel = new CombinedViewModel();
            finalModel.userAddresses = new List<AddressViewModel>();
            finalModel.details = new UserDetailsUpdateViewModel();
            var available = context.UserAddresses.Where(a => a.UserId == loggedUser.UserId);
            if(loggedUser.NationalityId == null)
            {
                loggedUser.NationalityId = (int)Nationality.Germen;
            }
            finalModel.details.Nationality = (int)loggedUser.NationalityId;
            if (available.Any())
            {
                var allAddresses = available.Select(a => new AddressViewModel
                {
                    AddressId = a.AddressId,
                    Street = a.AddressLine1,
                    HouseNumber = a.AddressLine2,
                    City = a.City,
                    Mobile = a.Mobile,
                    PostalCode = a.PostalCode,
                    IsDefault = a.IsDefault,
                });
                finalModel.userAddresses.AddRange(allAddresses);
            }
            else
            {
                var newAddress = new AddressViewModel();
                finalModel.userAddresses.Add(newAddress);
            }
            return View(finalModel);
        }

        [HttpPost]
        public IActionResult BlockUnblockUser(SpecialUserViewModel model)
        {
            if(ModelState.IsValid)
            {
                var available = context.FavoriteAndBlockeds.Where(m => m.UserId == loggedUser.UserId
                                                                       && m.TargetUserId == model.UserId);
                if(available.Any())
                {
                    var user = available.First();
                    user.IsBlocked = model.IsBlocked;
                }
                else
                {
                    FavoriteAndBlocked user = new FavoriteAndBlocked()
                    {
                        UserId = loggedUser.UserId,
                        TargetUserId = model.UserId,
                        IsBlocked = model.IsBlocked,
                    };
                    context.FavoriteAndBlockeds.Add(user);
                }
                context.SaveChanges();
                return Json(new { Status = true }, serializerOptions);
            }
            return Json(new { Status = false }, serializerOptions);
        }

        public async Task<IActionResult> AcceptService(RequestAcceptViewModel model)
        {
            if(ModelState.IsValid)
            {
                bool flag = false;
                var request = context.ServiceRequests.Where(s => s.ServiceRequestId == model.ServiceRequestId).FirstOrDefault();
                lock(ServiceRequestLock)
                {
                    if(request.RecordVersion == model.OldRecordVersion)
                    {
                        request.RecordVersion = Guid.NewGuid();
                        request.Status = (int)(ServiceRequestStatus.Accepted);
                        request.ServiceProviderId = model.ServiceProviderId;
                        request.SpacceptedDate = DateTime.Now;
                        context.SaveChanges();
                        flag = true;
                    }
                }
                if(flag == true)
                {
                    string[] allServiceProviders = context.Users.Where(s => s.ZipCode == model.ZipCode
                                                                        && s.UserTypeId == (int)(Roles.ServiceProvider)
                                                                        && s.UserId != model.ServiceProviderId).Select(s => s.Email).ToArray();
                    var subject = "Service no longer available!";
                    var body = "<div>" +
                                    "<h3>Hello,</h3>" +
                                    "<div>" +
                                            "Service request <b>#" + model.ServiceId + "</b> is no longer available. " +
                                    "</div><br>" +
                                    "<div>Best regards,</div>" +
                                    "<div style='font-size: 16px;'><b>Helperland team</b></div>" +
                                "</div>";

                    Message message = new Message(allServiceProviders, subject, body);
                    await emailSender.SendEmail(message);

                    return Json(new { Status = true, Msg = "Service request accepted successfully." }, serializerOptions);
                }
            }
            return Json(new { 
                                Status = false, 
                                Msg = "This service request is no more available. It has been assigned to another provider." 
                            }, serializerOptions);
        }
        
        [HttpPost]
        public IActionResult CancelRequest(CancelCompleteRequestViewModel model)
        {
            var res = CancelCompleteRequest(model, (int)ServiceRequestStatus.Cancelled);
            return res.Result;
        }

        [HttpPost]
        public IActionResult CompleteRequest(CancelCompleteRequestViewModel model)
        {
            var res = CancelCompleteRequest(model, (int)ServiceRequestStatus.Completed);
            return res.Result;
        }

        private CombinedViewModel getServiceRequests(int type)
        {
            // type = 1 -> NewServiceRequests
            // type = 2 -> UpcomingServiceRequests
            // type = 3 -> Service History

            DateTime dateTime = DateTime.Now;
            IQueryable<Models.ServiceRequest> available = null;
            if (type == 1) {
                var allBlockedUsersBySP = context.FavoriteAndBlockeds
                                                        .Where(x => x.UserId == loggedUser.UserId && x.IsBlocked == true)
                                                        .Select(x => x.TargetUserId).ToHashSet();
                var allUsersWhoBlockedSP = context.FavoriteAndBlockeds
                                                        .Where(x => x.TargetUserId == loggedUser.UserId && x.IsBlocked == true)
                                                        .Select(x => x.UserId).ToHashSet();

                available = context.ServiceRequests.Where(s => s.ServiceStartDate >= DateTime.Now
                                                               && s.Status < (int)ServiceRequestStatus.Accepted
                                                               && s.ServiceProviderId == null
                                                               && !allBlockedUsersBySP.Contains(s.UserId)
                                                               && !allUsersWhoBlockedSP.Contains(s.UserId));
            }
            else if (type == 2)
            {
                available = context.ServiceRequests.Where(s => s.ServiceProviderId == loggedUser.UserId
                                                               && s.Status < (int)ServiceRequestStatus.Cancelled);
            }
            else if(type == 3)
            {
                available = context.ServiceRequests.Where(s => s.ServiceProviderId == loggedUser.UserId
                                                               && s.Status == (int)ServiceRequestStatus.Completed);
            }
            CombinedViewModel combinedViewModel = new CombinedViewModel();
            combinedViewModel.serviceRequests = new ServiceRequestsWrapperViewModel();
            combinedViewModel.serviceRequests.Requests = new List<ServiceRequestsViewModel>();

            if (available.Any())
            {
                var allRequests = available.OrderBy(s => s.ServiceStartDate).ToList();
                foreach (var request in allRequests)
                {
                    TimeSpan StartTimeSpan = request.ServiceStartDate.TimeOfDay;
                    var TotalServiceHrs = (int)request.ServiceHours;
                    var TotalServiceMinutes = (int)request.ServiceHours < request.ServiceHours ? 30 : 0;
                    TimeSpan EndTimeSpan = new TimeSpan(
                                                    StartTimeSpan.Hours + TotalServiceHrs,
                                                    StartTimeSpan.Minutes + TotalServiceMinutes, 0);
                    var addresss = context.ServiceRequestAddresses.Where(s => s.ServiceRequestId == request.ServiceRequestId).FirstOrDefault();
                    var extras = context.ServiceRequestExtras.Where(s => s.ServiceRequestId == request.ServiceRequestId).Select(s => new
                    {
                        Id = s.ServiceExtraId
                    });
                    string extraServicesStr = "";
                    if (extras.Count() > 0)
                    {
                        foreach (var extra in extras)
                        {
                            if (extra.Id == 1) extraServicesStr += "Inside Cabinets, ";
                            if (extra.Id == 2) extraServicesStr += "Inside Fridge, ";
                            if (extra.Id == 3) extraServicesStr += "Inside Oven, ";
                            if (extra.Id == 4) extraServicesStr += "Laudary wash & Dry, ";
                            if (extra.Id == 5) extraServicesStr += "Interior Windows, ";
                        }
                        extraServicesStr = extraServicesStr.Remove(extraServicesStr.Length - 2);
                    }
                    var customer = context.Users.Where(m => m.UserId == request.UserId).First();

                    float serviceStartTime = convertTimeSpanToFloat(StartTimeSpan);
                    float serviceEndTime = convertTimeSpanToFloat(EndTimeSpan);

                    ServiceRequestsViewModel currentRequest = new ServiceRequestsViewModel()
                    {
                        Distance = 0,
                        CustomerName = customer.FirstName + " " + customer.LastName,
                        ServiceRequestId = request.ServiceRequestId,
                        ServiceId = request.ServiceId,
                        ServiceDate = formatDate(request.ServiceStartDate.Date),
                        ServiceStartTime = convertToString(StartTimeSpan),
                        ServiceEndTime = convertToString(EndTimeSpan),
                        TotalAmount = (float)request.TotalCost,
                        Duration = (float)request.ServiceHours,
                        Comments = request.Comments,
                        Email = addresss.Email,
                        Mobile = addresss.Mobile,
                        ServiceAddress = addresss.AddressLine1 + " " + addresss.AddressLine2 + ", " +
                                         addresss.PostalCode + " " + addresss.City,
                        ExtraServices = extraServicesStr,
                        HasPets = request.HasPets,
                        RecordVersion = (Guid)request.RecordVersion,
                        ZipCode = request.ZipCode,
                        IsCompleted = (request.ServiceStartDate.AddHours(TotalServiceHrs).AddMinutes(TotalServiceMinutes) < DateTime.Now),
                    };
                    combinedViewModel.serviceRequests.Requests.Add(currentRequest);
                }
            }
            return combinedViewModel;
        }
        
        private async Task<IActionResult> CancelCompleteRequest(CancelCompleteRequestViewModel model, int type)
        {
            if (ModelState.IsValid)
            {
                var valid = context.ServiceRequests.Where(m => m.ServiceRequestId == model.ServiceRequestId);
                if (valid.Any())
                {
                    string errorMsg = "";
                    string additionalMsg = "";
                    var prevRequest = valid.FirstOrDefault();
                    prevRequest.Status = (int)ServiceRequestStatus.Completed;
                    if(type == (int)ServiceRequestStatus.Cancelled)
                    {
                        errorMsg = "Service has been cancelled successfully";
                        additionalMsg = "Cancelled Request Id: " + model.ServiceId;
                        prevRequest.Status = (int)ServiceRequestStatus.Cancelled;
                        string[] Customer = context.Users.Where(x => x.UserId == prevRequest.UserId)
                                                    .Select(x => x.Email).ToArray();
                        string subject = "Service Cancelled!";
                        string body = "<div>" +
                                            "<h3>Hello,</h3>" +
                                            "<div>" +
                                                    "Service request <b>#" + model.ServiceId + "</b> has been cancelled by service provider." +
                                            "</div><br>" +
                                            "<div>Best regards,</div>" +
                                            "<div style='font-size: 16px;'><b>Helperland team</b></div>" +
                                       "</div>";

                        Message message = new Message(Customer, subject, body);
                        await emailSender.SendEmail(message);
                    }

                    context.ServiceRequests.Update(prevRequest);
                    context.SaveChanges();

                    return Json(new { Status = true, Msg = errorMsg, AdditionalMsg = additionalMsg }, serializerOptions);
                }
            }

            return Json(new { Status = false, Msg = "Something went wrong.", AdditionalMsg = "" }, serializerOptions);
        }

        [HttpPost]
        public IActionResult UpdateDetails(UserDetailsUpdateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var available = context.Users.Where(s => s.UserId == model.UserId);
                if (available.Any())
                {
                    var user = available.First();
                    DateTime dob = new DateTime(model.Year, model.Month, model.Day);
                    user.FirstName = model.FirstName;
                    user.LastName = model.LastName;
                    user.Mobile = model.Mobile;
                    user.DateOfBirth = dob;
                    user.NationalityId = model.Nationality;
                    user.Gender = model.Gender;
                    user.UserProfilePicture = model.UserAvatar;
                    user.ModifiedBy = model.UserId;
                    user.ModifiedDate = DateTime.Now;
                    context.Update(user);
                    HttpContext.Session.Remove("User");
                    HttpContext.Session.Set<User>("User", user);
                    loggedUser = user;

                    var addressAvailable = context.UserAddresses.Where(s => s.UserId == model.UserId);
                    if(addressAvailable.Any())
                    {
                        var address = addressAvailable.First();
                        address.AddressLine1 = model.Address.Street;
                        address.AddressLine2 = model.Address.HouseNumber;
                        address.PostalCode = model.Address.PostalCode;
                        address.City = model.Address.City;
                        context.Update(address);
                    }
                    else
                    {
                        var address = new UserAddress()
                        {
                            UserId = model.UserId,
                            AddressLine1 = model.Address.Street,
                            AddressLine2 = model.Address.HouseNumber,
                            PostalCode = model.Address.PostalCode,
                            City = model.Address.City,
                            Email = loggedUser.Email,
                            IsDefault = true,
                            IsDeleted = false,
                            Mobile = loggedUser.Mobile,
                        };
                        context.Add(address);
                    }
                    context.SaveChanges();
                    return Json(new { Status = true, Msg = "Done", AdditionalMsg = "" }, serializerOptions);
                }

            }
            return Json(new { Status = false, Msg = "Something went wrong.", AdditionalMsg = "" }, serializerOptions);
        }

        [HttpPost]
        public IActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = context.Users.Where(u => u.UserId == loggedUser.UserId).First();
                if (user != null)
                {
                    if (protector.Decrypt(user.Password) != model.OldPassword)
                    {
                        return Json(new { Status = false, Msg = "Incorrect old password", AdditionalMsg = "" }, serializerOptions);
                    }
                    user.Password = protector.Encrypt(model.Password);
                    context.Update(user);
                    context.SaveChanges();

                    return Json(new { Status = true, Msg = "Password updated successfully.", AdditionalMsg = "" }, serializerOptions);
                }
            }
            return Json(new { Status = false, Msg = "Something went wrong.", AdditionalMsg = "" }, serializerOptions);
        }
        // ------------- Utility Functions ------------- //
        private string convertToString(TimeSpan timeSpan)
        {
            var Hrs = timeSpan.Hours;
            var HrsStr = "";
            if (Hrs >= 0 && Hrs <= 9)
            {
                HrsStr += "0";
            }
            HrsStr += Hrs.ToString();

            var Mins = timeSpan.Minutes;
            var MinsStr = "";
            if (Mins >= 0 && Mins <= 9)
            {
                MinsStr += "0";
            }
            MinsStr += Mins.ToString();
            return HrsStr + ":" + MinsStr;
        }
        private string convertToStringUsingFloatTime(float time)
        {
            TimeSpan timeSpan = new TimeSpan((int)time, (Math.Floor(time) == Math.Ceiling(time) ? 0 : 30), 0);
            string ans = convertToString(timeSpan);
            return ans;
        }
        private float convertTimeSpanToFloat(TimeSpan timeSpan)
        {
            float ans = timeSpan.Hours;
            if (timeSpan.Minutes > 0) ans += (float)(0.5);
            return ans;
        }
        private float getRating(int? id)
        {
            float averageRating = 0;
            float ratingSum = 0;
            int noOfRatings = 0;

            if (context.Users.Any(s => s.UserId == id))
            {
                var allRatings = context.Ratings.Where(s => s.RatingTo == id).Select(x => new
                {
                    userRating = x.Ratings,
                }).ToList();
                noOfRatings = allRatings.Count;
                foreach (var rating in allRatings)
                {
                    ratingSum += (float)rating.userRating;
                }
                noOfRatings = Math.Max(noOfRatings, 1);
                averageRating = ratingSum / noOfRatings;
            }
            return averageRating;
        }
        private string formatDate(DateTime dateTime)
        {
            string formattedDate = "";
            var Day = dateTime.Day;
            var DayStr = (Day >= 0 && Day <= 9) ? "0" : "";
            DayStr += Day.ToString();

            var Month = dateTime.Month;
            var MonthStr = (Month >= 1 && Month <= 9) ? "0" : "";
            MonthStr += Month.ToString();

            formattedDate = DayStr + "/" + MonthStr + "/" + dateTime.Year.ToString();
            return formattedDate;
        }
        private bool isConflicting(float startTime, float endTime, DateTime date, float duration, int ServiceId)
        {
            var allUpcomingServices = context.ServiceRequests.Where(s => s.ServiceId != ServiceId && s.ServiceProviderId == loggedUser.UserId &&
                                                                         s.ServiceStartDate.Date == date.Date);
            if (allUpcomingServices.Any())
            {
                var allRequestsTimeSpan = allUpcomingServices.Select(s => new
                {
                    ServiceId = s.ServiceId,
                    StartTime = s.ServiceStartDate.TimeOfDay,
                    EndTime = s.ServiceStartDate.TimeOfDay.Add(
                                new TimeSpan(
                                    (int)s.ServiceHours,
                                    Math.Floor(s.ServiceHours) == Math.Ceiling(s.ServiceHours) ? 0 : 30, 0)
                              ),
                    ServiceDate = s.ServiceStartDate.Date,
                }).ToList();

                var allRequestsTime = allRequestsTimeSpan.Select(s => new
                {
                    ServiceId = s.ServiceId,
                    StartTime = convertTimeSpanToFloat(s.StartTime),
                    EndTime = (float)(s.EndTime.Hours + (s.EndTime.Minutes > 0 ? 0.5 : 0)),
                    ServiceDate = s.ServiceDate,
                }).ToList();

                var conflictedServices = allRequestsTime.Where(s => s.ServiceId != ServiceId &&
                                                (startTime >= s.StartTime && startTime <= s.EndTime ||
                                                 endTime >= s.StartTime && endTime <= s.EndTime ||
                                                 startTime <= s.StartTime && endTime >= s.EndTime));
                if (conflictedServices.Any())
                {
                    return true;
                }
            }
            return false;
        }
    }

    public enum Nationality
    {
        Germen = 1,
        Indian,
    }
}
