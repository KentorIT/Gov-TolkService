


--om vi inte vill göra ConfirmedBy nullable (inte gjort vid denna incheckning)
--så kan man sätta att den som tillsatt också bekräftat att det inte blev något (finns ingen än så länge i prod. 24 jan)
SELECT * FROM Requests r WHERE r.Status = 16

--lägg in med samma person som svarat
BEGIN TRAN 

INSERT INTO RequestStatusConfirmation (RequestId, RequestStatus, ConfirmedAt, ConfirmedBy, ImpersonatingConfirmedBy)
	SELECT r.RequestId, 16, o.StartAt, r.AnsweredBy, r.ImpersonatingAnsweredBy
	FROM Requests r
	JOIN Orders o ON r.OrderId = o.OrderId
	 WHERE r.Status = 16 AND r.RequestId NOT IN (SELECT RequestId FROM 
	 RequestStatusConfirmation WHERE requestStatus = 16)

ROLLBACK TRAN



--annars måste göra den nullable för att "systemet" ska kunna bekräfta om ej ta API-user

SELECT * FROM Requests r WHERE r.Status = 16

INSERT INTO RequestStatusConfirmation (RequestId, RequestStatus, ConfirmedAt, ConfirmedBy, ImpersonatingConfirmedBy)
	SELECT r.RequestId, 16, o.StartAt, NULL, NULL
	FROM Requests r
	JOIN Orders o ON r.OrderId = o.OrderId
	 WHERE r.Status = 16