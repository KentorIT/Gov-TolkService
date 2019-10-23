-- Update all customers that should not have 1 == LocalAgreeement.
Select * from CustomerOrganisations

Update CustomerOrganisations
Set TravelCostAgreementType = 1


