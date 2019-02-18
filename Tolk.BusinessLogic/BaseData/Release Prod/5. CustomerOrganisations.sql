

SET IDENTITY_INSERT CustomerOrganisations ON

INSERT CustomerOrganisations (CustomerOrganisationId, Name, PriceListType, EmailDomain, ParentCustomerOrganisationId, OrganisationPrefix)
	VALUES 
	(1,	'Kammarkollegiet',	2, 'kammarkollegiet.se', NULL, 'KamK'),
	(2, 'Domstolsverket', 1, 'dom.se', NULL, 'Dom'),
	(3, 'Pensionsmyndigheten', 2, 'pensionsmyndigheten.se', NULL, 'PM'),
	(4, 'Södertörns tingsrätt', 1, 'sodertornstingsratt.domstol.se', 2, 'SödTing'),
	(5, 'Förvaltningsrätten i Stockholm', 1, 'forvaltningsrattenistockholm.domstol.se', 2, 'FörvSthm')
	--(6, 'Polismyndigheten', 2, 'polisen.se', NULL, 'Pol'),
	--(7, 'Migrationsverket', 2, 'migrationsverket.se', NULL, 'Mig')

SET IDENTITY_INSERT CustomerOrganisations OFF

