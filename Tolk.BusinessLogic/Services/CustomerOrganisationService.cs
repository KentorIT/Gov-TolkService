using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Services
{
    public class CustomerOrganisationService
    {
        private readonly TolkDbContext _dbContext;        
        private readonly ISwedishClock _clock;        
        public CustomerOrganisationService(TolkDbContext dbContext, ISwedishClock clock)
        {
            _dbContext = dbContext;
            _clock = clock;
        }

        public async Task<List<CustomerOrderAgreementSettings>> UpdateOrderAgreementSettings(CustomerOrganisation customerToUpdate,DateTimeOffset? updatedUseOrderAgreementsFromDate, int userId)
        {            
            var dateChanged = customerToUpdate.UseOrderAgreementsFromDate != updatedUseOrderAgreementsFromDate;
            if (!dateChanged)
            {
                return customerToUpdate.CustomerOrderAgreementSettings;
            }

            // DISABLE
            if (updatedUseOrderAgreementsFromDate == null)
            {
                customerToUpdate.UpdateCustomerOrderAgreementBrokerSettings(_clock.SwedenNow, userId);
                customerToUpdate.CustomerOrderAgreementSettings.ForEach(coas => coas.EnabledAt = null);
                return customerToUpdate.CustomerOrderAgreementSettings;
            }

            // CREATE                         
            if (!customerToUpdate.CustomerOrderAgreementSettings.Any())
            {
                return await CreateInitialCustomerOrderAgreementSettings(updatedUseOrderAgreementsFromDate);
            }

            // UPDATE
            customerToUpdate.UpdateCustomerOrderAgreementBrokerSettings(_clock.SwedenNow, userId);
            if (customerToUpdate.CustomerOrderAgreementSettings.All(coas => coas.EnabledAt == null))
            {
                // If "UseOrderAgreementFromDate" is updated and all existing settings are disabled => enabled all
                customerToUpdate.CustomerOrderAgreementSettings.ForEach(coas => coas.EnabledAt = updatedUseOrderAgreementsFromDate);
            }
            else
            {
                customerToUpdate.CustomerOrderAgreementSettings.Where(coas => coas.EnabledAt != null).ToList().ForEach(coas => coas.EnabledAt = updatedUseOrderAgreementsFromDate);
            }

            return customerToUpdate.CustomerOrderAgreementSettings;
        }

        public async Task<List<CustomerOrderAgreementSettings>> CreateInitialCustomerOrderAgreementSettings(DateTimeOffset? useOrderAgreementsFromDate)
        {
            return useOrderAgreementsFromDate != null ?
                await _dbContext.Brokers.Select(b => new CustomerOrderAgreementSettings
                {
                    Broker = b,
                    EnabledAt = useOrderAgreementsFromDate,
                }).ToListAsync() : null;
        }

        public async Task<CustomerOrganisation> ToggleSpecificOrderAgreementSettings(int customerOrganisationId, int brokerId, int userId)
        {
            var customer = await _dbContext.CustomerOrganisations.Include(c => c.CustomerOrderAgreementSettings).GetCustomerById(customerOrganisationId);

            customer.UpdateCustomerOrderAgreementBrokerSettings(_clock.SwedenNow, userId);
            var setting = customer.CustomerOrderAgreementSettings.Where(coas => coas.CustomerOrganisationId == customerOrganisationId && coas.BrokerId == brokerId).SingleOrDefault();
            setting.EnabledAt = setting.Disabled ? _clock.SwedenNow : null;

            await _dbContext.SaveChangesAsync();
            return customer;
        }
    }
}
