

SET IDENTITY_INSERT CustomerOrganisations ON

INSERT CustomerOrganisations (CustomerOrganisationId, Name, PriceListType, EmailDomain, ParentCustomerOrganisationId)
	VALUES 
	(1,	'Kammarkollegiet',	2, 'kammarkollegiet.se', NULL),
	(2, 'Domstolsverket', 1, 'dom.se', NULL),
	(3, 'Pensionsmyndigheten', 2, 'pensionsmyndigheten.se', NULL),
	(4, 'Södertörns tingsrätt', 1, 'sodertornstingsratt.domstol.se', 2),
	(5, 'Förvaltningsrätten i Stockholm', 1, 'forvaltningsrattenistockholm.domstol.se', 2)
	--(6, 'Polismyndigheten', 2, 'polisen.se', NULL),
	--(7, 'Migrationsverket', 2, 'migrationsverket.se', NULL)

SET IDENTITY_INSERT CustomerOrganisations OFF

