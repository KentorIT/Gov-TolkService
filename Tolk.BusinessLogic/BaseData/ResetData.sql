Use TolkDev
declare @increment bit
declare @reseed int

truncate table InterpreterBrokerRegion
truncate table OrderRequirements
truncate table Requests

select @increment = IsNull(Max(OrderId), 1)
from Orders 
set @reseed = 1 - @increment

Select @reseed

delete Orders --
DBCC CHECKIDENT (Orders, reseed, @reseed)--

-- Remove roles that are no longer directly assigned (instead policies are used
-- that relies on present of CustomerId etc.
delete AspNetRoles where Id in ('TolkBrokerRole', 'TolkCustomerRole', 'TolkInterpreterRole')

