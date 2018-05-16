use TolkDev

insert Brokers
Values(1, 'Förmedling'),
(2, 'De som förmedlar'),
(3, 'Förmedling av tolkar')

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
Select BrokerId, RegionId, 10, 1, '19990101', '29990101' from BrokerRegions
Where BrokerId = 1

Insert Rankings(BrokerId, RegionId, BrokerFee, Rank, StartDate, EndDate)
Select BrokerId, RegionId, 12, 2, '19990101', '29990101' from BrokerRegions
Where BrokerId = 2

Insert Rankings(BrokerId, RegionId, BrokerFee, Rank, StartDate, EndDate)
Select BrokerId, RegionId, 14, 3, '19990101', '29990101' from BrokerRegions
Where BrokerId = 3
