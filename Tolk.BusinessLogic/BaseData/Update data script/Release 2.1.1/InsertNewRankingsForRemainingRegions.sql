

-- OBS! Kolla och ändra eventuellt datum
DECLARE @firstValidDate DATE = '2023-04-26'

--72 rader
--OBS! 3 regioner är bortkommenterade (Södermanland, Kronoberg, Jämtland - de har redan lagts in)
INSERT INTO Rankings (Rank, FirstValidDate, LastValidDate, BrokerId, RegionId, FrameworkAgreementId)
	VALUES
	--Stockholm	1	Järva tolk	7	1
	--Stockholm	2	Språkpoolen	8	1
	--Stockholm	3	Digitaltolk	3	1
	--Stockholm	4	Språkservice 2  1
	(1, @firstValidDate, '2099-12-31', 7, 1, 2),
	(2, @firstValidDate, '2099-12-31', 8, 1, 2),
	(3, @firstValidDate, '2099-12-31', 3, 1, 2),
	(4, @firstValidDate, '2099-12-31', 2, 1, 2),

	--Uppsala	1	Järva tolk	7	2
	--Uppsala	2	Språkservice 2	2
	--Uppsala	3	Transvoice	5	2
	--Uppsala	4	Språkpoolen	8	2
	(1, @firstValidDate, '2099-12-31', 7, 2, 2),
	(2, @firstValidDate, '2099-12-31', 2, 2, 2),
	(3, @firstValidDate, '2099-12-31', 5, 2, 2),
	(4, @firstValidDate, '2099-12-31', 8, 2, 2),

	--OBS! REDAN INLAGD
	----Södermanland	1	Språkservice 2	3
	----Södermanland	2	Digitaltolk	3	3
	----Södermanland	3	Järva tolk	7	3
	----Södermanland	4	Språkpoolen	8	3
	--(1, @firstValidDate, '2099-12-31', 2, 3, 2), --OBS! kolla om de skrivit avtal, annars bortkommentera
	--(2, @firstValidDate, '2099-12-31', 3, 3, 2),
	--(3, @firstValidDate, '2099-12-31', 7, 3, 2),
	--(4, @firstValidDate, '2099-12-31', 8, 3, 2),

	--Östergötland	1	Järva tolk	7	4
	--Östergötland	2	Transvoice	5	4
	--Östergötland	3	Digitaltolk	3	4
	--Östergötland	4	Språkpoolen	8	4
	(1, @firstValidDate, '2099-12-31', 7, 4, 2),
	(2, @firstValidDate, '2099-12-31', 5, 4, 2),
	(3, @firstValidDate, '2099-12-31', 3, 4, 2),
	(4, @firstValidDate, '2099-12-31', 8, 4, 2),

	--Jönköping	1	Språkpoolen	8	5
	--Jönköping	2	Järva tolk	7	5
	--Jönköping	3	Språkservice 2	5
	--Jönköping	4	Digitaltolk	3	5
	(1, @firstValidDate, '2099-12-31', 8, 5, 2),
	(2, @firstValidDate, '2099-12-31', 7, 5, 2),
	(3, @firstValidDate, '2099-12-31', 2, 5, 2),
	(4, @firstValidDate, '2099-12-31', 3, 5, 2),

	--OBS! REDAN INLAGD
	----Kronoberg	1	Språkservice 2	6
	----Kronoberg	2	Transvoice	5	6
	----Kronoberg	3	Digitaltolk	3	6
	----Kronoberg	4	Järva tolk	7	6
	--(1, @firstValidDate, '2099-12-31', 2, 6, 2),--OBS! kolla om de skrivit avtal, annars bortkommentera
	--(2, @firstValidDate, '2099-12-31', 5, 6, 2),--OBS! kolla om de skrivit avtal, annars bortkommentera
	--(3, @firstValidDate, '2099-12-31', 3, 6, 2),
	--(4, @firstValidDate, '2099-12-31', 7, 6, 2),

	--Kalmar	1	Transvoice	5	7
	--Kalmar	2	Språkpoolen	8	7
	--Kalmar	3	Språkservice 2	7
	--Kalmar	4	Digitaltolk	3	7
	(1, @firstValidDate, '2099-12-31', 5, 7, 2),
	(2, @firstValidDate, '2099-12-31', 8, 7, 2),
	(3, @firstValidDate, '2099-12-31', 2, 7, 2),
	(4, @firstValidDate, '2099-12-31', 3, 7, 2),

	--Blekinge	1	Transvoice	5	8
	--Blekinge	2	Språkpoolen	8	8
	--Blekinge	3	Digitaltolk	3	8
	--Blekinge	4	Järva tolk	7	8
	(1, @firstValidDate, '2099-12-31', 5, 8, 2),
	(2, @firstValidDate, '2099-12-31', 8, 8, 2),
	(3, @firstValidDate, '2099-12-31', 3, 8, 2),
	(4, @firstValidDate, '2099-12-31', 7, 8, 2),

	--Halland	1	Järva tolk	7	11
	--Halland	2	Språkservice 2	11
	--Halland	3	Språkpoolen	8	11
	--Halland	4	Digitaltolk	3	11
	(1, @firstValidDate, '2099-12-31', 7, 11, 2),
	(2, @firstValidDate, '2099-12-31', 2, 11, 2),
	(3, @firstValidDate, '2099-12-31', 8, 11, 2),
	(4, @firstValidDate, '2099-12-31', 3, 11, 2),

	--Västa Götaland	1	Språkpoolen	8	13
	--Västa Götaland	2	Transvoice	5	13
	--Västa Götaland	3	Järva tolk	7	13
	--Västa Götaland	4	Språkservice 2	13
	(1, @firstValidDate, '2099-12-31', 8, 13, 2),
	(2, @firstValidDate, '2099-12-31', 5, 13, 2),
	(3, @firstValidDate, '2099-12-31', 7, 13, 2),
	(4, @firstValidDate, '2099-12-31', 2, 13, 2),

	--Värmland	1	Digitaltolk	3	15
	--Värmland	2	Järva tolk	7	15
	--Värmland	3	Språkservice 2	15
	--Värmland	4	Språkpoolen	8	15
	(1, @firstValidDate, '2099-12-31', 3, 15, 2),
	(2, @firstValidDate, '2099-12-31', 7, 15, 2),
	(3, @firstValidDate, '2099-12-31', 2, 15, 2),
	(4, @firstValidDate, '2099-12-31', 8, 15, 2),

	--Örebro	1	Språkpoolen	8	16
	--Örebro	2	Språkservice 2	16
	--Örebro	3	Digitaltolk	3	16
	--Örebro	4	Järva tolk	7	16
	(1, @firstValidDate, '2099-12-31', 8, 16, 2),
	(2, @firstValidDate, '2099-12-31', 2, 16, 2),
	(3, @firstValidDate, '2099-12-31', 3, 16, 2),
	(4, @firstValidDate, '2099-12-31', 7, 16, 2),

	--Västmanland	1	Digitaltolk	3	17
	--Västmanland	2	Järva tolk	7	17
	--Västmanland	3	Språkservice2	17
	--Västmanland	4	Språkpoolen	8	17
	(1, @firstValidDate, '2099-12-31', 3, 17, 2),
	(2, @firstValidDate, '2099-12-31', 7, 17, 2),
	(3, @firstValidDate, '2099-12-31', 2, 17, 2),
	(4, @firstValidDate, '2099-12-31', 8, 17, 2),

	--Dalarna	1	Järva tolk	7	18
	--Dalarna	2	Språkpoolen	8	18
	--Dalarna	3	Språkservice 2	18
	--Dalarna	4	Digitaltolk	3	18
	(1, @firstValidDate, '2099-12-31', 7, 18, 2),
	(2, @firstValidDate, '2099-12-31', 8, 18, 2),
	(3, @firstValidDate, '2099-12-31', 2, 18, 2),
	(4, @firstValidDate, '2099-12-31', 3, 18, 2),

	--Gävleborg	1	Transvoice	5	19
	--Gävleborg	2	Språkpoolen	8	19
	--Gävleborg	3	Språkservice 2	19
	--Gävleborg	4	Järva tolk	7	19
	(1, @firstValidDate, '2099-12-31', 5, 19, 2),
	(2, @firstValidDate, '2099-12-31', 8, 19, 2),
	(3, @firstValidDate, '2099-12-31', 2, 19, 2),
	(4, @firstValidDate, '2099-12-31', 7, 19, 2),

	--Västernorrland	1	Transvoice	5	20
	--Västernorrland	2	Järva tolk	7	20
	--Västernorrland	3	Språkpoolen	8	20
	--Västernorrland	4	Språkservice 2	20
	(1, @firstValidDate, '2099-12-31', 5, 20, 2),
	(2, @firstValidDate, '2099-12-31', 7, 20, 2),
	(3, @firstValidDate, '2099-12-31', 8, 20, 2),
	(4, @firstValidDate, '2099-12-31', 2, 20, 2),

	--OBS! REDAN INLAGD
	----Jämtland	1	Språkservice 2	21
	----Jämtland	2	Digitaltolk	3	21
	----Jämtland	3	Språkpoolen	8	21
	----Jämtland	4	Järva tolk	7	21
	--(1, @firstValidDate, '2099-12-31', 2, 21, 2),--OBS! kolla om de skrivit avtal, annars bortkommentera
	--(2, @firstValidDate, '2099-12-31', 3, 21, 2),
	--(3, @firstValidDate, '2099-12-31', 8, 21, 2),
	--(4, @firstValidDate, '2099-12-31', 7, 21, 2)

	--Västerbotten	1	Järva tolk	7	22
	--Västerbotten	2	Transvoice	5	22
	--Västerbotten	3	Språkpoolen	8	22
	--Västerbotten	4	Språkservice 2	22
	(1, @firstValidDate, '2099-12-31', 7, 22, 2),
	(2, @firstValidDate, '2099-12-31', 5, 22, 2),
	(3, @firstValidDate, '2099-12-31', 8, 22, 2),
	(4, @firstValidDate, '2099-12-31', 2, 22, 2),

	--Norrbotten	1	Digitaltolk	3	23
	--Norrbotten	2	Språkservice 2	23
	--Norrbotten	3	Järva tolk	7	23
	--Norrbotten	4	Språkpoolen	8	23
	(1, @firstValidDate, '2099-12-31', 3, 23, 2),
	(2, @firstValidDate, '2099-12-31', 2, 23, 2),
	(3, @firstValidDate, '2099-12-31', 7, 23, 2),
	(4, @firstValidDate, '2099-12-31', 8, 23, 2),

	--Skåne	1	Järva tolk	7	25
	--Skåne	2	Språkpoolen	8	25
	--Skåne	3	Språkservice 2	25
	--Skåne	4	Transvoice	5	25
	(1, @firstValidDate, '2099-12-31', 7, 25, 2),
	(2, @firstValidDate, '2099-12-31', 8, 25, 2),
	(3, @firstValidDate, '2099-12-31', 2, 25, 2),
	(4, @firstValidDate, '2099-12-31', 5, 25, 2),

	--Gotland	1	Järva tolk	7	80
	--Gotland	2	Digitaltolk	3	80
	--Gotland	3	Språkpoolen	8	80
	--Gotland	4	Språkservice 2	80
	(1, @firstValidDate, '2099-12-31', 7, 80, 2),
	(2, @firstValidDate, '2099-12-31', 3, 80, 2),
	(3, @firstValidDate, '2099-12-31', 8, 80, 2),
	(4, @firstValidDate, '2099-12-31', 2, 80, 2)


