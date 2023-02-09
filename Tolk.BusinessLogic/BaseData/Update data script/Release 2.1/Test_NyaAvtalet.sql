


--kolla slutdatum på förra, men bör väl vara 2023-01-25, för att sätta när vi ska lägga in rankings
	SELECT * FROM Rankings r where r.LastValidDate > '2023-01-01'

--här vore det bra att lägga in i förtid att det inte har börjat gälla och se att allt funkar som vanligt under fredagen 10/2
	DECLARE @firstValidDate DATE = '2023-02-11'
	DECLARE @lastValidDate DATE = DATEADD(YEAR, 4, @firstValidDate);
	
--Lägg bara in om ej inlagt, ska väl få FrameworkAgreementId 2...
INSERT INTO FrameworkAgreements (AgreementNumber, Description, FirstValidDate, LastValidDate, BrokerFeeCalculationType, FrameworkAgreementResponseRuleset, OriginalLastValidDate)
	VALUES (N'23.3-12000-20', N'Andra ramavtalet som tolkavropstjänsten hanterar', @firstValidDate, @lastValidDate, 2, 2, @lastValidDate);

--lägg in nya rankings som bygger på gammal ranking per region (vi kopierar som från första avtalet)
	INSERT INTO Rankings (Rank, FirstValidDate, LastValidDate, BrokerId, RegionId, FrameworkAgreementId)
	SELECT r.Rank, @firstValidDate, '20991231', r.BrokerId, r.RegionId, 2
	FROM Rankings r WHERE LastValidDate = '20230125'

--59 request_created_requires_acceptance_only
INSERT INTO UserNotificationSettings (UserId, NotificationChannel, NotificationType)
SELECT id, 1, 59
FROM AspnetUsers a
left join UserNotificationSettings u on a.id = u.UserId and u.NotificationType = 59
WHERE IsApiUser = 1 AND BrokerId IN (SELECT r.BrokerId FROM Rankings r WHERE r.FrameworkAgreementId = 2) 
and u.NotificationChannel is null

--60 request_group_created_requires_acceptance_only
INSERT UserNotificationSettings (UserId, NotificationChannel, NotificationType)
SELECT id, 1, 60
FROM AspnetUsers a
left join UserNotificationSettings u on a.id = u.UserId and u.NotificationType = 60
WHERE IsApiUser = 1 AND BrokerId IN (SELECT r.BrokerId FROM Rankings r WHERE r.FrameworkAgreementId = 2) 
and u.NotificationChannel is NULL

--64 request_lost_due_to_not_fully_answered
INSERT INTO UserNotificationSettings (UserId, NotificationChannel, NotificationType)
SELECT id, 1, 64
FROM AspnetUsers a
left join UserNotificationSettings u on a.id = u.UserId and u.NotificationType = 64
WHERE IsApiUser = 1 AND BrokerId IN (SELECT r.BrokerId FROM Rankings r WHERE r.FrameworkAgreementId = 2) 
and u.NotificationChannel is null

--65 request_group_lost_due_to_not_fully_answered
INSERT UserNotificationSettings (UserId, NotificationChannel, NotificationType)
SELECT id, 1, 65
FROM AspnetUsers a
left join UserNotificationSettings u on a.id = u.UserId and u.NotificationType = 65
WHERE IsApiUser = 1 AND BrokerId IN (SELECT r.BrokerId FROM Rankings r WHERE r.FrameworkAgreementId = 2) 
and u.NotificationChannel is NULL



--kolla om inlagt
SELECT * FROM BrokerFeeByServiceTypePriceListRows bfbstplr

--annars lägg in
INSERT BrokerFeeByServiceTypePriceListRows (Price, CompetenceLevel, InterpreterLocation, FirstValidDate, LastValidDate, RegionGroupId)
Select 50, 1, 1, '@firstValidDate', '20991231', 1
UNION ALL
Select 60, 2, 1, '@firstValidDate', '20991231', 1
UNION ALL
Select 80, 3, 1, '@firstValidDate', '20991231', 1
UNION ALL
Select 90, 4, 1, '@firstValidDate', '20991231', 1

