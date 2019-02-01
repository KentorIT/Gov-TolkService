use TolkDev

CREATE TABLE #Brokers(
	[BrokerId] [int] NOT NULL,
	[Name] [nvarchar](255) NOT NULL,
	[EmailDomain] [nvarchar](max) NULL,
	[EmailAddress] [nvarchar](255) NULL,
	[OrganizationNumber] [nvarchar](32) NULL,
	[OrganizationPrefix] [nvarchar](255) NULL
)
GO


insert #Brokers(BrokerId, Name, EmailDomain, EmailAddress, OrganizationNumber, OrganizationPrefix)
Values(1, 'Första förmedlingen', 'formedling1.se', 'avrop@formedling1.se', '550122-1525', 'FörFörm'),
(2, 'Andra förmedlingen', 'formedling2.se', 'avrop@formedling2.se', '560417-2896', 'AndFörm'),
(3, 'Tredje förmedlingen', 'formedling3.se', 'avrop@formedling3.se', '520901-4528', 'TreFörm'),
(4, 'Fjärde förmedlingen', 'formedling4.se', 'avrop@formedling4.se', '550108-5281', 'FjäFörm')


MERGE Brokers dst
USING #Brokers src
ON (src.BrokerId = dst.BrokerId)
WHEN MATCHED THEN
UPDATE SET dst.Name = src.Name, dst.EmailDomain = src.EmailDomain, dst.EmailAddress = src.EmailAddress, dst.OrganizationNumber = src.OrganizationNumber, dst.OrganizationPrefix = src.OrganizationPrefix
WHEN NOT MATCHED THEN
INSERT (BrokerId, Name, EmailDomain, EmailAddress, OrganizationNumber, OrganizationPrefix)
VALUES (src.BrokerId, src.Name, src.EmailDomain, src.EmailAddress, src.OrganizationNumber, src.OrganizationPrefix);

drop table #Brokers

Insert Rankings(BrokerId, RegionId, BrokerFee, Rank, FirstValidDate, LastValidDate)
Select 1, RegionId, 0.1, 1, '19990101', '99991231' from Regions

Insert Rankings(BrokerId, RegionId, BrokerFee, Rank, FirstValidDate, LastValidDate)
Select 2, RegionId, 0.12, 2, '19990101', '99991231' from Regions

Insert Rankings(BrokerId, RegionId, BrokerFee, Rank, FirstValidDate, LastValidDate)
Select 3, RegionId, 0.14, 3, '19990101', '99991231' from Regions

Insert Rankings(BrokerId, RegionId, BrokerFee, Rank, FirstValidDate, LastValidDate)
Select 4, RegionId, 0.16, 4, '19990101', '99991231' from Regions

