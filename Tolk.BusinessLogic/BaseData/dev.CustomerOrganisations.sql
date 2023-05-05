USE TolkDev

SET IDENTITY_INSERT CustomerOrganisations ON

INSERT CustomerOrganisations (CustomerOrganisationId, Name, PriceListType, EmailDomain, ParentCustomerOrganisationId, OrganisationPrefix)
	VALUES 
	(1, 'Polismyndigheten', 2, 'polisen.se', NULL, 'Pol'),
	(2, 'Migrationsverket', 2, 'migrationsverket.se', NULL, 'Mig'),
	(3, 'Domstolsverket', 1, 'dom.se', NULL, 'Dom'),
	(4, 'Sopra Steria', 2, 'soprasteria.com', NULL, 'Sop'),
	(5, 'Södertörns tingsrätt', 1, 'sodertorns.domstol.se', 3, 'SödTing'),
	(6, 'Förvaltningsrätten i Stockholm', 1, 'forvaltningsrattenistockholm.domstol.se', 3, 'FörvSthm')

SET IDENTITY_INSERT CustomerOrganisations OFF

insert CustomerSettings
Values
(1, 1, 0),
(1, 2, 0),
(1, 3, 0),
(1, 4, 0),
(2, 1, 0),
(2, 2, 0),
(2, 3, 0),
(2, 4, 0),
(3, 1, 0),
(3, 2, 0),
(3, 3, 0),
(3, 4, 0),
(4, 1, 0),
(4, 2, 0),
(4, 3, 0),
(4, 4, 0),
(5, 1, 0),
(5, 2, 0),
(5, 3, 0),
(5, 4, 0),
(6, 1, 0),
(6, 2, 0),
(6, 3, 0),
(6, 4, 0)
