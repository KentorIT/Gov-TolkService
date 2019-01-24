
--delete OrderCompetenceRequirements with rank 3 since that's not an option anymore 
--(no crash will occure if not deleted, but it's bad data...)

BEGIN TRAN

DELETE FROM OrderCompetenceRequirements WHERE Rank = 3

COMMIT TRAN 