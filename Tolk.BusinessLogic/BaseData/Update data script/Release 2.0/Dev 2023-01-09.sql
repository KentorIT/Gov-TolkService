--det kördes även ett skript innan detta vid tidigare dev-release
DECLARE @lastValidDate DATETIME = '2023-01-10'

UPDATE rankings SET LastValidDate = @lastValidDate WHERE 
LastValidDate = '2022-12-13'


UPDATE FrameworkAgreements 
SET AgreementNumber = N'23.3-9066-16'
   ,Description = N'Första ramavtalet som tolkavropstjänsten hanterar'
   ,FirstValidDate = '2019-02-01'
   ,LastValidDate = @lastValidDate
   ,BrokerFeeCalculationType = 1
   ,FrameworkAgreementResponseRuleset = 1
   ,OriginalLastValidDate = '2021-01-10'
   ,PossibleAgreementExtensionsInMonths = 24
WHERE FrameworkAgreementId = 1;

--57 order_terminated_due_to_terminated_framework_agreement
INSERT INTO UserNotificationSettings (UserId, NotificationChannel, NotificationType)
SELECT id, 1, 57
FROM AspnetUsers a
left join UserNotificationSettings u on a.id = u.UserId and u.NotificationType = 57
WHERE IsApiUser = 1 AND BrokerId IN (SELECT r.BrokerId FROM Rankings r WHERE r.LastValidDate > GETDATE()) 
and u.NotificationChannel is null

--58 order_group_terminated_due_to_terminated_framework_agreement
INSERT UserNotificationSettings (UserId, NotificationChannel, NotificationType)
SELECT id, 1, 58
FROM AspnetUsers a
left join UserNotificationSettings u on a.id = u.UserId and u.NotificationType = 58
WHERE IsApiUser = 1 AND BrokerId IN (SELECT r.BrokerId FROM Rankings r WHERE r.LastValidDate > GETDATE()) 
and u.NotificationChannel is null