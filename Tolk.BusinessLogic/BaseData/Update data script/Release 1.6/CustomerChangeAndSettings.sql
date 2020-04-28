

--PROD - LÄGG TILL EN HISTORIK PÅ NÄR SAMMANHÅÅLEN BOKNING SLOGS PÅ FÖR GÖTA HOVRÄTT

--la till sammanhållen bokning 3 april 07:30, uppdaterad av UserId 18 för Göta hovrätt (21)
INSERT INTO CustomerChangeLogEntries (CustomerChangeLogType, UpdatedByUserId, LoggedAt, CustomerOrganisationId)
	VALUES (1, 18, '2020-04-03 07:30:00.000000 +02:00', 21);

DECLARE @customerChangeLogEntryId INT
SELECT @customerChangeLogEntryId = SCOPE_IDENTITY()

--inserta en rad som det såg ut innan CustomerSettingType = 1 SB Value = false
INSERT INTO CustomerSettingHistoryEntries (CustomerChangeLogEntryId, CustomerSettingType, Value)
	VALUES (@customerChangeLogEntryId, 1, 0);
	
--kolla att det kommit in rätt

	SELECT * FROM CustomerChangeLogEntries ccle
	JOIN CustomerSettingHistoryEntries cshe ON ccle.CustomerChangeLogEntryId = cshe.CustomerChangeLogEntryId
	JOIN CustomerOrganisations co ON ccle.CustomerOrganisationId = co.CustomerOrganisationId
	
	
	
	--kolla också Customerorganisations CustomerSettings så att det kommit in rätt
	
	SELECT * FROM CustomerOrganisations co

	--ska vara lika många som organisationerna x 3
	SELECT co.Name, cs.* FROM CustomerOrganisations co
	JOIN CustomerSettings cs ON co.CustomerOrganisationId = cs.CustomerOrganisationId
	ORDER BY co.CustomerOrganisationId, cs.CustomerSettingType

	-- kolla att endast Göta Hovrätt har typ 1 (sammanhållen) satt
		SELECT co.Name, cs.* FROM CustomerOrganisations co
	JOIN CustomerSettings cs ON co.CustomerOrganisationId = cs.CustomerOrganisationId
	WHERE cs.CustomerSettingType = 1 AND cs.Value = 1
	ORDER BY co.CustomerOrganisationId, cs.CustomerSettingType 

	-- kolla att ingen har självfakturerande tolk typ 2 satt (om KamK vet vilka kan vi sätta det i UI)
	SELECT co.Name, cs.* FROM CustomerOrganisations co
	JOIN CustomerSettings cs ON co.CustomerOrganisationId = cs.CustomerOrganisationId
	WHERE cs.CustomerSettingType = 2 AND cs.Value = 1
	ORDER BY co.CustomerOrganisationId, cs.CustomerSettingType 

	-- kolla att alla har bifogade filer typ 3 satt (om vi ska stänga av för FK mm kan vi göra det från UI)
	SELECT co.Name, cs.* FROM CustomerOrganisations co
	JOIN CustomerSettings cs ON co.CustomerOrganisationId = cs.CustomerOrganisationId
	WHERE cs.CustomerSettingType = 3 AND cs.Value = 1
	ORDER BY co.CustomerOrganisationId, cs.CustomerSettingType 

	-- kolla att ingen har bifogade filer typ 3 satt (om vi ska stänga av för FK mm kan vi göra det från UI)
	SELECT co.Name, cs.* FROM CustomerOrganisations co
	JOIN CustomerSettings cs ON co.CustomerOrganisationId = cs.CustomerOrganisationId
	WHERE cs.CustomerSettingType = 3 AND cs.Value = 0
	ORDER BY co.CustomerOrganisationId, cs.CustomerSettingType 