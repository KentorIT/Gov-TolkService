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

Select DATEADD(day, @DaysToNext, @LastDate) 'Projecerad datum f�r n�sta tusende'

Select 'Under tills�ttning' 'tillst�nd', Count(*)
From Orders
Where status in(2, 3, 10, 16, 18, 19, 21)
union all 
Select 'tillsatta/utf�rda', Count(*)
From Orders
Where status in(4, 5, 7) 
union all 
Select 'Bokningsf�rfr�gan avb�jd av samtliga f�rmedlingar', Count(*)
From Orders
Where status in(9) 
union all 
Select 'Tills�ttning ej besvarad', Count(*)
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
Select 'Uppdrag avbokat av f�rmedling', Count(*)
From Orders
Where status in(12) 
union all
Select 'Totalt antal avrop' 'tillst�nd', Count(*)
From Orders

Select * from
(
Select 1 'sortering', 'Under tills�ttning' 'tillst�nd', year(CreatedAt) 'Best�llning skapad �r',  Count(*) 'Antal'
From Orders
Where status in(2, 3, 10, 16, 18, 19, 21) 
group by year(CreatedAt)
union all 
Select 2, 'Tillsatta/Utf�rda', year(CreatedAt), Count(*)
From Orders
Where status in(4, 5, 7)
group by year(CreatedAt) 
union all 
Select 3, 'Bokningsf�rfr�gan avb�jd av samtliga f�rmedlingar', year(CreatedAt), Count(*)
From Orders
Where status in(9) 
group by year(CreatedAt)
union all 
Select 4 ,'Tills�ttning ej besvarad', year(CreatedAt), Count(*)
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
Select 7, 'Uppdrag avbokat av f�rmedling', year(CreatedAt), Count(*)
From Orders
Where status in(12) 
group by year(CreatedAt)
union all
Select 8, 'Totalt antal avrop' 'tillst�nd', year(CreatedAt), Count(*)
From Orders
group by year(CreatedAt)
) a
order by sortering, 'Best�llning skapad �r'


Select * from
(
Select 'Sammanh�llen' 'Typ',  year(CreatedAt) '�r', Count(*) 'Antal'
From Orders
Where OrderGroupId is not null
group by year(CreatedAt)
union 
Select 'Frist�ende', year(CreatedAt), Count(*)
From Orders
Where OrderGroupId is null
group by year(CreatedAt)
) a
order by Typ, '�r'

Select * from
(
Select 'Med Enhet' 'Bas', year(CreatedAt) '�r', Count(*) 'Antal' from orders
Where CustomerUnitId is not null
group by year(CreatedAt)
union
Select 'Utan Enhet', year(CreatedAt) '�r', Count(*) from orders
Where CustomerUnitId is null
group by year(CreatedAt)
) a
order by Bas, '�r'

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


Select Count(distinct OrderId) 'F�rfr�gningar till f�rmedlingar med l�ng svarstid'
from Requests
Where LastAcceptAt is not null


Select Count(distinct OrderId) 'Best�llningar med flexibel starttid'
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
