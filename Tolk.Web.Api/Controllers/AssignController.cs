using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Tolk.Api.Payloads;

namespace Tolk.Web.Api.Controllers
{
    [Route("api/Request/[controller]")]
    [ApiController]

    public class AssignController : ControllerBase
    {
        [HttpPost]
        public ActionResult<bool> Post([FromBody] RequestAssignModel model)
        {
            X509Certificate2 clientCertInRequest = Request.HttpContext.Connection.ClientCertificate;
            //if (!clientCertInRequest.Verify() || !AllowedCerialNumbers(clientCertInRequest.SerialNumber))
            //{
            //    Response.StatusCode = 404;
            //    return null;
            //}
            return true;
        }
    }
}
