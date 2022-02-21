using HelperLand.Data;
using HelperLand.Models;
using HelperLand.ViewModels;
using HelperLand.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using HelperLand.Utility;

namespace HelperLand.Controllers
{
    [Authorize]
    public class BookServiceController : BaseController
    {
        private readonly HelperlandDBContext context;
        private readonly IEmailSender emailSender;
        private User loggedUser;

        public BookServiceController(HelperlandDBContext context, IEmailSender emailSender)
        {
            this.context = context;
            this.emailSender = emailSender;
        }

        public IActionResult Index()
        {
            return View("book_service");
        }

        [HttpPost]
        public bool PostalCodeCheck(PostalCodeViewModel model)
        {
            bool found = context.Users.Any(s => s.ZipCode == model.code);
            return found;
        }

        [HttpPost]
        public IActionResult ScheduleService(ScheduleAndPlanViewModel model)
        {
            if (ModelState.IsValid)
            {
                loggedUser = HttpContext.Session.Get<User>("User");
                var addressRepo = context.UserAddresses
                                    .Where(m => m.UserId == loggedUser.UserId && m.PostalCode == model.Code)
                                    .Select(x => new { 
                                        Street = x.AddressLine1, 
                                        HouseNumber = x.AddressLine2, 
                                        Mobile = x.Mobile,
                                        PostalCode = x.PostalCode,
                                        City = x.City,
                                    }).ToList();
                return Json(new { Status = true, AddressRepo = addressRepo }, new System.Text.Json.JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = false
                });
            }
            return Json(new { Status = false });
        }

        [HttpPost]
        public IActionResult ServiceAddress(AddressViewModel model)
        {
            if(ModelState.IsValid)
            {
                loggedUser = HttpContext.Session.Get<User>("User");
                return Json(new { Status = true }, new System.Text.Json.JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = false
                });
            }
            return Json(new { Status = false }, new System.Text.Json.JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = false
            });
        }

        [HttpPost]
        public async Task<IActionResult> MakePayment(BookServiceCombinedViewModel model)
        {
            var day = model.scheduleAndPlan.ServiceStartDate.Day;
            var month = model.scheduleAndPlan.ServiceStartDate.Month;
            var year = model.scheduleAndPlan.ServiceStartDate.Year;
            var startHr = (int)model.startTime;
            var halfHr = (startHr < model.startTime ? 30 : 0);
            var finalDateTime = new DateTime(year, month, day, startHr, halfHr, 0);
            loggedUser = HttpContext.Session.Get<User>("User");
            ServiceRequest req = new ServiceRequest()
            {
                CreatedDate = DateTime.Now,
                HasIssue = false,
                ModifiedDate = DateTime.Now,
                PaymentDue = false,
                UserId = loggedUser.UserId,
                ServiceStartDate = finalDateTime,
                ZipCode = model.postalcode.code,
                ServiceHours = model.totalservicetime,
                ExtraHours = model.extraservicetime,
                SubTotal = (decimal)model.totalamount,
                Discount = (decimal)(model.totalamount - model.scheduleAndPlan.Total),
                TotalCost = (decimal)(model.scheduleAndPlan.Total),
                Comments = model.scheduleAndPlan.Comments,
                HasPets = model.scheduleAndPlan.HavePets,
                Status = (int)(ServiceRequestStatus.Paid),
                ModifiedBy = loggedUser.UserId,
                PaymentDone = true,
                RecordVersion = Guid.NewGuid(),
            };

            int newServiceId = 999;
            try
            {
                newServiceId = context.ServiceRequests.Max(x => x.ServiceId);
            } 
            catch(Exception)
            {
                newServiceId = 999;
            }

            newServiceId++;
            req.ServiceId = newServiceId;
            context.ServiceRequests.Add(req);
            context.SaveChanges();
            int reqId = req.ServiceRequestId;

            int i = 1;
            foreach(var extra in model.scheduleAndPlan.ExtraServices)
            {
                if(extra.Value == true)
                {
                    ServiceRequestExtra cur = new ServiceRequestExtra()
                    {
                        ServiceRequestId = reqId,
                        ServiceExtraId = i
                    };
                    context.ServiceRequestExtras.Add(cur);
                }
                i++;
            }
            context.SaveChanges();

            ServiceRequestAddress address = new ServiceRequestAddress()
            {
                AddressLine1 = model.address.Street,
                AddressLine2 = model.address.HouseNumber,
                City = model.address.City,
                Mobile = model.address.Mobile,
                PostalCode = model.address.PostalCode,
                ServiceRequestId = reqId,
                Email = loggedUser.Email,
            };
            context.ServiceRequestAddresses.Add(address);
            context.SaveChanges();

            string[] allServiceProviders = context.Users
                                                    .Where(x => x.UserTypeId == 2 && x.ZipCode == model.address.PostalCode)
                                                    .Select(x => x.Email).ToArray();

            var subject = "New service request available";
            var body = "<div>" +
                            "<h3>Hello,</h3>" +
                            "<div>" +
                                  "Service request <b>" + newServiceId + "</b> is now available in your area. " +
                                  "Please log in at <b>www.Helperland.com</b> to view further details of the request and to confirm it." +
                            "</div><br>" +
                            "<div>Best regards,</div>" +
                            "<div style='font-size: 16px;'><b>Helperland team</b></div>" + 
                        "</div>";

            Message message = new Message(allServiceProviders, subject, body);
            await emailSender.SendEmail(message);

            return Json(new { Status = true, ServiceId = newServiceId }, new System.Text.Json.JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = false
            });
        }

        [HttpPost]
        public bool AddAdress(AddressViewModel model)
        {
            if(ModelState.IsValid)
            {
                loggedUser = HttpContext.Session.Get<User>("User");
                UserAddress newAddress = new UserAddress()
                {
                    AddressLine1 = model.Street,
                    AddressLine2 = model.HouseNumber,
                    City = model.City,
                    Mobile = model.Mobile,
                    PostalCode = model.PostalCode,
                    Email = loggedUser.Email,
                    IsDefault = false,
                    IsDeleted = false,
                    UserId = loggedUser.UserId,
                };
                var found = context.UserAddresses.Any(m => 
                                                        m.AddressLine1 == newAddress.AddressLine1 &&
                                                        m.AddressLine2 == newAddress.AddressLine2 &&
                                                        m.City == newAddress.City &&
                                                        m.PostalCode == newAddress.PostalCode && 
                                                        m.Mobile == newAddress.Mobile &&
                                                        m.IsDeleted == false);
                if (found) return false;
                context.UserAddresses.Add(newAddress);
                context.SaveChanges();
                return true;
            }
            return false;
        }
    }

    enum ServiceRequestStatus
    {
        Created=1,
        Paid,
        Accepted,
        Cancelled,
        Completed
    }

    enum ExtraServices
    {
        Cabinets=1,
        Fridge,
        Oven,
        Laundary,
        Windows
    }
}
