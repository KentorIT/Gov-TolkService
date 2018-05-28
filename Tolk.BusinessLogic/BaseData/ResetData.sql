Use TolkDev
declare @increment bit
declare @reseed int

truncate table InterpreterBrokerRegion
truncate table OrderRequirementRequestAnswer 

select @increment = IsNull(Max(RequestId), 1)
from Requests 
set @reseed = 1 - @increment

delete Requests --
DBCC CHECKIDENT (Requests, reseed, @reseed)

select @increment = IsNull(Max(OrderRequirementId), 1)
from OrderRequirements 
set @reseed = 1 - @increment

delete OrderRequirements --
DBCC CHECKIDENT (OrderRequirements, reseed, @reseed)

select @increment = IsNull(Max(OrderId), 1)
from Orders 
set @reseed = 1 - @increment

delete Orders --
DBCC CHECKIDENT (Orders, reseed, @reseed)--

-- Remove roles that are no longer directly assigned (instead policies are used
-- that relies on present of CustomerId etc.

-- Should reset data from dev data too:
-- 1. CustomerOrganisations
-- 2. Brokers
-- 3. Users
-- 4. PriceListRows