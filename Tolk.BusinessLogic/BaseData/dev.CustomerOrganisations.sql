USE TolkDev

SET IDENTITY_INSERT CustomerOrganisations ON

INSERT CustomerOrganisations (CustomerOrganisationId, Name, PriceListType, EmailDomain, ParentCustomerOrganisationId, OrganisationPrefix)
	VALUES 
	(1, 'Polismyndigheten', 2, 'polisen.se', NULL, 'Pol'),
	(2, 'Migrationsverket', 2, 'migrationsverket.se', NULL, 'Mig'),
	(3, 'Domstolsverket', 1, 'dom.se', NULL, 'Dom'),
	(4, 'Sopra Steria', 2, 'soprasteria.com', NULL, 'Sop'),
	(5, 'S�dert�rns tingsr�tt', 1, 'sodertorns.domstol.se', 3, 'S�dTing'),
	(6, 'F�rvaltningsr�tten i Stockholm', 1, 'forvaltningsrattenistockholm.domstol.se', 3, 'F�rvSthlm')

SET IDENTITY_INSERT CustomerOrganisations OFF
