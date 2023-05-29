--66 FlexibleRequestCreated Email NotificationChannel
INSERT UserNotificationSettings (UserId, NotificationChannel, NotificationType)
SELECT id, 1, 66
FROM AspnetUsers
WHERE IsApiUser = 1 AND BrokerId IN (SELECT r.BrokerId FROM Rankings r WHERE r.LastValidDate > GETDATE()) 

--67 expires_at_for_request_changed Email NotificationChannel
INSERT UserNotificationSettings (UserId, NotificationChannel, NotificationType)
SELECT id, 1, 67
FROM AspnetUsers
WHERE IsApiUser = 1 AND BrokerId IN (SELECT r.BrokerId FROM Rankings r WHERE r.LastValidDate > GETDATE()) 