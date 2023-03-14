use TolkDev;
  
INSERT [CustomerSpecificProperties]
(
       [CustomerOrganisationId]
      ,[PropertyType] -- 1 == PropertyType.InvoiceReference
      ,[DisplayName]
      ,[DisplayDescription]
      ,[InputPlaceholder]
      ,[Required]
      ,[RemoteValidation]
      ,[RegexPattern]
      ,[RegexErrorMessage]
      ,[MaxLength]
)
VALUES
(1,1,'Fakturareferens, u-nummer','Ange u följt av 7 siffror (utan bindestreck eller mellanslag)','Kontrollattestantens u-nummer',1,1,'^([uU])([0-9]{7})$','U-nummer måste anges utan mellanslag och bindestreck',8)