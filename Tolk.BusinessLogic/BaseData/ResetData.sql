--Reset data in dev/test-environments

--ROLLBACK INCLUDED!

--DELETE all orders, requests etc

USE TolkDev --change if running in test

BEGIN TRAN

DELETE FROM OrderInterpreterLocation
DELETE FROM OrderPriceRows
DELETE FROM OrderRequirementRequestAnswer
DELETE FROM RequestPriceRows
DELETE FROM RequisitionPriceRows
DELETE FROM Orders
DELETE FROM Requests
DELETE FROM OrderRequirements
DELETE FROM Requisitions
DELETE FROM Complaints
DELETE FROM OrderCompetenceRequirements
DELETE FROM OrderAttachments
DELETE FROM RequestAttachments
DELETE FROM RequisitionAttachments
DELETE FROM OrderContactPersonHistory 
DELETE FROM Attachments
DELETE FROM TemporaryAttachmentGroups

TRUNCATE TABLE OrderInterpreterLocation
TRUNCATE TABLE OrderPriceRows
TRUNCATE TABLE OrderRequirementRequestAnswer
TRUNCATE TABLE RequestPriceRows
TRUNCATE TABLE RequisitionPriceRows

DBCC CHECKIDENT('Orders', RESEED, 0)
DBCC CHECKIDENT('Requests', RESEED, 0)
DBCC CHECKIDENT('OrderRequirements', RESEED, 0)
DBCC CHECKIDENT('Requisitions', RESEED, 0)
DBCC CHECKIDENT('Complaints', RESEED, 0)

ROLLBACK TRAN