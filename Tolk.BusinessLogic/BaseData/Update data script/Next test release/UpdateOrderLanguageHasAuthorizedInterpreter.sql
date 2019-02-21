
--update old orders - set that LanguageHasAuthorizedInterpreter = 1 cause it was possible to set competence levels when making order
BEGIN TRAN

UPDATE Orders SET LanguageHasAuthorizedInterpreter = 1

ROLLBACK TRAN
 
 
-- It would be possible to set to true if language has, but the prices are wrong anyway
-- BEGIN TRAN

-- SELECT * FROM Orders o WHERE o.LanguageHasAuthorizedInterpreter = 0 AND 
-- o.LanguageId IN (SELECT L.LanguageId FROM Languages l WHERE l.TellusName IS NOT NULL)


-- UPDATE Orders SET LanguageHasAuthorizedInterpreter = 1
-- FROM Orders o JOIN Languages l
-- ON o.LanguageId = l.LanguageId
-- WHERE l.TellusName IS NOT NULL 

-- SELECT * FROM Orders o WHERE o.LanguageHasAuthorizedInterpreter = 0 AND 
-- o.LanguageId IN (SELECT L.LanguageId FROM Languages l WHERE l.TellusName IS NOT NULL)

-- ROLLBACK TRAN