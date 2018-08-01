Use TolkDev
declare @increment bit
declare @reseed int

truncate table OrderPriceRow
truncate table RequestPriceRow
truncate table RequisitionPriceRow
truncate table OrderInterpreterLocation
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

select @increment = IsNull(Max(RequisitionId), 1)
from Requisitions 
set @reseed = 1 - @increment

delete Requisitions --
DBCC CHECKIDENT (Requisitions, reseed, @reseed)--
