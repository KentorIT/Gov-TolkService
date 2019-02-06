
--OBS!!!!! Semantix ska tas bort så detta måste göras om
--Email to Broker and UserNotificationSettings must be set!

DELETE FROM Brokers
DBCC CHECKIDENT ('Brokers', RESEED, 0)

-- To be completed: EmailAddress to Broker
INSERT Brokers (BrokerId, Name, EmailDomain, EmailAddress, OrganizationNumber)
	VALUES 
	(1, 'Hero Tolk AB', 'herotolk.se', '', '556400-4546'),
	(2, 'Språkservice Sverige AB', 'sprakservice.se', '', '556629-1513'),
	(3, 'Semantix Tolkjouren AB', 'semantix.se', '', '556526-2630'),
	(4, 'Digital Interpretations Scandianvia AB', 'digitaltolk.se', '', '559032-5394'),
	(5, 'Linguacom AB', 'linguacom.se', '', '556863-9628'),
	(6, 'Stockholm Tolkförmedling AB', 'transcom.com', '', '556482-8654'),
	(7, 'Folkhälsobyrån Tolkförmedling', 'folkhalsobyran.se', '', '556580-0744'),
	(8, 'Järva Tolk och Översättning AB', 'jarvatolk.se', '', '556613-1792')
	

--Blekinge (8)	Hero Tolk AB	1%	Semantix Tolkjouren AB	7%	Språkservice Sverige AB	9%	Digital Interpretations Scandianvia AB	12%
INSERT Rankings (BrokerId, RegionId, BrokerFee, Rank, FirstValidDate, LastValidDate)
VALUES 
(1, 8, 0.01, 1, '20190101', '99991231'),
(3, 8, 0.07, 2, '20190101', '99991231'),
(2, 8, 0.09, 3, '20190101', '99991231'),
(4, 8, 0.12, 4, '20190101', '99991231')


--Dalarna (18)	Hero Tolk AB	1%	Semantix Tolkjouren AB	7%	Linguacom AB	8%	Digital Interpretations Scandianvia AB	12%
INSERT Rankings (BrokerId, RegionId, BrokerFee, Rank, FirstValidDate, LastValidDate)
VALUES 
(1, 18, 0.01, 1, '20190101', '99991231'),
(3, 18, 0.07, 2, '20190101', '99991231'),
(5, 18, 0.08, 3, '20190101', '99991231'),
(4, 18, 0.12, 4, '20190101', '99991231')

--Gotland (80)	Hero Tolk AB	1%	Semantix Tolkjouren AB	7%	Linguacom AB	7%	Digital Interpretations Scandianvia AB	12%
INSERT Rankings (BrokerId, RegionId, BrokerFee, Rank, FirstValidDate, LastValidDate)
VALUES 
(1, 80, 0.01, 1, '20190101', '99991231'),
(3, 80, 0.07, 2, '20190101', '99991231'),
(5, 80, 0.07, 3, '20190101', '99991231'),
(4, 80, 0.12, 4, '20190101', '99991231')

--Gävleborg	(19) Hero Tolk AB	1%	Semantix Tolkjouren AB	7%	Digital Interpretations Scandianvia AB	7%	Linguacom AB	8%
INSERT Rankings (BrokerId, RegionId, BrokerFee, Rank, FirstValidDate, LastValidDate)
VALUES 
(1, 19, 0.01, 1, '20190101', '99991231'),
(3, 19, 0.07, 2, '20190101', '99991231'),
(4, 19, 0.07, 3, '20190101', '99991231'),
(5, 19, 0.08, 4, '20190101', '99991231')

--Halland (11) Hero Tolk AB	1%	Semantix Tolkjouren AB	7%	Digital Interpretations Scandianvia AB	7%	Språkservice Sverige AB	9%
INSERT Rankings (BrokerId, RegionId, BrokerFee, Rank, FirstValidDate, LastValidDate)
VALUES 
(1, 11, 0.01, 1, '20190101', '99991231'),
(3, 11, 0.07, 2, '20190101', '99991231'),
(4, 11, 0.07, 3, '20190101', '99991231'),
(2, 11, 0.09, 4, '20190101', '99991231')

--Jämtland (21)	Hero Tolk AB	1%, Digital Interpretations Scandianvia AB	7%, Semantix Tolkjouren AB	7%, Linguacom AB	17%
INSERT Rankings (BrokerId, RegionId, BrokerFee, Rank, FirstValidDate, LastValidDate)
VALUES 
(1, 21, 0.01, 1, '20190101', '99991231'),
(4, 21, 0.07, 2, '20190101', '99991231'),
(3, 21, 0.07, 3, '20190101', '99991231'),
(5, 21, 0.17, 4, '20190101', '99991231')

