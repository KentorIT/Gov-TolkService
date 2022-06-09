ALTER PROCEDURE[dbo].[GetOrdersForExcelReport] @dateFrom DATE, @dateTo DATE, @userId INT, @onlyDelivered BIT, @customerId INT = NULL
AS

--NOTE! When ALTER THIS SP - also change file \GOV Tolk\Tolk.BusinessLogic\Data\Stored procedures\GetOrdersForExcelReport.sql

	CREATE TABLE #reportOrders (
		orderId INT
	   , latestRequestId INT
	   , il1 NVARCHAR(50)
	   , il2 NVARCHAR(50)
	   , il3 NVARCHAR(50)
	   , dialect NVARCHAR(250)
	   , dialectIsReq BIT
	   , dialectIsFulfilled BIT
	   , hasRequisition BIT
	   , hasComplaint BIT
	   , desiredComp1 NVARCHAR(50)
	   , desiredComp2 NVARCHAR(50)
	   , requiredComp NVARCHAR(50)
	   , otherRequiredComp NVARCHAR(50)
	   , noOfDesired INT
	   , noOfDesiredFulfilled INT
	   , noOfRequired INT
	   , noOfRequiredFulfilled INT
	   , customerName NVARCHAR(250)
	   , totalPrice DECIMAL
	)

	CREATE TABLE #orderStatus (
		Id INT
	   , statusName NVARCHAR(100)
	)

	CREATE TABLE #interpreterLocation (
		Id INT
	   , ilName NVARCHAR(100)
	)

	CREATE TABLE #compLevels (
		Id INT
	   , compName NVARCHAR(100)
	)

	INSERT INTO #interpreterLocation (Id, ilName)
		VALUES(1, 'På plats'),
		(2, 'Distans per telefon'),
		(3, 'Distans per video'),
		(4, 'Distans i anvisad lokal per video')

	INSERT INTO #compLevels (Id, compName)
		VALUES(1, 'Övrig tolk'),
		(2, 'Utbildad tolk'),
		(3, 'Auktoriserad tolk'),
		(4, 'Sjukvårdstolk'),
		(5, 'Rättstolk')

	INSERT INTO #reportOrders (orderId, latestRequestId)
		SELECT
			o.orderId
		   ,(SELECT TOP 1
					r.RequestId
				FROM Requests r
				WHERE r.orderId = o.orderId
				ORDER BY r.RequestId DESC)
		FROM Orders o
		WHERE(o.CustomerOrganisationId = @customerId
		OR @customerId IS NULL)
		AND((@onlyDelivered = 0
		AND CONVERT(DATE, o.CreatedAt) >= @dateFrom
		AND CONVERT(DATE, o.CreatedAt) <= @dateTo)--ordered
		OR(@onlyDelivered = 1
		AND o.Status IN(4, 5, 7)
		AND(o.EndAt <= GETDATE()
		AND CONVERT(DATE, o.StartAt) >= @dateFrom
		AND CONVERT(DATE, o.StartAt) <= @dateTo))) --delivered

	INSERT INTO #orderStatus (Id, statusName)
		VALUES(2, 'Bokningsförfrågan skickad'),
		(3, 'Tolk är tillsatt'),
		(4, 'Tillsättning är godkänd'),
		(5, 'Uppdrag har utförts'),
		(6, 'Uppdrag avbokat av myndighet'),
		(7, 'Utförande bekräftat'),
		(8, 'Uppdraget har annullerats via reklamation'),
		(9, 'Bokningsförfrågan avböjd av samtliga förmedlingar'),
		(10, 'Tolk är tillsatt(Ny tolk)'),
		(12, 'Uppdrag avbokat av förmedling'),
		(15, 'Tillsättning ej besvarad'),
		(16, 'Sista svarstid ej satt'),
		(17, 'Uppdrag annullerat, sista svarstid ej satt')

	--if customer check roles, if central admin take only admin units
	DECLARE @onlyUnits BIT = 0;

			IF(@customerId IS NOT NULL


				AND NOT EXISTS(SELECT
						*
					FROM AspNetUsers anu


					JOIN AspNetUserRoles anur


						ON anur.UserId = anu.Id


					INNER JOIN AspNetRoles anr


						ON anur.RoleId = anr.Id


					WHERE anr.Name = 'CentralAdministrator'


					AND anu.Id = @userId)
				)
		SET @onlyUnits = 1

	UPDATE #reportOrders
	SET il1 = il.ilName
	FROM OrderInterpreterLocation oil
	JOIN #reportOrders o
		ON oil.orderId = o.orderId
	JOIN #interpreterLocation il
		ON il.Id = oil.InterpreterLocation
	WHERE oil.Rank = 1

	UPDATE #reportOrders
	SET il2 = il.ilName
	FROM OrderInterpreterLocation oil
	JOIN #reportOrders o
		ON oil.orderId = o.orderId
	JOIN #interpreterLocation il
		ON il.Id = oil.InterpreterLocation
	WHERE oil.Rank = 2

	UPDATE #reportOrders
	SET il3 = il.ilName
	FROM OrderInterpreterLocation oil
	JOIN #reportOrders o
		ON oil.orderId = o.orderId
	JOIN #interpreterLocation il
		ON il.Id = oil.InterpreterLocation
	WHERE oil.Rank = 3

	UPDATE #reportOrders
	SET dialect = orReq.Description
	   ,dialectIsReq = orReq.IsRequired
	FROM OrderRequirements orReq
	JOIN #reportOrders o
		ON orReq.orderId = o.orderId
	WHERE orReq.RequirementType = 2--dialect

   UPDATE #reportOrders
	SET dialectIsFulfilled = orra.CanSatisfyRequirement
	FROM #reportOrders o
	JOIN OrderRequirements orReq
		ON orReq.orderId = o.orderId
	JOIN OrderRequirementRequestAnswer orra
		ON orra.OrderRequirementId = orReq.OrderRequirementId
		AND o.latestRequestId = orra.RequestId
	WHERE orReq.RequirementType = 2--dialect

   UPDATE #reportOrders
	SET dialectIsFulfilled = 0
	FROM #reportOrders o
	WHERE o.dialect IS NOT NULL
	AND ISNULL(o.dialectIsFulfilled, 0) = 0

	UPDATE #reportOrders
	SET requiredComp = l.compName
	FROM OrderCompetenceRequirements ocr
	JOIN #reportOrders o
		ON ocr.orderId = o.orderId
	JOIN Orders ord
		ON o.orderId = ord.orderId
	JOIN #compLevels l
		ON l.Id = ocr.CompetenceLevel
		AND ord.SpecificCompetenceLevelRequired = 1
		AND ord.LanguageHasAuthorizedInterpreter = 1

	UPDATE #reportOrders
	SET otherRequiredComp = l.compName
	FROM OrderCompetenceRequirements ocr
	JOIN #reportOrders o
		ON ocr.orderId = o.orderId
	JOIN Orders ord
		ON o.orderId = ord.orderId
	JOIN #compLevels l
		ON l.Id = ocr.CompetenceLevel
		AND ord.SpecificCompetenceLevelRequired = 1
	WHERE l.compName<> o.requiredComp

	IF(@onlyDelivered = 1)
	BEGIN
		UPDATE #reportOrders
		SET hasRequisition = 1
		FROM #reportOrders o
		INNER JOIN Requisitions req
			ON req.RequestId = o.latestRequestId

		UPDATE #reportOrders
		SET hasComplaint = 1
		FROM #reportOrders o
		INNER JOIN Complaints c
			ON o.latestRequestId = c.RequestId

		UPDATE #reportOrders
		SET totalPrice = (SELECT
				SUM(rpr.Price * rpr.Quantity)
			FROM RequestPriceRows rpr
			WHERE #reportOrders.latestRequestId = rpr.RequestId)
	END

	--use LanguageHasAuthorizedInterpreter = 1 ? is used in EF reports, Example 2021 - 111053 has LanguageHasAuthorizedInterpreter = 0 but they managed to set required/ desired competence level
	UPDATE #reportOrders
	SET desiredComp1 = l.compName
	FROM OrderCompetenceRequirements ocr
	JOIN #reportOrders o
		ON ocr.orderId = o.orderId
	JOIN Orders ord
		ON o.orderId = ord.orderId
	JOIN #compLevels l
		ON l.Id = ocr.CompetenceLevel
		AND ord.SpecificCompetenceLevelRequired = 0
	WHERE ocr.Rank = 1
	AND ord.LanguageHasAuthorizedInterpreter = 1

	UPDATE #reportOrders
	SET desiredComp2 = l.compName
	FROM OrderCompetenceRequirements ocr
	JOIN #reportOrders o
		ON ocr.orderId = o.orderId
	JOIN Orders ord
		ON o.orderId = ord.orderId
	JOIN #compLevels l
		ON l.Id = ocr.CompetenceLevel
		AND ord.SpecificCompetenceLevelRequired = 0
	WHERE ocr.Rank = 2
	AND ord.LanguageHasAuthorizedInterpreter = 1

	UPDATE #reportOrders
	SET noOfDesired = (SELECT
			COUNT(oreq.OrderRequirementId)
		FROM OrderRequirements oreq
		JOIN #reportOrders o
			ON oreq.orderId = o.orderId
		WHERE oreq.IsRequired = 0
		AND oreq.RequirementType <> 2--not dialect
		AND o.orderId = #reportOrders.orderId
		GROUP BY o.orderId)

	UPDATE #reportOrders
	SET noOfDesiredFulfilled = (SELECT
			COUNT(orra.OrderRequirementId)
		FROM OrderRequirements oreq
		JOIN #reportOrders o
			ON oreq.orderId = o.orderId
		INNER JOIN OrderRequirementRequestAnswer orra
			ON orra.OrderRequirementId = oreq.OrderRequirementId
			AND orra.CanSatisfyRequirement = 1
			AND o.latestRequestId = orra.RequestId
		WHERE oreq.IsRequired = 0
		AND oreq.RequirementType <> 2--not dialect
		AND o.orderId = #reportOrders.orderId
		GROUP BY o.orderId)

	UPDATE #reportOrders
	SET noOfRequired = (SELECT
			COUNT(oreq.OrderRequirementId)
		FROM OrderRequirements oreq
		JOIN #reportOrders o
			ON oreq.orderId = o.orderId
		WHERE oreq.IsRequired = 1
		AND oreq.RequirementType <> 2--not dialect
		AND o.orderId = #reportOrders.orderId
		GROUP BY o.orderId)

	UPDATE #reportOrders
	SET noOfRequiredFulfilled = (SELECT
			COUNT(orra.OrderRequirementId)
		FROM OrderRequirements oreq
		JOIN #reportOrders o
			ON oreq.orderId = o.orderId
		INNER JOIN OrderRequirementRequestAnswer orra
			ON orra.OrderRequirementId = oreq.OrderRequirementId
			AND o.latestRequestId = orra.RequestId
			AND orra.CanSatisfyRequirement = 1
		WHERE oreq.IsRequired = 1
		AND oreq.RequirementType <> 2--not dialect
		AND o.orderId = #reportOrders.orderId
		GROUP BY o.orderId)

	SELECT DISTINCT
		o.OrderNumber 'BokningsId'
	   ,CASE
			WHEN @onlyDelivered = 1 THEN CONVERT(CHAR(16), o.StartAt, 121)
			ELSE CONVERT(CHAR(16), o.CreatedAt, 121)
		END 'Rapportdatum'
	   ,COALESCE(l.Name, o.OtherLanguage) 'Språk'
	   ,reg.Name 'Län'
	   ,CASE
			WHEN o.AssignmentType = 1 THEN 'Tolkning'
			ELSE 'Tolkning inkl. avista'
		END 'Uppdragstyp'
	   ,ISNULL(cl.compName, 'Tolk ej tillsatt') 'Tolkens kompetensnivå'
	   ,ISNULL(ib.OfficialInterpreterId, '') 'Kammarkollegiets tolknr'
	   ,ISNULL(ilReq.ilName, '') 'Inställelsesätt'
	   ,CONVERT(CHAR(16), o.StartAt, 121) + '-' + SUBSTRING(CONVERT(CHAR(16), o.EndAt, 121), 12, 5) 'Tid för uppdrag'
	   ,ISNULL(o.CustomerReferenceNumber, '') 'Myndighetens ärendenummer'
	   ,CASE
			WHEN(o.AllowExceedingTravelCost = 1) THEN 'Ja, och jag vill godkänna bedömd resekostnad i förväg'
			WHEN(o.AllowExceedingTravelCost = 2) THEN 'Ja, men jag behöver inte godkänna bedömd resekostnad i förväg'
			WHEN(o.AllowExceedingTravelCost = 3) THEN 'Nej'
			ELSE ''
		END 'Accepterar restid'
	   ,s.statusName 'Status'
	   ,ISNULL(po.dialect, '') 'Dialekt'
	   ,CASE
			WHEN po.dialectIsReq = 1 THEN 'Ja'
			WHEN po.dialectIsReq = 0 THEN 'Nej'
			ELSE ''
		END 'Dialekt är krav'
	   ,CASE
			WHEN po.dialectIsFulfilled = 1 THEN 'Ja'
			WHEN po.dialectIsFulfilled = 0 THEN 'Nej'
			ELSE ''
		END 'Uppfyllt krav/önskemål om dialekt'
	   ,ISNULL(po.il1, '') 'Inställelsesätt 1:a hand'
	   ,ISNULL(po.il2, '') 'Inställelsesätt 2:a hand'
	   ,ISNULL(po.il3, '') 'Inställelsesätt 3:e hand'
	   ,ISNULL(po.desiredComp1, '') 'Önskad kompetensnivå 1:a hand'
	   ,ISNULL(po.desiredComp2, '') 'Önskad kompetensnivå 2:a hand'
	   ,ISNULL(po.requiredComp, '') 'Krav på kompetensnivå'
	   ,ISNULL(po.otherRequiredComp, '') 'Ytterligare krav på kompetensnivå'
	   ,ISNULL(po.noOfRequired, 0) 'Antal övriga krav'
	   ,ISNULL(po.noOfRequiredFulfilled, 0) 'Antal uppfyllda övriga krav'
	   ,ISNULL(po.noOfDesired, 0) 'Antal övriga önskemål'
	   ,ISNULL(po.noOfDesiredFulfilled, 0) 'Antal uppfyllda övriga önskemål'
	   ,ISNULL(cu.Name, '') 'Enhet'
	   ,ISNULL(o.UnitName, '') 'Avdelning'
	   ,b.Name 'Förmedling'
	   ,anu.NameFirst + ' ' + anu.NameFamily 'Rapportperson'
	   ,ISNULL(o.InvoiceReference, '') 'Fakturareferens'
	   ,ISNULL(anu.Email, '') 'E-postadress'
	   ,co.Name 'Myndighet'
	   ,ISNULL(po.hasRequisition, 0) 'Rekvisition finns'
	   ,ISNULL(po.hasComplaint, 0) 'Reklamation finns'
	   ,po.totalPrice 'Totalt pris'
	FROM Orders o
	INNER JOIN #reportOrders po
		ON po.orderId = o.orderId
	INNER JOIN Requests r
		ON r.RequestId = po.latestRequestId
	LEFT JOIN Languages l
		ON l.LanguageId = o.LanguageId
	LEFT JOIN #compLevels cl
		ON cl.Id = r.CompetenceLevel
	INNER JOIN Regions reg
		ON reg.RegionId = o.RegionId
	LEFT JOIN OrderRequirements ordreq
		ON o.orderId = ordreq.orderId
	LEFT JOIN OrderRequirementRequestAnswer orra
		ON orra.OrderRequirementId = ordreq.OrderRequirementId
	LEFT JOIN InterpreterBrokers ib
		ON r.InterpreterBrokerId = ib.InterpreterBrokerId
	LEFT JOIN #interpreterLocation ilReq
		ON ilReq.Id = r.InterpreterLocation
	INNER JOIN #orderStatus s
		ON s.Id = o.Status
	INNER JOIN AspNetUsers anu
		ON o.CreatedBy = anu.Id
	INNER JOIN Rankings ra
		ON r.RankingId = ra.RankingId
	INNER JOIN Brokers b
		ON ra.BrokerId = b.BrokerId
	INNER JOIN CustomerOrganisations co
		ON co.CustomerOrganisationId = o.CustomerOrganisationId
	LEFT JOIN CustomerUnits cu
		ON o.CustomerUnitId = cu.CustomerUnitId
	WHERE(@onlyUnits = 0
	OR cu.CustomerUnitId IN(SELECT
			cuu.CustomerUnitId
		FROM CustomerUnitUsers cuu
		WHERE cuu.UserId = @userId
		AND cuu.IsLocalAdmin = 1)
	)

	DROP TABLE #reportOrders, #orderStatus, #interpreterLocation, #compLevels