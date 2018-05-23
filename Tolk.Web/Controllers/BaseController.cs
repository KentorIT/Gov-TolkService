using Microsoft.AspNetCore.Mvc;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System;
using System.Security.Claims;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Services;
using Microsoft.EntityFrameworkCore;
using Tolk.BusinessLogic.Utilities;
using System.Collections.Generic;

namespace Tolk.Web.Controllers
{
    public class BaseController : Controller
    {
        private readonly UserManager<AspNetUser> _userManager;

        public BaseController(UserManager<AspNetUser> userManager)
        {
            _userManager = userManager;
        }


        protected int CurrentUserId
        {
            get
            {
                return int.Parse(_userManager.GetUserId(User));
            }
        }

        protected int? CurrentImpersonatorId
        {
            get
            {
                return !string.IsNullOrWhiteSpace(User.FindFirstValue(TolkClaimTypes.ImpersonatingUserId)) ?
                             (int?)int.Parse(User.FindFirstValue(TolkClaimTypes.ImpersonatingUserId)) :
                             null;
            }
        }
    }
}
