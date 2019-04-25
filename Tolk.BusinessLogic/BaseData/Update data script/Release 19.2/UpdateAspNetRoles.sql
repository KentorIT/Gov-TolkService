
BEGIN TRAN 

SELECT * FROM AspNetRoles anr

UPDATE AspNetRoles SET NAME = 'SystemAdministrator', NormalizedName = 'SYSTEMADMINISTRATOR' WHERE id = 1
UPDATE AspNetRoles SET NAME = 'CentralAdministrator', NormalizedName = 'CENTRALADMINISTRATOR' WHERE id = 3

SELECT * FROM AspNetRoles anr

ROLLBACK TRAN