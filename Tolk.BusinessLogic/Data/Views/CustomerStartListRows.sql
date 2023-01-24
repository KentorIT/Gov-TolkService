ALTER VIEW [dbo].[CustomerStartListRows]
AS

--NOTE! When ALTER VIEW - also change file \GOV Tolk\Tolk.BusinessLogic\Data\Views\CustomerStartListRows.sql

--Orders
SELECT
DISTINCT
	1 'RowType'
	,o.OrderId 
   ,COALESCE(l.Name, o.OtherLanguage) 'LanguageName'
   ,o.OrderNumber 
   ,og.OrderGroupNumber
   ,o.StartAt
   ,o.EndAt
   ,o.Status 'OrderStatus'
   ,og.Status 'OrderGroupStatus'
   ,o.CreatedBy
   ,o.CustomerUnitId
   ,o.CreatedAt 'EntityDate'
   ,o.CustomerOrganisationId
   ,CONVERT(BIT, COALESCE(cu.IsActive, 0)) 'CustomerUnitIsActive'
   ,o.OrderGroupId 
   ,o.ContactPersonId
   ,o.ReplacingOrderId
   ,r.CompetenceLevel
   ,NULL 'ExtraCompetencelevel'
   ,r.LatestAnswerTimeForCustomer
   ,r.AnswerDate 'AnsweredAt'
   ,r.CancelledAt
   ,r.AcceptedAt
   ,r.ExpiresAt 'RequestExpiresAt'
   ,r.CreatedAt 'LastRequestCreatedUpdatedAt'
   ,NULL 'NoOfChildren'
   ,NULL 'NoOfExtraInterpreter'
   ,NULL 'RequisitionStatus'
   ,NULL 'ComplaintStatus'
FROM dbo.Orders o
INNER JOIN dbo.CustomerOrganisations c
	ON c.CustomerOrganisationId = o.CustomerOrganisationId
LEFT JOIN dbo.CustomerUnits cu 
	ON o.CustomerUnitId = cu.CustomerUnitId
LEFT JOIN dbo.OrderGroups og
	ON og.OrderGroupId = o.OrderGroupId
LEFT JOIN dbo.Languages l
	ON l.LanguageId = o.LanguageId
INNER JOIN dbo.Requests r
	ON r.OrderId = o.OrderId
		AND r.RequestId IN (SELECT TOP 1
				req.RequestId
			FROM dbo.Requests req
			WHERE req.OrderId = o.OrderId
			ORDER BY req.RequestId DESC)
LEFT JOIN dbo.OrderStatusConfirmation osc
	ON o.OrderId = osc.OrderId
		AND osc.OrderStatus IN (9, 15)
LEFT JOIN dbo.RequestStatusConfirmation rsc
	ON r.RequestId = rsc.RequestId
		AND rsc.RequestStatus = 14 
WHERE (o.Status IN (2, 3, 4, 10, 16, 21) 
OR (o.Status IN (9, 15) AND osc.OrderId IS NULL)
OR (o.Status = 12 AND rsc.RequestId IS NULL))
UNION
--OrderGroups (PartlyAccepted request not implemented for groups)
SELECT
DISTINCT
	2 'RowType'
   ,o.OrderId 
   ,COALESCE(l.Name, og.OtherLanguage) 'LanguageName'
   ,NULL 'OrderNumber'
   ,og.OrderGroupNumber
   ,o.StartAt
   ,o.EndAt
   ,o.Status 'OrderStatus'
   ,og.Status 'OrderGroupStatus'
   ,og.CreatedBy
   ,og.CustomerUnitId
   ,og.CreatedAt 'EntityDate'
   ,og.CustomerOrganisationId
   ,CONVERT(BIT, COALESCE(cu.IsActive, 0)) 'CustomerUnitIsActive'
   ,og.OrderGroupId 'ParentEntityId'
   ,o.ContactPersonId
   ,NULL ReplacingOrderId 
   ,(SELECT TOP 1 r.CompetenceLevel FROM Requests r
   JOIN Orders oreq ON r.OrderId = oreq.OrderId AND OrderGroupId = og.OrderGroupId AND oreq.IsExtraInterpreterForOrderId IS NULL) 'Competencelevel'
   ,(SELECT TOP 1 r.CompetenceLevel FROM Requests r 
   JOIN Orders oreq ON r.OrderId = oreq.OrderId AND OrderGroupId = og.OrderGroupId AND oreq.IsExtraInterpreterForOrderId IS NOT NULL) 'ExtraCompetencelevel'
   ,rg.LatestAnswerTimeForCustomer
   ,rg.AnswerDate 'AnsweredAt'
   ,rg.CancelledAt
   ,rg.AcceptedAt
   ,rg.ExpiresAt 'RequestExpiresAt'
   ,rg.CreatedAt 'LastRequestCreatedUpdatedAt'
   ,(SELECT COUNT(OrderId) FROM Orders WHERE OrderGroupId = og.OrderGroupId) 'NoOfChildren'
   ,(SELECT COUNT(OrderId) FROM Orders WHERE OrderGroupId = og.OrderGroupId AND IsExtraInterpreterForOrderId IS NOT NULL) 'NoOfExtraInterpreter'
   ,NULL 'RequisitionStatus'
   ,NULL 'ComplaintStatus'
FROM dbo.OrderGroups og 
INNER JOIN dbo.CustomerOrganisations c
	ON c.CustomerOrganisationId = og.CustomerOrganisationId
LEFT JOIN dbo.CustomerUnits cu 
	ON og.CustomerUnitId = cu.CustomerUnitId
