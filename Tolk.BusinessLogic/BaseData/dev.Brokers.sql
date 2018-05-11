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

Insert UserBroker(UserId, BrokerId) 
Select 'e5d21235-232a-43bd-9a64-8ab6cf6e15e3', 1

Insert UserCustomerOrganisation(UserId, CustomerOrganisationId)
Select '3202981c-166d-46b2-9d3d-894d24e07320', 1

Insert UserCustomerOrganisation(UserId, CustomerOrganisationId)
Select '7e1a3d87-ca74-43b5-9739-b5530fd18732', 2


-- Connect users (frida to Förmedling) 
-- frida 
-- Add users for all customers
-- Add table for connection 
-- Create rankings
-- 
