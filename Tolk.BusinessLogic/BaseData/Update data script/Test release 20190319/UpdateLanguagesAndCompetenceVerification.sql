
--delete verfication of InterpreterCompetence on requests
BEGIN TRAN

UPDATE Requests SET InterpreterCompetenceVerificationResultOnAssign = NULL, InterpreterCompetenceVerificationResultOnStart = NULL

ROLLBACK TRAN 


--Update languages bosniska, kroatiska, serbiska

--17	Bosniska	1	bos	bosniska, kroatiska, serbiska
--106	Serbiska	1	srp	bosniska, kroatiska, serbiska
--107	Kroatiska	1	hrv	bosniska, kroatiska, serbiska

BEGIN TRAN

SELECT * FROM Languages l WHERE l.LanguageId IN (17, 106, 107) 

UPDATE Languages SET TellusName = 'bosniska, kroatiska, serbiska' WHERE LanguageId IN (106, 107)

UPDATE Languages SET Name = 'Kroatiska', ISO_639_Code = 'hrv' WHERE LanguageId IN (107)

SELECT * FROM Languages l WHERE l.LanguageId IN (17, 106, 107) 

ROLLBACK TRAN 