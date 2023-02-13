
--detta kördes 2023-02-13 (dagen innan release 2.1) så Charlotte kunde lägga in användare
SELECT * FROM Brokers b

SELECT * FROM AspNetUsers anu WHERE 
anu.IsApiUser = 1


DECLARE @newBrokerId INT = 8
	   ,@newBrokerApiUser INT
	   ,@UserNameGuid UNIQUEIDENTIFIER = NEWID()
	   ,@SecStampGuid UNIQUEIDENTIFIER = NEWID()
	   ,@ConcurrencyStampGuid UNIQUEIDENTIFIER = NEWID()


INSERT INTO Brokers (BrokerId, Name, EmailDomain, EmailAddress, OrganizationNumber, OrganizationPrefix)
	VALUES (@newBrokerId, N'Språkpoolen Skandinavien AB', N'sprakpoolen.se', N'info@sprakpoolen.se', N'559033-3034', N'SPS');

--nedan följer de redan inlagda i Prod, kan man bara slumpa GUIDS?
INSERT AspNetUsers (UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount, BrokerId, NameFamily, NameFirst, PhoneNumberCellphone, IsActive, IsApiUser)
	VALUES (@UserNameGuid, @UserNameGuid, N'info@sprakpoolen.se', N'INFO@SPRAKPOOLEN.SE', 0, @SecStampGuid, @ConcurrencyStampGuid, 0, 0, 1, 0, @newBrokerId, N'User', N'Api', N'xx', 1, 1);

	SELECT * FROM Brokers b

--5793 blev nya id på apiUser
SELECT * FROM AspNetUsers anu WHERE 
anu.IsApiUser = 1
and
anu.BrokerId = 8