
--kolla vilka vi har
SELECT * FROM Languages l
WHERE L.Name LIKE '%kines%' OR L.Name LIKE '%kanto%' OR L.Name LIKE '%mand%'

--kolla om finns order (verkade funka i dev)
SELECT * FROM Orders o WHERE o.LanguageId IN (
SELECT l.LanguageId FROM Languages l
WHERE L.Name LIKE '%kines%' OR L.Name LIKE '%kanto%' OR L.Name LIKE '%mand%')

-- Innehåller ROLLBACK! Uppdatera språk OBS! Också lägga till zho i Tellus:UnusedIsoCodes
BEGIN TRAN

UPDATE Languages SET ACTIVE = 0 WHERE LanguageId = 59 AND Name = 'Kinesiska'

UPDATE Languages SET NAME = 'Rikskinesiska (mandarin)' WHERE LanguageId = 96 AND Name = 'Rikskinesiska'

ROLLBACK TRAN

--uppdatera med BKS, Innehåller ROLLBACK!
BEGIN TRAN

SELECT * FROM Languages l WHERE L.TellusName='bos,hrv,srp'

UPDATE Languages SET NAME = 'Bosniska (BKS)' WHERE TellusName='bos,hrv,srp' AND Name = 'Bosniska'
UPDATE Languages SET NAME = 'Serbiska (BKS)' WHERE TellusName='bos,hrv,srp' AND Name = 'Serbiska'
UPDATE Languages SET NAME = 'Kroatiska (BKS)' WHERE TellusName='bos,hrv,srp' AND Name = 'Kroatiska'

SELECT * FROM Languages l WHERE L.TellusName='bos,hrv,srp'

ROLLBACK TRAN