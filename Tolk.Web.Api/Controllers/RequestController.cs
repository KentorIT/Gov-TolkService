using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Tolk.Api.Payloads;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Api.Helpers;

namespace Tolk.Web.Api.Controllers
{
    public class RequestController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly TolkApiOptions _options;

        public RequestController(TolkDbContext tolkDbContext, IOptions<TolkApiOptions> options)
        {
            _dbContext = tolkDbContext;
            _options = options.Value;
        }

        [HttpPost]
        public ActionResult<string> AssignWithText([FromBody] RequestAssignModel model)
        {
            X509Certificate2 clientCertInRequest = Request.HttpContext.Connection.ClientCertificate;
            var user = GetApiUser();
            return $"User: {user?.FullName ?? "[None found]"} Cert: {clientCertInRequest?.Subject}, Oid: {model.OrderNumber}, Inter: {model.Interpreter} Time: {GetTimeAsync().Result}";
        }

        [HttpPost]
        public ActionResult<bool> AssignInterpreter([FromBody] RequestAssignModel model)
        {
            var order = _dbContext.Orders
                .Include(o => o.Requests).ThenInclude(r=> r.Ranking)
                .SingleOrDefault(o => o.OrderNumber == model.OrderNumber);
            if (order == null)
            {
                Response.StatusCode = 401;
                return false;
            }
            var apiUser = GetApiUser();
            var user = GetUser(model.CallingUser);
            if (user == null)
            {
                Response.StatusCode = 403;
                return false;
            }
            var request = order.Requests.SingleOrDefault(r =>
                apiUser.BrokerId == r.Ranking.BrokerId &&
                //Possibly other statuses, but this code is only temporary. Should be coalesced with the controller code.
                (r.Status == BusinessLogic.Enums.RequestStatus.Created || r.Status == BusinessLogic.Enums.RequestStatus.Received));
            if (request == null)
            {
                //Other status to describe that the request is not in sync!!
                Response.StatusCode = 401;
                return false;
            }
            var interpreter = GetUser(model.Interpreter).Interpreter;
            //Supposed to crash on empty Interpreter, does not handle new interpreter.
            //Does not handle Tolk-Id

            request.Accept(GetTimeAsync().Result, user?.Id ?? apiUser.Id, (user != null ? (int?)apiUser.Id : null), interpreter,
                EnumHelper.GetEnumByCustomName<InterpreterLocation>(model.Location),
                EnumHelper.GetEnumByCustomName<CompetenceAndSpecialistLevel>(model.CompetenceLevel),
                //Does not handle reqmts yet
                new OrderRequirementRequestAnswer[] { },
                //Does not handle attachments yet.
                new List<RequestAttachment>(),
                //Does not handle price info yet, either...
                new PriceInformation { PriceRows = new List<PriceRowBase>()}
            );
            _dbContext.SaveChanges();
            return true;
        }

        private AspNetUser GetApiUser()
        {
            X509Certificate2 clientCertInRequest = Request.HttpContext.Connection.ClientCertificate;
            if (Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-CallerSecret", out var type))
            {
                //Need a lot more security here
                return _dbContext.Users.SingleOrDefault(u => u.Claims.Any(c => c.ClaimType == "Secret" && c.ClaimValue == type));
            }
            else
            {
                return _dbContext.Users.SingleOrDefault(u => u.Claims.Any(c => c.ClaimType == "CertSerialNumber" && c.ClaimValue == clientCertInRequest.SerialNumber));
            }

        }
        private AspNetUser GetUser(string caller)
        {
            return !string.IsNullOrWhiteSpace(caller) ?
                _dbContext.Users.SingleOrDefault(u => u.NormalizedEmail == caller.ToUpper()) :
                null;
        }

        private async Task<DateTimeOffset> GetTimeAsync()
        {
            using (var client = new HttpClient())
            {

                client.DefaultRequestHeaders.Accept.Clear();
                //Also add cert to call
                var response = await client.GetAsync($"{_options.TolkWebBaseUrl}/Time/");
                return await response.Content.ReadAsAsync<DateTimeOffset>();
            }
        }
    }
}
