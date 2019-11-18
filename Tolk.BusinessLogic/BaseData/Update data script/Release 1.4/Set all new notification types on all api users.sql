Select * from UserNotificationSettings

Select * from AspnetUsers
Where IsApiUser = 1 


-- For each of them that does not have an email setting (NotificationChannel = 1) on NotificationType > 16
-- Look in web code, to make sure that all fields in the enum NotificationType is represented for all users.
-- Could remove all specified emails in ConnectionInformation, as long as they are the same as the api-user's since that amounts to the same.
INSERT UserNotificationSettings (UserId, NotificationChannel, NotificationType)
SELECT id, 1, 17
FROM AspnetUsers
WHERE IsApiUser = 1 

INSERT UserNotificationSettings (UserId, NotificationChannel, NotificationType)
SELECT id, 1, 18
FROM AspnetUsers
WHERE IsApiUser = 1 

INSERT UserNotificationSettings (UserId, NotificationChannel, NotificationType)
SELECT id, 1, 19
FROM AspnetUsers
WHERE IsApiUser = 1 

INSERT UserNotificationSettings (UserId, NotificationChannel, NotificationType)
SELECT id, 1, 20
FROM AspnetUsers
WHERE IsApiUser = 1 

INSERT UserNotificationSettings (UserId, NotificationChannel, NotificationType)
SELECT id, 1, 21
FROM AspnetUsers
WHERE IsApiUser = 1 

INSERT UserNotificationSettings (UserId, NotificationChannel, NotificationType)
SELECT id, 1, 22
FROM AspnetUsers
WHERE IsApiUser = 1 
