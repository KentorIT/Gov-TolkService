using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Models.OrderAgreement
{
    public class PriceRowComparisonResult
    {              

        public InvoiceableArticle ArticleType { get; set; }     
      
        public decimal TotalPrice { get; set; } 
        
        public bool HasChanged { get; set; }
    }
}
