ALTER VIEW [dbo].[BrokerStartListRows]
AS

--NOTE! When ALTER VIEW - also change file \GOV Tolk\Tolk.BusinessLogic\Data\Views\BrokerStartListRows.sql

--Requests
SELECT
DISTINCT
	5 RowType
   ,r.RequestId 
   ,ra.BrokerId
   ,COALESCE(l.Name, o.OtherLanguage) LanguageName
   ,o.OrderNumber 
   ,og.OrderGroupNumber
   ,o.StartAt
   ,o.EndAt
   ,r.Status RequestStatus
   ,rg.Status RequestGroupStatus
   ,anu.NameFirst + ' ' + anu.NameFamily ViewedBy
   ,anu.Id ViewedByUserId
   ,r.CreatedAt EntityDate
   ,r.RequestGroupId 
   ,co.Name CustomerName
   ,o.ReplacingOrderId
   ,r.CompetenceLevel
   ,NULL ExtraCompetencelevel
   ,ru.UpdatedAt LastRequestCreatedUpdatedAt
   ,r.LatestAnswerTimeForCustomer
   ,r.AnswerDate AnsweredAt
   ,r.AnswerProcessedAt AnswerProcessedAt
   ,r.CancelledAt
   ,r.ExpiresAt RequestExpiresAt
   ,r.AcceptedAt
   ,r.LastAcceptAt
   ,r.RequestAnswerRuleType
   ,o.ExpectedLength
   ,r.RespondedStartAt
   ,NULL NoOfChildren
   ,NULL NoOfExtraInterpreter
   ,NULL RequisitionStatus
   ,NULL ComplaintStatus
   ,ocle.LoggedAt OrderChangedAt
FROM dbo.Requests r
INNER JOIN dbo.Orders o
	ON r.OrderId = o.OrderId
INNER JOIN dbo.Rankings ra 
	ON ra.RankingId = r.RankingId
INNER JOIN dbo.CustomerOrganisations co
	ON o.CustomerOrganisationId = co.CustomerOrganisationId
LEFT JOIN dbo.RequestUpdateLatestAnswerTime ru
	ON r.RequestId = ru.RequestId
LEFT JOIN dbo.RequestGroups rg
	ON rg.RequestGroupId = r.RequestGroupId
LEFT JOIN dbo.OrderGroups og
	ON og.OrderGroupId = o.OrderGroupId
LEFT JOIN dbo.Languages l
	ON l.LanguageId = o.LanguageId
LEFT JOIN dbo.RequestViews rv
	ON r.RequestId = rv.RequestId AND rv.RequestViewId IN 
	(SELECT TOP 1 rv1.RequestViewId FROM RequestViews rv1 WHERE r.RequestId = rv1.RequestId ORDER BY rv1.ViewedAt DESC)
LEFT JOIN dbo.AspNetUsers anu ON rv.ViewedBy = anu.Id
LEFT JOIN dbo.RequestStatusConfirmation rsc
	ON r.RequestId = rsc.RequestId
		AND rsc.RequestStatus IN (8, 10, 16)
LEFT JOIN dbo.OrderChangeLogEntries ocle
	ON o.OrderId = ocle.OrderId AND ocle.BrokerId = ra.BrokerId AND ocle.OrderChangeLogType <> 2
	AND ocle.OrderChangeLogEntryId IN 
	(SELECT TOP 1 oc.OrderChangeLogEntryId FROM dbo.OrderChangeLogEntries oc 
	LEFT JOIN dbo.OrderChangeConfirmations occo ON occo.OrderChangeLogEntryId = oc.OrderChangeLogEntryId
	WHERE oc.BrokerId = ra.BrokerId AND oc.OrderChangeLogType <> 2 AND occo.OrderChangeConfirmationId IS NULL
	ORDER BY oc.LoggedAt DESC)
LEFT JOIN OrderChangeConfirmations occ
	ON ocle.OrderChangeLogEntryId = occ.OrderChangeLogEntryId
