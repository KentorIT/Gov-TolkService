
use tolkdev
Declare @userid int

Select @userid = Id
from AspNetUsers
Where IsApiUser = 1
and BrokerId = 1

Insert UserNotificationSettings
--Get broker 1 api user id
Select @UserId, 2, 1, 'https://localhost:5001/Request/Created'
union
Select @UserId, 2, 2, 'https://localhost:5001/Request/Updated'
union
Select @UserId, 2, 3, 'https://localhost:5001/Request/CancelledByCustomer'
union
Select @UserId, 2, 4, 'https://localhost:5001/Request/GroupCancelledByCustomer'
union
Select @UserId, 2, 5, 'https://localhost:5001/Request/Approved'
union
Select @UserId, 2, 6, 'https://localhost:5001/Request/AnswerDenied'
union
Select @UserId, 2, 7, 'https://localhost:5001/Request/RequestLostDueToInactivity'
union
Select @UserId, 2, 8, 'https://localhost:5001/Request/ReplacementCreated'
union
Select @UserId, 2, 9, 'https://localhost:5001/Requisition/Reviewed'
union
Select @UserId, 2, 10, 'https://localhost:5001/Requisition/Commented'
union
Select @UserId, 2, 11, 'https://localhost:5001/Complaint/Created'
union
Select @UserId, 2, 12, 'https://localhost:5001/Complaint/ComplaintDisputedAccepted'
union
Select @UserId, 2, 13, 'https://localhost:5001/Complaint/ComplaintDisputePendingTrial'
union
Select @UserId, 2, 16, 'https://localhost:5001/Request/ChangedInterpreterAccepted'
union
Select @UserId, 2, 17, 'https://localhost:5001/Request/GroupCreated'
union
Select @UserId, 2, 18, 'https://localhost:5001/Request/RequestGroupLostDueToInactivity'
union
Select @UserId, 2, 19, 'https://localhost:5001/Home/ErrorMessage'
union
Select @UserId, 2, 20, 'https://localhost:5001/Home/CustomerCreated'
union
Select @UserId, 2, 21, 'https://localhost:5001/Request/GroupAnswerApproved'
union
Select @UserId, 2, 22, 'https://localhost:5001/Request/GroupAnswerDenied'
union
Select @UserId, 2, 23, 'https://localhost:5001/Request/RequestNoAnswerFromCustomer'
union
Select @UserId, 2, 24, 'https://localhost:5001/Request/RequestGroupNoAnswerFromCustomer'
union
Select @UserId, 2, 25, 'https://localhost:5001/Request/RequestAssignmentTimePassed'
union
Select @UserId, 2, 59, 'https://localhost:5001/Request/CreatedRequiresAcceptance'
union
Select @UserId, 2, 60, 'https://localhost:5001/Request/GroupCreatedRequiresAcceptance'


--Get broker 2 api user id
Select @userid = Id
from AspNetUsers
Where IsApiUser = 1
and BrokerId = 2

Insert UserNotificationSettings
Select @UserId, 2, 1, 'https://localhost:5001/Request/CreatedToOther'
union
Select @UserId, 2, 17, 'https://localhost:5001/Request/GroupCreated'


----Get polisen api user id for customer api
--Select @UserId, 2, 26, 'https://localhost:5002/Order/OrderAccepted'
--Select @UserId, 2, 27, 'https://localhost:5002/Order/OrderAnswered'
--Select @UserId, 2, 28, 'https://localhost:5002/Order/OrderDeclined'
--Select @UserId, 2, 29, 'https://localhost:5002/Order/OrderCancelled'
