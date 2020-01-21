
--There are two new notification types (23 RequestLostDueToNoAnswerFromCustomer, 24 RequestGroupLostDueToNoAnswerFromCustomer)

Select * from UserNotificationSettings

Select * from AspnetUsers
Where IsApiUser = 1 


-- For each of them that does not have an email setting (NotificationChannel = 1) on NotificationType > 22
-- Look in web code, to make sure that all fields in the enum NotificationType is represented for all users.
-- Could remove all specified emails in ConnectionInformation, as long as they are the same as the api-user's since that amounts to the same.



INSERT UserNotificationSettings (UserId, NotificationChannel, NotificationType)
SELECT id, 1, 23
FROM AspnetUsers
WHERE IsApiUser = 1 

INSERT UserNotificationSettings (UserId, NotificationChannel, NotificationType)
SELECT id, 1, 24
FROM AspnetUsers
WHERE IsApiUser = 1 