WHERE (r.Status IN (1, 2, 23) 
OR (r.Status IN (8, 10, 16) AND rsc.RequestId IS NULL)
OR (r.Status IN (5, 12) AND ocle.OrderChangeLogEntryId IS NOT NULL AND occ.OrderChangeLogEntryId IS NULL)
OR (r.Status = 5 AND o.StartAt < GETDATE() AND r.RequestId NOT IN (SELECT r1.RequestId FROM Requisitions r1))
OR (r.Status IN (4, 12) AND o.StartAt > GETDATE())
OR (r.Status = 5  AND o.StartAt > GETDATE()))
--RequestGroups
UNION
SELECT
DISTINCT
	6 RowType
   ,NULL RequestId
   ,ra.BrokerId
   ,COALESCE(l.Name, o.OtherLanguage) LanguageName
   ,'' OrderNumber
   ,og.OrderGroupNumber
   ,o.StartAt
   ,o.EndAt
   ,NULL RequestStatus
   ,rg.Status RequestGroupStatus
   ,anu.NameFirst + ' ' + anu.NameFamily ViewedBy
   ,anu.Id ViewedByUserId
   ,rg.CreatedAt EntityDate
   ,rg.RequestGroupId 
   ,co.Name CustomerName
   ,NULL ReplacingOrderId 
      ,(SELECT TOP 1 rComp.CompetenceLevel FROM dbo.Requests rComp
   INNER JOIN dbo.Orders oComp ON rComp.OrderId = oComp.OrderId
   WHERE rComp.RequestGroupId = rg.RequestGroupId AND oComp.IsExtraInterpreterForOrderId IS NULL) Competencelevel
   ,(SELECT TOP 1 rXComp.CompetenceLevel FROM dbo.Requests rXComp
   INNER JOIN dbo.Orders oXComp ON rXComp.OrderId = oXComp.OrderId
   WHERE rXComp.RequestGroupId = rg.RequestGroupId AND oXComp.IsExtraInterpreterForOrderId IS NOT NULL) ExtraCompetencelevel
   ,ru.UpdatedAt LastRequestCreatedUpdatedAt
   ,rg.LatestAnswerTimeForCustomer
   ,rg.AnswerDate AnsweredAt
   ,rg.AnswerProcessedAt AnswerProcessedAt
   ,rg.CancelledAt
   ,rg.ExpiresAt RequestExpiresAt
   ,rg.AcceptedAt
   ,rg.LastAcceptAt
   ,rg.RequestAnswerRuleType
   ,NULL ExpectedLength
   ,NULL RespondedStartAt
   ,(SELECT COUNT(OrderId) FROM Orders WHERE OrderGroupId = og.OrderGroupId) NoOfChildren
   ,(SELECT COUNT(OrderId) FROM Orders WHERE OrderGroupId = og.OrderGroupId AND IsExtraInterpreterForOrderId  IS NOT NULL) NoOfExtraInterpreter
   ,NULL RequisitionStatus
   ,NULL ComplaintStatus
   ,NULL OrderChangedAt
FROM dbo.RequestGroups rg
INNER JOIN dbo.OrderGroups og
	ON og.OrderGroupId = rg.OrderGroupId
INNER JOIN dbo.Orders o ON o.OrderGroupId = og.OrderGroupId AND o.OrderId IN 
	(SELECT TOP 1 ord.OrderId FROM dbo.Orders ord WHERE ord.OrderGroupId = og.OrderGroupId ORDER BY ord.StartAt)
INNER JOIN dbo.Requests r
	ON o.OrderId = r.OrderId
INNER JOIN dbo.Rankings ra 
	ON ra.RankingId = rg.RankingId
INNER JOIN dbo.CustomerOrganisations co
	ON og.CustomerOrganisationId = co.CustomerOrganisationId
LEFT JOIN dbo.Languages l
	ON l.LanguageId = og.LanguageId
LEFT JOIN dbo.RequestGroupUpdateLatestAnswerTime ru
	ON rg.RequestGroupId = ru.RequestGroupId
LEFT JOIN dbo.RequestGroupViews rv
	ON rg.RequestGroupId = rv.RequestGroupId AND rv.RequestGroupViewId IN 
	(SELECT TOP 1 rv1.RequestGroupViewId FROM dbo.RequestGroupViews rv1 
	WHERE rg.RequestGroupId = rv1.RequestGroupId ORDER BY rv1.ViewedAt DESC)
LEFT JOIN dbo.AspNetUsers anu ON rv.ViewedBy = anu.Id
LEFT JOIN dbo.RequestGroupStatusConfirmations rsc
	ON rg.RequestGroupId = rsc.RequestGroupId
		AND rsc.RequestStatus IN (8, 10, 16)
