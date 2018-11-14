using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Tolk.Api.Payloads;

namespace Tolk.Web.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RequestsController : ControllerBase
    {
        // GET api/requests
        [HttpGet]
        public ActionResult<IEnumerable<RequestModel>> Get()
        {
            return new RequestModel[] {
                new RequestModel { OrderNumber = "xx" },
                new RequestModel { OrderNumber = "yy" },
            };
        }

        // GET api/requests/xx
        [HttpGet("{orderNumber}")]
        public ActionResult<RequestModel> Get(string orderNumber)
        {
            return new RequestModel { OrderNumber = orderNumber};
        }

        [HttpGet]
        public ActionResult<bool> Ping()
        {
            X509Certificate2 clientCertInRequest = Request.HttpContext.Connection.ClientCertificate;
            //if (!clientCertInRequest.Verify() || !AllowedCerialNumbers(clientCertInRequest.SerialNumber))
            //{
            //    Response.StatusCode = 404;
            //    return null;
            //}
            return true;
        }

        public ActionResult<bool> Assign([FromBody] RequestAssignModel model)
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
