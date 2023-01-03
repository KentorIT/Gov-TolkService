BEGIN TRANSACTION

:setvar path "C:\git\Gov-TolkService\Tolk.BusinessLogic\BaseData\"

:r $(path)dev.CustomerOrganisations.sql
:r $(path)dev.Brokers.sql
:r $(path)dev.Users.sql
:r $(path)dev.Holidays.sql
:r $(path)dev.Languages.sql
:r $(path)dev.PriceListRows.sql
:r $(path)dev.PriceCalculationCharges.sql
:r $(path)dev.BrokerFeeByServiceTypePriceListRows.sql
COMMIT
