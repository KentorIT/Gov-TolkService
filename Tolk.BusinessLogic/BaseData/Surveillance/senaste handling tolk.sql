--use TolkDev

Declare @Today as varchar(12)

Set @Today = CAST(datepart(yyyy, GETDATE()) as varchar(4)) + '-'  + cast(datepart(mm, GETDATE()) as varchar(2)) + '-' + cast(datepart(dd, GETDATE()) as varchar(2))

--Senaste handling
Select Max(CreatedAt) 'at', 'Created order' 'type'
from Orders
Where OrderGroupId is null
union 
Select Max(CreatedAt) 'At', 'Created order group' 'type'
from OrderGroups og
union
Select Max(AcceptedAt) 'at', 'Accepted request' 'type'
from Requests
union
Select Max(CreatedAt) 'at', 'Created requisition' 'type'
from Requisitions
union
Select Max(CreatedAt) 'at', 'Created complaint' 'type'
from Complaints
union
Select Max(LoggedAt) 'at', 'Created user' 'type'
from UserAuditLogEntries ua
Where UserChangeType = 1
union 
Select Max(l.LoggedInAt) 'at', 'Login Customer' 'type'
from UserLoginLogEntries l
Join AspNetUsers u
on u.id = l.UserId
Where u.CustomerOrganisationId is not null
union 
Select Max(l.LoggedInAt) 'at', 'Login Broker' 'type'
from UserLoginLogEntries l
Join AspNetUsers u
on u.id = l.UserId
Where u.BrokerId is not null
union 
Select Max(l.LoggedInAt) 'at', 'Login Admin' 'type'
from UserLoginLogEntries l
Join AspNetUsers u
on u.id = l.UserId
Where u.BrokerId is null And u.CustomerOrganisationId is null and u.InterpreterId is null


Select * from
(
	Select o.OrderNumber 'Entity', CreatedAt 'At', u.NameFirst + ' ' + u.NameFamily 'By', c.name 'org', 'Created Order' 'type'
	from Orders o
	Join AspNetUsers u
	On u.id = o.CreatedBy
	Join CustomerOrganisations c
	On c.CustomerOrganisationId = o.CustomerOrganisationId
	Where o.OrderGroupId is null
	
	union
	
	Select o.OrderNumber, r.AcceptedAt 'At', u.NameFirst + ' ' + u.NameFamily 'By', b.Name 'org', 'accepted request' 'type'
	from Requests r
	Join AspNetUsers u
	On u.id = r.AcceptedBy
	JOin orders o
	On o.OrderId = r.OrderId
	Join Rankings ra
	On ra.RankingId = r.RankingId
	Join Brokers b
	On b.BrokerId = ra.BrokerId
	
	union
	
	Select o.OrderNumber, req.CreatedAt 'At', isnull(u.NameFirst + ' ' + u.NameFamily, 'Systemet') 'By', c.Name 'org', 'created Requisition' 'type'
	from Requisitions req
	Join Requests r
	On req.RequestId = r.RequestId
	JOin orders o
	On o.OrderId = r.OrderId
	Join CustomerOrganisations c
	On o.CustomerOrganisationId = c.CustomerOrganisationId
	left Join AspNetUsers u
	On u.id = req.CreatedBy
	
	union
	
	Select o.OrderNumber, co.CreatedAt 'At', u.NameFirst + ' ' + u.NameFamily 'By', c.Name 'org', 'created Complaint' 'type'
	from Complaints co
	Join Requests r
	On co.RequestId = r.RequestId
	JOin orders o
	On o.OrderId = r.OrderId
	Join CustomerOrganisations c
	On o.CustomerOrganisationId = c.CustomerOrganisationId
	left Join AspNetUsers u
	On u.id = co.CreatedBy
	
	union 
	
	Select null, ua.LoggedAt 'At', isnull(uAdm.NameFirst + ' ' + uAdm.NameFamily, 'Self-registry') 'By', coalesce(c.Name, b.Name, 'Kammarkollegiet') 'org', 'created User' 'type'
	from UserAuditLogEntries ua
	Join AspNetUsers u
	On u.id = ua.UserId
	left Join AspNetUsers uAdm
	On uAdm.id = ua.UpdatedByUserId
	Left Join CustomerOrganisations c
	On u.CustomerOrganisationId = c.CustomerOrganisationId
	Left Join Brokers b
	On u.BrokerId = b.BrokerId
	Where UserChangeType = 1
	
	union
	
	Select og.OrderGroupNumber, CreatedAt 'At', u.NameFirst + ' ' + u.NameFamily 'By', c.name 'org', 'Created Order Group' 'type'
	from OrderGroups og
	Join AspNetUsers u
	On u.id = og.CreatedBy
	Join CustomerOrganisations c
	On c.CustomerOrganisationId = og.CustomerOrganisationId
)a
Where a.At > @Today
order by a.At desc


Select * from 
(

	Select u.Email 'Login', l.LoggedInAt 'At', u.NameFirst + ' ' + u.NameFamily 'By', c.Name 'Organisation', 'Login Customer' 'type'
	from UserLoginLogEntries l
	Join AspNetUsers u
	On u.id = l.UserId
	Join CustomerOrganisations c
	On c.CustomerOrganisationId = u.CustomerOrganisationId

	union 

	Select u.Email, l.LoggedInAt, u.NameFirst + ' ' + u.NameFamily, b.Name, 'Login Broker'
	from UserLoginLogEntries l
	Join AspNetUsers u
	On u.id = l.UserId
	Join Brokers b
	On b.BrokerId = u.BrokerId

	union 

	Select u.Email, l.LoggedInAt, u.NameFirst + ' ' + u.NameFamily, 'Admins', 'Login Admin'
	from UserLoginLogEntries l
	Join AspNetUsers u
	On u.id = l.UserId
	Where u.CustomerOrganisationId is null and u.BrokerId is null and u.InterpreterId is null

)a
Where a.At > @Today
order by a.At desc


