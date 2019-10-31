use tolkdev

Select * from UserNotificationSettings

Select Id from AspnetUsers
Where IsApiUser = 1


-- For each of them that does not have an email setting (NotificationChannel = 1) on NotificationType > 16
-- Look in web code, to make sure that all fields in the enum NotificationType is represented for all users.
-- Could remove all specified emails in ConnectionInformation, as long as they are the same as the api-user's since that amounts to the same.
Insert UserNotificationSettings


Select Id from AspnetUsers
Where IsApiUser = 1

