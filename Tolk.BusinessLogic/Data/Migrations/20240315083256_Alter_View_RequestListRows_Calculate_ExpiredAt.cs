using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tolk.BusinessLogic.Data.Migrations
{
    /// <inheritdoc />
    public partial class Alter_View_RequestListRows_Calculate_ExpiredAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"Exec('ALTER VIEW [dbo].[RequestListRows]
AS

--NOTE! When ALTER VIEW - also change file \GOV Tolk\Tolk.BusinessLogic\Data\Views\RequestListRows.sql

SELECT
	1 RowType
   ,r.RequestId EntityId
   ,CASE WHEN (r.LastAcceptAt IS NOT NULL AND r.AcceptedAt IS NULL) THEN [LastAcceptAt] ELSE r.ExpiresAt END as ExpiresAt
   ,COALESCE(l.Name, o.OtherLanguage) LanguageName
   ,o.LanguageId
   ,o.OrderNumber EntityNumber
   ,og.OrderGroupNumber EntityParentNumber
   ,re.Name RegionName
   ,o.RegionId
   ,CASE WHEN r.RespondedStartAt IS NOT NULL THEN r.RespondedStartAt ELSE o.StartAt END ''StartAt''
   ,CASE WHEN r.RespondedStartAt IS NOT NULL THEN DATEADD(MINUTE, (DATEPART(HOUR, o.ExpectedLength) * 60) + DATEPART(MINUTE, o.ExpectedLength), r.RespondedStartAt) ELSE o.EndAt END ''EndAt''
   ,r.Status
   ,ra.BrokerId
   ,r.CreatedAt
   ,c.Name CustomerName
   ,o.CustomerOrganisationId
   ,o.OrderGroupId
   ,o.CustomerReferenceNumber
   ,r.AnsweredBy
   ,r.BrokerReferenceNumber
FROM Requests r
JOIN Rankings ra
	ON ra.RankingId = r.RankingId
JOIN Brokers br
	ON br.BrokerId = ra.BrokerId
JOIN Orders o
	ON o.OrderId = r.OrderId
JOIN Regions re
	ON re.RegionId = o.RegionId
JOIN CustomerOrganisations c
	ON c.CustomerOrganisationId = o.CustomerOrganisationId
LEFT JOIN OrderGroups og
	ON og.OrderGroupId = o.OrderGroupId
LEFT JOIN Languages l
	ON l.LanguageId = o.LanguageId
WHERE r.Status NOT IN (13, 17, 18, 24)
UNION
SELECT
	2
   ,r.RequestGroupId
   ,CASE WHEN (r.LastAcceptAt IS NOT NULL AND r.AcceptedAt IS NULL) THEN [LastAcceptAt] ELSE r.ExpiresAt END as ExpiresAt
   ,COALESCE(l.Name, og.OtherLanguage)
   ,og.LanguageId
   ,og.OrderGroupNumber
   ,NULL
   ,re.Name RegionName
   ,og.RegionId
   ,(SELECT TOP 1
			_o.StartAt
		FROM Orders _o
		WHERE _o.OrderGroupId = og.OrderGroupId
		ORDER BY _o.StartAt)
   ,(SELECT TOP 1
			_o.EndAt
		FROM Orders _o
		WHERE _o.OrderGroupId = og.OrderGroupId
		ORDER BY _o.StartAt)
   ,r.Status
   ,ra.BrokerId
   ,r.CreatedAt
   ,c.Name CustomerName
   ,og.CustomerOrganisationId
   ,og.OrderGroupId
   ,(SELECT TOP 1
			_o.CustomerReferenceNumber
		FROM Orders _o
		WHERE _o.OrderGroupId = og.OrderGroupId
		ORDER BY _o.StartAt)
   ,r.AnsweredBy
   ,r.BrokerReferenceNumber
FROM RequestGroups r
JOIN Rankings ra
	ON ra.RankingId = r.RankingId
JOIN Brokers br
	ON br.BrokerId = ra.BrokerId
JOIN OrderGroups og
	ON og.OrderGroupId = r.OrderGroupId
JOIN Regions re
	ON re.RegionId = og.RegionId
JOIN CustomerOrganisations c
	ON c.CustomerOrganisationId = og.CustomerOrganisationId
LEFT JOIN Languages l
	ON l.LanguageId = og.LanguageId
WHERE r.Status NOT IN (13, 17, 18, 24)');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"Exec('ALTER VIEW [dbo].[RequestListRows]
AS

--NOTE! When ALTER VIEW - also change file \GOV Tolk\Tolk.BusinessLogic\Data\Views\RequestListRows.sql

SELECT
	1 RowType
   ,r.RequestId EntityId
   ,r.ExpiresAt
   ,COALESCE(l.Name, o.OtherLanguage) LanguageName
   ,o.LanguageId
   ,o.OrderNumber EntityNumber
   ,og.OrderGroupNumber EntityParentNumber
   ,re.Name RegionName
   ,o.RegionId
   ,CASE WHEN r.RespondedStartAt IS NOT NULL THEN r.RespondedStartAt ELSE o.StartAt END ''StartAt''
   ,CASE WHEN r.RespondedStartAt IS NOT NULL THEN DATEADD(MINUTE, (DATEPART(HOUR, o.ExpectedLength) * 60) + DATEPART(MINUTE, o.ExpectedLength), r.RespondedStartAt) ELSE o.EndAt END ''EndAt''
   ,r.Status
   ,ra.BrokerId
   ,r.CreatedAt
   ,c.Name CustomerName
   ,o.CustomerOrganisationId
   ,o.OrderGroupId
   ,o.CustomerReferenceNumber
   ,r.AnsweredBy
   ,r.BrokerReferenceNumber
FROM Requests r
JOIN Rankings ra
	ON ra.RankingId = r.RankingId
JOIN Brokers br
	ON br.BrokerId = ra.BrokerId
JOIN Orders o
	ON o.OrderId = r.OrderId
JOIN Regions re
	ON re.RegionId = o.RegionId
JOIN CustomerOrganisations c
	ON c.CustomerOrganisationId = o.CustomerOrganisationId
LEFT JOIN OrderGroups og
	ON og.OrderGroupId = o.OrderGroupId
LEFT JOIN Languages l
	ON l.LanguageId = o.LanguageId
WHERE r.Status NOT IN (13, 17, 18, 24)
UNION
SELECT
	2
   ,r.RequestGroupId
   ,r.ExpiresAt
   ,COALESCE(l.Name, og.OtherLanguage)
   ,og.LanguageId
   ,og.OrderGroupNumber
   ,NULL
   ,re.Name RegionName
   ,og.RegionId
   ,(SELECT TOP 1
			_o.StartAt
		FROM Orders _o
		WHERE _o.OrderGroupId = og.OrderGroupId
		ORDER BY _o.StartAt)
   ,(SELECT TOP 1
			_o.EndAt
		FROM Orders _o
		WHERE _o.OrderGroupId = og.OrderGroupId
		ORDER BY _o.StartAt)
   ,r.Status
   ,ra.BrokerId
   ,r.CreatedAt
   ,c.Name CustomerName
   ,og.CustomerOrganisationId
   ,og.OrderGroupId
   ,(SELECT TOP 1
			_o.CustomerReferenceNumber
		FROM Orders _o
		WHERE _o.OrderGroupId = og.OrderGroupId
		ORDER BY _o.StartAt)
   ,r.AnsweredBy
   ,r.BrokerReferenceNumber
FROM RequestGroups r
JOIN Rankings ra
	ON ra.RankingId = r.RankingId
JOIN Brokers br
	ON br.BrokerId = ra.BrokerId
JOIN OrderGroups og
	ON og.OrderGroupId = r.OrderGroupId
JOIN Regions re
	ON re.RegionId = og.RegionId
JOIN CustomerOrganisations c
	ON c.CustomerOrganisationId = og.CustomerOrganisationId
LEFT JOIN Languages l
	ON l.LanguageId = og.LanguageId
WHERE r.Status NOT IN (13, 17, 18, 24)');");
        }
    }
}
