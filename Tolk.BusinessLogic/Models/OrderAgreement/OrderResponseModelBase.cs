using System;
using System.Xml;
using System.Xml.Serialization;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Models.OrderAgreement
{    
    [XmlRoot("OrderResponse",Namespace = Constants.defaultNamespace)]    
    [XmlInclude(typeof(OrderResponseModel))]
    [XmlInclude(typeof(OrderAgreementModel))]
    public abstract class OrderResponseModelBase 
    {
        [XmlIgnore]
        public string DocumentID;

        [XmlIgnore]
        public string OrderNumber { get; set; }

        [XmlIgnore]
        public DateTimeOffset IssuedAt { get; set; }        

        [XmlElement(Namespace = Constants.cbc, Order = 1)]        
        public string CustomizationID { get; set; }

        [XmlElement(Namespace = Constants.cbc, Order = 2)]        
        public string ProfileID { get; set; }
        
        [XmlElement(Namespace = Constants.cbc, Order = 3)]
        public EndPointIDModel ID 
        {   
            get => new EndPointIDModel{Value = DocumentID};
            set => DocumentID = value.Value;
        }
        [XmlElement(Namespace = Constants.cbc, Order = 4)]
        public string SalesOrderID
        {
            get => OrderNumber;
            set => OrderNumber = value;
        }        
        [XmlElement(Namespace = Constants.cbc, Order = 5)]
        public string IssueDate
        {
            get => IssuedAt.ToString("yyyy-MM-dd");
            set => IssuedAt = IssuedAt.AddDate(value);
        }
        [XmlElement(Namespace = Constants.cbc, Order = 6)]
        public string IssueTime
        {
            get => IssuedAt.ToString("HH:mm:ss");
            set => IssuedAt = IssuedAt.AddTime(value);

        }
       
        protected static OrganizationPartyModel GetBuyerCustomerParty(Order order)
        {
            return new OrganizationPartyModel
            {
                Party = new PartyModel
                {
                    EndpointID = new EndPointIDModel { SchemeId = Constants.PeppolIdByOrganizationNumberSchemeId, Value = order.CustomerOrganisation.OrganisationNumber.ToNotHyphenatedFormat() },
                    PartyLegalEntity = new PartyLegalEntityModel { RegistrationName = order.CustomerOrganisation.Name }
                }
            };
        }

        protected static OrganizationPartyModel GetSellerSupplierParty(Request request)
        {
            return new OrganizationPartyModel
            {
                Party = new PartyModel
                {
                    EndpointID = new EndPointIDModel { SchemeId = Constants.PeppolIdByOrganizationNumberSchemeId, Value = request.Ranking.Broker.OrganizationNumber.ToNotHyphenatedFormat() },
                    PartyLegalEntity = new PartyLegalEntityModel { RegistrationName = request.Ranking.Broker.Name }
                }
            };
        }        
    }
}
