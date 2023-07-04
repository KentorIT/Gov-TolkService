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