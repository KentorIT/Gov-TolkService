--test och dev uppdatera ev. felaktiga defaultsettings

--kolla på inställelsesätt för användarna 
SELECT * FROM UserDefaultSettings uds
WHERE uds.DefaultSettingType IN (3,4,5)
ORDER BY uds.UserId, uds.DefaultSettingType

--hittar inga som har samma värde
SELECT * FROM UserDefaultSettings uds
JOIN UserDefaultSettings uds1 ON uds.UserId = uds1.UserId AND uds.Value = uds1.Value AND uds.DefaultSettingType <> uds1.DefaultSettingType
WHERE uds.DefaultSettingType IN (3,4,5) AND uds1.DefaultSettingType IN (3,4,5)

--inga som har ifyllt i bara 2 och 3 men ej i 1
SELECT * FROM UserDefaultSettings uds
WHERE uds.DefaultSettingType IN (4,5) AND uds.UserId 
NOT IN (SELECT uds.UserId FROM UserDefaultSettings uds
WHERE uds.DefaultSettingType IN (3))


--inga som har ifyllt i 3 men ej i 2
SELECT * FROM UserDefaultSettings uds
WHERE uds.DefaultSettingType IN (5) AND uds.UserId 
NOT IN (SELECT uds.UserId FROM UserDefaultSettings uds
WHERE uds.DefaultSettingType IN (4))
