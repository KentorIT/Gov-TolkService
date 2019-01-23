

--får för närvarande bara vara 20 tecken i namn på organisation
SET IDENTITY_INSERT CustomerOrganisations ON

INSERT CustomerOrganisations (CustomerOrganisationId, Name, PriceListType, EmailDomain)
	VALUES 
	(1,	'Kammarkollegiet',	2,	'kammarkollegiet.se'),
	(2, 'Domstolsverket', 1, 'dom.se'),
	(3, 'Pensionsmyndigheten', 2, 'pensionsmyndigheten.se'),
	(4, 'Södertörns tingsrätt', 1, 'sodertornstingsratt.domstol.se'),
	(5, 'Förvalt.rätten Sthlm', 1, 'forvaltningsrattenistockholm.domstol.se') --'Förvaltningsrätten i Stockholm'
	--(6, 'Polismyndigheten', 2, 'polisen.se'),
	--(7, 'Migrationsverket', 2, 'migrationsverket.se'),

SET IDENTITY_INSERT CustomerOrganisations OFF