--lägg sen in nya notifieringstyper för alla brokers som har rankings för avtal 2

--dessa bör inte bli några, då de redan lästs in 
--INSERT INTO UserNotificationSettings (UserId, NotificationChannel, NotificationType)
	SELECT
		Id
	   ,1
	   ,59
	FROM AspNetUsers a
	LEFT JOIN UserNotificationSettings u
		ON a.Id = u.UserId
			AND u.NotificationType = 59
	WHERE IsApiUser = 1
	AND BrokerId IN (SELECT
			r.BrokerId
		FROM Rankings r
		WHERE r.FrameworkAgreementId = 2)
	AND u.NotificationChannel IS NULL

--dessa bör inte bli några, då de redan lästs in 
--60 request_group_created_requires_acceptance_only
--INSERT UserNotificationSettings (UserId, NotificationChannel, NotificationType)
	SELECT
		Id
	   ,1
	   ,60
	FROM AspNetUsers a
	LEFT JOIN UserNotificationSettings u
		ON a.Id = u.UserId
			AND u.NotificationType = 60
	WHERE IsApiUser = 1
	AND BrokerId IN (SELECT
			r.BrokerId
		FROM Rankings r
		WHERE r.FrameworkAgreementId = 2)
	AND u.NotificationChannel IS NULL

