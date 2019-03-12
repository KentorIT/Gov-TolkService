

--set AllowExceedingTravelCost = 1 (ShouldBeApproved) for requests having RequestPriceRows with PriceRowType = 5 (travel costs)
BEGIN TRAN

SELECT DISTINCT r.OrderId FROM Requests r
JOIN Orders o ON r.OrderId = o.OrderId
JOIN RequestPriceRows rpr ON r.RequestId = rpr.RequestId
WHERE rpr.PriceRowType = 5  

UPDATE Orders SET AllowExceedingTravelCost = 1 WHERE OrderId IN (
SELECT r.OrderId FROM Requests r
JOIN Orders o ON r.OrderId = o.OrderId
JOIN RequestPriceRows rpr ON r.RequestId = rpr.RequestId
WHERE rpr.PriceRowType = 5)

ROLLBACK TRAN 