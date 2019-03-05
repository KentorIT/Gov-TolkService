


SELECT * FROM Orders o WHERE o.Status = 7

--update orders with status 7 (DeliveryAccepted) to status 5 (Delivered) 
BEGIN TRAN

UPDATE Orders SET STATUS = 5 WHERE Status = 7

ROLLBACK TRAN