WHERE (rg.Status IN (1, 2, 4, 23) 
OR (rg.Status IN (8, 10, 16) AND rsc.RequestGroupId IS NULL))
UNION
--Requisitions
SELECT
DISTINCT
	3 RowType
   ,r.RequestId
    ,ra.BrokerId
   ,COALESCE(l.Name, o.OtherLanguage) LanguageName
   ,o.OrderNumber
   ,og.OrderGroupNumber
   ,o.StartAt
   ,o.EndAt
   ,NULL RequestStatus
   ,NULL RequestGroupStatus
   ,anu.NameFirst + ' ' + anu.NameFamily ViewedBy
   ,anu.Id ViewedByUserId
   ,rs.CreatedAt EntityDate
   ,rs.RequestGroupId RequestGroupId
   ,co.Name CustomerName
   ,NULL ReplacingOrderId 
   ,rs.CompetenceLevel Competencelevel
   ,NULL ExtraCompetencelevel
   ,NULL LastRequestCreatedUpdatedAt
   ,NULL LatestAnswerTimeForCustomer
   ,r.ProcessedAt AnsweredAt
   ,NULL AnswerProcessedAt
   ,NULL CancelledAt
   ,NULL RequestExpiresAt
   ,NULL AcceptedAt
   ,NULL LastAcceptAt
   ,NULL RequestAnswerRuleType
   ,NULL ExpectedLength
   ,NULL RespondedStartAt
   ,0 NoOfChildren
   ,0 NoOfExtraInterpreter
   ,r.Status RequisitionStatus
   ,NULL ComplaintStatus
   ,NULL OrderChangedAt
FROM dbo.Requisitions r
INNER JOIN dbo.Requests rs
	ON r.RequestId = rs.RequestId
INNER JOIN dbo.Rankings ra
	ON ra.RankingId = rs.RankingId
INNER JOIN dbo.Orders o 
	ON rs.OrderId = o.OrderId
INNER JOIN dbo.CustomerOrganisations co 
	ON co.CustomerOrganisationId = o.CustomerOrganisationId
LEFT JOIN dbo.OrderGroups og 
	ON o.OrderGroupId = og.OrderGroupId
JOIN dbo.CustomerOrganisations c
	ON c.CustomerOrganisationId = o.CustomerOrganisationId
LEFT JOIN dbo.Languages l
	ON l.LanguageId = o.LanguageId
LEFT JOIN dbo.RequestViews rv
	ON r.RequestId = rv.RequestId AND rv.RequestViewId IN 
	(SELECT TOP 1 rv1.RequestViewId FROM RequestViews rv1 WHERE r.RequestId = rv1.RequestId ORDER BY rv1.ViewedAt DESC)
LEFT JOIN dbo.AspNetUsers anu
	ON rv.ViewedBy = anu.Id
WHERE r.Status = 3 AND r.ReplacedByRequisitionId IS NULL
UNION
--Complaints
SELECT
DISTINCT
	4 RowType
   ,r.RequestId
   ,ra.BrokerId
   ,COALESCE(l.Name, o.OtherLanguage) LanguageName
   ,o.OrderNumber
   ,og.OrderGroupNumber
   ,o.StartAt
   ,o.EndAt
   ,NULL RequestStatus
   ,NULL RequestGroupStatus
   ,anu.NameFirst + ' ' + anu.NameFamily ViewedBy
   ,anu.Id ViewedByUserId
   ,c.CreatedAt EntityDate
   ,r.RequestGroupId
   ,co.Name CustomerName
   ,NULL ReplacingOrderId 
   ,r.CompetenceLevel Competencelevel
   ,NULL ExtraCompetencelevel
   ,NULL LastRequestCreatedUpdatedAt
   ,NULL LatestAnswerTimeForCustomer
   ,NULL AnsweredAt
   ,NULL AnswerProcessedAt
   ,NULL CancelledAt
   ,NULL AcceptedAt
   ,NULL LastAcceptAt
   ,NULL RequestAnswerRuleType
   ,NULL RequestExpiresAt
   ,NULL ExpectedLength
   ,NULL RespondedStartAt
   ,NULL NoOfChildren
   ,NULL NoOfExtraInterpreter
   ,NULL RequisitionStatus
   ,c.Status ComplaintStatus
   ,NULL OrderChangedAt
FROM dbo.Complaints c
INNER JOIN dbo.Requests r
	ON c.RequestId = r.RequestId
INNER JOIN dbo.Rankings ra
	ON r.RankingId = ra.RankingId
INNER JOIN dbo.Orders o 
	ON r.OrderId = o.OrderId
INNER JOIN dbo.CustomerOrganisations co
	ON co.CustomerOrganisationId = o.CustomerOrganisationId
LEFT JOIN dbo.OrderGroups og 
	ON o.OrderGroupId = og.OrderGroupId
LEFT JOIN dbo.Languages l
	ON l.LanguageId = o.LanguageId
LEFT JOIN dbo.RequestViews rv
	ON r.RequestId = rv.RequestId AND rv.RequestViewId IN 
	(SELECT TOP 1 rv1.RequestViewId FROM RequestViews rv1 WHERE r.RequestId = rv1.RequestId ORDER BY rv1.ViewedAt DESC)
LEFT JOIN dbo.AspNetUsers anu
	ON rv.ViewedBy = anu.Id
WHERE c.Status = 1