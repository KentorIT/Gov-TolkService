

-- OBS! Kolla och �ndra eventuellt datum
DECLARE @firstValidDate DATE = '2023-04-26'

--72 rader
--OBS! 3 regioner �r bortkommenterade (S�dermanland, Kronoberg, J�mtland - de har redan lagts in)
INSERT INTO Rankings (Rank, FirstValidDate, LastValidDate, BrokerId, RegionId, FrameworkAgreementId)
	VALUES
	--Stockholm	1	J�rva tolk	7	1
	--Stockholm	2	Spr�kpoolen	8	1
	--Stockholm	3	Digitaltolk	3	1
	--Stockholm	4	Spr�kservice 2  1
	(1, @firstValidDate, '2099-12-31', 7, 1, 2),
	(2, @firstValidDate, '2099-12-31', 8, 1, 2),
	(3, @firstValidDate, '2099-12-31', 3, 1, 2),
	(4, @firstValidDate, '2099-12-31', 2, 1, 2),

	--Uppsala	1	J�rva tolk	7	2
	--Uppsala	2	Spr�kservice 2	2
	--Uppsala	3	Transvoice	5	2
	--Uppsala	4	Spr�kpoolen	8	2
	(1, @firstValidDate, '2099-12-31', 7, 2, 2),
	(2, @firstValidDate, '2099-12-31', 2, 2, 2),
	(3, @firstValidDate, '2099-12-31', 5, 2, 2),
	(4, @firstValidDate, '2099-12-31', 8, 2, 2),

	--OBS! REDAN INLAGD
	----S�dermanland	1	Spr�kservice 2	3
	----S�dermanland	2	Digitaltolk	3	3
	----S�dermanland	3	J�rva tolk	7	3
	----S�dermanland	4	Spr�kpoolen	8	3
	--(1, @firstValidDate, '2099-12-31', 2, 3, 2), --OBS! kolla om de skrivit avtal, annars bortkommentera
	--(2, @firstValidDate, '2099-12-31', 3, 3, 2),
	--(3, @firstValidDate, '2099-12-31', 7, 3, 2),
	--(4, @firstValidDate, '2099-12-31', 8, 3, 2),

	--�sterg�tland	1	J�rva tolk	7	4
	--�sterg�tland	2	Transvoice	5	4
	--�sterg�tland	3	Digitaltolk	3	4
	--�sterg�tland	4	Spr�kpoolen	8	4
	(1, @firstValidDate, '2099-12-31', 7, 4, 2),
	(2, @firstValidDate, '2099-12-31', 5, 4, 2),
	(3, @firstValidDate, '2099-12-31', 3, 4, 2),
	(4, @firstValidDate, '2099-12-31', 8, 4, 2),

	--J�nk�ping	1	Spr�kpoolen	8	5
	--J�nk�ping	2	J�rva tolk	7	5
	--J�nk�ping	3	Spr�kservice 2	5
	--J�nk�ping	4	Digitaltolk	3	5
	(1, @firstValidDate, '2099-12-31', 8, 5, 2),
	(2, @firstValidDate, '2099-12-31', 7, 5, 2),
	(3, @firstValidDate, '2099-12-31', 2, 5, 2),
	(4, @firstValidDate, '2099-12-31', 3, 5, 2),

	--OBS! REDAN INLAGD
	----Kronoberg	1	Spr�kservice 2	6
	----Kronoberg	2	Transvoice	5	6
	----Kronoberg	3	Digitaltolk	3	6
	----Kronoberg	4	J�rva tolk	7	6
	--(1, @firstValidDate, '2099-12-31', 2, 6, 2),--OBS! kolla om de skrivit avtal, annars bortkommentera
	--(2, @firstValidDate, '2099-12-31', 5, 6, 2),--OBS! kolla om de skrivit avtal, annars bortkommentera
	--(3, @firstValidDate, '2099-12-31', 3, 6, 2),
	--(4, @firstValidDate, '2099-12-31', 7, 6, 2),

	--Kalmar	1	Transvoice	5	7
	--Kalmar	2	Spr�kpoolen	8	7
	--Kalmar	3	Spr�kservice 2	7
	--Kalmar	4	Digitaltolk	3	7
	(1, @firstValidDate, '2099-12-31', 5, 7, 2),
	(2, @firstValidDate, '2099-12-31', 8, 7, 2),
	(3, @firstValidDate, '2099-12-31', 2, 7, 2),
	(4, @firstValidDate, '2099-12-31', 3, 7, 2),

	--Blekinge	1	Transvoice	5	8
	--Blekinge	2	Spr�kpoolen	8	8
	--Blekinge	3	Digitaltolk	3	8
	--Blekinge	4	J�rva tolk	7	8
	(1, @firstValidDate, '2099-12-31', 5, 8, 2),
	(2, @firstValidDate, '2099-12-31', 8, 8, 2),
	(3, @firstValidDate, '2099-12-31', 3, 8, 2),
	(4, @firstValidDate, '2099-12-31', 7, 8, 2),

	--Halland	1	J�rva tolk	7	11
	--Halland	2	Spr�kservice 2	11
	--Halland	3	Spr�kpoolen	8	11
	--Halland	4	Digitaltolk	3	11
	(1, @firstValidDate, '2099-12-31', 7, 11, 2),
	(2, @firstValidDate, '2099-12-31', 2, 11, 2),
	(3, @firstValidDate, '2099-12-31', 8, 11, 2),
	(4, @firstValidDate, '2099-12-31', 3, 11, 2),

	--V�sta G�taland	1	Spr�kpoolen	8	13
	--V�sta G�taland	2	Transvoice	5	13
	--V�sta G�taland	3	J�rva tolk	7	13
	--V�sta G�taland	4	Spr�kservice 2	13
	(1, @firstValidDate, '2099-12-31', 8, 13, 2),
	(2, @firstValidDate, '2099-12-31', 5, 13, 2),
	(3, @firstValidDate, '2099-12-31', 7, 13, 2),
	(4, @firstValidDate, '2099-12-31', 2, 13, 2),

	--V�rmland	1	Digitaltolk	3	15
	--V�rmland	2	J�rva tolk	7	15
	--V�rmland	3	Spr�kservice 2	15
	--V�rmland	4	Spr�kpoolen	8	15
	(1, @firstValidDate, '2099-12-31', 3, 15, 2),
	(2, @firstValidDate, '2099-12-31', 7, 15, 2),
	(3, @firstValidDate, '2099-12-31', 2, 15, 2),
	(4, @firstValidDate, '2099-12-31', 8, 15, 2),

	--�rebro	1	Spr�kpoolen	8	16
	--�rebro	2	Spr�kservice 2	16
	--�rebro	3	Digitaltolk	3	16
	--�rebro	4	J�rva tolk	7	16
	(1, @firstValidDate, '2099-12-31', 8, 16, 2),
	(2, @firstValidDate, '2099-12-31', 2, 16, 2),
	(3, @firstValidDate, '2099-12-31', 3, 16, 2),
	(4, @firstValidDate, '2099-12-31', 7, 16, 2),

	--V�stmanland	1	Digitaltolk	3	17
	--V�stmanland	2	J�rva tolk	7	17
	--V�stmanland	3	Spr�kservice2	17
	--V�stmanland	4	Spr�kpoolen	8	17
	(1, @firstValidDate, '2099-12-31', 3, 17, 2),
	(2, @firstValidDate, '2099-12-31', 7, 17, 2),
	(3, @firstValidDate, '2099-12-31', 2, 17, 2),
	(4, @firstValidDate, '2099-12-31', 8, 17, 2),

	--Dalarna	1	J�rva tolk	7	18
	--Dalarna	2	Spr�kpoolen	8	18
	--Dalarna	3	Spr�kservice 2	18
	--Dalarna	4	Digitaltolk	3	18
	(1, @firstValidDate, '2099-12-31', 7, 18, 2),
	(2, @firstValidDate, '2099-12-31', 8, 18, 2),
	(3, @firstValidDate, '2099-12-31', 2, 18, 2),
	(4, @firstValidDate, '2099-12-31', 3, 18, 2),

	--G�vleborg	1	Transvoice	5	19
	--G�vleborg	2	Spr�kpoolen	8	19
	--G�vleborg	3	Spr�kservice 2	19
	--G�vleborg	4	J�rva tolk	7	19
	(1, @firstValidDate, '2099-12-31', 5, 19, 2),
	(2, @firstValidDate, '2099-12-31', 8, 19, 2),
	(3, @firstValidDate, '2099-12-31', 2, 19, 2),
	(4, @firstValidDate, '2099-12-31', 7, 19, 2),

	--V�sternorrland	1	Transvoice	5	20
	--V�sternorrland	2	J�rva tolk	7	20
	--V�sternorrland	3	Spr�kpoolen	8	20
	--V�sternorrland	4	Spr�kservice 2	20
	(1, @firstValidDate, '2099-12-31', 5, 20, 2),
	(2, @firstValidDate, '2099-12-31', 7, 20, 2),
	(3, @firstValidDate, '2099-12-31', 8, 20, 2),
	(4, @firstValidDate, '2099-12-31', 2, 20, 2),

	--OBS! REDAN INLAGD
	----J�mtland	1	Spr�kservice 2	21
	----J�mtland	2	Digitaltolk	3	21
	----J�mtland	3	Spr�kpoolen	8	21
	----J�mtland	4	J�rva tolk	7	21
	--(1, @firstValidDate, '2099-12-31', 2, 21, 2),--OBS! kolla om de skrivit avtal, annars bortkommentera
	--(2, @firstValidDate, '2099-12-31', 3, 21, 2),
	--(3, @firstValidDate, '2099-12-31', 8, 21, 2),
	--(4, @firstValidDate, '2099-12-31', 7, 21, 2)

	--V�sterbotten	1	J�rva tolk	7	22
	--V�sterbotten	2	Transvoice	5	22
	--V�sterbotten	3	Spr�kpoolen	8	22
	--V�sterbotten	4	Spr�kservice 2	22
	(1, @firstValidDate, '2099-12-31', 7, 22, 2),
	(2, @firstValidDate, '2099-12-31', 5, 22, 2),
	(3, @firstValidDate, '2099-12-31', 8, 22, 2),
	(4, @firstValidDate, '2099-12-31', 2, 22, 2),

	--Norrbotten	1	Digitaltolk	3	23
	--Norrbotten	2	Spr�kservice 2	23
	--Norrbotten	3	J�rva tolk	7	23
	--Norrbotten	4	Spr�kpoolen	8	23
	(1, @firstValidDate, '2099-12-31', 3, 23, 2),
	(2, @firstValidDate, '2099-12-31', 2, 23, 2),
	(3, @firstValidDate, '2099-12-31', 7, 23, 2),
	(4, @firstValidDate, '2099-12-31', 8, 23, 2),

	--Sk�ne	1	J�rva tolk	7	25
	--Sk�ne	2	Spr�kpoolen	8	25
	--Sk�ne	3	Spr�kservice 2	25
	--Sk�ne	4	Transvoice	5	25
	(1, @firstValidDate, '2099-12-31', 7, 25, 2),
	(2, @firstValidDate, '2099-12-31', 8, 25, 2),
	(3, @firstValidDate, '2099-12-31', 2, 25, 2),
	(4, @firstValidDate, '2099-12-31', 5, 25, 2),

	--Gotland	1	J�rva tolk	7	80
	--Gotland	2	Digitaltolk	3	80
	--Gotland	3	Spr�kpoolen	8	80
	--Gotland	4	Spr�kservice 2	80
	(1, @firstValidDate, '2099-12-31', 7, 80, 2),
	(2, @firstValidDate, '2099-12-31', 3, 80, 2),
	(3, @firstValidDate, '2099-12-31', 8, 80, 2),
	(4, @firstValidDate, '2099-12-31', 2, 80, 2)


--l�gg sen in nya notifieringstyper f�r alla brokers som har rankings f�r avtal 2

--dessa b�r inte bli n�gra, d� de redan l�sts in 
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

--dessa b�r inte bli n�gra, d� de redan l�sts in 
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

--dessa b�r inte bli n�gra, d� de redan l�sts in 
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

--dessa b�r inte bli n�gra, d� de redan l�sts in 
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

 --ta ut alla Rankings p� nya avtalen ur databasen och j�mf�r
SELECT reg.Name, r.Rank, b.Name, r.FirstValidDate, r.LastValidDate
FROM Rankings r
JOIN Regions reg ON reg.RegionId = r.RegionId
JOIN Brokers b ON b.BrokerId = r.BrokerId
WHERE r.FrameworkAgreementId = 2 AND r.LastValidDate > GETDATE()
ORDER BY 1, 2


--OBS Rensa cachen!