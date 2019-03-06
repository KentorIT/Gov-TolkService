

--script to convert old per diem and car compensation to the new columns, delete the RequisitionPriceRows with these PriceRowTypes
--includes ROLLBACK

BEGIN TRAN

SELECT * FROM RequisitionPriceRows rpr WHERE rpr.PriceRowType IN (8)

UPDATE Requisitions SET PerDiem = 'Ska ha ersättning: ' + CAST(rpr.Price AS VARCHAR) + ' SEK'
FROM Requisitions r JOIN RequisitionPriceRows rpr ON r.RequisitionId = rpr.RequisitionId
 WHERE rpr.PriceRowType IN (8)

SELECT * FROM Requisitions r WHERE r.PerDiem <> ''

SELECT * FROM RequisitionPriceRows rpr WHERE rpr.PriceRowType IN (9)

UPDATE Requisitions SET CarCompensation = 12
FROM Requisitions r JOIN RequisitionPriceRows rpr ON r.RequisitionId = rpr.RequisitionId
 WHERE rpr.PriceRowType IN (9)

SELECT * FROM Requisitions r WHERE r.CarCompensation > 0

DELETE FROM RequisitionPriceRows  WHERE PriceRowType IN (8, 9)

ROLLBACK TRAN



