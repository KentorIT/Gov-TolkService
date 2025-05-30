ALTER PROCEDURE [dbo].[GetUsersForExcelReport] @customerId INT, @userId INT
AS

--NOTE! When ALTER THIS SP - also change file \GOV Tolk\Tolk.BusinessLogic\Data\Stored procedures\GetUsersForExcelReport.sql

	CREATE TABLE #reportUsers (
		userId INT
	   ,nameFirst NVARCHAR(255)
	   ,nameFamily NVARCHAR(255)
	   ,email NVARCHAR(255)
	   ,invoiceRef NVARCHAR(1000)
	   ,lastLoginAt NVARCHAR(16)
	   ,customerUnits NVARCHAR(1000)
	   ,isActive BIT
	   ,userStatusType INT
	   ,roles NVARCHAR(255)
	   ,createdAt NVARCHAR(16)
	   ,customerName NVARCHAR(255)
	)

	CREATE TABLE #userStatus(
		Id INT
	   , statusName NVARCHAR(100)
	)

	--if customer check roles, if not central admin take only units
	DECLARE @onlyUnits BIT = 0;

		IF (NOT EXISTS (SELECT
						*
					FROM AspNetUsers anu
					JOIN AspNetUserRoles anur
						ON anur.UserId = anu.Id
					INNER JOIN AspNetRoles anr
						ON anur.RoleId = anr.Id
					WHERE (anr.Name = 'CentralAdministrator'
					AND anu.Id = @userId
					AND anu.CustomerOrganisationId = @customerId)
					OR (anr.Name IN ('SystemAdministrator', 'ApplicationAdministrator')
					AND anu.Id = @userId)))
		SET @onlyUnits = 1

		CREATE TABLE #customerUnits (Id INT)

		IF (@onlyUnits = 1) 
			BEGIN 
				INSERT INTO #customerUnits 
				SELECT cuu.CustomerUnitId
				FROM AspNetUsers a
				JOIN CustomerUnitUsers cuu ON cuu.UserId = a.id 
				WHERE a.id = @userId AND a.CustomerOrganisationId = @customerId
				AND cuu.IsLocalAdmin = 1
			END

	INSERT INTO #userStatus (Id, statusName)
		VALUES
		(1, 'Aktiv'),
		(2, 'Inaktiverad av adminstratör'),
		(3, 'Inaktiverad pga inaktivitet')

	INSERT INTO #reportUsers (
		userId
	   ,nameFamily 
	   ,nameFirst 
	   ,email 
	   ,invoiceRef 
	   ,lastLoginAt
	   ,isActive
	   ,userStatusType
	   ,customerName
	   ,createdAt)
	SELECT DISTINCT
		a.Id,
		ISNULL(a.NameFamily, ''),
		ISNULL(a.NameFirst, ''),
		ISNULL(a.Email, ''),
		ISNULL(ud.Value, ''),
		ISNULL(CONVERT(NVARCHAR(16), a.LastLoginAt), ''),
		a.IsActive,
		CASE a.IsActive 
			WHEN 1 THEN 1
			ELSE 0
			END,
		co.Name,
		ISNULL(CONVERT(NVARCHAR(16), e.LoggedAt), '')
	FROM AspNetUsers a
		JOIN CustomerOrganisations co ON co.CustomerOrganisationId = a.CustomerOrganisationId
		LEFT JOIN CustomerUnitUsers cuu ON cuu.UserId = a.id
		LEFT JOIN CustomerUnits cu ON cu.CustomerOrganisationId = a.CustomerOrganisationId AND cu.CustomerUnitId = cuu.CustomerUnitId
		LEFT JOIN UserDefaultSettings ud ON ud.UserId = a.id AND ud.DefaultSettingType = 13 --13 invoiceRef
		LEFT JOIN UserAuditLogEntries e ON e.UserId = a.id and e.UserChangeType = 1  --1 created
		AND e.UserAuditLogEntryId IN (SELECT MIN(uae.UserAuditLogEntryId) FROM UserAuditLogEntries uae
		WHERE uae.UserChangeType = 1 AND e.UserId = uae.UserId)
	WHERE
		a.CustomerOrganisationId = @customerId 
		AND (@onlyUnits = 0 OR cu.CustomerUnitId IN (SELECT * FROM #customerUnits))
		AND a.IsApiUser <> 1

	--Update user status for inactive users
	UPDATE #reportUsers
		SET userStatusType =
		CASE WHEN e.UserId IS NULL
				THEN 3 ELSE 2
		END
	FROM #reportUsers r
		LEFT JOIN UserAuditLogEntries e ON e.UserId = r.userId AND e.UserAuditLogEntryId IN 
			(SELECT MAX(uae.UserAuditLogEntryId) FROM UserAuditLogEntries uae --try find latest row for user where IsActive and changed by a user
			JOIN AspNetUserHistoryEntries uh ON uh.UserAuditLogEntryId = uae.UserAuditLogEntryId
			WHERE uae.UserChangeType = 2 AND e.UserId = uae.UserId AND uh.IsActive = 1) 
	WHERE r.isActive = 0

	--Update units for user and if local admin
	Update #reportUsers
		SET customerUnits = (SELECT STRING_AGG((cu.Name + CASE WHEN (cuu.IsLocalAdmin = 1) THEN ' (Admin)' ELSE '' END), '; ')
		FROM #reportUsers r
		JOIN CustomerUnitUsers cuu ON cuu.UserId = r.userId
		JOIN CustomerUnits cu ON cu.CustomerUnitId = cuu.CustomerUnitId and cu.CustomerOrganisationId = @customerId
		WHERE #reportUsers.UserId = r.UserId
		AND (@onlyUnits = 0 OR cu.CustomerUnitId IN (SELECT * FROM #customerUnits))
		GROUP BY r.userId)

	--Update roles for user
	Update #reportUsers
		SET roles = (SELECT STRING_AGG((CASE ar.Name WHEN 'CentralAdministrator' THEN 'Central administratör' ELSE 'Rätt att hantera alla myndighetens avrop' END), '; ')
		FROM #reportUsers r
		JOIN AspNetUserRoles a ON a.UserId = r.userId
		JOIN AspNetRoles ar ON ar.Id = a.RoleId
		WHERE #reportUsers.UserId = r.UserId
		GROUP BY r.userId)

	SELECT DISTINCT
	 r.nameFamily 'Efternamn'
	,r.nameFirst 'Förnamn'
	,r.email 'E-postadress'
	,r.invoiceRef 'Fakturareferens'
	,r.lastLoginAt 'Senast inloggad'
	,ISNULL(r.customerUnits, '') 'Kopplade enheter'
	,u.statusName 'Status'
	,ISNULL(r.roles, '') 'Utökad behörighet'
	,r.createdAt 'Skapad'
	,r.customerName 'CustomerName' --get for Excel file title
	FROM #reportUsers r
	JOIN #userStatus u ON r.userStatusType = u.Id
	ORDER BY 1, 2

	DROP TABLE #reportUsers, #userStatus, #customerUnits