using HelperLand.Security;
using HelperLand.Data;
using HelperLand.Models;
using HelperLand.ViewModels;
using HelperLand.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Text.Json;
using HelperLand.Utility;

namespace HelperLand.Controllers
{
    public class HomeController : BaseController
    {
        private readonly HelperlandDBContext context;
        private readonly ICustomDataProtector protector;
        private readonly IWebHostEnvironment env;
        private readonly IHttpContextAccessor contextAccessor;
        private readonly IEmailSender emailSender;

        public HomeController(HelperlandDBContext context, ICustomDataProtector protector, 
            IWebHostEnvironment env, IHttpContextAccessor contextAccessor, IEmailSender emailSender) : base(contextAccessor)
        {
            this.context = context;
            this.protector = protector;
            this.env = env;
            this.contextAccessor = contextAccessor;
            this.emailSender = emailSender;
        }

        public IActionResult Index()
        {
            var popupstatus = TempData["SuccessPopUpStatus"];
            var invalidcreds = TempData["InvalidCreds"];
            var errormsg = TempData["Error"];

            if (popupstatus != null)
            {
                ViewBag.PopUpStatus = popupstatus;
                TempData["SuccessPopUpStatus"] = null;
            }
            if(invalidcreds != null)
            {
                ViewBag.InvalidCreds = invalidcreds;
                TempData["InvalidCreds"] = null;
            }
            if(errormsg != null)
            {
                ViewBag.Error = errormsg;
                TempData["Error"] = null;
            }
            if (Request.Query["ReturnUrl"].Count > 0)
            {
                ViewBag.LoginRequired = Request.QueryString.Value.Split("=")[1].Replace("%2F","/");
            }
            return View();
        }

        public IActionResult faq()
        {
            return View();
        }

        public IActionResult prices()
        {
            return View();
        }
    
        public IActionResult contact()
        {
            return View();
        }

        public IActionResult about()
        {
            return View();
        }

