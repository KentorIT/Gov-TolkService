use TolkDev

insert Brokers
Values(1, 'Första förmedlingen', 'formedling1.se'),
(2, 'Andra förmedlingen', 'formedling2.se'),
(3, 'Tredje förmedlingen', 'formedling3.se'),
(4, 'Fjärde förmedlingen', 'formedling4.se')

Insert Rankings(BrokerId, RegionId, BrokerFee, Rank, FirstValidDate, LastValidDate)
Select 1, RegionId, 0.1, 1, '19990101', '29990101' from Regions

Insert Rankings(BrokerId, RegionId, BrokerFee, Rank, FirstValidDate, LastValidDate)
Select 2, RegionId, 0.12, 2, '19990101', '29990101' from Regions

Insert Rankings(BrokerId, RegionId, BrokerFee, Rank, FirstValidDate, LastValidDate)
Select 3, RegionId, 0.14, 3, '19990101', '29990101' from Regions

Insert Rankings(BrokerId, RegionId, BrokerFee, Rank, FirstValidDate, LastValidDate)
Select 4, RegionId, 0.16, 4, '19990101', '29990101' from Regions

