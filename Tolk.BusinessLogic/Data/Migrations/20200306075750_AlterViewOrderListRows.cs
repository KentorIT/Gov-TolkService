using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AlterViewOrderListRows : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"Exec('ALTER VIEW[dbo].[OrderListRows]
            AS

            SELECT
                1 ''RowType''
                ,o.OrderId ''EntityId''
                ,COALESCE(l.Name, o.OtherLanguage) ''LanguageName''
                ,o.LanguageId
                ,o.OrderNumber ''EntityNumber''
                ,og.OrderGroupNumber ''EntityParentNumber''
                ,r.Name ''RegionName''
                ,o.RegionId
                ,o.StartAt
                ,o.EndAt
                ,o.Status
                ,u.NameFirst + '' '' + u.NameFamily ''CreatorName''
                ,o.CreatedBy
                ,br.Name ''BrokerName''
                ,ra.BrokerId
                ,o.CustomerUnitId
                ,o.CreatedAt
                ,c.Name ''CustomerName''
                ,o.CustomerOrganisationId
                ,CONVERT(BIT, COALESCE(cu.IsActive, 0)) ''CustomerUnitIsActive''
                ,o.OrderGroupId
                ,o.CustomerReferenceNumber
                ,o.ContactPersonId
            FROM Orders o
            JOIN Regions r
                ON r.RegionId = o.RegionId
            JOIN AspNetUsers u
                ON u.Id = o.CreatedBy
            JOIN CustomerOrganisations c
                ON c.CustomerOrganisationId = o.CustomerOrganisationId
            LEFT JOIN OrderGroups og
                ON og.OrderGroupId = o.OrderGroupId
            LEFT JOIN Languages l
                ON l.LanguageId = o.LanguageId
            LEFT JOIN Requests req
                ON req.OrderId = o.OrderId
                AND req.Status IN (1, 2, 3, 4, 5, 6, 10, 12, 14, 16)
            LEFT JOIN Rankings ra
                ON ra.RankingId = req.RankingId
            LEFT JOIN Brokers br
                ON br.BrokerId = ra.BrokerId
            LEFT JOIN CustomerUnits cu
                ON cu.CustomerUnitId = o.CustomerUnitId
            UNION
            
            SELECT
                2
                ,o.OrderGroupId
                ,COALESCE(l.Name, o.OtherLanguage)
                ,o.LanguageId
                ,o.OrderGroupNumber
                ,NULL
                ,r.Name
                ,o.RegionId
                ,(SELECT TOP 1 
                    _o.StartAt
                FROM Orders _o
                WHERE _o.OrderGroupId = o.OrderGroupId
                ORDER BY _o.StartAt)
                ,(SELECT TOP 1
	                _o.EndAt
                FROM Orders _o
                WHERE _o.OrderGroupId = o.OrderGroupId
                ORDER BY _o.StartAt)
                ,o.Status
                ,u.NameFirst + '' '' + u.NameFamily
                ,o.CreatedBy
                ,br.Name
                ,ra.BrokerId
                ,o.CustomerUnitId
                ,o.CreatedAt
                ,c.Name
                ,o.CustomerOrganisationId
                ,CONVERT(BIT, COALESCE(cu.IsActive, 0))
                ,NULL
                ,(SELECT TOP 1
	                _o.CustomerReferenceNumber
                FROM Orders _o
                WHERE _o.OrderGroupId = o.OrderGroupId
                ORDER BY _o.StartAt)
                ,NULL
            FROM OrderGroups o
            JOIN Regions r
                ON r.RegionId = o.RegionId
            JOIN AspNetUsers u
                ON u.Id = o.CreatedBy
            JOIN CustomerOrganisations c
                ON c.CustomerOrganisationId = o.CustomerOrganisationId
            LEFT JOIN Languages l
                ON l.LanguageId = o.LanguageId
            LEFT JOIN RequestGroups req
                ON req.OrderGroupId = o.OrderGroupId
            AND req.Status IN (1, 2, 3, 4, 5, 6, 10, 12, 14, 16)
            LEFT JOIN Rankings ra
                ON ra.RankingId = req.RankingId
            LEFT JOIN Brokers br
                ON br.BrokerId = ra.BrokerId
            LEFT JOIN CustomerUnits cu
                ON cu.CustomerUnitId = o.CustomerUnitId');");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"Exec('ALTER VIEW OrderListRows