        public IActionResult userRegistration()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> userRegistration(UserViewModel model)
        {
            if (ModelState.IsValid)
            {
                if(EmailPresent(model.Email))
                {
                    ModelState.AddModelError("Email", "Email already in use");
                    return View();
                }
                
                User user = new User
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    Mobile = model.Mobile,
                    Password = protector.Encrypt(model.Password),
                    UserTypeId = 1,
                    IsRegisteredUser = true,
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now,
                    IsApproved = true,
                    IsActive = true,
                };
                user.ModifiedBy = user.UserId;
                context.Users.Add(user);
                context.SaveChanges();

                using(HelperlandDBContext db = new HelperlandDBContext())
                {
                    var tmp = db.Users.Where(s => s.Email == user.Email).First();
                    tmp.ModifiedBy = user.UserId;
                    db.SaveChanges();
                };

                LoginViewModel loginModel = new LoginViewModel
                {
                    Email = model.Email,
                    Password = model.Password,
                    RememberMe = false,
                };
                CombinedViewModel finalModel = new CombinedViewModel
                {
                    LoginModel = loginModel,
                };
                await Login(finalModel);
                return RedirectToAction("Index");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(CombinedViewModel model, string returnUrl="")
        {
            if (ModelState.IsValid)
            {
                if(EmailPresent(model.LoginModel.Email)==false)
                {
                    ModelState.AddModelError("Email", "Invalid Email Address");
                    TempData["InvalidCreds"] = "Invalid Email Address";
                    return RedirectToAction("Index");
                }
                var user = context.Users.Where(s => s.Email == model.LoginModel.Email).First();
                string pass = protector.Decrypt(user.Password);
                if (pass != model.LoginModel.Password)
                {
                    TempData["InvalidCreds"] = "Invalid Credentials";
                    return RedirectToAction("Index");
                }
                var username = user.FirstName.ToString() + " " + user.LastName.ToString();
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, username),
                };
                string userrole = "";
                if (user.UserTypeId == (int)Roles.ServiceProvider)
                {
                    userrole = "ServiceProvider";
                    if(user.IsApproved == false)
                    {
                        TempData["Error"] = "Your account is not yet approved!";
                        return RedirectToAction("Index");
                    }
                }
                if(user.IsActive == false)
                {
                    TempData["Error"] = "Your account is not active at the moment!";
                    return RedirectToAction("Index");
                }
                if (user.UserTypeId == (int)Roles.Customer)
                {
                    userrole = "Customer";
                }
                else if (user.UserTypeId == (int)Roles.Admin)
                {
                    userrole = "Admin";
                }

                if(model.LoginModel.RememberMe == true)
                {
                    CookieOptions cookieOptions = new CookieOptions()
                    {
                        Expires = DateTime.Now.AddDays(30),
                        MaxAge = TimeSpan.FromDays(30),
                        SameSite = SameSiteMode.Strict,
                    };

                    model.LoginModel.Password = protector.Encrypt(model.LoginModel.Password);
                    string serialized = JsonSerializer.Serialize(model.LoginModel);
                    Response.Cookies.Append("UserCredentials", serialized, cookieOptions);
                }

                claims.Add(new Claim(ClaimTypes.Role, userrole));
                ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                ClaimsPrincipal principal = new ClaimsPrincipal(claimsIdentity);
                AuthenticationProperties authProperties = new AuthenticationProperties() {
                    IsPersistent = model.LoginModel.RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30),
                    RedirectUri = Url.Action("Logout"),
                };
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                                                new ClaimsPrincipal(claimsIdentity), authProperties);
                HttpContext.Session.Set<User>("User", user);
                loggedUser = user;
                if(returnUrl.Length > 0)
                {
                    ViewBag.LoginRequired = "";
                    return LocalRedirect(returnUrl);
                }
                if(user.UserTypeId == (int)Roles.ServiceProvider)
                {
                    return RedirectToAction("NewServiceRequests", "ServiceProvider");
                }
                if(user.UserTypeId == (int)Roles.Admin)
                {
                    return RedirectToAction("ServiceRequests", "Admin");
                }
                return RedirectToAction("ServiceRequests", "Customer");
            }
            return RedirectToAction("Index");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["SuccessPopUpStatus"] = "Logout";
            HttpContext.Session.Remove("User");
            loggedUser = new User();
            HttpContext.Response.Cookies.Delete(".AspNetCore.Cookies");
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult ForgotPassword(CombinedViewModel model)
        {
            if (ModelState.IsValid)
            {
                if(EmailPresent(model.forgotPassModel.Email) == false)
                {
                    ModelState.AddModelError("Email", "Email not found");
                    TempData["InvalidCreds"] = "Invalid Email Address Forgot Pass";
                    return RedirectToAction("Index");
                }
                var resetObj = new ChangePasswordData { Email = model.forgotPassModel.Email, Hash = Guid.NewGuid().ToString()};
                var serializedResetObj = JsonSerializer.Serialize(resetObj);
                var protectedObj = protector.Encrypt(serializedResetObj);
                var resetUrl = Url.Action("changePassword", "Home", new { token = protectedObj }, Request.Scheme);
                var temp = JsonSerializer.Deserialize<ChangePasswordData>(serializedResetObj);
                var user = context.Users.Where(s => s.Email == model.forgotPassModel.Email).First();
                var subject = "Reset Password";
                var message = "<div>" +
                                    "<h3>Hello " + user.FirstName + ",</h3>" +
                                    "<div>" +
                                          "You have submitted a password change request. <br>" +
                                          "If it wasn't you please disregard this email and make sure you can still login to your account. " +
                                          "If it was you, then <a href='" + resetUrl + "' title='Reset Password'>Click Here</a> to change your password." +
                                    "</div><br>" +
                                    "<div>Regards,</div>" +
                                    "<div><h4>Helperland Team</h4></div>" +
                               "</div>";
                try
                {
                    Message emailMesssage = new Message(new string[] {model.forgotPassModel.Email}, subject, message);
                    emailSender.SendEmail(emailMesssage);
                    TempData["SuccessPopUpStatus"] = "PasswordResetLinkSent";
                    return RedirectToAction("Index", "Home");
                }
                catch(Exception)
                {
                    TempData["Error"] = "Something went wrong!";
                    return RedirectToAction("Index", "Home");
                }
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult changePassword(string token)
        {
            try
            {
                var exapndedObj = protector.Decrypt(token);
                var deserializedObj = JsonSerializer.Deserialize<ChangePasswordData>(exapndedObj);
                ViewBag.Email = deserializedObj.Email;
                return View();
            }
            catch (Exception)
            {
                TempData["Error"] = "Something went wrong!";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        public async Task<IActionResult> changePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                if(EmailPresent(model.Email)==false) 
                { 
                    return RedirectToAction("Error");
                }
                var user = context.Users.Where(s => s.Email == model.Email).First();
                user.Password = protector.Encrypt(model.Password);
                context.SaveChanges();

                LoginViewModel loginModel = new LoginViewModel
                {
                    Email = model.Email,
                    Password = model.Password,
                    RememberMe = false,
                };

                CombinedViewModel finalModel = new CombinedViewModel
                {
                    LoginModel = loginModel,
                };

                TempData["SuccessPopUpStatus"] = "ChangePassword";
                await Login(finalModel);
                return RedirectToAction("Index");
            }
            return View();
        }

        [AllowAnonymous]
        public IActionResult become_service_provider()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> spRegistration(UserViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (EmailPresent(model.Email))
                {
                    ModelState.AddModelError("Email", "Email already in use");
                    return View("become_service_provider");
                }
                User user = new User
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    Mobile = model.Mobile,
                    Password = protector.Encrypt(model.Password),
                    UserTypeId = 2,
                    IsRegisteredUser = true,
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now,
                };
                user.ModifiedBy = user.UserId;
                context.Users.Add(user);
                context.SaveChanges();

                using (HelperlandDBContext db = new HelperlandDBContext())
                {
                    var tmp = db.Users.Where(s => s.Email == user.Email).First();
                    tmp.ModifiedBy = user.UserId;
                    db.SaveChanges();
                };

                LoginViewModel loginModel = new LoginViewModel
                {
                    Email = model.Email,
                    Password = model.Password,
                    RememberMe = false,
                };
                CombinedViewModel finalModel = new CombinedViewModel
                {
                    LoginModel = loginModel,
                };
                await Login(finalModel);
                return RedirectToAction("Index");
            }
            return View();
        }
        
        [HttpPost]
        public IActionResult contact(ContactViewModel model)
        {
            if(ModelState.IsValid)
            {
                ContactU newContact = new ContactU
                {
                    Name = model.FirstName + " " + model.LastName,
                    Email = model.Email,
                    Subject = model.Type,
                    PhoneNumber = model.Mobile,
                    Message = model.Message,
                    CreatedOn = DateTime.Now,
                };

                if(model.FilePath != null)
                {
                    newContact.UploadFileName = model.FilePath.FileName;
                    string path = Path.Combine(env.WebRootPath, "Uploads");
                    path = Path.Combine(path, Guid.NewGuid().ToString() + "_" + model.FilePath.FileName);
                    newContact.FileName = path;
                    FileStream fileStream = new FileStream(path, FileMode.Create);
                    model.FilePath.CopyTo(fileStream);
                }

                if(User.Identity.IsAuthenticated)
                {
                    newContact.CreatedBy = loggedUser.UserId;
                }

                context.ContactUs.Add(newContact);
                context.SaveChanges();
                return RedirectToAction("contact");
            }
            return View();
        }

        // Utility Functions
        bool EmailPresent(string email)
        {
            return context.Users.Any(s => s.Email == email);
        }
    }

    public class ChangePasswordData
    {
        public string Email { get; set; }
        public string Hash { get; set; }
    }
    public enum Roles {
        Customer = 1,
        ServiceProvider,
        Admin,
    }
}