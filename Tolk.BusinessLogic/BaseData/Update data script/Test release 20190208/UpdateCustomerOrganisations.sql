
--dessa ska köras vid nästa release till test. 
--Obs olika script för TolkSysTest och TolkProd
--Innehåller Rollback


--ska köras i TolkSysTest 

--kolla värden
SELECT * FROM CustomerOrganisations co

BEGIN TRAN

USE TolkSysTest

INSERT CustomerOrganisations (Name, PriceListType, EmailDomain, ParentCustomerOrganisationId)
	VALUES 
	('Södertörns tingsrätt', 1, 'sodertornstingsratt.domstol.se', 3),
	('Förvaltningsrätten i Stockholm', 1, 'forvaltningsrattenistockholm.domstol.se', 3)

	SELECT * FROM CustomerOrganisations co

ROLLBACK TRAN



--ska köras i Utbildning/TolkProd

--kolla värden
SELECT * FROM CustomerOrganisations co

BEGIN TRAN

USE TolkProd

--sätt Domstolsverket till parent på Förvaltningsrätten i Stockholm och Södertörns tingsrätt
UPDATE CustomerOrganisations SET ParentCustomerOrganisationId = 2 WHERE CustomerOrganisationId IN (4, 5) 

--ändra till riktiga namnet på Förvaltningsrätten i Stockholm
UPDATE CustomerOrganisations SET Name = 'Förvaltningsrätten i Stockholm' WHERE CustomerOrganisationId IN (5) 

SELECT * FROM CustomerOrganisations co

ROLLBACK TRAN