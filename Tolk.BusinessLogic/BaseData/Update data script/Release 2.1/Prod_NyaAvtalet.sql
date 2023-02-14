
--detta kördes i samband med releasen, då det inte var klart vilka förmedlingar som skrivit avtal så gick det inte att lägga in dessa i samband med release
--ändrade även en felaktig e-post för en Språkpoolen-användare)

DECLARE @newBrokerApiUser INT = 5793

--Den nya brokerId ska få e-postnotifiering för alla tidigare befintliga notifieringstyper också (är det verkligen alla)?
--kollat mot prod. på de som har "alla" 26 st 1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,57,58

INSERT INTO UserNotificationSettings (UserId, NotificationChannel, NotificationType)
	VALUES 
	(@newBrokerApiUser, 1, 1),
	(@newBrokerApiUser, 1, 2),
	(@newBrokerApiUser, 1, 3),
	(@newBrokerApiUser, 1, 4),
	(@newBrokerApiUser, 1, 5),
	(@newBrokerApiUser, 1, 6),
	(@newBrokerApiUser, 1, 7),
	(@newBrokerApiUser, 1, 8),
	(@newBrokerApiUser, 1, 9),
	(@newBrokerApiUser, 1, 10),
	(@newBrokerApiUser, 1, 11),
	(@newBrokerApiUser, 1, 12),
	(@newBrokerApiUser, 1, 13),
	(@newBrokerApiUser, 1, 14),
	(@newBrokerApiUser, 1, 15),
	(@newBrokerApiUser, 1, 16),
	(@newBrokerApiUser, 1, 17),
	(@newBrokerApiUser, 1, 18),
	(@newBrokerApiUser, 1, 19),
	(@newBrokerApiUser, 1, 20),
	(@newBrokerApiUser, 1, 21),
	(@newBrokerApiUser, 1, 22),
	(@newBrokerApiUser, 1, 23),
	(@newBrokerApiUser, 1, 24),
	(@newBrokerApiUser, 1, 57),
	(@newBrokerApiUser, 1, 58)

