
--OBS innehåller ROLLBACK
BEGIN TRAN

UPDATE Requests SET CompletedNotificationIsHandled = 1 
WHERE RequestId IN (SELECT r.RequestId FROM Requests r 
JOIN Orders o ON o.OrderId = r.OrderId
WHERE o.Status IN (4,5,7) AND r.Status IN (5, 6) AND o.EndAt < GETDATE())

ROLLBACK TRAN 