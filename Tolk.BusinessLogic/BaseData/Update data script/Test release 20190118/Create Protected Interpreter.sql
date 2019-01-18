  BEGIN TRAN
  
  INSERT INTO [dbo].[InterpreterBrokers]
  (BrokerId, Email, FirstName, LastName, OfficialInterpreterId)  
  SELECT BrokerId, 'tolk@'+EmailDomain, 'Tolk', 'Skyddad Identitet', 1 FROM [dbo].[Brokers];

  COMMIT TRAN;