UNION ALL

Select 90, 1, 1, '@firstValidDate', '20991231', 2
UNION ALL
Select 100, 2, 1, '@firstValidDate', '20991231', 2
UNION ALL
Select 120, 3, 1, '@firstValidDate', '20991231', 2
UNION ALL
Select 130, 4, 1, '@firstValidDate', '20991231', 2

UNION ALL

Select 70, 1, 1, '@firstValidDate', '20991231', 3
UNION ALL
Select 80, 2, 1, '@firstValidDate', '20991231', 3
UNION ALL
Select 100, 3, 1, '@firstValidDate', '20991231', 3
UNION ALL
Select 110, 4, 1, '@firstValidDate', '20991231', 3

UNION ALL

Select 50, 1, 4, '@firstValidDate', '20991231', 1
UNION ALL
Select 60, 2, 4, '@firstValidDate', '20991231', 1
UNION ALL
Select 80, 3, 4, '@firstValidDate', '20991231', 1
UNION ALL
Select 90, 4, 4, '@firstValidDate', '20991231', 1

UNION ALL

Select 90, 1, 4, '@firstValidDate', '20991231', 2
UNION ALL
Select 100, 2, 4, '@firstValidDate', '20991231', 2
UNION ALL
Select 120, 3, 4, '@firstValidDate', '20991231', 2
UNION ALL
Select 130, 4, 4, '@firstValidDate', '20991231', 2

UNION ALL

Select 70, 1, 4, '@firstValidDate', '20991231', 3
UNION ALL
Select 80, 2, 4, '@firstValidDate', '20991231', 3
UNION ALL
Select 100, 3, 4, '@firstValidDate', '20991231', 3
UNION ALL
Select 110, 4, 4, '@firstValidDate', '20991231', 3


UNION ALL

Select 20, 1, 2, '@firstValidDate', '20991231', 1
UNION ALL
Select 30, 2, 2, '@firstValidDate', '20991231', 1
UNION ALL
Select 50, 3, 2, '@firstValidDate', '20991231', 1
UNION ALL
Select 60, 4, 2, '@firstValidDate', '20991231', 1


UNION ALL

Select 20, 1, 2, '@firstValidDate', '20991231', 2
UNION ALL
Select 30, 2, 2, '@firstValidDate', '20991231', 2
UNION ALL
Select 50, 3, 2, '@firstValidDate', '20991231', 2
UNION ALL
Select 60, 4, 2, '@firstValidDate', '20991231', 2


UNION ALL

Select 20, 1, 2, '@firstValidDate', '20991231', 3
UNION ALL
Select 30, 2, 2, '@firstValidDate', '20991231', 3
UNION ALL
Select 50, 3, 2, '@firstValidDate', '20991231', 3
UNION ALL
Select 60, 4, 2, '@firstValidDate', '20991231', 3


UNION ALL

Select 20, 1, 3, '@firstValidDate', '20991231', 1
UNION ALL
Select 30, 2, 3, '@firstValidDate', '20991231', 1
UNION ALL
Select 50, 3, 3, '@firstValidDate', '20991231', 1
UNION ALL
Select 60, 4, 3, '@firstValidDate', '20991231', 1

UNION ALL

Select 20, 1, 3, '@firstValidDate', '20991231', 2
UNION ALL
Select 30, 2, 3, '@firstValidDate', '20991231', 2
UNION ALL
Select 50, 3, 3, '@firstValidDate', '20991231', 2
UNION ALL
Select 60, 4, 3, '@firstValidDate', '20991231', 2

UNION ALL

Select 20, 1, 3, '@firstValidDate', '20991231', 3
UNION ALL
Select 30, 2, 3, '@firstValidDate', '20991231', 3
UNION ALL
Select 50, 3, 3, '@firstValidDate', '20991231', 3
UNION ALL
Select 60, 4, 3, '@firstValidDate', '20991231', 3

