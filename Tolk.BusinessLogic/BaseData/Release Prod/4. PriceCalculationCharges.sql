
DELETE FROM PriceCalculationCharges
DBCC CHECKIDENT ('PriceCalculationCharges', RESEED, 0)

INSERT INTO PriceCalculationCharges (StartDate, EndDate, ChargePercentage, ChargeTypeId)
	VALUES 
	('20190101', '99991231', 31.42, 1),
	('20190101', '99991231', 0.7, 2)
	