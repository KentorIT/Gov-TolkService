
--Ta bort rollen CentralAdministrator för alla SystemAdmin/KamK-anv då den inte har nån funktion där
--innehåller Rollback!
BEGIN TRAN 

SELECT * FROM AspNetUserRoles au
JOIN AspNetUsers ON AspNetUsers.Id = au.UserId
JOIN AspNetRoles anr ON au.RoleId = anr.Id
 WHERE anr.Name = 'CentralAdministrator'
AND  CustomerOrganisationId IS NULL AND BrokerId IS NULL AND InterpreterId IS NULL

DELETE AspNetUserRoles 
FROM AspNetUserRoles au
JOIN AspNetUsers ON AspNetUsers.Id = au.UserId
JOIN AspNetRoles anr ON au.RoleId = anr.Id
 WHERE anr.Name = 'CentralAdministrator'
AND  CustomerOrganisationId IS NULL AND BrokerId IS NULL AND InterpreterId IS NULL

SELECT * FROM AspNetUserRoles au
JOIN AspNetUsers ON AspNetUsers.Id = au.UserId
JOIN AspNetRoles anr ON au.RoleId = anr.Id
 WHERE anr.Name = 'CentralAdministrator'
AND  CustomerOrganisationId IS NULL AND BrokerId IS NULL AND InterpreterId IS NULL

ROLLBACK TRAN