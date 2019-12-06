
--check if they still have the name Skyddad Identitet
SELECT * FROM InterpreterBrokers ib
WHERE ib.LastName = 'Skyddad Identitet' 

INSERT INTO Interpreters (IsProtected)
	VALUES (1)

UPDATE InterpreterBrokers SET InterpreterId = SCOPE_IDENTITY() 
	WHERE LastName = 'Skyddad Identitet' 
