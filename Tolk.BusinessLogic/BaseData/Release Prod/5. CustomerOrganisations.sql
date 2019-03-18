

SET IDENTITY_INSERT CustomerOrganisations ON

INSERT CustomerOrganisations (CustomerOrganisationId, Name, PriceListType, EmailDomain, ParentCustomerOrganisationId, OrganizationPrefix)
	VALUES 
	(1, 'Domstolsverket', 1, 'dom.se', NULL, 'Dom')
	,(2, 'Pensionsmyndigheten', 2, 'pensionsmyndigheten.se', NULL, 'PM')
	,(3, 'Södertörns tingsrätt', 1, 'dom.se', 1, 'SodTing')
	--,(4, 'Förvaltningsrätten i Stockholm', 1, 'forvaltningsrattenistockholm.domstol.se', 1, 'ForvSthm')
	--,(5, 'Polismyndigheten', 2, 'polisen.se', NULL, 'Pol')
	--,(6, 'Migrationsverket', 2, 'migrationsverket.se', NULL, 'Mig')

SET IDENTITY_INSERT CustomerOrganisations OFF

