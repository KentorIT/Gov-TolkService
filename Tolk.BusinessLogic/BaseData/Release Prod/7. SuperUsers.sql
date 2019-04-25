
-- Create system admin with impersonator role

DECLARE @userId INT

INSERT AspNetUsers
(concurrencystamp, email, normalizedemail, normalizedusername, securitystamp, username, accessfailedcount, emailconfirmed, lockoutenabled, phonenumberconfirmed, twofactorenabled, customerorganisationid, brokerid, interpreterid, namefirst, namefamily, phonenumbercellphone, passwordhash)
VALUES
('f7e32e64-bbb4-43b4-81be-28f157cc39cc', 'liv.winell@soprasteria.com', 'liv.winell@soprasteria.com', 'liv.winell@soprasteria.com', 'ead3c9ec-4ef6-4fa6-999f-f1c3c72e6695', 'liv.winell@soprasteria.com', 0, 1, 1, 0, 0, null, null, null, 'liv', 'winell', '0708-966080', 'aqaaaaeaaccqaaaaeludi9yo+1234knhe5l51kuawbscm/heorf8w8l7vk828+/higpkzso/gk85so/3aa=1')

SELECT @userId = SCOPE_IDENTITY()

 --lägg in roller för impersonator och system admin
	INSERT INTO AspNetUserRoles (UserId, RoleId)
	VALUES (@userId, 1), (@userId,2)
	
	

-- INSERT AspNetUsers
-- (ConcurrencyStamp, Email, NormalizedEmail, NormalizedUserName, SecurityStamp, UserName, AccessFailedCount, EmailConfirmed, LockoutEnabled, PhoneNumberConfirmed, TwoFactorEnabled, CustomerOrganisationId, BrokerId, InterpreterId, NameFirst, NameFamily, PhoneNumberCellphone, PasswordHash)
-- VALUES
-- ('f7e32e64-bbb4-43b4-81be-28f157cc39c1', 'emma.calderon@soprasteria.com', 'EMMA.CALDERON@SOPRASTERIA.COM', 'EMMA.CALDERON@SOPRASTERIA.COM', 'ead3c9ec-4ef6-4fa6-999f-f1c3c72e6693', 'emma.calderon@soprasteria.com', 0, 1, 1, 0, 0, NULL, NULL, NULL, 'Emma', 'Calderon', '0708-964052', 'AQAAAAEAACcQAAAAELuDi9yo+1234KnHe5L51kuAWBSCm/HeORf8w8L7Vk828+/HIGPKzso/gK85So/3AA=1')

-- SELECT @userId = SCOPE_IDENTITY()

 -- --lägg in roller för impersonator och system admin
	-- INSERT INTO AspNetUserRoles (UserId, RoleId)
	-- VALUES (@userId, 1), (@userId,2)
	
	


