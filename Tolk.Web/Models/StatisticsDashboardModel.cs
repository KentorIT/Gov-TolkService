﻿using System.Collections.Generic;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.Web.Models
{
    public class StatisticsDashboardModel
    {
        public IEnumerable<WeeklyStatisticsModel> WeeklyStatisticsModels { get; set; }

        public IEnumerable<OrderStatisticsModel> OrderStatisticsModels { get; set; }

        public int TotalNoOfOrders { get; set; }
    }
}
