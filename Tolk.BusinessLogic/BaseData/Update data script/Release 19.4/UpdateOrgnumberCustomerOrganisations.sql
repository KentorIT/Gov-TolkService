

--TEST!
BEGIN TRAN

SELECT * FROM CustomerOrganisations co

UPDATE CustomerOrganisations SET OrganisationNumber = '202100-2742' WHERE Name = 'Domstolsverket'
UPDATE CustomerOrganisations SET OrganisationNumber = '202100-2742' WHERE ParentCustomerOrganisationId IS NOT NULL
UPDATE CustomerOrganisations SET OrganisationNumber = '202100-0076' WHERE Name = 'Polismyndigheten'
UPDATE CustomerOrganisations SET OrganisationNumber = '202100-6255' WHERE Name = 'Pensionsmyndigheten'
UPDATE CustomerOrganisations SET OrganisationNumber = '202100-2163' WHERE Name = 'Migrationsverket'
UPDATE CustomerOrganisations SET OrganisationNumber = '202100-0225' WHERE Name = 'Kriminalvården'
UPDATE CustomerOrganisations SET OrganisationNumber = '202100-2114' WHERE Name = 'Arbetsförmedlingen'
UPDATE CustomerOrganisations SET OrganisationNumber = '202100-5521' WHERE Name = 'Försäkringskassan'
UPDATE CustomerOrganisations SET OrganisationNumber = '202100-0829' WHERE Name = 'Kammarkollegiet'

UPDATE CustomerOrganisations SET OrganisationNumber = '000000-1111' WHERE OrganisationNumber = ''

SELECT * FROM CustomerOrganisations co

ROLLBACK TRAN 


--PRODUCTION
BEGIN TRAN

SELECT * FROM CustomerOrganisations co

UPDATE CustomerOrganisations SET OrganisationNumber = '202100-2742' WHERE Name = 'Domstolsverket'
UPDATE CustomerOrganisations SET OrganisationNumber = '202100-2742' WHERE ParentCustomerOrganisationId IS NOT NULL
UPDATE CustomerOrganisations SET OrganisationNumber = '202100-0076' WHERE Name = 'Polismyndigheten'
UPDATE CustomerOrganisations SET OrganisationNumber = '202100-6255' WHERE Name = 'Pensionsmyndigheten'
UPDATE CustomerOrganisations SET OrganisationNumber = '202100-4227' WHERE Name = 'Rättsmedicinalverket'

SELECT * FROM CustomerOrganisations co

ROLLBACK TRAN 