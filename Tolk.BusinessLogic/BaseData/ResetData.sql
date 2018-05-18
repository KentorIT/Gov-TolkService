declare @increment bit
declare @reseed int

--TODO: Clear the users too...
truncate table InterpreterBrokerRegion
truncate table OrderRequirements

select @increment = Count(*)
from Orders 
set @reseed = 1 - @increment

delete Orders --
DBCC CHECKIDENT (Orders, reseed, @reseed)--

