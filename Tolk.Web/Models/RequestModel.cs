using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;

namespace Tolk.Web.Models
{
    public class RequestModel
    {
        public int RequestId { get; set; }

        public int BrokerId { get; set; }

        public RequestStatus SetStatus { get; set; }

        public OrderModel OrderModel { get; set; }

        [Required]
        [Display(Name = "Tolk")]
        public int InterpreterId { get; set; }

        #region methods

        public Request UpdateRequest(Request request)
        {
            return request;
        }

        public static RequestModel GetModelFromRequest(Request request)
        {
            return new RequestModel
            {
                RequestId = request.RequestId,
                OrderModel = OrderModel.GetModelFromOrder(request.Order),
            };
        }

        #endregion
    }
}
