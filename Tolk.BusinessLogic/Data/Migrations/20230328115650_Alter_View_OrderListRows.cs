using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class Alter_View_OrderListRows : Migration
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
                ,CASE WHEN req.RespondedStartAt IS NOT NULL THEN req.RespondedStartAt ELSE o.StartAt END ''StartAt''
				,CASE WHEN req.RespondedStartAt IS NOT NULL THEN DATEADD(MINUTE, (DATEPART(HOUR, o.ExpectedLength) * 60) + DATEPART(MINUTE, o.ExpectedLength), req.RespondedStartAt) ELSE o.EndAt END ''EndAt''
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
                AND req.Status IN (1, 2, 3, 4, 5, 6, 10, 12, 14, 16, 23)
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
            AND req.Status IN (1, 2, 3, 4, 5, 6, 10, 12, 14, 16, 23)
            LEFT JOIN Rankings ra
                ON ra.RankingId = req.RankingId
            LEFT JOIN Brokers br
                ON br.BrokerId = ra.BrokerId
            LEFT JOIN CustomerUnits cu
                ON cu.CustomerUnitId = o.CustomerUnitId');");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
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
                AND req.Status IN (1, 2, 3, 4, 5, 6, 10, 12, 14, 16, 23)
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
            AND req.Status IN (1, 2, 3, 4, 5, 6, 10, 12, 14, 16, 23)
            LEFT JOIN Rankings ra
                ON ra.RankingId = req.RankingId
            LEFT JOIN Brokers br
                ON br.BrokerId = ra.BrokerId
            LEFT JOIN CustomerUnits cu
                ON cu.CustomerUnitId = o.CustomerUnitId');");
        }
    }
}