--Jönköping (5)	Hero Tolk AB	1%	Digital Interpretations Scandianvia AB	7%	Semantix Tolkjouren AB	7%	Språkservice Sverige AB	9%
INSERT Rankings (BrokerId, RegionId, BrokerFee, Rank, FirstValidDate, LastValidDate)
VALUES 
(1, 5, 0.01, 1, '20190101', '99991231'),
(4, 5, 0.07, 2, '20190101', '99991231'),
(3, 5, 0.07, 3, '20190101', '99991231'),
(2, 5, 0.09, 4, '20190101', '99991231')

--Kalmar (7) Hero Tolk AB	1%	Semantix Tolkjouren AB	7%	Stockholm Tolkförmedling AB	8%	Språkservice Sverige AB	9%
INSERT Rankings (BrokerId, RegionId, BrokerFee, Rank, FirstValidDate, LastValidDate)
VALUES 
(1, 7, 0.01, 1, '20190101', '99991231'),
(3, 7, 0.07, 2, '20190101', '99991231'),
(6, 7, 0.08, 3, '20190101', '99991231'),
(2, 7, 0.09, 4, '20190101', '99991231')

--Kronoberg (6)	Hero Tolk AB	1%	Språkservice Sverige AB	6%	Digital Interpretations Scandianvia AB	7%	Semantix Tolkjouren AB	7%
INSERT Rankings (BrokerId, RegionId, BrokerFee, Rank, FirstValidDate, LastValidDate)
VALUES 
(1, 6, 0.01, 1, '20190101', '99991231'),
(2, 6, 0.06, 2, '20190101', '99991231'),
(4, 6, 0.07, 3, '20190101', '99991231'),
(3, 6, 0.07, 4, '20190101', '99991231')

--Norrbotten (23)	Hero Tolk AB	1%	Linguacom AB	7%	Semantix Tolkjouren AB	7%	Digital Interpretations Scandianvia AB	7%
INSERT Rankings (BrokerId, RegionId, BrokerFee, Rank, FirstValidDate, LastValidDate)
VALUES 
(1, 23, 0.01, 1, '20190101', '99991231'),
(5, 23, 0.07, 2, '20190101', '99991231'),
(3, 23, 0.07, 3, '20190101', '99991231'),
(4, 23, 0.07, 4, '20190101', '99991231')

--Skåne (25)	Språkservice Sverige AB	3%	Hero Tolk AB	1%	Stockholm Tolkförmedling AB	6%	Semantix Tolkjouren AB	7%
INSERT Rankings (BrokerId, RegionId, BrokerFee, Rank, FirstValidDate, LastValidDate)
VALUES 
(2, 25, 0.03, 1, '20190101', '99991231'),
(1, 25, 0.01, 2, '20190101', '99991231'),
(6, 25, 0.06, 3, '20190101', '99991231'),
(3, 25, 0.07, 4, '20190101', '99991231')

--Stockholm (1)	Språkservice Sverige AB	3%	Hero Tolk AB	1%	Linguacom AB	4%	Stockholm Tolkförmedling AB	4%
INSERT Rankings (BrokerId, RegionId, BrokerFee, Rank, FirstValidDate, LastValidDate)
VALUES 
(2, 1, 0.03, 1, '20190101', '99991231'),
(1, 1, 0.01, 2, '20190101', '99991231'),
(5, 1, 0.04, 3, '20190101', '99991231'),
(6, 1, 0.04, 4, '20190101', '99991231')

--Södermanland(3)	Språkservice Sverige AB	3%	Hero Tolk AB	1%	Folkhälsobyrån Tolkförmedling	2%	Digital Interpretations Scandianvia AB	7%
INSERT Rankings (BrokerId, RegionId, BrokerFee, Rank, FirstValidDate, LastValidDate)
VALUES 
(2, 3, 0.03, 1, '20190101', '99991231'),
(1, 3, 0.01, 2, '20190101', '99991231'),
(7, 3, 0.02, 3, '20190101', '99991231'),
(4, 3, 0.07, 4, '20190101', '99991231')

--Uppsala (2)	Språkservice Sverige AB	3%	Hero Tolk AB	1%	Järva Tolk och Översättning AB	5%	Semantix Tolkjouren AB	7%
INSERT Rankings (BrokerId, RegionId, BrokerFee, Rank, FirstValidDate, LastValidDate)
VALUES 
(2, 2, 0.03, 1, '20190101', '99991231'),
(1, 2, 0.01, 2, '20190101', '99991231'),
(8, 2, 0.05, 3, '20190101', '99991231'),
(3, 2, 0.07, 4, '20190101', '99991231')

