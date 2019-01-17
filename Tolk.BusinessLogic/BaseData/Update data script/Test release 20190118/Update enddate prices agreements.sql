

BEGIN TRAN 

SELECT * FROM PriceCalculationCharges WHERE EndDate > GETDATE()
SELECT * FROM PriceListRows WHERE EndDate > GETDATE()
SELECT * FROM Rankings WHERE LastValidDate > GETDATE()

UPDATE PriceCalculationCharges SET EndDate = '99991231' WHERE EndDate > GETDATE()

UPDATE PriceListRows SET EndDate = '99991231' WHERE EndDate > GETDATE()

UPDATE Rankings SET LastValidDate = '99991231' WHERE LastValidDate > GETDATE()

COMMIT TRAN 