LEFT JOIN dbo.Languages l
	ON l.LanguageId = og.LanguageId
INNER JOIN dbo.RequestGroups rg
	ON og.OrderGroupId = rg.OrderGroupId
		AND rg.RequestGroupId IN (SELECT TOP 1
				reqG.RequestGroupId
			FROM RequestGroups reqG 
			WHERE reqG.OrderGroupId = og.OrderGroupId
			ORDER BY reqG.RequestGroupId DESC)
LEFT JOIN dbo.OrderGroupStatusConfirmations ogsc
	ON og.OrderGroupId = ogsc.OrderGroupId
		AND ogsc.OrderStatus IN (9, 15)
	INNER JOIN Orders o ON o.OrderGroupId = og.OrderGroupId AND o.OrderId IN 
	(SELECT TOP 1 ord.OrderId FROM dbo.Orders ord WHERE ord.OrderGroupId = og.OrderGroupId ORDER BY ord.StartAt)
WHERE (og.Status IN (2, 3, 4, 10, 16, 21)
OR (og.Status IN (9, 15)
AND ogsc.OrderGroupId IS NULL))
UNION
--Requisitions
SELECT
DISTINCT
	3 'RowType'
   ,o.OrderId
   ,COALESCE(l.Name, o.OtherLanguage) 'LanguageName'
   ,o.OrderNumber
   ,og.OrderGroupNumber
   ,o.StartAt
   ,o.EndAt
   ,o.Status 'OrderStatus'
   ,og.Status 'OrderGroupStatus'
   ,o.CreatedBy
   ,o.CustomerUnitId
   ,r.CreatedAt 'EntityDate'
   ,o.CustomerOrganisationId
   ,CONVERT(BIT, COALESCE(cu.IsActive, 0)) 'CustomerUnitIsActive'
   ,og.OrderGroupId
   ,o.ContactPersonId
   ,NULL ReplacingOrderId 
   ,rs.CompetenceLevel 'Competencelevel'
   ,NULL 'ExtraCompetencelevel'
   ,NULL 'LatestAnswerTimeForCustomer'
   ,NULL 'AnsweredAt'
   ,NULL 'CancelledAt'
   ,NULL 'AcceptedAt'
   ,NULL 'RequestExpiresAt'
   ,NULL 'LastRequestCreatedUpdatedAt'
   ,NULL 'NoOfChildren'
   ,NULL 'NoOfExtraInterpreter'
   ,r.Status 'RequisitionStatus'
   ,NULL 'ComplaintStatus'
FROM dbo.Requisitions r
INNER JOIN dbo.Requests rs
	ON r.RequestId = rs.RequestId
INNER JOIN dbo.Orders o 
	ON rs.OrderId = o.OrderId
LEFT JOIN dbo.OrderGroups og 
	ON o.OrderGroupId = og.OrderGroupId
JOIN dbo.CustomerOrganisations c
	ON c.CustomerOrganisationId = o.CustomerOrganisationId
LEFT JOIN dbo.CustomerUnits cu 
	ON o.CustomerUnitId = cu.CustomerUnitId
LEFT JOIN dbo.Languages l
	ON l.LanguageId = o.LanguageId
LEFT JOIN dbo.RequisitionStatusConfirmations rsc 
	ON r.RequisitionId = rsc.RequisitionId AND rsc.RequisitionStatus = 1
WHERE r.Status = 1 AND rsc.RequisitionStatusConfirmationId IS NULL
UNION
--Complaints
SELECT
DISTINCT
	4 'RowType'
   ,o.OrderId
   ,COALESCE(l.Name, o.OtherLanguage) 'LanguageName'
   ,o.OrderNumber
   ,og.OrderGroupNumber
   ,o.StartAt
   ,o.EndAt
   ,o.Status 'OrderStatus'
   ,og.Status 'OrderGroupStatus'
   ,o.CreatedBy
   ,o.CustomerUnitId
   ,c.CreatedAt 'EntityDate'
   ,o.CustomerOrganisationId
   ,CONVERT(BIT, COALESCE(cu.IsActive, 0)) 'CustomerUnitIsActive'
   ,og.OrderGroupId
   ,o.ContactPersonId
   ,NULL ReplacingOrderId 
   ,rs.CompetenceLevel 'Competencelevel'
   ,NULL 'ExtraCompetencelevel'
   ,NULL 'LatestAnswerTimeForCustomer'
   ,c.AnsweredAt
   ,NULL 'CancelledAt'
   ,NULL 'AcceptedAt'
   ,NULL 'RequestExpiresAt'
   ,NULL 'LastRequestCreatedUpdatedAt'
   ,0 'NoOfChildren'
   ,0 'NoOfExtraInterpreter'
   ,NULL 'RequisitionStatus'
   ,c.Status 'ComplaintStatus'
FROM dbo.Complaints c
INNER JOIN dbo.Requests rs
	ON c.RequestId = rs.RequestId
INNER JOIN dbo.Orders o 
	ON rs.OrderId = o.OrderId
INNER JOIN dbo.CustomerOrganisations co
	ON co.CustomerOrganisationId = o.CustomerOrganisationId
LEFT JOIN dbo.OrderGroups og 
	ON o.OrderGroupId = og.OrderGroupId
LEFT JOIN dbo.CustomerUnits cu 
	ON o.CustomerUnitId = cu.CustomerUnitId
LEFT JOIN dbo.Languages l
	ON l.LanguageId = o.LanguageId
WHERE c.Status = 3