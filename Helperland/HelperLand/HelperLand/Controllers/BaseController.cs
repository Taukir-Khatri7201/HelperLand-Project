using HelperLand.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Http;
using HelperLand.Extensions;

namespace HelperLand.Controllers
{
    public class BaseController : Controller
    {
        public User loggedUser;
        public BaseController(IHttpContextAccessor httpContextAccessor)
        {
            loggedUser = httpContextAccessor.HttpContext.Session.Get<User>("User");
            if (loggedUser == null) loggedUser = new User();
        }
    }
}
