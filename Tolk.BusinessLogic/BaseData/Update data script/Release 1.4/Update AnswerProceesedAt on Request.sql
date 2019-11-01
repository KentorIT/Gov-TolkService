


--kolla om det finns några requests med lika datum idag
SELECT r.AnswerDate,  r.AnswerProcessedAt,* FROM Requests r 
WHERE r.AnswerProcessedAt = r.AnswerDate

--uppdatera alla requests med AnswerProcessedAt för de som har blivit automatiskt godkända (även tolkbyten)

--kolla på dem vi ska uppdatera 
--alla med 3 (avbokad myndighet) de kan varit autogodkända om det är ersättningsuppdrag (tar de för sig)
-- 5, 10, 13, 14
SELECT * FROM Requests  WHERE Status NOT IN 
(
3, --tar dessa för sig
4, --tillsatt (ej godkänd)
7, --Bokningsförfrågan avböjd av förmedling
8, --Tillsättning är avböjd
9, --Bokningsförfrågan ej besvarad
12, --Bekräftelse är skickad - Ny tolk ej godkänd
16, --Tillsättning ej besvarad
17, --Inväntar sista svarstid från myndighet
18, --Ingen sista svarstid från myndighet
19 --Förlorad på grund av karantän
)
AND AnswerProcessedAt IS NULL --ska ej ha något datum
AND AnswerDate IS NOT NULL --måste vara besvarad
ORDER BY Status

--uppdatera dessa
BEGIN TRAN

UPDATE Requests SET AnswerProcessedAt = AnswerDate WHERE Status NOT IN 
(
3, --tar dessa för sig
4, --tillsatt (ej godkänd)
7, --Bokningsförfrågan avböjd av förmedling
8, --Tillsättning är avböjd
9, --Bokningsförfrågan ej besvarad
12, --Bekräftelse är skickad - Ny tolk ej godkänd
16, --Tillsättning ej besvarad
17, --Inväntar sista svarstid från myndighet
18, --Ingen sista svarstid från myndighet
19 --Förlorad på grund av karantän
)
AND AnswerProcessedAt IS NULL --ska ej ha något datum
AND AnswerDate IS NOT NULL --måste vara besvarad

ROLLBACK TRAN 

--ta de som fått erstättningsuppdrag
SELECT * FROM Requests  WHERE Status  IN 
(
3 --uppdrag avbokat ej godkänt
)
AND AnswerProcessedAt IS NULL --ska ej ha något datum
AND AnswerDate IS NOT NULL --måste vara besvarad
AND OrderId IN (SELECT o.ReplacingOrderId FROM Orders o)
ORDER BY Status


BEGIN TRAN

UPDATE Requests SET AnswerProcessedAt = AnswerDate WHERE 
 Status  IN 
(
3 --uppdrag avbokat ej godkänt
)
AND AnswerProcessedAt IS NULL --ska ej ha något datum
AND AnswerDate IS NOT NULL --måste vara besvarad
AND OrderId IN (SELECT o.ReplacingOrderId FROM Orders o)

ROLLBACK TRAN 


--vi måste även uppdatera de "felaktiga" tolkbytena som fått kopierad tid, ex 6261

SELECT * FROM Requests WHERE Status NOT IN 
(4, --tillsatt (ej godkänd)
8, --Tillsättning är avböjd
9, --Bokningsförfrågan ej besvarad
12, --Bekräftelse är skickad - Ny tolk ej godkänd
16, --Tillsättning ej besvarad
17, --Inväntar sista svarstid från myndighet
18, --Ingen sista svarstid från myndighet
19 --Förlorad på grund av karantän
)
AND ReplacingRequestId IS NOT NULL --ska vara ett tolkutbyte som 
AND AnswerDate IS NOT NULL --måste vara besvarad
AND AnswerDate > AnswerProcessedAt

BEGIN TRAN

UPDATE Requests SET AnswerProcessedAt = AnswerDate,  AnswerProcessedBy = NULL, 
ImpersonatingAnswerProcessedBy = NULL WHERE Status NOT IN 
(4, --tillsatt (ej godkänd)
8, --Tillsättning är avböjd
9, --Bokningsförfrågan ej besvarad
12, --Bekräftelse är skickad - Ny tolk ej godkänd
16, --Tillsättning ej besvarad
17, --Inväntar sista svarstid från myndighet
18, --Ingen sista svarstid från myndighet
19 --Förlorad på grund av karantän
)
AND ReplacingRequestId IS NOT NULL --ska vara ett tolkutbyte som 
AND AnswerDate IS NOT NULL --måste vara besvarad
AND AnswerDate > AnswerProcessedAt

ROLLBACK TRAN 

SELECT * FROM requests 
WHERE OrderId = 4161


SELECT * FROM Requests WHERE Status NOT IN 
(4, --tillsatt (ej godkänd)
8, --Tillsättning är avböjd
9, --Bokningsförfrågan ej besvarad
12, --Bekräftelse är skickad - Ny tolk ej godkänd
16, --Tillsättning ej besvarad
17, --Inväntar sista svarstid från myndighet
18, --Ingen sista svarstid från myndighet
19 --Förlorad på grund av karantän
)
AND ReplacingRequestId IS NOT NULL --ska vara ett tolkutbyte som 
AND AnswerDate IS NOT NULL --måste vara besvarad
AND AnswerDate = AnswerProcessedAt
AND AnswerProcessedBy IS NOT NULL

BEGIN TRAN 

UPDATE Requests SET AnswerProcessedBy = NULL, ImpersonatingAnswerProcessedBy = NULL WHERE Status NOT IN 
(4, --tillsatt (ej godkänd)
8, --Tillsättning är avböjd
9, --Bokningsförfrågan ej besvarad
12, --Bekräftelse är skickad - Ny tolk ej godkänd
16, --Tillsättning ej besvarad
17, --Inväntar sista svarstid från myndighet
18, --Ingen sista svarstid från myndighet
19 --Förlorad på grund av karantän
)
AND ReplacingRequestId IS NOT NULL --ska vara ett tolkutbyte som 
AND AnswerDate IS NOT NULL --måste vara besvarad
AND AnswerDate = AnswerProcessedAt
AND AnswerProcessedBy IS NOT NULL

ROLLBACK TRAN