
--detta kördes i efterhand
--64 request_lost_due_to_not_fully_answered
--i Prod ska vi först lägga in ny Broker och Rankings innan dessa kan köras (har aktiva rankings för avtal 2, eller kommer få om de läggs in innan)
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

--OBS kördes i efterhand 
--i Prod ska vi inte köra för de befintliga utan de som består + nya (har aktiva rankings för avtal 2, eller kommer få om de läggs in innan)
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


--kolla slutdatum på förra, men bör väl vara 2023-01-10, för att sätta när vi ska lägga in rankings
	SELECT * FROM Rankings r where r.LastValidDate > '2023-01-01'

--kolla om nytt inlagt redan
	SELECT * FROM FrameworkAgreements fa
	
--Lägg bara in om ej inlagt, ska väl få FrameworkAgreementId 2...
INSERT INTO FrameworkAgreements (AgreementNumber, Description, FirstValidDate, LastValidDate, BrokerFeeCalculationType, FrameworkAgreementResponseRuleset, OriginalLastValidDate, PossibleAgreementExtensionsInMonths)
	VALUES (N'24.4-2992-11', N'Andra ramavtalet som tolkavropstjänsten hanterar', '2023-01-23', '2027-01-23', 2, 2, '2027-01-23', 0);


--lägg in nya rankings som bygger på gammal ranking per region
	INSERT INTO Rankings (Rank, FirstValidDate, LastValidDate, BrokerId, RegionId, FrameworkAgreementId)
	SELECT r.Rank, '20230123', '20991231', r.BrokerId, r.RegionId, 2
	FROM Rankings r WHERE LastValidDate = '20230110'

--kolla om inlagt
SELECT * FROM BrokerFeeByServiceTypePriceListRows bfbstplr

--OBS! Kolla om nedan finns först (om inte lägg in)
INSERT BrokerFeeByServiceTypePriceListRows (Price, CompetenceLevel, InterpreterLocation, FirstValidDate, LastValidDate, RegionGroupId)
Select 50, 1, 1, '20230123', '20991231', 1
UNION ALL
Select 60, 2, 1, '20230123', '20991231', 1
UNION ALL
Select 80, 3, 1, '20230123', '20991231', 1
UNION ALL
Select 90, 4, 1, '20230123', '20991231', 1

UNION ALL

Select 90, 1, 1, '20230123', '20991231', 2
UNION ALL
Select 100, 2, 1, '20230123', '20991231', 2
UNION ALL
Select 120, 3, 1, '20230123', '20991231', 2
UNION ALL
Select 130, 4, 1, '20230123', '20991231', 2

UNION ALL

Select 70, 1, 1, '20230123', '20991231', 3
UNION ALL
Select 80, 2, 1, '20230123', '20991231', 3
UNION ALL
Select 100, 3, 1, '20230123', '20991231', 3
UNION ALL
Select 110, 4, 1, '20230123', '20991231', 3

UNION ALL

Select 50, 1, 4, '20230123', '20991231', 1
UNION ALL
Select 60, 2, 4, '20230123', '20991231', 1
UNION ALL
Select 80, 3, 4, '20230123', '20991231', 1
UNION ALL
Select 90, 4, 4, '20230123', '20991231', 1

UNION ALL

Select 90, 1, 4, '20230123', '20991231', 2
UNION ALL
Select 100, 2, 4, '20230123', '20991231', 2
UNION ALL
Select 120, 3, 4, '20230123', '20991231', 2
UNION ALL
Select 130, 4, 4, '20230123', '20991231', 2

UNION ALL

Select 70, 1, 4, '20230123', '20991231', 3
UNION ALL
Select 80, 2, 4, '20230123', '20991231', 3
UNION ALL
Select 100, 3, 4, '20230123', '20991231', 3
UNION ALL
Select 110, 4, 4, '20230123', '20991231', 3


UNION ALL

Select 20, 1, 2, '20230123', '20991231', 1
UNION ALL
Select 30, 2, 2, '20230123', '20991231', 1
UNION ALL
Select 50, 3, 2, '20230123', '20991231', 1
UNION ALL
Select 60, 4, 2, '20230123', '20991231', 1


UNION ALL

Select 20, 1, 2, '20230123', '20991231', 2
UNION ALL
Select 30, 2, 2, '20230123', '20991231', 2
UNION ALL
Select 50, 3, 2, '20230123', '20991231', 2
UNION ALL
Select 60, 4, 2, '20230123', '20991231', 2


UNION ALL

Select 20, 1, 2, '20230123', '20991231', 3
UNION ALL
Select 30, 2, 2, '20230123', '20991231', 3
UNION ALL
Select 50, 3, 2, '20230123', '20991231', 3
UNION ALL
Select 60, 4, 2, '20230123', '20991231', 3


UNION ALL

Select 20, 1, 3, '20230123', '20991231', 1
UNION ALL
Select 30, 2, 3, '20230123', '20991231', 1
UNION ALL
Select 50, 3, 3, '20230123', '20991231', 1
UNION ALL
Select 60, 4, 3, '20230123', '20991231', 1

UNION ALL

Select 20, 1, 3, '20230123', '20991231', 2
UNION ALL
Select 30, 2, 3, '20230123', '20991231', 2
UNION ALL
Select 50, 3, 3, '20230123', '20991231', 2
UNION ALL
Select 60, 4, 3, '20230123', '20991231', 2

UNION ALL

Select 20, 1, 3, '20230123', '20991231', 3
UNION ALL
Select 30, 2, 3, '20230123', '20991231', 3
UNION ALL
Select 50, 3, 3, '20230123', '20991231', 3
UNION ALL
Select 60, 4, 3, '20230123', '20991231', 3

