use TolkDev

insert Brokers
Values(1, 'Första förmedlingen'),
(2, 'Andra förmedlingen'),
(3, 'Tredje förmedlingen'),
(4, 'Fjärde förmedlingen')

Insert BrokerRegions
Select RegionId, 1 from Regions

Insert BrokerRegions
Select RegionId, 2 from Regions

Insert BrokerRegions
Select RegionId, 3 from Regions

Insert BrokerRegions
Select RegionId, 4 from Regions

Insert Rankings(BrokerId, RegionId, BrokerFee, Rank, FirstValidDate, LastValidDate)
Select BrokerId, RegionId, 0.1, 1, '19990101', '29990101' from BrokerRegions
Where BrokerId = 1

Insert Rankings(BrokerId, RegionId, BrokerFee, Rank, FirstValidDate, LastValidDate)
Select BrokerId, RegionId, 0.12, 2, '19990101', '29990101' from BrokerRegions
Where BrokerId = 2

Insert Rankings(BrokerId, RegionId, BrokerFee, Rank, FirstValidDate, LastValidDate)
Select BrokerId, RegionId, 0.14, 3, '19990101', '29990101' from BrokerRegions
Where BrokerId = 3

Insert Rankings(BrokerId, RegionId, BrokerFee, Rank, FirstValidDate, LastValidDate)
Select BrokerId, RegionId, 0.16, 3, '19990101', '29990101' from BrokerRegions
Where BrokerId = 4
