using Microsoft.AspNetCore.Mvc;
using Tolk.Api.Payloads;

namespace BrokerMock.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
