use TolkDev

insert Brokers
Values(1, 'F�rmedling'),
(2, 'De som f�rmedlar'),
(3, 'F�rmedling av tolkar')

Insert BrokerRegions
Select RegionId, 1 from Regions


Insert BrokerRegions
Select RegionId, 2 from Regions


Insert BrokerRegions
Select RegionId, 3 from Regions

Insert InterpreterBrokerRegion
Select BrokerRegionId, '51470982-4b9c-4202-b3d9-f8404ed5cb81' from BrokerRegions


Insert Rankings(BrokerRegionId, BrokerFee, Rank, StartDate, EndDate)
Select BrokerRegionId, 10, 1, '19990101', '29990101' from BrokerRegions
Where BrokerId = 1


Insert Rankings(BrokerRegionId, BrokerFee, Rank, StartDate, EndDate)
Select BrokerRegionId, 12, 2, '19990101', '29990101' from BrokerRegions
Where BrokerId = 2


Insert Rankings(BrokerRegionId, BrokerFee, Rank, StartDate, EndDate)
Select BrokerRegionId, 14, 3, '19990101', '29990101' from BrokerRegions
Where BrokerId = 3

-- Connect users (frida to F�rmedling) 
-- frida 
-- Add users for all customers
-- Add table for connection 
-- Create rankings
-- 
