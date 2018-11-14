using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Tolk.Api.Payloads;
using Tolk.BusinessLogic.Data;

namespace Tolk.Web.Api.Controllers
{
    [Route("api/Request/[controller]")]
    [ApiController]
    public class AssignController : ControllerBase
    {
        private readonly TolkDbContext _dbContext;

        public AssignController(TolkDbContext tolkDbContext)
        {
            _dbContext = tolkDbContext;
        }

        [HttpPost]
        public ActionResult<bool> Post([FromBody] RequestAssignModel model)
        {
            X509Certificate2 clientCertInRequest = Request.HttpContext.Connection.ClientCertificate;
            var order = _dbContext.Orders.SingleOrDefault(o => o.OrderNumber == model.OrderNumber);
            if (order == null)
            //if (!clientCertInRequest.Verify() || !AllowedSerialNumbers(clientCertInRequest.SerialNumber))
            {
                Response.StatusCode = 401;
                return false;
            }
            return true;
        }
    }
}
