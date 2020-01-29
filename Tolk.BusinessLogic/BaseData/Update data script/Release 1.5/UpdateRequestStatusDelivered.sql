--de med requeststatus 10, requisition status 4 (avbokning) ska ej sättas till levererade
SELECT * FROM Requests r
JOIN Requisitions rn ON r.RequestId = rn.RequestId
ORDER BY r.Status

--x st
SELECT DISTINCT r.* FROM Requests r
JOIN Requisitions rn ON r.RequestId = rn.RequestId
WHERE r.Status <> 10

--x st
SELECT DISTINCT r.* FROM Requests r
JOIN Requisitions rn ON r.RequestId = rn.RequestId
WHERE rn.Status <> 4

--OBS! ROLLBACK sätt status 6 = delivered, x st
BEGIN TRAN 

UPDATE Requests SET STATUS = 6 WHERE RequestId IN 
(SELECT rn.RequestId FROM Requisitions rn WHERE rn.Status <> 4)
AND Status <> 10

ROLLBACK TRAN 