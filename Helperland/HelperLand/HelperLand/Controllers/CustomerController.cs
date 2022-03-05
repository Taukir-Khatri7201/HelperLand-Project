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
    [Authorize(Roles = "Customer")]
    public class CustomerController : BaseController
    {
        private readonly HelperlandDBContext context;
        private readonly IEmailSender emailSender;
        private readonly ICustomDataProtector protector;
        public JsonSerializerOptions serializerOptions = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = false
        };

        public CustomerController(HelperlandDBContext context, IEmailSender emailSender, IHttpContextAccessor httpContextAccessor, ICustomDataProtector protector):base(httpContextAccessor)
        {
            this.context = context;
            this.emailSender = emailSender;
            this.protector = protector;
        }
        public IActionResult ServiceRequests()
        {
            DateTime dateTime = DateTime.Now;
            var available = context.ServiceRequests.Any(s => s.ServiceStartDate >= dateTime &&
                                                                 s.UserId == loggedUser.UserId && s.Status != (int)ServiceRequestStatus.Cancelled);
            CombinedViewModel combinedViewModel = new CombinedViewModel();
            combinedViewModel.serviceRequests = new ServiceRequestsWrapperViewModel();
            combinedViewModel.serviceRequests.Requests = new List<ServiceRequestsViewModel>();

            if(available)
            {
                var allRequests = context.ServiceRequests.Where(s => s.ServiceStartDate >= dateTime && 
                                                                     s.UserId == loggedUser.UserId && 
                                                                     s.Status != (int)ServiceRequestStatus.Cancelled).OrderBy(s => s.ServiceStartDate).ToList();
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
                    if(extras.Count() > 0)
                    {
                        foreach(var extra in extras)
                        {
                            if (extra.Id == 1) extraServicesStr += "Inside Cabinets, ";
                            if (extra.Id == 2) extraServicesStr += "Inside Fridge, ";
                            if (extra.Id == 3) extraServicesStr += "Inside Oven, ";
                            if (extra.Id == 4) extraServicesStr += "Laudary wash & Dry, ";
                            if (extra.Id == 5) extraServicesStr += "Interior Windows, ";
                        }
                        extraServicesStr = extraServicesStr.Remove(extraServicesStr.Length - 2);
                    }

                    var serviceProvider = new User();
                    ServiceProviderData SPModel = new ServiceProviderData()
                    {
                        SPUserId = 0,
                        SPName = "",
                        SPAvtar = "",
                        SPRating = 0,
                    };
                    if(request.ServiceProviderId != null)
                    {
                        serviceProvider = context.Users.Where(s => s.UserId == request.ServiceProviderId).FirstOrDefault();
                        SPModel.SPUserId = serviceProvider.UserId;
                        SPModel.SPName = serviceProvider.FirstName + " " + serviceProvider.LastName;
                        SPModel.SPAvtar = serviceProvider.UserProfilePicture;
                        SPModel.SPRating = getRating(request.ServiceProviderId);
                    }
                    if(string.IsNullOrEmpty(SPModel.SPAvtar))
                    {
                        SPModel.SPAvtar = "/assets/avatar-male.png";
                    }
                    ServiceRequestsViewModel currentRequest = new ServiceRequestsViewModel() {
                        ServiceRequestId = request.ServiceRequestId,
                        ServiceId = request.ServiceId,
                        ServiceDate = formatDate(request.ServiceStartDate.Date),
                        ServiceStartTime = convertToString(StartTimeSpan),
                        ServiceEndTime = convertToString(EndTimeSpan),
                        TotalAmount = (float)request.TotalCost,
                        SPUserId = SPModel.SPUserId,
                        SPName = SPModel.SPName,
                        SPAvtar = SPModel.SPAvtar,
                        SPRating = SPModel.SPRating,
                        Duration = (float)request.ServiceHours,
                        Comments = request.Comments,
                        Email = addresss.Email,
                        Mobile = addresss.Mobile,
                        ServiceAddress = addresss.AddressLine1 + " " + addresss.AddressLine2 + ", " +
                                         addresss.PostalCode + " " + addresss.City,
                        ExtraServices = extraServicesStr,
                        HasPets = request.HasPets,
                    };
                    combinedViewModel.serviceRequests.Requests.Add(currentRequest);
                }
            }
            return View(combinedViewModel);
        }
        public IActionResult ServiceHistory()
        {
            var available = context.ServiceRequests.Any(s => s.UserId == loggedUser.UserId && s.Status >= 4);
            CombinedViewModel combinedViewModel = new CombinedViewModel();
            combinedViewModel.serviceRequests = new ServiceRequestsWrapperViewModel();
            combinedViewModel.serviceRequests.Requests = new List<ServiceRequestsViewModel>();

            if (available)
            {
                var allRequests = context.ServiceRequests.Where(s => s.UserId == loggedUser.UserId && s.Status >= 4)
                                                         .OrderBy(s => s.ServiceStartDate).ToList();
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

                    var serviceProvider = new User();
                    ServiceProviderData SPModel = new ServiceProviderData()
                    {
                        SPName = "",
                        SPAvtar = "",
                        SPRating = 0,
                    };
                    if (request.ServiceProviderId != null)
                    {
                        serviceProvider = context.Users.Where(s => s.UserId == request.ServiceProviderId).FirstOrDefault();
                        SPModel.SPUserId = serviceProvider.UserId;
                        SPModel.SPName = serviceProvider.FirstName + " " + serviceProvider.LastName;
                        SPModel.SPAvtar = serviceProvider.UserProfilePicture;
                        SPModel.SPRating = getRating(request.ServiceProviderId);
                    }
                    if (string.IsNullOrEmpty(SPModel.SPAvtar))
                    {
                        SPModel.SPAvtar = "/assets/avtar-male.png";
                    }
                    ServiceRequestsViewModel currentRequest = new ServiceRequestsViewModel()
                    {
                        ServiceRequestId = request.ServiceRequestId,
                        ServiceId = request.ServiceId,
                        ServiceDate = formatDate(request.ServiceStartDate.Date),
                        ServiceStartTime = convertToString(StartTimeSpan),
                        ServiceEndTime = convertToString(EndTimeSpan),
                        TotalAmount = (float)request.TotalCost,
                        SPName = SPModel.SPName,
                        SPAvtar = SPModel.SPAvtar,
                        SPRating = SPModel.SPRating,
                        SPUserId = SPModel.SPUserId,
                        Duration = (float)request.ServiceHours,
                        Comments = request.Comments,
                        Email = addresss.Email,
                        Mobile = addresss.Mobile,
                        ServiceAddress = addresss.AddressLine1 + " " + addresss.AddressLine2 + ", " +
                                         addresss.PostalCode + " " + addresss.City,
                        ExtraServices = extraServicesStr,
                        HasPets = request.HasPets,
                        Status = request.Status,
                    };
                    combinedViewModel.serviceRequests.Requests.Add(currentRequest);
                }
            }
            return View(combinedViewModel);
        }
        public IActionResult FavouritePros()
        {
            var available = context.ServiceRequests.Where(s => s.UserId == loggedUser.UserId && s.ServiceProviderId != null)
                                                   .Select(s => s.ServiceProviderId).Distinct().ToList();
            CombinedViewModel model = new CombinedViewModel();
            model.specialUsers = new List<SpecialUserViewModel>();
            foreach (var userId in available)
            {
                var user = context.Users.Where(u => u.UserId == userId).First();
                var userDetails = new SpecialUserViewModel()
                {
                    UserId = (int)userId,
                    UserName = user.FirstName + " " + user.LastName,
                    UserAvatar = user.UserProfilePicture,
                };
                var isBlocked = context.FavoriteAndBlockeds.Where(m => m.UserId == loggedUser.UserId
                                                                       && m.TargetUserId == userId
                                                                       && m.IsBlocked == true).Any();
                var isFavorite = context.FavoriteAndBlockeds.Where(m => m.UserId == loggedUser.UserId
                                                                       && m.TargetUserId == userId
                                                                       && m.IsFavorite == true).Any();
                userDetails.IsBlocked = isBlocked;
                userDetails.IsFavorite = isFavorite;
                userDetails.ratings = getRating(userId);
                userDetails.cleanings = context.ServiceRequests.Where(s => s.ServiceProviderId == userId
                                                                           && s.Status == (int)ServiceRequestStatus.Completed).Count();

                if (string.IsNullOrEmpty(userDetails.UserAvatar))
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
            var available = context.UserAddresses.Where(a => a.UserId == loggedUser.UserId);
            if(available.Any())
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
            return View(finalModel);
        }

        [HttpPost]
        public async Task<IActionResult> RescheduleService(RescheduleServiceViewModel model)
        {
            if(ModelState.IsValid)
            {
                var valid = context.ServiceRequests.Where(m => m.ServiceId == model.ServiceId);
                if (valid.Any() == true)
                {
                    var prevRequest = valid.FirstOrDefault();
                    var duration = (float)prevRequest.ServiceHours;
                    var startTime = model.StartTime;
                    var endTime = (float)(startTime + duration);
                    string errorMsg = isConflicting(startTime, endTime, model.ServiceStartDate, duration, prevRequest.ServiceProviderId, model.ServiceId);
                    if (!string.IsNullOrEmpty(errorMsg))
                    {
                        return Json(new { Status = false, Msg = "Service Provider is busy", AdditionalMsg = errorMsg }, serializerOptions);
                    }
                    errorMsg = $"Rescheduled ServiceId: {model.ServiceId}";

                    var newDate = formatDate(model.ServiceStartDate);
                    var StartTime = convertToStringUsingFloatTime(model.StartTime);
                    var EndTime = convertToStringUsingFloatTime(model.StartTime + duration);

                    var newDateTime = new DateTime(model.ServiceStartDate.Year, model.ServiceStartDate.Month, model.ServiceStartDate.Day,
                                                    (int)model.StartTime, Math.Floor(model.StartTime) == Math.Ceiling(model.StartTime) ? 0 : 30, 0);
                    prevRequest.ServiceStartDate = newDateTime;
                    context.ServiceRequests.Update(prevRequest);
                    context.SaveChanges();

                    string[] ServiceProvider;
                    string subject = "Service Rescheduled!";
                    string body;
                    if (prevRequest.ServiceProviderId != null)
                    {
                        ServiceProvider = context.Users
                                                        .Where(x => x.UserId == prevRequest.ServiceProviderId)
                                                        .Select(x => x.Email).ToArray();
                        body = "<div>" +
                                        "<h3>Hello,</h3>" +
                                        "<div>" +
                                                "Service request <b>#" + model.ServiceId + "</b> has been rescheduled by customer." +
                                                $" New date and time are { formatDate(model.ServiceStartDate) } {StartTime} - {EndTime}" +
                                        "</div><br>" +
                                        "<div>Best regards,</div>" +
                                        "<div style='font-size: 16px;'><b>Helperland team</b></div>" +
                                    "</div>";
                    } 
                    else
                    {
                        ServiceProvider = context.Users
                                                        .Where(x => x.UserTypeId == 2 && x.ZipCode == prevRequest.ZipCode)
                                                        .Select(x => x.Email).ToArray();
                        subject = "New Service Available (Rescheduled)";

                        body = "<div>" +
                                    "<h3>Hello,</h3>" +
                                    "<div>" +
                                          "Service request <b>" + model.ServiceId + "</b> is now available in your area. " +
                                          "Please log in at <b>www.Helperland.com</b> to view further details of the request and to confirm it." +
                                    "</div><br>" +
                                    "<div>Best regards,</div>" +
                                    "<div style='font-size: 16px;'><b>Helperland team</b></div>" +
                                "</div>";
                    }

                    Message message = new Message(ServiceProvider, subject, body);
                    await emailSender.SendEmail(message);

                    return Json(new { Status = true, Msg = "Service has been rescheduled successfully", AdditionalMsg = errorMsg }, serializerOptions);
                }
            }

            return Json(new { Status = false, Msg = "Something went wrong.", AdditionalMsg = "" }, serializerOptions);
        }

        [HttpPost]
        public async Task<IActionResult> CancelRequest(CancelCompleteRequestViewModel model)
        {
            if (ModelState.IsValid)
            {
                var valid = context.ServiceRequests.Where(m => m.ServiceId == model.ServiceId);
                if (valid.Any())
                {
                    string errorMsg = "";
                    var prevRequest = valid.FirstOrDefault();
                    string[] ServiceProvider;
                    string subject = "Service Cancelled!";
                    string body;
                    if (prevRequest.ServiceProviderId != null)
                    {
                        ServiceProvider = context.Users
                                                        .Where(x => x.UserId == prevRequest.ServiceProviderId)
                                                        .Select(x => x.Email).ToArray();

                        body = "<div>" +
                                    "<h3>Hello,</h3>" +
                                    "<div>" +
                                            "Service request <b>#" + model.ServiceId + "</b> has been cancelled by customer." +
                                    "</div><br>" +
                                    "<div>Best regards,</div>" +
                                    "<div style='font-size: 16px;'><b>Helperland team</b></div>" +
                                "</div>";

                        Message message = new Message(ServiceProvider, subject, body);
                        await emailSender.SendEmail(message);
                    }

                    prevRequest.Status = (int)ServiceRequestStatus.Cancelled;
                    context.ServiceRequests.Update(prevRequest);
                    context.SaveChanges();

                    errorMsg = "Service has been cancelled successfully";
                    string additionalMsg = "Cancelled Request Id: " + model.ServiceId;

                    return Json(new { Status = true, Msg = errorMsg, AdditionalMsg = additionalMsg }, serializerOptions);
                }
            }

            return Json(new { Status = false, Msg = "Something went wrong.", AdditionalMsg = "" }, serializerOptions);
        }

        [HttpPost]
        public IActionResult ServiceProviderRating(Rating rating)
        {
            if (ModelState.IsValid)
            {
                rating.RatingDate = DateTime.Now;
                var available = context.Ratings.Where(r => r.ServiceRequestId == rating.ServiceRequestId);
                if(available.Any())
                {
                    var updateRating = available.First();
                    updateRating.OnTimeArrival = rating.OnTimeArrival;
                    updateRating.Friendly = rating.Friendly;
                    updateRating.QualityOfService = rating.QualityOfService;
                    updateRating.Comments = rating.Comments;
                    updateRating.RatingDate = rating.RatingDate;
                    updateRating.Ratings = rating.Ratings;
                    context.Update(updateRating);
                    context.SaveChanges();
                }
                else
                {
                    context.Ratings.Add(rating);
                    context.SaveChanges();
                }
                return Json(new { Status = true, Msg = "Done", AdditionalMsg = "" }, serializerOptions);
            }
            return Json(new { Status = false, Msg = "Something went wrong.", AdditionalMsg = "" }, serializerOptions);
        }

        [HttpPost]
        public IActionResult FavoriteUnfavoriteUser(SpecialUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var available = context.FavoriteAndBlockeds.Where(m => m.UserId == loggedUser.UserId
                                                                       && m.TargetUserId == model.UserId);
                if (available.Any())
                {
                    var user = available.First();
                    user.IsFavorite = model.IsFavorite;
                }
                else
                {
                    FavoriteAndBlocked user = new FavoriteAndBlocked()
                    {
                        UserId = loggedUser.UserId,
                        TargetUserId = model.UserId,
                        IsFavorite = model.IsFavorite,
                    };
                    context.FavoriteAndBlockeds.Add(user);
                }
                context.SaveChanges();
                return Json(new { Status = true }, serializerOptions);
            }
            return Json(new { Status = false }, serializerOptions);
        }

        [HttpPost]
        public IActionResult BlockUnblockUser(SpecialUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var available = context.FavoriteAndBlockeds.Where(m => m.UserId == loggedUser.UserId
                                                                       && m.TargetUserId == model.UserId);
                if (available.Any())
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

        [HttpPost]
        public IActionResult UpdateDetails(UserDetailsUpdateViewModel model)
        {
            if(ModelState.IsValid)
            {
                var available = context.Users.Where(s => s.UserId == model.UserId);
                if(available.Any()) 
                { 
                    var user = available.First();
                    DateTime dob = new DateTime(model.Year, model.Month, model.Day);
                    user.FirstName = model.FirstName;
                    user.LastName = model.LastName;
                    user.Mobile = model.Mobile;
                    user.DateOfBirth = dob;
                    user.LanguageId = 1;
                    user.ModifiedBy = model.UserId;
                    user.ModifiedDate = DateTime.Now;
                    context.Update(user);
                    context.SaveChanges();
                    HttpContext.Session.Remove("User");
                    HttpContext.Session.Set<User>("User", user);
                    loggedUser = user;
                    return Json(new { Status = true, Msg = "Done", AdditionalMsg = "" }, serializerOptions);
                }

            }
            return Json(new { Status = false, Msg = "Something went wrong.", AdditionalMsg = "" }, serializerOptions);
        }

        [HttpPost]
        public IActionResult EditAddress(AddressViewModel model)
        {
            if(ModelState.IsValid)
            {
                var available = context.UserAddresses.Where(s => s.AddressId == model.AddressId);
                if (available.Any())
                {
                    var address = available.First();
                    address.AddressLine1 = model.Street;
                    address.AddressLine2 = model.HouseNumber;
                    address.PostalCode = model.PostalCode;
                    address.City = model.City;
                    address.Mobile = model.Mobile;
                    context.Update(address);
                    context.SaveChanges();

                    return Json(new { Status = true, Msg = "Done", AdditionalMsg = "" }, serializerOptions);
                }
            }
            return Json(new { Status = false, Msg = "Something went wrong.", AdditionalMsg = "" }, serializerOptions);
        }
        
        [HttpPost]
        public IActionResult UpdateDefaultAddress(DeleteDefaultAddressViewModel model)
        {
            if(ModelState.IsValid)
            {
                var available = context.UserAddresses.Where(m => m.UserId == loggedUser.UserId);
                if(available.Any())
                {
                    var allAddresses = available.Select(x => x);
                    foreach(var address in allAddresses)
                    {
                        address.IsDefault = false;
                    }
                    var newDefault = allAddresses.Where(x => x.AddressId == model.AddressId).Select(x => x).First();
                    newDefault.IsDefault = true;
                    context.SaveChanges();
                    return Json(new { Status = true, Msg = "Done", AdditionalMsg = "" }, serializerOptions);
                }
            }
            return Json(new { Status = false, Msg = "Something went wrong.", AdditionalMsg = "" }, serializerOptions);
        }

        [HttpPost]
        public IActionResult DeleteAddress(DeleteDefaultAddressViewModel model)
        {
            if (ModelState.IsValid)
            {
                var available = context.UserAddresses.Where(m => m.UserId == loggedUser.UserId && m.AddressId == model.AddressId);
                if (available.Any())
                {
                    var addresesToBeDeleted = available.First();
                    context.Remove(addresesToBeDeleted);
                    context.SaveChanges();
                    return Json(new { Status = true, Msg = "Done", AdditionalMsg = "" }, serializerOptions);
                }
            }
            return Json(new { Status = false, Msg = "Something went wrong.", AdditionalMsg = "" }, serializerOptions);
        }

        [HttpPost]
        public IActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if(ModelState.IsValid)
            {
                var user = context.Users.Where(u => u.UserId == loggedUser.UserId).Select(u => u).First();
                if(user != null)
                {
                    if(protector.Decrypt(user.Password) != model.OldPassword)
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
            if(Hrs >= 0 && Hrs <= 9)
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
            TimeSpan timeSpan = new TimeSpan((int)time, (Math.Floor(time) == Math.Ceiling(time) ? 0: 30), 0);
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
            
            if(context.Users.Any(s=>s.UserId == id))
            {
                var allRatings = context.Ratings.Where(s => s.RatingTo == id).Select(x => new
                {
                    userRating = x.Ratings,
                }).ToList();
                noOfRatings = allRatings.Count;
                foreach(var rating in allRatings)
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
        private string isConflicting(float startTime, float endTime, DateTime date, float duration, int? ServiceProviderId, int ServiceId)
        {
            string msg = "";
            if (endTime > 21)
            {
                msg = "Total Duration of the service Exceeds time limit of 9:00 PM";
                return msg;
            }
            if(ServiceProviderId == null)
            {
                return msg;
            }
            var allRequestsOnGivenDate = context.ServiceRequests
                                            .Where(s => s.ServiceStartDate.Date == date.Date && 
                                                        s.ServiceProviderId == ServiceProviderId &&
                                                        s.Status != (int)ServiceRequestStatus.Cancelled);
            if(allRequestsOnGivenDate.Any())
            {
                var allRequestsTimeSpan = allRequestsOnGivenDate.Select(s => new
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

                var conflictedServices = allRequestsTime.Where(s =>
                                                (startTime >= s.StartTime && startTime <= s.EndTime) ||
                                                (endTime >= s.StartTime && endTime <= s.EndTime) ||
                                                (startTime <= s.StartTime && endTime >= s.EndTime));
                if (!conflictedServices.Any() || (conflictedServices.Count() == 1 && conflictedServices.First().ServiceId == ServiceId)) 
                {
                    return msg;
                }
                var firstConflict = conflictedServices.First();

                msg = "Another service request has been assigned to the service provider " +
                    $"on {formatDate(firstConflict.ServiceDate)} " +
                    $"from {convertToStringUsingFloatTime(firstConflict.StartTime)} " +
                    $"to {convertToStringUsingFloatTime(firstConflict.EndTime)}. " +
                    "Either choose another date or pick up a different time slot.";
            }
            return msg;
        }
    }

    public class ServiceProviderData
    {
        public int SPUserId { get; set; }
        public string SPName { get; set; }
        public string SPAvtar { get; set; }
        public float SPRating { get; set; }
    };
}
