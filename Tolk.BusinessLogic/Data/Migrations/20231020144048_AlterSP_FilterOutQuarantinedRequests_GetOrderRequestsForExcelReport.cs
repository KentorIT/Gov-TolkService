using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tolk.BusinessLogic.Data.Migrations
{
    /// <inheritdoc />
    public partial class AlterSP_FilterOutQuarantinedRequests_GetOrderRequestsForExcelReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"Exec('ALTER PROCEDURE[dbo].[GetOrderRequestsForExcelReport] @dateFrom DATE, @dateTo DATE, @onlyDelivered BIT, @brokerId INT
AS

--NOTE! When ALTER THIS SP - also change file \GOV Tolk\Tolk.BusinessLogic\Data\Stored procedures\GetOrderRequestsForExcelReport.sql

	CREATE TABLE #reportOrderRequests (
		orderId INT
	   ,latestRequestId INT
	   ,il1 NVARCHAR(50)
	   ,il2 NVARCHAR(50)
	   ,il3 NVARCHAR(50)
	   ,dialect NVARCHAR(250)
	   ,dialectIsReq BIT
	   ,dialectIsFulfilled BIT
	   ,hasRequisition BIT
	   ,hasComplaint BIT
	   ,desiredComp1 NVARCHAR(50)
	   ,desiredComp2 NVARCHAR(50)
	   ,requiredComp NVARCHAR(50)
	   ,otherRequiredComp NVARCHAR(50)
	   ,noOfDesired INT
	   ,noOfDesiredFulfilled INT
	   ,noOfRequired INT
	   ,noOfRequiredFulfilled INT
	   ,customerName NVARCHAR(250)
	   ,totalPrice DECIMAL
	)

	CREATE TABLE #requestStatus (
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
		VALUES(1, ''På plats''),
		(2, ''Distans per telefon''),
		(3, ''Distans per video''),
		(4, ''Distans i anvisad lokal per video'')

	INSERT INTO #compLevels (Id, compName)
		VALUES(1, ''Övrig tolk''),
		(2, ''Utbildad tolk''),
		(3, ''Auktoriserad tolk''),
		(4, ''Sjukvårdstolk''),
		(5, ''Rättstolk'')

	INSERT INTO #reportOrderRequests (orderId, latestRequestId)
		SELECT
			o.orderId
		   ,r.RequestId
		FROM Requests r
		INNER JOIN Rankings ra
			ON r.RankingId = ra.RankingId
		INNER JOIN Brokers b
			ON ra.BrokerId = b.BrokerId
		INNER JOIN Orders o
			ON r.orderId = o.orderId
		WHERE ra.BrokerId = @brokerId
		AND((@onlyDelivered = 0
		AND CONVERT(DATE, r.CreatedAt) >= @dateFrom
		AND CONVERT(DATE, r.CreatedAt) <= @dateTo
		AND r.Status NOT IN(13, 17, 18, 24, 25, 19))--ordered
		OR(@onlyDelivered = 1
		AND o.Status IN(4, 5, 7)
		AND r.Status IN(5, 6)
		AND(CASE WHEN r.RespondedStartAt IS NOT NULL THEN DATEADD(mi, (datepart(HOUR,o.ExpectedLength)*60)+datepart(MINUTE,o.ExpectedLength), r.RespondedStartAt) ELSE o.EndAt END <= GETDATE()
		AND CONVERT(DATE, o.StartAt) >= @dateFrom
		AND CONVERT(DATE, o.StartAt) <= @dateTo))) --delivered

	INSERT INTO #requestStatus (Id, statusName)
		VALUES(1, ''Bokningsförfrågan inkommen''),
		(2, ''Bokningsförfrågan mottagen''),
		(3, ''Avbokad av myndighet''),
		(4, ''Bekräftelse är skickad''),
		(5, ''Tillsättning är godkänd''),
		(6, ''Uppdrag har utförts''),
		(7, ''Bokningsförfrågan avböjd''),
		(8, ''Tillsättning är avböjd''),
		(9, ''Bokningsförfrågan ej besvarad''),
		(10, ''Uppdrag avbokat av myndighet''),
		(12, ''Bekräftelse är skickad - Ny tolk''),
		(14, ''Uppdrag avbokat av förmedling''),
		(16, ''Tillsättning ej besvarad''),		
		(22, ''Förfrågan avbruten p.g.a. utgånget ramavtal''),
		(23, ''Förfrågan bekräftad av förmedling, inväntar tolktillsättning'')

	UPDATE #reportOrderRequests
	SET il1 = il.ilName
	FROM OrderInterpreterLocation oil
	JOIN #reportOrderRequests o
		ON oil.orderId = o.orderId
	JOIN #interpreterLocation il
		ON il.Id = oil.InterpreterLocation
	WHERE oil.Rank = 1

	UPDATE #reportOrderRequests
	SET il2 = il.ilName
	FROM OrderInterpreterLocation oil
	JOIN #reportOrderRequests o
		ON oil.orderId = o.orderId
	JOIN #interpreterLocation il
		ON il.Id = oil.InterpreterLocation
	WHERE oil.Rank = 2

	UPDATE #reportOrderRequests
	SET il3 = il.ilName
	FROM OrderInterpreterLocation oil
	JOIN #reportOrderRequests o
		ON oil.orderId = o.orderId
	JOIN #interpreterLocation il
		ON il.Id = oil.InterpreterLocation
	WHERE oil.Rank = 3

	UPDATE #reportOrderRequests
	SET dialect = orReq.Description
	   ,dialectIsReq = orReq.IsRequired
	FROM OrderRequirements orReq
	JOIN #reportOrderRequests o
		ON orReq.orderId = o.orderId
	WHERE orReq.RequirementType = 2--dialect

   UPDATE #reportOrderRequests
	SET dialectIsFulfilled = orra.CanSatisfyRequirement
	FROM #reportOrderRequests o
	JOIN OrderRequirements orReq
		ON orReq.orderId = o.orderId
	JOIN OrderRequirementRequestAnswer orra
		ON orra.OrderRequirementId = orReq.OrderRequirementId
		AND o.latestRequestId = orra.RequestId
	WHERE orReq.RequirementType = 2--dialect

   UPDATE #reportOrderRequests
	SET dialectIsFulfilled = 0
	FROM #reportOrderRequests o
	WHERE o.dialect IS NOT NULL
	AND ISNULL(o.dialectIsFulfilled, 0) = 0

	UPDATE #reportOrderRequests
	SET requiredComp = l.compName
	FROM OrderCompetenceRequirements ocr
	JOIN #reportOrderRequests o
		ON ocr.orderId = o.orderId
	JOIN Orders ord
		ON o.orderId = ord.orderId
	JOIN #compLevels l
		ON l.Id = ocr.CompetenceLevel
		AND ord.SpecificCompetenceLevelRequired = 1
		AND ord.LanguageHasAuthorizedInterpreter = 1

	UPDATE #reportOrderRequests
	SET otherRequiredComp = l.compName
	FROM OrderCompetenceRequirements ocr
	JOIN #reportOrderRequests o
		ON ocr.orderId = o.orderId
	JOIN Orders ord
		ON o.orderId = ord.orderId
	JOIN #compLevels l
		ON l.Id = ocr.CompetenceLevel
		AND ord.SpecificCompetenceLevelRequired = 1
	WHERE l.compName<> o.requiredComp

	IF(@onlyDelivered = 1)
	BEGIN
		UPDATE #reportOrderRequests
		SET hasRequisition = 1
		FROM #reportOrderRequests o
		INNER JOIN Requisitions req
			ON req.RequestId = o.latestRequestId

		UPDATE #reportOrderRequests
		SET hasComplaint = 1
		FROM #reportOrderRequests o
		INNER JOIN Complaints c
			ON o.latestRequestId = c.RequestId

		UPDATE #reportOrderRequests
		SET totalPrice = (SELECT
				SUM(rpr.Price * rpr.Quantity)
			FROM RequestPriceRows rpr
			WHERE #reportOrderRequests.latestRequestId = rpr.RequestId)
	END

	--use LanguageHasAuthorizedInterpreter = 1 ? is used in EF reports, Example 2021 - 111053 has LanguageHasAuthorizedInterpreter = 0 but they managed to set required/ desired competence level
	UPDATE #reportOrderRequests
	SET desiredComp1 = l.compName
	FROM OrderCompetenceRequirements ocr
	JOIN #reportOrderRequests o
		ON ocr.orderId = o.orderId
	JOIN Orders ord
		ON o.orderId = ord.orderId
	JOIN #compLevels l
		ON l.Id = ocr.CompetenceLevel
		AND ord.SpecificCompetenceLevelRequired = 0
	WHERE ocr.Rank = 1
	AND ord.LanguageHasAuthorizedInterpreter = 1

	UPDATE #reportOrderRequests
	SET desiredComp2 = l.compName
	FROM OrderCompetenceRequirements ocr
	JOIN #reportOrderRequests o
		ON ocr.orderId = o.orderId
	JOIN Orders ord
		ON o.orderId = ord.orderId
	JOIN #compLevels l
		ON l.Id = ocr.CompetenceLevel
		AND ord.SpecificCompetenceLevelRequired = 0
	WHERE ocr.Rank = 2
	AND ord.LanguageHasAuthorizedInterpreter = 1

	UPDATE #reportOrderRequests
	SET noOfDesired = (SELECT
			COUNT(oreq.OrderRequirementId)
		FROM OrderRequirements oreq
		JOIN #reportOrderRequests o
			ON oreq.orderId = o.orderId
		WHERE oreq.IsRequired = 0
		AND oreq.RequirementType <> 2--not dialect
		AND o.orderId = #reportOrderRequests.orderId
		GROUP BY o.orderId)

	UPDATE #reportOrderRequests
	SET noOfDesiredFulfilled = (SELECT
			COUNT(orra.OrderRequirementId)
		FROM OrderRequirements oreq
		JOIN #reportOrderRequests o
			ON oreq.orderId = o.orderId
		INNER JOIN OrderRequirementRequestAnswer orra
			ON orra.OrderRequirementId = oreq.OrderRequirementId
			AND orra.CanSatisfyRequirement = 1
			AND o.latestRequestId = orra.RequestId
		WHERE oreq.IsRequired = 0
		AND oreq.RequirementType <> 2--not dialect
		AND o.orderId = #reportOrderRequests.orderId
		GROUP BY o.orderId)

	UPDATE #reportOrderRequests
	SET noOfRequired = (SELECT
			COUNT(oreq.OrderRequirementId)
		FROM OrderRequirements oreq
		JOIN #reportOrderRequests o
			ON oreq.orderId = o.orderId
		WHERE oreq.IsRequired = 1
		AND oreq.RequirementType <> 2
		AND o.orderId = #reportOrderRequests.orderId
		GROUP BY o.orderId)--not dialect

	UPDATE #reportOrderRequests
	SET noOfRequiredFulfilled = (SELECT
			COUNT(orra.OrderRequirementId)
		FROM OrderRequirements oreq
		JOIN #reportOrderRequests o
			ON oreq.orderId = o.orderId
		INNER JOIN OrderRequirementRequestAnswer orra
			ON orra.OrderRequirementId = oreq.OrderRequirementId
			AND o.latestRequestId = orra.RequestId
			AND orra.CanSatisfyRequirement = 1
		WHERE oreq.IsRequired = 1
		AND oreq.RequirementType <> 2
		AND o.orderId = #reportOrderRequests.orderId
		GROUP BY o.orderId)--not dialect

	SELECT DISTINCT
		o.OrderNumber ''BokningsId''
	   ,CASE
			WHEN @onlyDelivered = 1 THEN CONVERT(CHAR(16), o.StartAt, 121)
			ELSE CONVERT(CHAR(16), r.CreatedAt, 121)
		END ''Rapportdatum''
	   ,COALESCE(l.Name, o.OtherLanguage) ''Språk''
	   ,reg.Name ''Län''
	   ,CASE
			WHEN o.AssignmentType = 1 THEN ''Tolkning''
			ELSE ''Tolkning inkl. avista''
		END ''Uppdragstyp''
	   ,ISNULL(cl.compName, ''Tolk ej tillsatt'') ''Tolkens kompetensnivå''
	   ,ISNULL(ib.OfficialInterpreterId, '''') ''Kammarkollegiets tolknr''
	   ,ISNULL(ilReq.ilName, '''') ''Inställelsesätt''
	   ,CONVERT(CHAR(16), COALESCE(r.RespondedStartAt, o.StartAt), 121) + ''-'' + SUBSTRING(CONVERT(CHAR(16), CASE WHEN r.RespondedStartAt IS NOT NULL THEN DATEADD(mi, (datepart(HOUR,o.ExpectedLength)*60)+datepart(MINUTE,o.ExpectedLength), r.RespondedStartAt) ELSE o.EndAt END, 121), 12, 5) + 
			(Case When o.ExpectedLength IS NOT NULL And r.RespondedStartAt IS NULL THEN '' (F)'' ELSE '''' END) ''Tid för uppdrag''
	   ,ISNULL(o.CustomerReferenceNumber, '''') ''Myndighetens ärendenummer''
	   ,CASE
			WHEN(o.AllowExceedingTravelCost = 1 OR
				o.AllowExceedingTravelCost = 2) THEN ''Ja''
			WHEN(o.AllowExceedingTravelCost = 3) THEN ''Nej''
			ELSE ''''
		END ''Accepterar restid''
	   ,s.statusName ''Status''
	   ,ISNULL(po.dialect, '''') ''Dialekt''
	   ,CASE
			WHEN po.dialectIsReq = 1 THEN ''Ja''
			WHEN po.dialectIsReq = 0 THEN ''Nej''
			ELSE ''''
		END ''Dialekt är krav''
	   ,CASE
			WHEN po.dialectIsFulfilled = 1 THEN ''Ja''
			WHEN po.dialectIsFulfilled = 0 THEN ''Nej''
			ELSE ''''
		END ''Uppfyllt krav/önskemål om dialekt''
	   ,ISNULL(po.il1, '''') ''Inställelsesätt 1:a hand''
	   ,ISNULL(po.il2, '''') ''Inställelsesätt 2:a hand''
	   ,ISNULL(po.il3, '''') ''Inställelsesätt 3:e hand''
	   ,ISNULL(po.desiredComp1, '''') ''Önskad kompetensnivå 1:a hand''
	   ,ISNULL(po.desiredComp2, '''') ''Önskad kompetensnivå 2:a hand''
	   ,ISNULL(po.requiredComp, '''') ''Krav på kompetensnivå''
	   ,ISNULL(po.otherRequiredComp, '''') ''Ytterligare krav på kompetensnivå''
	   ,ISNULL(po.noOfRequired, 0) ''Antal övriga krav''
	   ,ISNULL(po.noOfRequiredFulfilled, 0) ''Antal uppfyllda övriga krav''
	   ,ISNULL(po.noOfDesired, 0) ''Antal övriga önskemål''
	   ,ISNULL(po.noOfDesiredFulfilled, 0) ''Antal uppfyllda övriga önskemål''
	   ,CASE anu.Id
			WHEN NULL THEN ''''
			ELSE anu.NameFirst + '' '' + anu.NameFamily
		END ''Rapportperson''
	   ,co.Name ''Myndighet''
	   ,ISNULL(po.hasRequisition, 0) ''Rekvisition finns''
	   ,ISNULL(po.hasComplaint, 0) ''Reklamation finns''
	   ,po.totalPrice ''Totalt pris''
	   ,b.Name ''Förmedling''
	   ,fa.AgreementNumber ''Avtalsnummer''
	   ,CASE
			WHEN o.ExpectedLength IS NOT NULL THEN ''Ja''
			ELSE ''Nej''
		END ''Flexibel bokning''
	FROM Orders o
	INNER JOIN #reportOrderRequests po
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
	INNER JOIN #requestStatus s
		ON s.Id = r.Status
	LEFT JOIN AspNetUsers anu
		ON r.AnsweredBy = anu.Id
	INNER JOIN Rankings ra
		ON r.RankingId = ra.RankingId
	INNER JOIN FrameworkAgreements fa
		ON ra.FrameworkAgreementId = fa.FrameworkAgreementId
	INNER JOIN Brokers b
		ON ra.BrokerId = b.BrokerId
	INNER JOIN CustomerOrganisations co
		ON co.CustomerOrganisationId = o.CustomerOrganisationId

	DROP TABLE #reportOrderRequests, #requestStatus, #interpreterLocation, #compLevels');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"Exec('ALTER PROCEDURE[dbo].[GetOrderRequestsForExcelReport] @dateFrom DATE, @dateTo DATE, @onlyDelivered BIT, @brokerId INT
AS

--NOTE! When ALTER THIS SP - also change file \GOV Tolk\Tolk.BusinessLogic\Data\Stored procedures\GetOrderRequestsForExcelReport.sql

	CREATE TABLE #reportOrderRequests (
		orderId INT
	   ,latestRequestId INT
	   ,il1 NVARCHAR(50)
	   ,il2 NVARCHAR(50)
	   ,il3 NVARCHAR(50)
	   ,dialect NVARCHAR(250)
	   ,dialectIsReq BIT
	   ,dialectIsFulfilled BIT
	   ,hasRequisition BIT
	   ,hasComplaint BIT
	   ,desiredComp1 NVARCHAR(50)
	   ,desiredComp2 NVARCHAR(50)
	   ,requiredComp NVARCHAR(50)
	   ,otherRequiredComp NVARCHAR(50)
	   ,noOfDesired INT
	   ,noOfDesiredFulfilled INT
	   ,noOfRequired INT
	   ,noOfRequiredFulfilled INT
	   ,customerName NVARCHAR(250)
	   ,totalPrice DECIMAL
	)

	CREATE TABLE #requestStatus (
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
		VALUES(1, ''På plats''),
		(2, ''Distans per telefon''),
		(3, ''Distans per video''),
		(4, ''Distans i anvisad lokal per video'')

	INSERT INTO #compLevels (Id, compName)
		VALUES(1, ''Övrig tolk''),
		(2, ''Utbildad tolk''),
		(3, ''Auktoriserad tolk''),
		(4, ''Sjukvårdstolk''),
		(5, ''Rättstolk'')

	INSERT INTO #reportOrderRequests (orderId, latestRequestId)
		SELECT
			o.orderId
		   ,r.RequestId
		FROM Requests r
		INNER JOIN Rankings ra
			ON r.RankingId = ra.RankingId
		INNER JOIN Brokers b
			ON ra.BrokerId = b.BrokerId
		INNER JOIN Orders o
			ON r.orderId = o.orderId
		WHERE ra.BrokerId = @brokerId
		AND((@onlyDelivered = 0
		AND CONVERT(DATE, r.CreatedAt) >= @dateFrom
		AND CONVERT(DATE, r.CreatedAt) <= @dateTo
		AND r.Status NOT IN(13, 17, 18, 24, 25))--ordered
		OR(@onlyDelivered = 1
		AND o.Status IN(4, 5, 7)
		AND r.Status IN(5, 6)
		AND(CASE WHEN r.RespondedStartAt IS NOT NULL THEN DATEADD(mi, (datepart(HOUR,o.ExpectedLength)*60)+datepart(MINUTE,o.ExpectedLength), r.RespondedStartAt) ELSE o.EndAt END <= GETDATE()
		AND CONVERT(DATE, o.StartAt) >= @dateFrom
		AND CONVERT(DATE, o.StartAt) <= @dateTo))) --delivered

	INSERT INTO #requestStatus (Id, statusName)
		VALUES(1, ''Bokningsförfrågan inkommen''),
		(2, ''Bokningsförfrågan mottagen''),
		(3, ''Avbokad av myndighet''),
		(4, ''Bekräftelse är skickad''),
		(5, ''Tillsättning är godkänd''),
		(6, ''Uppdrag har utförts''),
		(7, ''Bokningsförfrågan avböjd''),
		(8, ''Tillsättning är avböjd''),
		(9, ''Bokningsförfrågan ej besvarad''),
		(10, ''Uppdrag avbokat av myndighet''),
		(12, ''Bekräftelse är skickad - Ny tolk''),
		(14, ''Uppdrag avbokat av förmedling''),
		(16, ''Tillsättning ej besvarad''),
		(19, ''Förlorad på grund av karantän''),
		(22, ''Förfrågan avbruten p.g.a. utgånget ramavtal''),
		(23, ''Förfrågan bekräftad av förmedling, inväntar tolktillsättning'')

	UPDATE #reportOrderRequests
	SET il1 = il.ilName
	FROM OrderInterpreterLocation oil
	JOIN #reportOrderRequests o
		ON oil.orderId = o.orderId
	JOIN #interpreterLocation il
		ON il.Id = oil.InterpreterLocation
	WHERE oil.Rank = 1

	UPDATE #reportOrderRequests
	SET il2 = il.ilName
	FROM OrderInterpreterLocation oil
	JOIN #reportOrderRequests o
		ON oil.orderId = o.orderId
	JOIN #interpreterLocation il
		ON il.Id = oil.InterpreterLocation
	WHERE oil.Rank = 2

	UPDATE #reportOrderRequests
	SET il3 = il.ilName
	FROM OrderInterpreterLocation oil
	JOIN #reportOrderRequests o
		ON oil.orderId = o.orderId
	JOIN #interpreterLocation il
		ON il.Id = oil.InterpreterLocation
	WHERE oil.Rank = 3

	UPDATE #reportOrderRequests
	SET dialect = orReq.Description
	   ,dialectIsReq = orReq.IsRequired
	FROM OrderRequirements orReq
	JOIN #reportOrderRequests o
		ON orReq.orderId = o.orderId
	WHERE orReq.RequirementType = 2--dialect

   UPDATE #reportOrderRequests
	SET dialectIsFulfilled = orra.CanSatisfyRequirement
	FROM #reportOrderRequests o
	JOIN OrderRequirements orReq
		ON orReq.orderId = o.orderId
	JOIN OrderRequirementRequestAnswer orra
		ON orra.OrderRequirementId = orReq.OrderRequirementId
		AND o.latestRequestId = orra.RequestId
	WHERE orReq.RequirementType = 2--dialect

   UPDATE #reportOrderRequests
	SET dialectIsFulfilled = 0
	FROM #reportOrderRequests o
	WHERE o.dialect IS NOT NULL
	AND ISNULL(o.dialectIsFulfilled, 0) = 0

	UPDATE #reportOrderRequests
	SET requiredComp = l.compName
	FROM OrderCompetenceRequirements ocr
	JOIN #reportOrderRequests o
		ON ocr.orderId = o.orderId
	JOIN Orders ord
		ON o.orderId = ord.orderId
	JOIN #compLevels l
		ON l.Id = ocr.CompetenceLevel
		AND ord.SpecificCompetenceLevelRequired = 1
		AND ord.LanguageHasAuthorizedInterpreter = 1

	UPDATE #reportOrderRequests
	SET otherRequiredComp = l.compName
	FROM OrderCompetenceRequirements ocr
	JOIN #reportOrderRequests o
		ON ocr.orderId = o.orderId
	JOIN Orders ord
		ON o.orderId = ord.orderId
	JOIN #compLevels l
		ON l.Id = ocr.CompetenceLevel
		AND ord.SpecificCompetenceLevelRequired = 1
	WHERE l.compName<> o.requiredComp

	IF(@onlyDelivered = 1)
	BEGIN
		UPDATE #reportOrderRequests
		SET hasRequisition = 1
		FROM #reportOrderRequests o
		INNER JOIN Requisitions req
			ON req.RequestId = o.latestRequestId

		UPDATE #reportOrderRequests
		SET hasComplaint = 1
		FROM #reportOrderRequests o
		INNER JOIN Complaints c
			ON o.latestRequestId = c.RequestId

		UPDATE #reportOrderRequests
		SET totalPrice = (SELECT
				SUM(rpr.Price * rpr.Quantity)
			FROM RequestPriceRows rpr
			WHERE #reportOrderRequests.latestRequestId = rpr.RequestId)
	END

	--use LanguageHasAuthorizedInterpreter = 1 ? is used in EF reports, Example 2021 - 111053 has LanguageHasAuthorizedInterpreter = 0 but they managed to set required/ desired competence level
	UPDATE #reportOrderRequests
	SET desiredComp1 = l.compName
	FROM OrderCompetenceRequirements ocr
	JOIN #reportOrderRequests o
		ON ocr.orderId = o.orderId
	JOIN Orders ord
		ON o.orderId = ord.orderId
	JOIN #compLevels l
		ON l.Id = ocr.CompetenceLevel
		AND ord.SpecificCompetenceLevelRequired = 0
	WHERE ocr.Rank = 1
	AND ord.LanguageHasAuthorizedInterpreter = 1

	UPDATE #reportOrderRequests
	SET desiredComp2 = l.compName
	FROM OrderCompetenceRequirements ocr
	JOIN #reportOrderRequests o
		ON ocr.orderId = o.orderId
	JOIN Orders ord
		ON o.orderId = ord.orderId
	JOIN #compLevels l
		ON l.Id = ocr.CompetenceLevel
		AND ord.SpecificCompetenceLevelRequired = 0
	WHERE ocr.Rank = 2
	AND ord.LanguageHasAuthorizedInterpreter = 1

	UPDATE #reportOrderRequests
	SET noOfDesired = (SELECT
			COUNT(oreq.OrderRequirementId)
		FROM OrderRequirements oreq
		JOIN #reportOrderRequests o
			ON oreq.orderId = o.orderId
		WHERE oreq.IsRequired = 0
		AND oreq.RequirementType <> 2--not dialect
		AND o.orderId = #reportOrderRequests.orderId
		GROUP BY o.orderId)

	UPDATE #reportOrderRequests
	SET noOfDesiredFulfilled = (SELECT
			COUNT(orra.OrderRequirementId)
		FROM OrderRequirements oreq
		JOIN #reportOrderRequests o
			ON oreq.orderId = o.orderId
		INNER JOIN OrderRequirementRequestAnswer orra
			ON orra.OrderRequirementId = oreq.OrderRequirementId
			AND orra.CanSatisfyRequirement = 1
			AND o.latestRequestId = orra.RequestId
		WHERE oreq.IsRequired = 0
		AND oreq.RequirementType <> 2--not dialect
		AND o.orderId = #reportOrderRequests.orderId
		GROUP BY o.orderId)

	UPDATE #reportOrderRequests
	SET noOfRequired = (SELECT
			COUNT(oreq.OrderRequirementId)
		FROM OrderRequirements oreq
		JOIN #reportOrderRequests o
			ON oreq.orderId = o.orderId
		WHERE oreq.IsRequired = 1
		AND oreq.RequirementType <> 2
		AND o.orderId = #reportOrderRequests.orderId
		GROUP BY o.orderId)--not dialect

	UPDATE #reportOrderRequests
	SET noOfRequiredFulfilled = (SELECT
			COUNT(orra.OrderRequirementId)
		FROM OrderRequirements oreq
		JOIN #reportOrderRequests o
			ON oreq.orderId = o.orderId
		INNER JOIN OrderRequirementRequestAnswer orra
			ON orra.OrderRequirementId = oreq.OrderRequirementId
			AND o.latestRequestId = orra.RequestId
			AND orra.CanSatisfyRequirement = 1
		WHERE oreq.IsRequired = 1
		AND oreq.RequirementType <> 2
		AND o.orderId = #reportOrderRequests.orderId
		GROUP BY o.orderId)--not dialect

	SELECT DISTINCT
		o.OrderNumber ''BokningsId''
	   ,CASE
			WHEN @onlyDelivered = 1 THEN CONVERT(CHAR(16), o.StartAt, 121)
			ELSE CONVERT(CHAR(16), r.CreatedAt, 121)
		END ''Rapportdatum''
	   ,COALESCE(l.Name, o.OtherLanguage) ''Språk''
	   ,reg.Name ''Län''
	   ,CASE
			WHEN o.AssignmentType = 1 THEN ''Tolkning''
			ELSE ''Tolkning inkl. avista''
		END ''Uppdragstyp''
	   ,ISNULL(cl.compName, ''Tolk ej tillsatt'') ''Tolkens kompetensnivå''
	   ,ISNULL(ib.OfficialInterpreterId, '''') ''Kammarkollegiets tolknr''
	   ,ISNULL(ilReq.ilName, '''') ''Inställelsesätt''
	   ,CONVERT(CHAR(16), COALESCE(r.RespondedStartAt, o.StartAt), 121) + ''-'' + SUBSTRING(CONVERT(CHAR(16), CASE WHEN r.RespondedStartAt IS NOT NULL THEN DATEADD(mi, (datepart(HOUR,o.ExpectedLength)*60)+datepart(MINUTE,o.ExpectedLength), r.RespondedStartAt) ELSE o.EndAt END, 121), 12, 5) + 
			(Case When o.ExpectedLength IS NOT NULL And r.RespondedStartAt IS NULL THEN '' (F)'' ELSE '''' END) ''Tid för uppdrag''
	   ,ISNULL(o.CustomerReferenceNumber, '''') ''Myndighetens ärendenummer''
	   ,CASE
			WHEN(o.AllowExceedingTravelCost = 1 OR
				o.AllowExceedingTravelCost = 2) THEN ''Ja''
			WHEN(o.AllowExceedingTravelCost = 3) THEN ''Nej''
			ELSE ''''
		END ''Accepterar restid''
	   ,s.statusName ''Status''
	   ,ISNULL(po.dialect, '''') ''Dialekt''
	   ,CASE
			WHEN po.dialectIsReq = 1 THEN ''Ja''
			WHEN po.dialectIsReq = 0 THEN ''Nej''
			ELSE ''''
		END ''Dialekt är krav''
	   ,CASE
			WHEN po.dialectIsFulfilled = 1 THEN ''Ja''
			WHEN po.dialectIsFulfilled = 0 THEN ''Nej''
			ELSE ''''
		END ''Uppfyllt krav/önskemål om dialekt''
	   ,ISNULL(po.il1, '''') ''Inställelsesätt 1:a hand''
	   ,ISNULL(po.il2, '''') ''Inställelsesätt 2:a hand''
	   ,ISNULL(po.il3, '''') ''Inställelsesätt 3:e hand''
	   ,ISNULL(po.desiredComp1, '''') ''Önskad kompetensnivå 1:a hand''
	   ,ISNULL(po.desiredComp2, '''') ''Önskad kompetensnivå 2:a hand''
	   ,ISNULL(po.requiredComp, '''') ''Krav på kompetensnivå''
	   ,ISNULL(po.otherRequiredComp, '''') ''Ytterligare krav på kompetensnivå''
	   ,ISNULL(po.noOfRequired, 0) ''Antal övriga krav''
	   ,ISNULL(po.noOfRequiredFulfilled, 0) ''Antal uppfyllda övriga krav''
	   ,ISNULL(po.noOfDesired, 0) ''Antal övriga önskemål''
	   ,ISNULL(po.noOfDesiredFulfilled, 0) ''Antal uppfyllda övriga önskemål''
	   ,CASE anu.Id
			WHEN NULL THEN ''''
			ELSE anu.NameFirst + '' '' + anu.NameFamily
		END ''Rapportperson''
	   ,co.Name ''Myndighet''
	   ,ISNULL(po.hasRequisition, 0) ''Rekvisition finns''
	   ,ISNULL(po.hasComplaint, 0) ''Reklamation finns''
	   ,po.totalPrice ''Totalt pris''
	   ,b.Name ''Förmedling''
	   ,fa.AgreementNumber ''Avtalsnummer''
	   ,CASE
			WHEN o.ExpectedLength IS NOT NULL THEN ''Ja''
			ELSE ''Nej''
		END ''Flexibel bokning''
	FROM Orders o
	INNER JOIN #reportOrderRequests po
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
	INNER JOIN #requestStatus s
		ON s.Id = r.Status
	LEFT JOIN AspNetUsers anu
		ON r.AnsweredBy = anu.Id
	INNER JOIN Rankings ra
		ON r.RankingId = ra.RankingId
	INNER JOIN FrameworkAgreements fa
		ON ra.FrameworkAgreementId = fa.FrameworkAgreementId
	INNER JOIN Brokers b
		ON ra.BrokerId = b.BrokerId
	INNER JOIN CustomerOrganisations co
		ON co.CustomerOrganisationId = o.CustomerOrganisationId

	DROP TABLE #reportOrderRequests, #requestStatus, #interpreterLocation, #compLevels');");
        }
    }
}
