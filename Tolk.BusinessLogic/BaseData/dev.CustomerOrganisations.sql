USE TolkDev

SET IDENTITY_INSERT CustomerOrganisations ON

INSERT CustomerOrganisations (CustomerOrganisationId, Name, PriceListType, EmailDomain, ParentCustomerOrganisationId, OrganisationPrefix, UseOrderGroups)
	VALUES 
	(1, 'Polismyndigheten', 2, 'polisen.se', NULL, 'Pol', 1),
	(2, 'Migrationsverket', 2, 'migrationsverket.se', NULL, 'Mig', 1),
	(3, 'Domstolsverket', 1, 'dom.se', NULL, 'Dom', 1),
	(4, 'Sopra Steria', 2, 'soprasteria.com', NULL, 'Sop', 1),
	(5, 'Södertörns tingsrätt', 1, 'sodertorns.domstol.se', 3, 'SödTing', 1),
	(6, 'Förvaltningsrätten i Stockholm', 1, 'forvaltningsrattenistockholm.domstol.se', 3, 'FörvSthm', 1)

SET IDENTITY_INSERT CustomerOrganisations OFF
