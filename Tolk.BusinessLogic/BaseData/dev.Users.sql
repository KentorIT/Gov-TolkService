use TolkDev;

DELETE AspNetUsers WHERE Email in ('pia@polisen.se', 'david@domstol.se', 'frida@formedling.se', 'tomas@tolk.se')

if not exists (select 1 from Interpreters)
INSERT Interpreters DEFAULT VALUES

INSERT AspNetUsers
(ConcurrencyStamp, Email, NormalizedEmail, NormalizedUserName, SecurityStamp, UserName, AccessFailedCount, EmailConfirmed, LockoutEnabled, PhoneNumberConfirmed, TwoFactorEnabled, CustomerOrganisationId, BrokerId, InterpreterId)
VALUES
('f7e32e64-bbb4-43b4-81be-28f157cc39cc', 'pia@polisen.se', 'PIA@POLISEN.SE', 'PIA@POLISEN.SE', 'ead3c9ec-4ef6-4fa6-999f-f1c3c72e6695', 'pia@polisen.se', 0, 1, 1, 0, 0, 1, null, null),
('f7e32e64-bbb4-43b4-81be-28f157cc39cd', 'patrik@polisen.se', 'PATRIK@POLISEN.SE', 'PATRIK@POLISEN.SE', 'ead3c9ec-4ef6-4fa6-999f-f1c3c72e6696', 'patrik@polisen.se', 0, 1, 1, 0, 0, 1, null, null),
('d2ce5d1a-1ffe-4938-9fe5-ecc155b0a5d3', 'mikael@migrationsverket.se', 'MIKAEL@MIGRATIONSVERKET.SE', 'MIKAEL@MIGRATIONSVERKET.SE', '8b087a08-7b34-4f50-996a-38a1395470b6', 'mikael@migrationsverket.se', 0, 1, 1, 0, 0, 2, null, null),
('d2ce5d1a-1ffe-4938-9fe5-ecc155b0a5d2', 'david@domstol.se', 'DAVID@DOMSTOL.SE', 'DAVID@DOMSTOL.SE', '8b087a08-7b34-4f50-996a-38a1395470b5', 'david@domstol.se', 0, 1, 1, 0, 0, 3, null, null),
('2735d701-afe5-4d12-8e18-e87d10e793fe', 'frida@formedling.se', 'FRIDA@FORMEDLING.SE', 'FRIDA@FORMEDLING.SE', 'ca04c2bd-f6f5-4f1d-a4e6-80ab6fd5c34b', 'frida@formedling.se', 0, 1, 1, 0, 0, null, 1, null),
('2735d701-afe5-4d12-8e18-e87d10e793ff', 'fredrik@formedling2.se', 'FREDRIK@FORMEDLING2.SE', 'FREDRIK@FORMEDLING2.SE', 'ca04c2bd-f6f5-4f1d-a4e6-80ab6fd5c34c', 'fredrik@formedling2.se', 0, 1, 1, 0, 0, null, 2, null),
('2735d701-afe5-4d12-8e18-e87d10e79400', 'fabian@formedling3.se', 'FABIAN@FORMEDLING3.SE', 'FABIAN@FORMEDLING3.SE', 'ca04c2bd-f6f5-4f1d-a4e6-80ab6fd5c34d', 'fabian@formedling3.se', 0, 1, 1, 0, 0, null, 3, null),
('2735d701-afe5-4d12-8e18-e87d10e79401', 'filippa@formedling4.se', 'FILIPPA@FORMEDLING4.SE', 'FILIPPA@FORMEDLING4.SE', 'ca04c2bd-f6f5-4f1d-a4e6-80ab6fd5c34e', 'filippa@formedling4.se', 0, 1, 1, 0, 0, null, 4, null),
('c7293f8c-caea-4262-bc4c-d869ff5a5644', 'tomas@tolk.se', 'TOMAS@TOLK.SE', 'TOMAS@TOLK.SE', '163ce957-af1b-48a6-8dd1-d07c45d308e9', 'tomas@tolk.se', 0, 1, 1, 0, 0, null, null, 1)

Insert InterpreterBrokerRegion
(RegionId, BrokerId, InterpreterId)
Select RegionId, BrokerId, 1 from BrokerRegions
