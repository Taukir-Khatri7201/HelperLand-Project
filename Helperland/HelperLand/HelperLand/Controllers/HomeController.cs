using DatabaseFirstApproachPractice.Security;
using HelperLand.Data;
using HelperLand.Models;
using HelperLand.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HelperLand.Controllers
{
    public class HomeController : Controller
    {
        private readonly HelperlandDBContext context;
        private readonly ICustomDataProtector protector;

        public HomeController(HelperlandDBContext context, ICustomDataProtector protector)
        {
            this.context = context;
            this.protector = protector;
        }

        public IActionResult Index()
        {
            var popupstatus = TempData["SuccessPopUpStatus"];
            var invalidcreds = TempData["InvalidCreds"];
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
        public async Task<IActionResult> Login(CombinedViewModel model)
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
                if (user.UserTypeId == 1)
                {
                    userrole = "User";
                }
                else if (user.UserTypeId == 2)
                {
                    userrole = "ServiceProvider";
                }
                else if (user.UserTypeId == 3)
                {
                    userrole = "Admin";
                }
                claims.Add(new Claim(ClaimTypes.Role, userrole));
                ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                ClaimsPrincipal principal = new ClaimsPrincipal(claimsIdentity);
                AuthenticationProperties authProperties = new AuthenticationProperties() { IsPersistent = model.LoginModel.RememberMe };
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                                                new ClaimsPrincipal(claimsIdentity), authProperties);
                return RedirectToAction("Index");
            }
            return RedirectToAction("Index");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["SuccessPopUpStatus"] = "Logout";
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
                var user = context.Users.Where(s => s.Email == model.forgotPassModel.Email).First();
                return RedirectToAction("changePassword", model.forgotPassModel);
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult changePassword(ForgotPasswordViewModel model)
        {
            string referrer = Request.Headers["Referer"].ToString();
            if(referrer.Length == 0) return View("AccessDenied");
            return View();
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

        bool EmailPresent(string email)
        {
            return context.Users.Any(s => s.Email == email);
        }
    }
}
