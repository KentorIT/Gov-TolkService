use TolkDev

set identity_insert CustomerOrganisations on 

insert CustomerOrganisations(CustomerOrganisationId, Name, PriceListType, EmailDomain)
Values(1, 'Polisen', 2, 'polisen.se'),
(2, 'Migrationsverket', 2, 'migrationsverket.se'),
(3, 'Domstolsverket', 1, 'domstol.se')

set identity_insert CustomerOrganisations off
