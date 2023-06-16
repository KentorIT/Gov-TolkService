use TolkProd
Select cast(createdAt as date), orderid
from ORders
Where orderid % 1000 = 0
or orderid = 1

declare @LastDate date, @LastOrderId int, @DaysToNext int
Select top 1 
@LastDate = cast(createdAt as date), 
@LastOrderId = orderid
from ORders
Where orderid % 1000 = 0
or orderid = 1
order by orderid desc

select @LastOrderId = max(orderid) % 1000 from Orders

select @DaysToNext = DATEDIFF(DAY, @LastDate, getdate()) / 
(cast(@LastOrderId as decimal(8,2)) / cast(1000 as decimal(8,2)))

Select DATEADD(day, @DaysToNext, @LastDate) 'Projecerad datum för nästa tusende'

Select 'Under tillsättning' 'tillstånd', Count(*)
From Orders
Where status in(2, 3, 10, 16, 18, 19, 21)
union all 
Select 'tillsatta/utförda', Count(*)
From Orders
Where status in(4, 5, 7) 
union all 
Select 'Bokningsförfrågan avböjd av samtliga förmedlingar', Count(*)
From Orders
Where status in(9) 
union all 
Select 'Tillsättning ej besvarad', Count(*)
From Orders
Where status in(15) 
union all 
Select 'Uppdrag annullerat, sista svarstid ej satt', Count(*)
From Orders
Where status in(17) 
union all 
Select 'Uppdrag avbokat av myndighet', Count(*)
From Orders
Where status in(6) 
union all 
Select 'Uppdrag avbokat av förmedling', Count(*)
From Orders
Where status in(12) 
union all
Select 'Totalt antal avrop' 'tillstånd', Count(*)
From Orders

Select * from
(
Select 1 'sortering', 'Under tillsättning' 'tillstånd', year(CreatedAt) 'Beställning skapad År',  Count(*) 'Antal'
From Orders
Where status in(2, 3, 10, 16, 18, 19, 21) 
group by year(CreatedAt)
union all 
Select 2, 'Tillsatta/Utförda', year(CreatedAt), Count(*)
From Orders
Where status in(4, 5, 7)
group by year(CreatedAt) 
union all 
Select 3, 'Bokningsförfrågan avböjd av samtliga förmedlingar', year(CreatedAt), Count(*)
From Orders
Where status in(9) 
group by year(CreatedAt)
union all 
Select 4 ,'Tillsättning ej besvarad', year(CreatedAt), Count(*)
From Orders
Where status in(15) 
group by year(CreatedAt)
union all 
Select 5, 'Uppdrag annullerat, sista svarstid ej satt', year(CreatedAt), Count(*)
From Orders
Where status in(17) 
group by year(CreatedAt)
union all 
Select 6, 'Uppdrag avbokat av myndighet', year(CreatedAt), Count(*)
From Orders
Where status in(6) 
group by year(CreatedAt)
union all 
Select 7, 'Uppdrag avbokat av förmedling', year(CreatedAt), Count(*)
From Orders
Where status in(12) 
group by year(CreatedAt)
union all
Select 8, 'Totalt antal avrop' 'tillstånd', year(CreatedAt), Count(*)
From Orders
group by year(CreatedAt)
) a
order by sortering, 'Beställning skapad År'


Select * from
(
Select 'Sammanhållen' 'Typ',  year(CreatedAt) 'År', Count(*) 'Antal'
From Orders
Where OrderGroupId is not null
group by year(CreatedAt)
union 
Select 'Fristående', year(CreatedAt), Count(*)
From Orders
Where OrderGroupId is null
group by year(CreatedAt)
) a
order by Typ, 'År'

Select * from
(
Select 'Med Enhet' 'Bas', year(CreatedAt) 'År', Count(*) 'Antal' from orders
Where CustomerUnitId is not null
group by year(CreatedAt)
union
Select 'Utan Enhet', year(CreatedAt) 'År', Count(*) from orders
Where CustomerUnitId is null
group by year(CreatedAt)
) a
order by Bas, 'År'

select u.brokerid, Count(*) 'Requests besvarade via api'
from Requests r
Join AspNetUsers u
On u.IsApiUser = 1
And
(
r.AnsweredBy = u.id or 
r.ReceivedBy  = u.id or
r.AcceptedBy  = u.id
)
Group by u.brokerid


Select Count(distinct OrderId) 'Förfrågningar till förmedlingar med lång svarstid'
from Requests
Where LastAcceptAt is not null


Select Count(distinct OrderId) 'Beställningar med flexibel starttid'
from Orders
Where ExpectedLength is not null


Select 
	c.Name 'Myndighet', 
	Count(o.orderid)
From CustomerOrganisations c
left Join Orders o
On o.CustomerOrganisationId = c.CustomerOrganisationId
Group By c.CustomerOrganisationId, c.Name
Order by Count(o.orderid) desc 
