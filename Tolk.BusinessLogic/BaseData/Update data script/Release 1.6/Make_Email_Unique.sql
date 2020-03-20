
--kolla om det finns enheter och  användare som redan har samma adress
--användaren i prod kommer att tas bort innan driftättning, men det kan komma in flera så detta måste kollas
SELECT cu.Email FROM CustomerUnits cu
INTERSECT
SELECT anu.Email FROM AspNetUsers anu 

--kolla dubbletter på cutomerunits (inga i prod 20200320)
SELECT
	COUNT(cu.Email) AS Antal
   ,cu.Email
FROM CustomerUnits cu
GROUP BY cu.Email
HAVING COUNT(cu.Email) > 1
ORDER BY Antal DESC

--kolla dubbletter anv
SELECT
	COUNT(anu.Email) AS Antal
   ,anu.Email
FROM AspNetUsers anu
GROUP BY anu.Email
HAVING COUNT(anu.Email) > 1
ORDER BY Antal DESC


--i Prod finns 2020-03-20 en användare som har samma som en enhet
--Karin Jansson på Trfikverkket e-post trafikverket.forarprov.falun@trafikverket.se (id 142)
--Hon har dock aldrig varit inne i systemet med sin användare utan verkar ha
--en ny Karin Jansson med personlig e-post (id 137)

BEGIN TRAN

DELETE FROM AspNetUsers WHERE id = 142 AND 
Email = 'trafikverket.forarprov.falun@trafikverket.se' AND LastLoginAt = NULL

ROLLBACK TRAN

--i test är detta kört för ett par tolkar som är "riktiga" användare som hade samma som enheter

begin tran

select * from AspNetUsers where id in (80, 81)

update AspNetUsers set email = 'kam_tolk1@testkamktolk.se', NormalizedEmail = 'KAM_TOLK1@TESTKAMKTOLK.SE' where id = 81 and
Email = 'ITKavrop@kammarkollegiet.se'

update AspNetUsers set email = 'kam_tolk2@testkamktolk.se', NormalizedEmail = 'KAM_TOLK2@TESTKAMKTOLK.SE' where id = 80 and
Email = 'IT-utb@kammarkollegiet.se'

select * from AspNetUsers where id in (80, 81)

rollback tran 


--I test fanns två enheter (olika org) med samma e-post, denna är omsatt i test
begin tran 

Update CustomerUnits set Email = 'test_123@testsasd.se' where CustomerUnitId = 3 and
Email = 'test@test.se'

rollback tran

SELECT
	COUNT(anu.Email) AS Antal
   ,anu.Email
FROM AspNetUsers anu
GROUP BY anu.Email
HAVING COUNT(anu.Email) > 1
ORDER BY Antal DESC