--Värmland(15)	Hero Tolk AB	1%	Semantix Tolkjouren AB	7%	Linguacom AB	7%	Digital Interpretations Scandianvia AB	14%
INSERT Rankings (BrokerId, RegionId, BrokerFee, Rank, FirstValidDate, LastValidDate)
VALUES 
(1, 15, 0.01, 1, '20190101', '99991231'),
(3, 15, 0.07, 2, '20190101', '99991231'),
(5, 15, 0.07, 3, '20190101', '99991231'),
(4, 15, 0.14, 4, '20190101', '99991231')

--Västerbotten (22)	Hero Tolk AB	1%	Semantix Tolkjouren AB	7%	Digital Interpretations Scandianvia AB	7%	Stockholm Tolkförmedling AB	9%
INSERT Rankings (BrokerId, RegionId, BrokerFee, Rank, FirstValidDate, LastValidDate)
VALUES 
(1, 22, 0.01, 1, '20190101', '99991231'),
(3, 22, 0.07, 2, '20190101', '99991231'),
(4, 22, 0.07, 3, '20190101', '99991231'),
(6, 22, 0.09, 4, '20190101', '99991231')

--Västernorrland (20)	Hero Tolk AB	1%	Semantix Tolkjouren AB	7%	Linguacom AB	8%	Stockholm Tolkförmedling AB	8%
INSERT Rankings (BrokerId, RegionId, BrokerFee, Rank, FirstValidDate, LastValidDate)
VALUES 
(1, 20, 0.01, 1, '20190101', '99991231'),
(3, 20, 0.07, 2, '20190101', '99991231'),
(5, 20, 0.08, 3, '20190101', '99991231'),
(6, 20, 0.08, 4, '20190101', '99991231')

--Västmanland (17)	Hero Tolk AB	1%	Linguacom AB	4%	Folkhälsobyrån Tolkförmedling	2%	Semantix Tolkjouren AB	7%
INSERT Rankings (BrokerId, RegionId, BrokerFee, Rank, FirstValidDate, LastValidDate)
VALUES 
(1, 17, 0.01, 1, '20190101', '99991231'),
(5, 17, 0.04, 2, '20190101', '99991231'),
(7, 17, 0.02, 3, '20190101', '99991231'),
(3, 17, 0.07, 4, '20190101', '99991231')

--Västra Götaland (13)	Språkservice Sverige AB	3%	Hero Tolk AB	1%	Järva Tolk och Översättning AB	5%	Semantix Tolkjouren AB	7%
INSERT Rankings (BrokerId, RegionId, BrokerFee, Rank, FirstValidDate, LastValidDate)
VALUES 
(2, 13, 0.03, 1, '20190101', '99991231'),
(1, 13, 0.01, 2, '20190101', '99991231'),
(8, 13, 0.05, 3, '20190101', '99991231'),
(3, 13, 0.07, 4, '20190101', '99991231')

--Örebro (16)	Hero Tolk AB	1%	Linguacom AB	4%	Semantix Tolkjouren AB	7%	Språkservice Sverige AB	9%
INSERT Rankings (BrokerId, RegionId, BrokerFee, Rank, FirstValidDate, LastValidDate)
VALUES 
(1, 16, 0.01, 1, '20190101', '99991231'),
(5, 16, 0.04, 2, '20190101', '99991231'),
(3, 16, 0.07, 3, '20190101', '99991231'),
(2, 16, 0.09, 4, '20190101', '99991231')

--Östergötland (4)	Hero Tolk AB	1%	Språkservice Sverige AB	6%	Semantix Tolkjouren AB	7%	Stockholm Tolkförmedling AB	8%
INSERT Rankings (BrokerId, RegionId, BrokerFee, Rank, FirstValidDate, LastValidDate)
VALUES 
(1, 4, 0.01, 1, '20190101', '99991231'),
(2, 4, 0.06, 2, '20190101', '99991231'),
(3, 4, 0.07, 3, '20190101', '99991231'),
(6, 4, 0.08, 4, '20190101', '99991231')


--to verify 
SELECT reg.Name 'Region', r.Rank 'Rang', b.Name 'Förmedling',  r.BrokerFee * 100 AS 'Förmedlingsavgift (%)'
--, * 
FROM Brokers b
JOIN Rankings r ON b.BrokerId = r.BrokerId
JOIN Regions reg ON reg.RegionId = r.RegionId
ORDER BY reg.Name, r.Rank

--lägg in skyddad tolk för varje förmedling (OBS! detta är ej gjort i Tolkprod ännu)
  BEGIN TRAN
  
  INSERT INTO [dbo].[InterpreterBrokers]
  (BrokerId, Email, FirstName, LastName, OfficialInterpreterId)  
  SELECT BrokerId, 'tolk@'+EmailDomain, 'Tolk', 'Skyddad Identitet', 1 FROM [dbo].[Brokers];

  COMMIT TRAN;