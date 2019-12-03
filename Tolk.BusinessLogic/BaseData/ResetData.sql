--Reset data in dev/test-environments

--ROLLBACK INCLUDED!

--DELETE all orders, requests etc

USE TolkDev --change if running in test

BEGIN TRAN

DELETE FROM Requests
DELETE FROM Orders
DELETE FROM OrderGroups
DELETE FROM OrderInterpreterLocation
DELETE FROM OrderPriceRows
DELETE FROM RequestPriceRows
DELETE FROM RequisitionPriceRows
DELETE FROM RequestGroups
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

delete from OutboundEmails
delete from OutboundWebHookCalls

DELETE FROM FAQ 

TRUNCATE TABLE OrderInterpreterLocation
TRUNCATE TABLE OrderPriceRows
TRUNCATE TABLE OrderRequirementRequestAnswer
TRUNCATE TABLE RequestPriceRows
TRUNCATE TABLE RequisitionPriceRows
TRUNCATE TABLE SystemMessages 

DBCC CHECKIDENT('Orders', RESEED, 0)
DBCC CHECKIDENT('Requests', RESEED, 0)
DBCC CHECKIDENT('OrderRequirements', RESEED, 0)
DBCC CHECKIDENT('Requisitions', RESEED, 0)
DBCC CHECKIDENT('Complaints', RESEED, 0)
DBCC CHECKIDENT('OrderGroups', RESEED, 0)
DBCC CHECKIDENT('RequestGroups', RESEED, 0)
DBCC CHECKIDENT('FAQ', RESEED, 0)
DBCC CHECKIDENT('OutboundEmails', RESEED, 0)
DBCC CHECKIDENT('OutboundWebhookCalls', RESEED, 0)

Commit TRAN

Select * FROM Attachments
Select * FROM Complaints
Select * From FailedWebHookCalls
Select * From MealBreaks
Select * FROM OrderAttachments
Select * FROM OrderCompetenceRequirements
Select * FROM OrderContactPersonHistory 
Select * FROM OrderGroups
Select * FROM OrderInterpreterLocation
Select * FROM OrderPriceRows
Select * FROM OrderRequirementRequestAnswer
Select * FROM OrderRequirements
Select * FROM Orders
Select * FROM OrderStatusConfirmation
Select * from OutboundEmails
Select * from OutboundWebHookCalls
Select * FROM RequestAttachments
Select * from RequestGroups
Select * FROM RequestPriceRows
Select * FROM Requests
Select * FROM RequestStatusConfirmation
Select * from RequestViews
Select * FROM RequisitionAttachments
Select * FROM RequisitionPriceRows
Select * FROM Requisitions
Select * FROM TemporaryAttachmentGroups


Select * from SystemMessages
Select * from Faq
Select * from FaqDisplayUserRole
