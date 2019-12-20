using System;
using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Entities;


namespace Tolk.BusinessLogic.Utilities
{
    public class PriceInformationBrokerFee
    {
        public int PriceListRowId { get; set; }

        public DateTimeOffset StartDatePriceList { get; set; }

        public DateTimeOffset EndDatePriceList { get; set; }

        public DateTimeOffset FirstValidDateRanking { get; set; }

        public DateTimeOffset LastValidDateRanking { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal BasePrice { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal PriceToUse => RoundDecimals ? Math.Round(BasePrice * BrokerFee, MidpointRounding.AwayFromZero) : BasePrice * BrokerFee;

        public bool RoundDecimals { get; set; }

        public CompetenceLevel CompetenceLevel { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal BrokerFee { get; set; }

        public int RankingId { get; set; }

        public DateTimeOffset StartDate => StartDatePriceList > FirstValidDateRanking ? StartDatePriceList : FirstValidDateRanking;

        public DateTimeOffset EndDate => EndDatePriceList > LastValidDateRanking ? LastValidDateRanking : EndDatePriceList;
    }
}
