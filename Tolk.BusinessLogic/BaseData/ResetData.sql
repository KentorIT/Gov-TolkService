Use TolkDev
declare @increment bit
declare @reseed int

truncate table OrderRequirements
truncate table Requests

select @increment = IsNull(Max(OrderId), 1)
from Orders 
set @reseed = 1 - @increment

Select @reseed

delete Orders --
DBCC CHECKIDENT (Orders, reseed, @reseed)--
/**/

Select * from Orders
