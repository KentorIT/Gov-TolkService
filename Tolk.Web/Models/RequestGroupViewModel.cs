using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Attributes;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class RequestGroupViewModel : RequestGroupBaseModel
    {
        public bool AllowConfirmationDenial => true;

        #region methods

        internal static RequestGroupViewModel GetModelFromRequestGroup(RequestGroup requestGroup)
        {
            return new RequestGroupViewModel
            {
                OrderGroupNumber = requestGroup.OrderGroup.OrderGroupNumber,
                Status = requestGroup.Status,
                CreatedAt = requestGroup.CreatedAt,
                RequestGroupId = requestGroup.RequestGroupId,
            };
        }

        #endregion
    }
}
