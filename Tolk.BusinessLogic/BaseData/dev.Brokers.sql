use TolkDev

insert Brokers
Values(1, 'F�rsta f�rmedlingen'),
(2, 'Andra f�rmedlingen'),
(3, 'Tredje f�rmedlingen')

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
Select BrokerId, RegionId, 1.10, 1, '19990101', '29990101' from BrokerRegions
Where BrokerId = 1

Insert Rankings(BrokerId, RegionId, BrokerFee, Rank, StartDate, EndDate)
Select BrokerId, RegionId, 1.2, 2, '19990101', '29990101' from BrokerRegions
Where BrokerId = 2

Insert Rankings(BrokerId, RegionId, BrokerFee, Rank, StartDate, EndDate)
Select BrokerId, RegionId, 1.4, 3, '19990101', '29990101' from BrokerRegions
Where BrokerId = 3
