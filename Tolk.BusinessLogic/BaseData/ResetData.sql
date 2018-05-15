declare @increment bit
declare @reseed int

--TODO: Clear the users to...
truncate table InterpreterBrokerRegion
truncate table OrderRequirements

select @increment = Count(*)
from Orders 
set @reseed = 1 - @increment

delete Orders --
DBCC CHECKIDENT (Orders, reseed, @reseed)--

-- Remove roles that are no longer directly assigned (instead policies are used
-- that relies on present of CustomerId etc.
delete AspNetRoles where Id in ('TolkBrokerRole', 'TolkCustomerRole', 'TolkInterpreterRole')
