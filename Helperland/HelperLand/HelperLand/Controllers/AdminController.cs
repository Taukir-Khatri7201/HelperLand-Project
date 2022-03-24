using HelperLand.Data;
using HelperLand.Models;
using HelperLand.Utility;
using HelperLand.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace HelperLand.Controllers
{
    public class AdminController : BaseController
    {
        private readonly HelperlandDBContext context;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IEmailSender emailSender;
        public JsonSerializerOptions serializerOptions = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = false
        };

        public AdminController(HelperlandDBContext context, IHttpContextAccessor httpContextAccessor, IEmailSender emailSender) : base(httpContextAccessor)
        {
            this.context = context;
            this.httpContextAccessor = httpContextAccessor;
            this.emailSender = emailSender;
        }
        public IActionResult ServiceRequests()
        {
            IQueryable<Models.ServiceRequest> available = null;
            available = context.ServiceRequests;

            CombinedViewModel combinedViewModel = new CombinedViewModel();
            combinedViewModel.serviceRequests = new ServiceRequestsWrapperViewModel();
            combinedViewModel.serviceRequests.Requests = new List<ServiceRequestsViewModel>();

            if (available.Any())
            {
                var allRequests = available.OrderByDescending(s => s.ServiceStartDate).ToList();
                foreach (var request in allRequests)
                {
                    TimeSpan StartTimeSpan = request.ServiceStartDate.TimeOfDay;
                    var TotalServiceHrs = (int)request.ServiceHours;
                    var TotalServiceMinutes = (int)request.ServiceHours < request.ServiceHours ? 30 : 0;
                    TimeSpan EndTimeSpan = new TimeSpan(
                                                    StartTimeSpan.Hours + TotalServiceHrs,
                                                    StartTimeSpan.Minutes + TotalServiceMinutes, 0);
                    var addresss = context.ServiceRequestAddresses.Where(s => s.ServiceRequestId == request.ServiceRequestId).FirstOrDefault();
                    var customer = context.Users.Where(m => m.UserId == request.UserId).First();

                    float serviceStartTime = convertTimeSpanToFloat(StartTimeSpan);
                    float serviceEndTime = convertTimeSpanToFloat(EndTimeSpan);

                    var serviceProvider = new User();
                    ServiceProviderData SPModel = new ServiceProviderData()
                    {
                        SPUserId = 0,
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
                        SPModel.SPAvtar = "/assets/avatar-male.png";
                    }

                    ServiceRequestsViewModel currentRequest = new ServiceRequestsViewModel()
                    {
                        Distance = 0,
                        CustomerName = customer.FirstName + " " + customer.LastName,
                        ServiceRequestId = request.ServiceRequestId,
                        ServiceId = request.ServiceId,
                        ServiceDate = formatDate(request.ServiceStartDate.Date),
                        ServiceStartTime = convertToString(StartTimeSpan),
                        ServiceEndTime = convertToString(EndTimeSpan),
                        StartTimeFloat = convertTimeSpanToFloat(StartTimeSpan),
                        TotalAmount = (float)request.TotalCost,
                        Duration = (float)request.ServiceHours,
                        Comments = request.Comments,
                        Email = addresss.Email,
                        Mobile = addresss.Mobile,
                        Street = addresss.AddressLine1,
                        HouseNumber = addresss.AddressLine2,
                        City = addresss.City,
                        PostalCode = addresss.PostalCode,
                        ServiceAddress = addresss.AddressLine1 + " " + addresss.AddressLine2 + ", " +
                                         addresss.PostalCode + " " + addresss.City,
                        HasPets = request.HasPets,
                        RecordVersion = (Guid)request.RecordVersion,
                        ZipCode = request.ZipCode,
                        IsCompleted = (request.ServiceStartDate.AddHours(TotalServiceHrs).AddMinutes(TotalServiceMinutes) < DateTime.Now),
                        Status = request.Status,
                        SPName = SPModel.SPName,
                        SPUserId = SPModel.SPUserId,
                        SPRating = SPModel.SPRating,
                        SPAvtar = SPModel.SPAvtar,
                    };
                    combinedViewModel.serviceRequests.Requests.Add(currentRequest);
                }
            }
            return View(combinedViewModel);
        }

        public IActionResult UserManagement()
        {
            var available = context.Users.Select(x => x);
            CombinedViewModel finalModel = new CombinedViewModel();
            finalModel.allUsers = new List<UserDetailsViewModel>();

            if (available.Any())
            {
                foreach(var user in available)
                {
                    if (user.IsDeleted || user.UserId == loggedUser.UserId) continue;

                    UserDetailsViewModel model = new UserDetailsViewModel()
                    {
                        UserId = user.UserId,
                        UserName = user.FirstName + " " + user.LastName,
                        RegistrationDate = formatDate(user.CreatedDate),
                        UserType = user.UserTypeId,
                        Mobile = user.Mobile,
                        Email = user.Email,
                        PostalCode = user.ZipCode,
                        Role = "",
                    };
                    if (user.IsApproved == false)
                    {
                        model.Status = (int)UserStatus.NeedsApproval;
                    }
                    else if(user.IsActive)
                    {
                        model.Status = (int)UserStatus.Active;
                    }
                    else
                    {
                        model.Status = (int)UserStatus.Inactive;
                    }
                    finalModel.allUsers.Add(model);
                }
            }
            return View(finalModel);
        }

        [HttpPost]
        public async Task<IActionResult> EditAndReschedule(AdminRescheduleServiceViewModel model)
        {
            if (ModelState.IsValid)
            {
                var available = context.ServiceRequests.Where(s => s.ServiceId == model.serviceDetails.ServiceId);
                if (available.Any())
                {
                    var request = available.First();
                    var conflicting = isConflicting(model.serviceDetails.StartTime,
                                                    (float)(model.serviceDetails.StartTime + request.ServiceHours),
                                                    model.serviceDetails.ServiceStartDate, (float)request.ServiceHours,
                                                    request.ServiceProviderId, request.ServiceId);
                    if(conflicting.Length == 0)
                    {
                        var serviceAddress = context.ServiceRequestAddresses.Where(s => s.ServiceRequestId == request.ServiceRequestId).First();
                        serviceAddress.AddressLine1 = model.address.Street;
                        serviceAddress.AddressLine2 = model.address.HouseNumber;
                        serviceAddress.City = model.address.City;
                        serviceAddress.PostalCode = model.address.PostalCode;
                        context.ServiceRequestAddresses.Update(serviceAddress);

                        var newDate = formatDate(model.serviceDetails.ServiceStartDate);
                        var StartTime = convertToStringUsingFloatTime(model.serviceDetails.StartTime);
                        var EndTime = convertToStringUsingFloatTime(model.serviceDetails.StartTime + (float)request.ServiceHours);

                        var newDateTime = new DateTime(model.serviceDetails.ServiceStartDate.Year, 
                                                        model.serviceDetails.ServiceStartDate.Month, 
                                                        model.serviceDetails.ServiceStartDate.Day,
                                                        (int)model.serviceDetails.StartTime, 
                                                        Math.Floor(model.serviceDetails.StartTime) == Math.Ceiling(model.serviceDetails.StartTime) ? 0 : 30, 0);
                        request.ServiceStartDate = newDateTime;
                        context.ServiceRequests.Update(request);
                        request.ModifiedBy = loggedUser.UserId;
                        request.ModifiedDate = DateTime.Now;
                        request.ZipCode = serviceAddress.PostalCode;
                        if(request.ServiceProviderId != null)
                        {
                            request.Status = (int)ServiceRequestStatus.Accepted;
                        }
                        else
                        {
                            request.Status = (int)ServiceRequestStatus.Paid;
                        }
                        context.SaveChanges();

                        string[] ServiceProvider = context.Users
                                                            .Where(x => x.UserId == request.ServiceProviderId)
                                                            .Select(x => x.Email).ToArray();
                        string[] Customer = context.Users.Where(x => x.UserId == request.UserId)
                                                    .Select(x => x.Email).ToArray();

                        string subject = "Service Rescheduled by Admin!";
                        string body = "<div>" +
                                          "<h3>Hello,</h3>" +
                                          "<div>" +
                                                  $"Service request <b>#{model.serviceDetails.ServiceId}</b> has been rescheduled by Admin." +
                                                  $" New date and time are { formatDate(model.serviceDetails.ServiceStartDate) } {StartTime} - {EndTime}" +
                                          "</div><br>" +
                                          "<div>Best regards,</div>" +
                                          "<div style='font-size: 16px;'><b>Helperland team</b></div>" +
                                      "</div>";
                        string[] combined = Customer.Concat(ServiceProvider).ToArray();
                        Message message = new Message(combined, subject, body);
                        await emailSender.SendEmail(message);
                        string successMsg = $"Rescheduled service id: {request.ServiceId}";
                        return Json(new { Status = true, Msg = "Service rescheduled successfully!", AdditionalMsg = successMsg }, serializerOptions);
                    }
                    else
                    {
                        return Json(new { Status = false, Msg = "Service Provider is busy!", AdditionalMsg = conflicting }, serializerOptions);
                    }
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
                    string[] Customer = context.Users.Where(x => x.UserId == prevRequest.UserId)
                                                     .Select(x => x.Email).ToArray();
                    string subject = "Service Cancelled!";
                    string body = "<div>" +
                                       "<h3>Hello,</h3>" +
                                       "<div>" +
                                            "Service request <b>#" + model.ServiceId + "</b> has been cancelled by Admin." +
                                       "</div><br>" +
                                       "<div>Best regards,</div>" +
                                       "<div style='font-size: 16px;'><b>Helperland team</b></div>" +
                                  "</div>";
                    ServiceProvider = context.Users.Where(x => x.UserId == prevRequest.ServiceProviderId)
                                                    .Select(x => x.Email).ToArray();

                    prevRequest.Status = (int)ServiceRequestStatus.Cancelled;
                    prevRequest.ModifiedBy = loggedUser.UserId;
                    prevRequest.ModifiedDate = DateTime.Now;
                    context.ServiceRequests.Update(prevRequest);
                    context.SaveChanges();
                    string[] combined = Customer.Concat(ServiceProvider).ToArray();
                    Message message = new Message(combined, subject, body);
                    await emailSender.SendEmail(message);
                    errorMsg = "Service has been cancelled successfully";
                    string additionalMsg = "Cancelled Request Id: " + model.ServiceId;

                    return Json(new { Status = true, Msg = errorMsg, AdditionalMsg = additionalMsg }, serializerOptions);
                }
            }

            return Json(new { Status = false, Msg = "Something went wrong.", AdditionalMsg = "" }, serializerOptions);
        }
        
        [HttpPost]
        public IActionResult UserActions(int userId, int type)
        {
            var available = context.Users.Where(u => u.UserId == userId);
            if(available.Any())
            {
                var user = available.First();
                var successMsg = "";
                if(type == (int)Actions.Approve)
                {
                    user.IsApproved = true;
                    user.IsActive = true;
                    successMsg = "User is now approved!";
                } 
                else if(type == (int)Actions.Activate)
                {
                    user.IsActive = true;
                    successMsg = "User Activated successfully!";
                }
                else if(type == (int)Actions.Deactivate)
                {
                    user.IsActive = false;
                    successMsg = "User Deactivated successfully!";
                }
                else if(type == (int)Actions.Delete)
                {
                    user.IsDeleted = true;
                    user.IsActive = false;
                    successMsg = "User Deleted successfully!";
                }
                context.Update(user);
                context.SaveChanges();

                return Json(new { Status = true, Msg = successMsg }, serializerOptions);
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
        private string isConflicting(float startTime, float endTime, DateTime date, float duration, int? ServiceProviderId, int ServiceId)
        {
            string msg = "";
            if (endTime > 21)
            {
                msg = "Total Duration of the service Exceeds time limit of 9:00 PM";
                return msg;
            }
            if (ServiceProviderId == null)
            {
                return msg;
            }
            var allRequestsOnGivenDate = context.ServiceRequests
                                            .Where(s => s.ServiceStartDate.Date == date.Date &&
                                                        s.ServiceProviderId == ServiceProviderId &&
                                                        s.Status != (int)ServiceRequestStatus.Cancelled);
            if (allRequestsOnGivenDate.Any())
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

    public enum UserStatus
    {
        Active = 1,
        Inactive,
        NeedsApproval,
    }

    public enum Actions
    {
        Activate = 1,
        Deactivate,
        Approve,
        Delete,
    }
}
