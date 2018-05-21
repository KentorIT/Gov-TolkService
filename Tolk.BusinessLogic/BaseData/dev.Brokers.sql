use TolkDev

insert Brokers
Values(1, 'Första förmedlingen'),
(2, 'Andra förmedlingen'),
(3, 'Tredje förmedlingen')

Insert BrokerRegions
Select RegionId, 1 from Regions

Insert BrokerRegions
Select RegionId, 2 from Regions

Insert BrokerRegions
Select RegionId, 3 from Regions

Insert InterpreterBrokerRegion
(RegionId, BrokerId, InterpreterId)
Select RegionId, BrokerId, 1 from BrokerRegions

Insert Rankings(BrokerId, RegionId, BrokerFee, Rank, StartDate, EndDate)
Select BrokerId, RegionId, 0.1, 1, '19990101', '29990101' from BrokerRegions
Where BrokerId = 1

Insert Rankings(BrokerId, RegionId, BrokerFee, Rank, StartDate, EndDate)
Select BrokerId, RegionId, 0.2, 2, '19990101', '29990101' from BrokerRegions
Where BrokerId = 2

Insert Rankings(BrokerId, RegionId, BrokerFee, Rank, StartDate, EndDate)
Select BrokerId, RegionId, 0.4, 3, '19990101', '29990101' from BrokerRegions
Where BrokerId = 3
