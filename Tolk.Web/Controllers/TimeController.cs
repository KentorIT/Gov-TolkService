using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using Tolk.BusinessLogic.Services;

namespace Tolk.Web.Controllers
{
    public class TimeController : Controller
    {
        private readonly ISwedishClock _clock;

        public TimeController(ISwedishClock clock)
        {
            _clock = clock;
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult<DateTimeOffset> Index()
        {
            return _clock.SwedenNow;
        }
    }
}
