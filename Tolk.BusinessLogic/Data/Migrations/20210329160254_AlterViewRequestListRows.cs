using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AlterViewRequestListRows : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
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
   ,o.StartAt
   ,o.EndAt
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
WHERE r.Status NOT IN (13, 17, 18)
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
WHERE r.Status NOT IN (13, 17, 18)');");
        }


        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"Exec('ALTER VIEW[dbo].[RequestListRows]
			As

Select
	1 ''RowType'',
	r.RequestId ''EntityId'',
	r.ExpiresAt,
	coalesce(l.Name, o.OtherLanguage) ''LanguageName'',
	o.LanguageId,
    o.OrderNumber ''EntityNumber'',
    og.OrderGroupNumber ''EntityParentNumber'',
	re.Name ''RegionName'',
	o.RegionId,
	o.StartAt,
	o.EndAt,
	r.Status,
	ra.BrokerId,
	r.CreatedAt,
	c.Name ''CustomerName'',
	o.CustomerOrganisationId,
	o.OrderGroupId, 
	o.CustomerReferenceNumber,
	r.AnsweredBy
From Requests r
Join Rankings ra
On ra.RankingId = r.RankingId
Join Brokers br
On br.BrokerId = ra.BrokerId
Join Orders o
on o.OrderId = r.OrderId
Join Regions re
On re.RegionId = o.RegionId
Join CustomerOrganisations c
On c.CustomerOrganisationId = o.CustomerOrganisationId
Left Join OrderGroups og
On og.OrderGroupId = o.OrderGroupId
Left Join Languages l
On l.LanguageId = o.LanguageId
Where r.Status Not In(13, 17, 18)
union
Select
	2,
	r.RequestGroupId,
	r.ExpiresAt,
	coalesce(l.Name, og.OtherLanguage),
	og.LanguageId,
    og.OrderGroupNumber,
    null,
	re.Name ''RegionName'',
	og.RegionId,
	(Select top 1 _o.StartAt From Orders _o Where _o.OrderGroupId = og.OrderGroupId Order By  _o.StartAt ),
	(Select top 1 _o.EndAt From Orders _o Where _o.OrderGroupId = og.OrderGroupId Order By  _o.StartAt ),
	r.Status,
	ra.BrokerId,
	r.CreatedAt,
	c.Name ''CustomerName'',
	og.CustomerOrganisationId,
	og.OrderGroupId, 
	(Select top 1 _o.CustomerReferenceNumber From Orders _o Where _o.OrderGroupId = og.OrderGroupId Order By  _o.StartAt ),
	r.AnsweredBy
From RequestGroups r
Join Rankings ra
On ra.RankingId = r.RankingId
Join Brokers br
On br.BrokerId = ra.BrokerId
Join OrderGroups og
on og.OrderGroupId = r.OrderGroupId
Join Regions re
On re.RegionId = og.RegionId
Join CustomerOrganisations c
On c.CustomerOrganisationId = og.CustomerOrganisationId
Left Join Languages l
On l.LanguageId = og.LanguageId
Where r.Status Not In(13, 17, 18)');");

        }
    }
}
