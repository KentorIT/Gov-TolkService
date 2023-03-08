


--Add role to a user. (The IsApiUser bit makes this user hidden in all user lists...)
INSERT AspNetUsers (ConcurrencyStamp, Email, NormalizedEmail, NormalizedUserName, SecurityStamp, UserName, AccessFailedCount, EmailConfirmed, LockoutEnabled, PhoneNumberConfirmed, TwoFactorEnabled, CustomerOrganisationId, BrokerId, InterpreterId, NameFirst, NameFamily, PhoneNumberCellphone, PasswordHash, IsActive, IsApiUser)
	SELECT
		NEWID()
	   ,b.EmailAddress
	   ,UPPER(b.EmailAddress)
	   ,UPPER('api@' + b.EmailDomain)
	   ,NEWID()
	   ,'api@' + b.EmailDomain
	   ,0
	   ,0
	   ,1
	   ,0
	   ,0
	   ,NULL
	   ,b.BrokerId
	   ,NULL
	   ,'Api'
	   ,'User'
	   ,'xx'
	   ,NULL
	   ,1
	   ,1
	FROM Brokers b
	LEFT JOIN AspNetUsers u
		ON b.BrokerId = u.BrokerId
			AND IsApiUser = 1
	WHERE u.BrokerId IS NULL

--add email for all NotificationTypes as UserNotificationSettings for each broker 
CREATE TABLE #NotificationType(
	[NotificationType] INT NOT NULL)

INSERT INTO #NotificationType (NotificationType)
	VALUES (1), (2), (3), (4), (5), (6), (7), (8), (9), (10), (11), (12), (13), (14), (15), (16) , (17), (18), (19), (20), (21), (22), (23), (24), (32), (57), (58), (59), (60), (64), (65)	


	INSERT UserNotificationSettings (UserId, NotificationChannel, NotificationType, ConnectionInformation)
	SELECT 
	u.Id, 1, nt.NotificationType, b.EmailAddress
	FROM  AspNetUsers u
	JOIN Brokers b
		ON b.BrokerId = u.BrokerId
		JOIN #NotificationType nt ON 1 = 1
			WHERE IsApiUser = 1

	DROP TABLE #NotificationType