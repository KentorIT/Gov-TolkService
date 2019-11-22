
--------------------- OBS DETTA SKA FÖRMODLIGEN INTE KÖRAS FÖRRÄN 30/11 -----------------------

--finns totalt 65 rankings (alla giltiga)
SELECT r.Name, ra.Rank, b.BrokerId, b.Name, r.RegionId, ra.FirstValidDate, ra.LastValidDate
FROM Rankings ra
JOIN Brokers b ON ra.BrokerId = b.BrokerId
JOIN Regions r ON r.RegionId = ra.RegionId
WHERE ra.LastValidDate > GETDATE()
ORDER BY r.RegionId, ra.Rank


--Hero är brokerId = 1
--de är etta i alla regioner utom 5 st där de ligger 2:a 
SELECT * FROM Rankings WHERE BrokerId = 1

--det betyder att vi borde kunna ha kvar de ranknings där någon annan är nummer 1 
--alla andra ska vi sätta slutdatum på, Hero som ska avslutas (Hero) och de andra som ska hamna ett steg högre upp i ranking (= måste göra ny rad för att behålla historik)
--ska vara 60 som avslutas (65 minus de fem ettorna som inte är Hero

--de nya som skapas borde vara de 60 som avslutats minus de 21 från Hero = 39 st

--Frågan är hur vi vill göra med datum, i varje fall ska det skilja en dag mellan gamla slut och nya start
--Först sa hon 30/11 sedan sa hon att Hero tas bort i samband med release 29/11 men att det gäller från 1/12?
--om man antar att ingen beställning kommer på helgen så kan man köra fredag kväll eller under lördag, men polisen la en beställning på en lördag för ett tag sen
--sedan vet man inte om användarna väntar sig att det kan komma beställningar till Hero under fredag eller inte (de vet ej när vi gör release)


-- de är tvåa i fem regioner, så alla utom fem ska uppdateras med LastValidDate
--dessa ska ej uppdateras 
SELECT * FROM Rankings r WHERE r.Rank = 1 AND r.BrokerId <> 1

-- Vi ska sätta en dag innan vi sätter den nya giltighetstiden på nya raderna,
-- 65-5 stycken = 60 st
SELECT * FROM Rankings WHERE LastValidDate = '99991231' AND RankingId NOT IN (
SELECT r.RankingId FROM Rankings r WHERE r.Rank = 1 AND r.BrokerId <> 1)


------- PÅBÖRJA UPPDATERING FÖRST SÄTT SLUTDATUM PÅ GAMLA RADERNA ---------

BEGIN TRAN

DECLARE @lastValidDate DATE 

SET @lastValidDate = CONVERT(DATE, DATEADD(DAY, -1, GETDATE()))--OBS KOLLA HÄR MED DATUM! 

--högsta RankingId innan = 65 (i fall vi vill börja om)

--Uppdatera slutdatum först på alla utom där någon annan är etta (5 st) = 65-5 = 60 st
--Vi ska sätta en dag innan vi sätter den nya giltighetstiden på nya raderna
UPDATE Rankings SET LastValidDate = @lastValidDate WHERE LastValidDate = '99991231' AND RankingId NOT IN (
SELECT r.RankingId FROM Rankings r WHERE r.Rank = 1 AND r.BrokerId <> 1)

SELECT * FROM Rankings r WHERE r.LastValidDate = @lastValidDate 
SELECT * FROM Rankings r 

ROLLBACK TRAN



------- SEDAN LÄGGA IN NYA RADERNA ---------

DECLARE @newFirstValidDate DATE 

SET @newFirstValidDate = CONVERT(DATE, GETDATE())--OBS KOLLA HÄR MED DATUM! 

--lägg in för alla utom broker 1 och där de andra är etta (ta samma info men uppdatera rank dra ifrån ett = -1)
--de nya som skapas ska bli de 60 som avslutats minus de 21 från Hero = 39 st
INSERT INTO Rankings (Rank, FirstValidDate, LastValidDate, BrokerFee, BrokerId, RegionId)
SELECT (r.Rank-1), @newFirstValidDate, '99991231', r.BrokerFee, r.BrokerId, r.RegionId
FROM Rankings r WHERE r.BrokerId <> 1 AND r.Rank > 1 AND r.LastValidDate = CONVERT(DATE, DATEADD(DAY, -1, @newFirstValidDate))

--kolla på alla aktiva rankings (39 +5 = 44 st, dessa kan klippas in i Excel för att se att de stämmer:
--Kammarkollegiets avropstjänst för tolkar, applikationsförvaltning - General\Ärenden\Avsluta Hero Tolk AB\Avsluta Hero Tolk.xlsx (blad Rang efter)
SELECT r.Name, ra.Rank, b.BrokerId, b.Name, r.RegionId, ra.FirstValidDate, ra.LastValidDate
FROM Rankings ra
JOIN Brokers b ON ra.BrokerId = b.BrokerId
JOIN Regions r ON r.RegionId = ra.RegionId
WHERE ra.LastValidDate > GETDATE()
ORDER BY r.RegionId, ra.Rank

--brokerid 1 ska inte finnas med bland aktiva
SELECT r.Name, ra.Rank, b.BrokerId, b.Name, r.RegionId, ra.FirstValidDate, ra.LastValidDate
FROM Rankings ra
JOIN Brokers b ON ra.BrokerId = b.BrokerId
JOIN Regions r ON r.RegionId = ra.RegionId
WHERE ra.LastValidDate > GETDATE() AND ra.BrokerId = 1





