USE TolkDev

SET IDENTITY_INSERT CustomerOrganisations ON

INSERT CustomerOrganisations (CustomerOrganisationId, Name, PriceListType, EmailDomain, ParentCustomerOrganisationId)
	VALUES 
	(1, 'Polismyndigheten', 2, 'polisen.se', NULL),
	(2, 'Migrationsverket', 2, 'migrationsverket.se', NULL),
	(3, 'Domstolsverket', 1, 'dom.se', NULL),
	(4, 'Sopra Steria', 2, 'soprasteria.com', NULL),
	(5, 'Södertörns tingsrätt', 1, 'sodertorns.domstol.se', 3),
	(6, 'Förvaltningsrätten i Stockholm', 1, 'forvaltningsrattenistockholm.domstol.se', 3)

SET IDENTITY_INSERT CustomerOrganisations OFF
