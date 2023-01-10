
--sätt lastValidDate (sista dagen avtalet ska gälla)
DECLARE @lastValidDate DATETIME = '2023-01-25'

UPDATE rankings SET LastValidDate = @lastValidDate WHERE 
LastValidDate > GETDATE()

--sätter allt för befintligt avtal (vissa saker har redan satts via migreringsscript)
UPDATE FrameworkAgreements 
SET AgreementNumber = N'23.3-9066-16'
   ,Description = N'Första ramavtalet som tolkavropstjänsten hanterar'
   ,FirstValidDate = '2019-02-01'
   ,LastValidDate = @lastValidDate
   ,BrokerFeeCalculationType = 1
   ,FrameworkAgreementResponseRuleset = 1
   ,OriginalLastValidDate = '2021-01-25'
   ,PossibleAgreementExtensionsInMonths = 24
WHERE FrameworkAgreementId = 1;


--add notification setting for brokers ApiUser for 
--only brokers that have active rankings

--57 order_terminated_due_to_terminated_framework_agreement
INSERT INTO UserNotificationSettings (UserId, NotificationChannel, NotificationType)
SELECT id, 1, 57
FROM AspnetUsers 
WHERE IsApiUser = 1 AND BrokerId IN (SELECT r.BrokerId FROM Rankings r WHERE r.LastValidDate > GETDATE()) 

--58 order_group_terminated_due_to_terminated_framework_agreement
INSERT UserNotificationSettings (UserId, NotificationChannel, NotificationType)
SELECT id, 1, 58
FROM AspnetUsers
WHERE IsApiUser = 1 AND BrokerId IN (SELECT r.BrokerId FROM Rankings r WHERE r.LastValidDate > GETDATE()) 