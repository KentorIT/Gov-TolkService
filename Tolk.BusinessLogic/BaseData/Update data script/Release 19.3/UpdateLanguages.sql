
--kolla vilka vi har
SELECT * FROM Languages l
WHERE L.Name LIKE '%kines%' OR L.Name LIKE '%kanto%' OR L.Name LIKE '%mand%'

--kolla om finns order (verkade funka i dev)
SELECT * FROM Orders o WHERE o.LanguageId IN (
SELECT l.LanguageId FROM Languages l
WHERE L.Name LIKE '%kines%' OR L.Name LIKE '%kanto%' OR L.Name LIKE '%mand%')

--uppdatera språk OBS! Också lägga till zho i Tellus:UnusedIsoCodes
BEGIN TRAN

UPDATE Languages SET ACTIVE = 0 WHERE LanguageId = 59 AND Name = 'Kinesiska'

UPDATE Languages SET NAME = 'Rikskinesiska (mandarin)' WHERE LanguageId = 96 AND Name = 'Rikskinesiska'

ROLLBACK TRAN