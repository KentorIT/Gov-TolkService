use TolkDev

set identity_insert CustomerOrganisations on 

insert CustomerOrganisations(CustomerOrganisationId, Name, PriceListType, EmailDomain)
Values(1, 'Polismyndigheten', 2, 'polisen.se'),
(2, 'Migrationsverket', 2, 'migrationsverket.se'),
(3, 'Domstolsverket', 1, 'dom.se'),
(4, 'Sopra Steria', 2, 'soprasteria.com')


set identity_insert CustomerOrganisations off