As

Select 
	1 ''RowType'',
	o.OrderId ''EntityId'',
	coalesce(l.Name, o.OtherLanguage) ''LanguageName'',
	o.LanguageId,
    o.OrderNumber ''EntityNumber'',
    og.OrderGroupNumber ''EntityParentNumber'',
	r.Name ''RegionName'',
	o.RegionId,
	o.StartAt,
	o.EndAt,
	o.Status,
	u.NameFirst + '' '' + u.NameFamily ''CreatorName'',
	o.CreatedBy,
	br.Name	''BrokerName'',
	ra.BrokerId,
	o.CustomerUnitId,
	o.CreatedAt,
	c.Name ''CustomerName'',
	o.CustomerOrganisationId,
	CONVERT(bit, coalesce(cu.IsActive, 0)) ''CustomerUnitIsActive'',
	o.OrderGroupId, 
	o.CustomerReferenceNumber,
	o.ContactPersonId
From Orders o 
Join Regions r
On r.RegionId = o.RegionId
Join AspNetUsers u
On u.Id = o.CreatedBy
Join CustomerOrganisations c
On c.CustomerOrganisationId = o.CustomerOrganisationId
Left Join OrderGroups og
On og.OrderGroupId = o.OrderGroupId
Left Join Languages l
On l.LanguageId = o.LanguageId
Left Join Requests req 
On req.OrderId = o.OrderId And req.Status In(1,2,4,5,12,17)
Left Join Rankings ra 
On ra.RankingId = req.RankingId
Left Join Brokers br
On br.BrokerId = ra.BrokerId
Left Join CustomerUnits cu
On cu.CustomerUnitId = o.CustomerUnitId
union

Select 
	2,
	o.OrderGroupId,
	coalesce(l.Name, o.OtherLanguage),
	o.LanguageId,
    o.OrderGroupNumber,
	null,
	r.Name,
	o.RegionId,
	(Select top 1 _o.StartAt From Orders _o Where _o.OrderGroupId = o.OrderGroupId ORder By  _o.StartAt ),
	(Select top 1 _o.EndAt From Orders _o Where _o.OrderGroupId = o.OrderGroupId ORder By  _o.StartAt ),
	o.Status,
	u.NameFirst + '' '' + u.NameFamily,
	o.CreatedBy,
	br.Name	,
	ra.BrokerId,
	o.CustomerUnitId,
	o.CreatedAt,
	c.Name,
	o.CustomerOrganisationId,
	CONVERT(bit, coalesce(cu.IsActive, 0)),
	null,
	(Select top 1 _o.CustomerReferenceNumber From Orders _o Where _o.OrderGroupId = o.OrderGroupId ORder By  _o.StartAt ),
	null
From OrderGroups o 
Join Regions r
On r.RegionId = o.RegionId
Join AspNetUsers u
On u.Id = o.CreatedBy
Join CustomerOrganisations c
On c.CustomerOrganisationId = o.CustomerOrganisationId
Left Join Languages l
On l.LanguageId = o.LanguageId
Left Join RequestGroups req 
On req.OrderGroupId = o.OrderGroupId And req.Status In(1,2,4,5,12,17)
Left Join Rankings ra 
On ra.RankingId = req.RankingId
Left Join Brokers br
On br.BrokerId = ra.BrokerId
Left Join CustomerUnits cu
On cu.CustomerUnitId = o.CustomerUnitId
');");

        }
    }
}