--dessa bör inte bli några, då de redan lästs in 
--64 request_lost_due_to_not_fully_answered
--INSERT INTO UserNotificationSettings (UserId, NotificationChannel, NotificationType)
	SELECT
		Id
	   ,1
	   ,64
	FROM AspNetUsers a
	LEFT JOIN UserNotificationSettings u
		ON a.Id = u.UserId
			AND u.NotificationType = 64
	WHERE IsApiUser = 1
	AND BrokerId IN (SELECT
			r.BrokerId
		FROM Rankings r
		WHERE r.FrameworkAgreementId = 2)
	AND u.NotificationChannel IS NULL

--dessa bör inte bli några, då de redan lästs in 
--65 request_group_lost_due_to_not_fully_answered
--INSERT UserNotificationSettings (UserId, NotificationChannel, NotificationType)
	SELECT
		Id
	   ,1
	   ,65
	FROM AspNetUsers a
	LEFT JOIN UserNotificationSettings u
		ON a.Id = u.UserId
			AND u.NotificationType = 65
	WHERE IsApiUser = 1
	AND BrokerId IN (SELECT
			r.BrokerId
		FROM Rankings r
		WHERE r.FrameworkAgreementId = 2)
	AND u.NotificationChannel IS NULL

 --ta ut alla Rankings på nya avtalen ur databasen och jämför
SELECT reg.Name, r.Rank, b.Name, r.FirstValidDate, r.LastValidDate
FROM Rankings r
JOIN Regions reg ON reg.RegionId = r.RegionId
JOIN Brokers b ON b.BrokerId = r.BrokerId
WHERE r.FrameworkAgreementId = 2 AND r.LastValidDate > GETDATE()
ORDER BY 1, 2


--OBS Rensa cachen!