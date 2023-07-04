using Irony.Parsing;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tolk.BusinessLogic.Data.Migrations
{
    /// <inheritdoc />
    public partial class Updaet_BrokerFeeByServiceType_With_Price_Per_Region : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SELECT	
       PRICEROW.[Price]
      ,PRICEROW.[CompetenceLevel]
      ,PRICEROW.[InterpreterLocation]      
	  ,REGION.RegionId
	  ,RANKING.FirstValidDate as [RankingFirstValidDate]
	  ,RANKING.LastValidDate as [RankingLastValidDate]  
	  INTO #TempBrokerFeesByServiceType 
  FROM [dbo].[BrokerFeeByServiceTypePriceListRows] PRICEROW
  JOIN dbo.Regions REGION on PRICEROW.RegionGroupId = REGION.RegionGroupId 
  JOIN dbo.Rankings RANKING on REGION.RegionId = RANKING.RegionId AND RANKING.Rank = 1 AND RANKING.LastValidDate > GETDATE()

  DELETE FROM dbo.BrokerFeeByServiceTypePriceListRows

  INSERT INTO dbo.BrokerFeeByServiceTypePriceListRows(	   
       [Price]
      ,[CompetenceLevel]
      ,[InterpreterLocation]
      ,[FirstValidDate]
      ,[LastValidDate]
      ,[RegionId])
 SELECT 
	Price
	,CompetenceLevel
	,InterpreterLocation
	,RankingFirstValidDate
	,RankingLastValidDate
	,RegionId
	FROM #TempBrokerFeesByServiceType 
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM dbo.BrokerFeeByServiceTypePriceListRows
GO
INSERT [dbo].[BrokerFeeByServiceTypePriceListRows] ([Price], [CompetenceLevel], [InterpreterLocation], [FirstValidDate], [LastValidDate], [RegionGroupId]) VALUES (CAST(50.00 AS Decimal(10, 2)), 1, 1, CAST(N'2023-02-14' AS Date), CAST(N'2099-12-31' AS Date), 1)
GO
INSERT [dbo].[BrokerFeeByServiceTypePriceListRows] ([Price], [CompetenceLevel], [InterpreterLocation], [FirstValidDate], [LastValidDate], [RegionGroupId]) VALUES (CAST(60.00 AS Decimal(10, 2)), 2, 1, CAST(N'2023-02-14' AS Date), CAST(N'2099-12-31' AS Date), 1)
GO
INSERT [dbo].[BrokerFeeByServiceTypePriceListRows] ([Price], [CompetenceLevel], [InterpreterLocation], [FirstValidDate], [LastValidDate], [RegionGroupId]) VALUES (CAST(80.00 AS Decimal(10, 2)), 3, 1, CAST(N'2023-02-14' AS Date), CAST(N'2099-12-31' AS Date), 1)
GO
INSERT [dbo].[BrokerFeeByServiceTypePriceListRows] ([Price], [CompetenceLevel], [InterpreterLocation], [FirstValidDate], [LastValidDate], [RegionGroupId]) VALUES (CAST(90.00 AS Decimal(10, 2)), 4, 1, CAST(N'2023-02-14' AS Date), CAST(N'2099-12-31' AS Date), 1)
GO
INSERT [dbo].[BrokerFeeByServiceTypePriceListRows] ([Price], [CompetenceLevel], [InterpreterLocation], [FirstValidDate], [LastValidDate], [RegionGroupId]) VALUES (CAST(90.00 AS Decimal(10, 2)), 1, 1, CAST(N'2023-02-14' AS Date), CAST(N'2099-12-31' AS Date), 2)
GO
INSERT [dbo].[BrokerFeeByServiceTypePriceListRows] ([Price], [CompetenceLevel], [InterpreterLocation], [FirstValidDate], [LastValidDate], [RegionGroupId]) VALUES (CAST(100.00 AS Decimal(10, 2)), 2, 1, CAST(N'2023-02-14' AS Date), CAST(N'2099-12-31' AS Date), 2)
GO
INSERT [dbo].[BrokerFeeByServiceTypePriceListRows] ([Price], [CompetenceLevel], [InterpreterLocation], [FirstValidDate], [LastValidDate], [RegionGroupId]) VALUES (CAST(120.00 AS Decimal(10, 2)), 3, 1, CAST(N'2023-02-14' AS Date), CAST(N'2099-12-31' AS Date), 2)
GO
INSERT [dbo].[BrokerFeeByServiceTypePriceListRows] ([Price], [CompetenceLevel], [InterpreterLocation], [FirstValidDate], [LastValidDate], [RegionGroupId]) VALUES (CAST(130.00 AS Decimal(10, 2)), 4, 1, CAST(N'2023-02-14' AS Date), CAST(N'2099-12-31' AS Date), 2)
GO
INSERT [dbo].[BrokerFeeByServiceTypePriceListRows] ([Price], [CompetenceLevel], [InterpreterLocation], [FirstValidDate], [LastValidDate], [RegionGroupId]) VALUES (CAST(70.00 AS Decimal(10, 2)), 1, 1, CAST(N'2023-02-14' AS Date), CAST(N'2099-12-31' AS Date), 3)
GO
INSERT [dbo].[BrokerFeeByServiceTypePriceListRows] ([Price], [CompetenceLevel], [InterpreterLocation], [FirstValidDate], [LastValidDate], [RegionGroupId]) VALUES (CAST(80.00 AS Decimal(10, 2)), 2, 1, CAST(N'2023-02-14' AS Date), CAST(N'2099-12-31' AS Date), 3)
GO
INSERT [dbo].[BrokerFeeByServiceTypePriceListRows] ([Price], [CompetenceLevel], [InterpreterLocation], [FirstValidDate], [LastValidDate], [RegionGroupId]) VALUES (CAST(100.00 AS Decimal(10, 2)), 3, 1, CAST(N'2023-02-14' AS Date), CAST(N'2099-12-31' AS Date), 3)
GO
INSERT [dbo].[BrokerFeeByServiceTypePriceListRows] ([Price], [CompetenceLevel], [InterpreterLocation], [FirstValidDate], [LastValidDate], [RegionGroupId]) VALUES (CAST(110.00 AS Decimal(10, 2)), 4, 1, CAST(N'2023-02-14' AS Date), CAST(N'2099-12-31' AS Date), 3)
GO
INSERT [dbo].[BrokerFeeByServiceTypePriceListRows] ([Price], [CompetenceLevel], [InterpreterLocation], [FirstValidDate], [LastValidDate], [RegionGroupId]) VALUES (CAST(50.00 AS Decimal(10, 2)), 1, 4, CAST(N'2023-02-14' AS Date), CAST(N'2099-12-31' AS Date), 1)
GO
INSERT [dbo].[BrokerFeeByServiceTypePriceListRows] ([Price], [CompetenceLevel], [InterpreterLocation], [FirstValidDate], [LastValidDate], [RegionGroupId]) VALUES (CAST(60.00 AS Decimal(10, 2)), 2, 4, CAST(N'2023-02-14' AS Date), CAST(N'2099-12-31' AS Date), 1)
GO
INSERT [dbo].[BrokerFeeByServiceTypePriceListRows] ([Price], [CompetenceLevel], [InterpreterLocation], [FirstValidDate], [LastValidDate], [RegionGroupId]) VALUES (CAST(80.00 AS Decimal(10, 2)), 3, 4, CAST(N'2023-02-14' AS Date), CAST(N'2099-12-31' AS Date), 1)
GO
INSERT [dbo].[BrokerFeeByServiceTypePriceListRows] ([Price], [CompetenceLevel], [InterpreterLocation], [FirstValidDate], [LastValidDate], [RegionGroupId]) VALUES (CAST(90.00 AS Decimal(10, 2)), 4, 4, CAST(N'2023-02-14' AS Date), CAST(N'2099-12-31' AS Date), 1)
GO
INSERT [dbo].[BrokerFeeByServiceTypePriceListRows] ([Price], [CompetenceLevel], [InterpreterLocation], [FirstValidDate], [LastValidDate], [RegionGroupId]) VALUES (CAST(90.00 AS Decimal(10, 2)), 1, 4, CAST(N'2023-02-14' AS Date), CAST(N'2099-12-31' AS Date), 2)
GO
INSERT [dbo].[BrokerFeeByServiceTypePriceListRows] ([Price], [CompetenceLevel], [InterpreterLocation], [FirstValidDate], [LastValidDate], [RegionGroupId]) VALUES (CAST(100.00 AS Decimal(10, 2)), 2, 4, CAST(N'2023-02-14' AS Date), CAST(N'2099-12-31' AS Date), 2)
GO
INSERT [dbo].[BrokerFeeByServiceTypePriceListRows] ([Price], [CompetenceLevel], [InterpreterLocation], [FirstValidDate], [LastValidDate], [RegionGroupId]) VALUES (CAST(120.00 AS Decimal(10, 2)), 3, 4, CAST(N'2023-02-14' AS Date), CAST(N'2099-12-31' AS Date), 2)
GO
INSERT [dbo].[BrokerFeeByServiceTypePriceListRows] ([Price], [CompetenceLevel], [InterpreterLocation], [FirstValidDate], [LastValidDate], [RegionGroupId]) VALUES (CAST(130.00 AS Decimal(10, 2)), 4, 4, CAST(N'2023-02-14' AS Date), CAST(N'2099-12-31' AS Date), 2)
GO
INSERT [dbo].[BrokerFeeByServiceTypePriceListRows] ([Price], [CompetenceLevel], [InterpreterLocation], [FirstValidDate], [LastValidDate], [RegionGroupId]) VALUES (CAST(70.00 AS Decimal(10, 2)), 1, 4, CAST(N'2023-02-14' AS Date), CAST(N'2099-12-31' AS Date), 3)
GO
INSERT [dbo].[BrokerFeeByServiceTypePriceListRows] ([Price], [CompetenceLevel], [InterpreterLocation], [FirstValidDate], [LastValidDate], [RegionGroupId]) VALUES (CAST(80.00 AS Decimal(10, 2)), 2, 4, CAST(N'2023-02-14' AS Date), CAST(N'2099-12-31' AS Date), 3)
GO
INSERT [dbo].[BrokerFeeByServiceTypePriceListRows] ([Price], [CompetenceLevel], [InterpreterLocation], [FirstValidDate], [LastValidDate], [RegionGroupId]) VALUES (CAST(100.00 AS Decimal(10, 2)), 3, 4, CAST(N'2023-02-14' AS Date), CAST(N'2099-12-31' AS Date), 3)
GO
INSERT [dbo].[BrokerFeeByServiceTypePriceListRows] ([Price], [CompetenceLevel], [InterpreterLocation], [FirstValidDate], [LastValidDate], [RegionGroupId]) VALUES (CAST(110.00 AS Decimal(10, 2)), 4, 4, CAST(N'2023-02-14' AS Date), CAST(N'2099-12-31' AS Date), 3)
GO
INSERT [dbo].[BrokerFeeByServiceTypePriceListRows] ([Price], [CompetenceLevel], [InterpreterLocation], [FirstValidDate], [LastValidDate], [RegionGroupId]) VALUES (CAST(20.00 AS Decimal(10, 2)), 1, 2, CAST(N'2023-02-14' AS Date), CAST(N'2099-12-31' AS Date), 1)
GO
INSERT [dbo].[BrokerFeeByServiceTypePriceListRows] ([Price], [CompetenceLevel], [InterpreterLocation], [FirstValidDate], [LastValidDate], [RegionGroupId]) VALUES (CAST(30.00 AS Decimal(10, 2)), 2, 2, CAST(N'2023-02-14' AS Date), CAST(N'2099-12-31' AS Date), 1)
GO
INSERT [dbo].[BrokerFeeByServiceTypePriceListRows] ([Price], [CompetenceLevel], [InterpreterLocation], [FirstValidDate], [LastValidDate], [RegionGroupId]) VALUES (CAST(50.00 AS Decimal(10, 2)), 3, 2, CAST(N'2023-02-14' AS Date), CAST(N'2099-12-31' AS Date), 1)
GO
INSERT [dbo].[BrokerFeeByServiceTypePriceListRows] ([Price], [CompetenceLevel], [InterpreterLocation], [FirstValidDate], [LastValidDate], [RegionGroupId]) VALUES (CAST(60.00 AS Decimal(10, 2)), 4, 2, CAST(N'2023-02-14' AS Date), CAST(N'2099-12-31' AS Date), 1)
GO
INSERT [dbo].[BrokerFeeByServiceTypePriceListRows] ([Price], [CompetenceLevel], [InterpreterLocation], [FirstValidDate], [LastValidDate], [RegionGroupId]) VALUES (CAST(20.00 AS Decimal(10, 2)), 1, 2, CAST(N'2023-02-14' AS Date), CAST(N'2099-12-31' AS Date), 2)
GO
INSERT [dbo].[BrokerFeeByServiceTypePriceListRows] ([Price], [CompetenceLevel], [InterpreterLocation], [FirstValidDate], [LastValidDate], [RegionGroupId]) VALUES (CAST(30.00 AS Decimal(10, 2)), 2, 2, CAST(N'2023-02-14' AS Date), CAST(N'2099-12-31' AS Date), 2)
GO
INSERT [dbo].[BrokerFeeByServiceTypePriceListRows] ([Price], [CompetenceLevel], [InterpreterLocation], [FirstValidDate], [LastValidDate], [RegionGroupId]) VALUES (CAST(50.00 AS Decimal(10, 2)), 3, 2, CAST(N'2023-02-14' AS Date), CAST(N'2099-12-31' AS Date), 2)
GO
INSERT [dbo].[BrokerFeeByServiceTypePriceListRows] ([Price], [CompetenceLevel], [InterpreterLocation], [FirstValidDate], [LastValidDate], [RegionGroupId]) VALUES (CAST(60.00 AS Decimal(10, 2)), 4, 2, CAST(N'2023-02-14' AS Date), CAST(N'2099-12-31' AS Date), 2)
GO
INSERT [dbo].[BrokerFeeByServiceTypePriceListRows] ([Price], [CompetenceLevel], [InterpreterLocation], [FirstValidDate], [LastValidDate], [RegionGroupId]) VALUES (CAST(20.00 AS Decimal(10, 2)), 1, 2, CAST(N'2023-02-14' AS Date), CAST(N'2099-12-31' AS Date), 3)
GO
INSERT [dbo].[BrokerFeeByServiceTypePriceListRows] ([Price], [CompetenceLevel], [InterpreterLocation], [FirstValidDate], [LastValidDate], [RegionGroupId]) VALUES (CAST(30.00 AS Decimal(10, 2)), 2, 2, CAST(N'2023-02-14' AS Date), CAST(N'2099-12-31' AS Date), 3)
GO
INSERT [dbo].[BrokerFeeByServiceTypePriceListRows] ([Price], [CompetenceLevel], [InterpreterLocation], [FirstValidDate], [LastValidDate], [RegionGroupId]) VALUES (CAST(50.00 AS Decimal(10, 2)), 3, 2, CAST(N'2023-02-14' AS Date), CAST(N'2099-12-31' AS Date), 3)
GO
INSERT [dbo].[BrokerFeeByServiceTypePriceListRows] ([Price], [CompetenceLevel], [InterpreterLocation], [FirstValidDate], [LastValidDate], [RegionGroupId]) VALUES (CAST(60.00 AS Decimal(10, 2)), 4, 2, CAST(N'2023-02-14' AS Date), CAST(N'2099-12-31' AS Date), 3)
GO
INSERT [dbo].[BrokerFeeByServiceTypePriceListRows] ([Price], [CompetenceLevel], [InterpreterLocation], [FirstValidDate], [LastValidDate], [RegionGroupId]) VALUES (CAST(20.00 AS Decimal(10, 2)), 1, 3, CAST(N'2023-02-14' AS Date), CAST(N'2099-12-31' AS Date), 1)
GO
INSERT [dbo].[BrokerFeeByServiceTypePriceListRows] ([Price], [CompetenceLevel], [InterpreterLocation], [FirstValidDate], [LastValidDate], [RegionGroupId]) VALUES (CAST(30.00 AS Decimal(10, 2)), 2, 3, CAST(N'2023-02-14' AS Date), CAST(N'2099-12-31' AS Date), 1)
GO
INSERT [dbo].[BrokerFeeByServiceTypePriceListRows] ([Price], [CompetenceLevel], [InterpreterLocation], [FirstValidDate], [LastValidDate], [RegionGroupId]) VALUES (CAST(50.00 AS Decimal(10, 2)), 3, 3, CAST(N'2023-02-14' AS Date), CAST(N'2099-12-31' AS Date), 1)
GO
INSERT [dbo].[BrokerFeeByServiceTypePriceListRows] ([Price], [CompetenceLevel], [InterpreterLocation], [FirstValidDate], [LastValidDate], [RegionGroupId]) VALUES (CAST(60.00 AS Decimal(10, 2)), 4, 3, CAST(N'2023-02-14' AS Date), CAST(N'2099-12-31' AS Date), 1)
GO
INSERT [dbo].[BrokerFeeByServiceTypePriceListRows] ([Price], [CompetenceLevel], [InterpreterLocation], [FirstValidDate], [LastValidDate], [RegionGroupId]) VALUES (CAST(20.00 AS Decimal(10, 2)), 1, 3, CAST(N'2023-02-14' AS Date), CAST(N'2099-12-31' AS Date), 2)
GO
INSERT [dbo].[BrokerFeeByServiceTypePriceListRows] ([Price], [CompetenceLevel], [InterpreterLocation], [FirstValidDate], [LastValidDate], [RegionGroupId]) VALUES (CAST(30.00 AS Decimal(10, 2)), 2, 3, CAST(N'2023-02-14' AS Date), CAST(N'2099-12-31' AS Date), 2)
GO
INSERT [dbo].[BrokerFeeByServiceTypePriceListRows] ([Price], [CompetenceLevel], [InterpreterLocation], [FirstValidDate], [LastValidDate], [RegionGroupId]) VALUES (CAST(50.00 AS Decimal(10, 2)), 3, 3, CAST(N'2023-02-14' AS Date), CAST(N'2099-12-31' AS Date), 2)
GO
INSERT [dbo].[BrokerFeeByServiceTypePriceListRows] ([Price], [CompetenceLevel], [InterpreterLocation], [FirstValidDate], [LastValidDate], [RegionGroupId]) VALUES (CAST(60.00 AS Decimal(10, 2)), 4, 3, CAST(N'2023-02-14' AS Date), CAST(N'2099-12-31' AS Date), 2)
GO
INSERT [dbo].[BrokerFeeByServiceTypePriceListRows] ([Price], [CompetenceLevel], [InterpreterLocation], [FirstValidDate], [LastValidDate], [RegionGroupId]) VALUES (CAST(20.00 AS Decimal(10, 2)), 1, 3, CAST(N'2023-02-14' AS Date), CAST(N'2099-12-31' AS Date), 3)
GO
INSERT [dbo].[BrokerFeeByServiceTypePriceListRows] ([Price], [CompetenceLevel], [InterpreterLocation], [FirstValidDate], [LastValidDate], [RegionGroupId]) VALUES (CAST(30.00 AS Decimal(10, 2)), 2, 3, CAST(N'2023-02-14' AS Date), CAST(N'2099-12-31' AS Date), 3)
GO
INSERT [dbo].[BrokerFeeByServiceTypePriceListRows] ([Price], [CompetenceLevel], [InterpreterLocation], [FirstValidDate], [LastValidDate], [RegionGroupId]) VALUES (CAST(50.00 AS Decimal(10, 2)), 3, 3, CAST(N'2023-02-14' AS Date), CAST(N'2099-12-31' AS Date), 3)
GO
INSERT [dbo].[BrokerFeeByServiceTypePriceListRows] ([Price], [CompetenceLevel], [InterpreterLocation], [FirstValidDate], [LastValidDate], [RegionGroupId]) VALUES (CAST(60.00 AS Decimal(10, 2)), 4, 3, CAST(N'2023-02-14' AS Date), CAST(N'2099-12-31' AS Date), 3)
GO
");
        }
    }
}
