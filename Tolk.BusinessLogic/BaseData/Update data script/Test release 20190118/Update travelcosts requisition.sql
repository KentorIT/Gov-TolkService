
BEGIN TRAN

UPDATE RequisitionPriceRows SET PriceRowType = 7 WHERE PriceRowType = 5

COMMIT TRAN