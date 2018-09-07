using System;
using System.Collections.Generic;
using System.Text;
using Tolk.BusinessLogic.Entities;

namespace Tolk.Web.Tests.Filters
{
    public class RequisitionFilterModelTests
    {
        private Request[] mockRequests;
        private Requisition[] mockRequisitions;

        public RequisitionFilterModelTests()
        {
            

            mockRequisitions = new[]
            {
                new Requisition { },
            };
        }
    }
}
