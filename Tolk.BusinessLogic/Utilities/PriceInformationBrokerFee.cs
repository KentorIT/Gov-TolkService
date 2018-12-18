using System;
using System.Collections.Generic;
using System.Text;
using Tolk.BusinessLogic.Entities;
using System.ComponentModel.DataAnnotations.Schema;


namespace Tolk.BusinessLogic.Utilities
{
    public class PriceInformationBrokerFee
    {
        public int PriceListRowId { get; set; }

        public DateTimeOffset StartDatePriceList { private get; set; }

        public DateTimeOffset EndDatePriceList { private get; set; }

        public DateTimeOffset FirstValidDateRanking { private get; set; }

        public DateTimeOffset LastValidDateRanking { private get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal BasePrice { private get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal PriceToUse => RoundDecimals ? Math.Round(BasePrice * BrokerFee, MidpointRounding.AwayFromZero) : BasePrice * BrokerFee;

        public bool RoundDecimals { private get; set; }

        public CompetenceLevel CompetenceLevel { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal BrokerFee { get; set; }

        public int RankingId { get; set; }

        public DateTimeOffset StartDate => StartDatePriceList > FirstValidDateRanking ? StartDatePriceList : FirstValidDateRanking;

        public DateTimeOffset EndDate => EndDatePriceList > LastValidDateRanking ? LastValidDateRanking : EndDatePriceList;




    }
}
