
--Run for version 1.6.1

--Kolla rekvisitioner som har travelcost, alla har status 4 (autogenererade)
--i prod requestid 1148, 1350, orderId 638, 895
SELECT r.Status, * FROM 
Requisitions r JOIN RequisitionPriceRows rpr 
	ON r.RequisitionId = rpr.RequisitionId
WHERE rpr.PriceRowType = 5

--uppdatera dessa till utlägg så är vi konsekventa (PriceRowType 7)
BEGIN TRAN 

UPDATE RequisitionPriceRows SET PriceRowType = 7 WHERE 
PriceRowType = 5

ROLLBACK TRAN