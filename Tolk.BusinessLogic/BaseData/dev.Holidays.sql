USE TolkDev

CREATE TABLE #Holidays(
	[Date] [date] NOT NULL,
	[Name] [nvarchar](255) NOT NULL,
	[DateType] [int] NOT NULL)

GO

INSERT INTO #Holidays (Date, Name, DateType)
	VALUES
--2018
('2018-01-01', 'Nyår', 5),
('2018-01-02', 'Nyår', 6),
('2018-01-06', 'Trettondedag jul', 3),
('2018-03-29', 'Påsk', 4),
('2018-03-30', 'Påsk', 5),  --varför är inte påskafton med?
('2018-04-01', 'Påsk', 5),
('2018-04-02', 'Påsk', 5),
('2018-04-03', 'Påsk', 6),
('2018-05-01', 'Första maj', 3),
('2018-05-10', 'Kristi himmelfärdsdag', 3),
('2018-05-18', 'Pingst', 4),
('2018-05-19', 'Pingst', 5),
('2018-05-20', 'Pingst', 5),
('2018-05-21', 'Pingst', 6),
('2018-06-06', 'Sveriges nationaldag', 3),
('2018-06-22', 'Midsommar', 4),  --dan före midsommarafton bör vara med och själva midsommarafton ska vara 5 eller?
('2018-06-23', 'Midsommar', 5),
('2018-06-24', 'Midsommar', 5),
('2018-06-25', 'Midsommar', 6),
('2018-11-03', 'Alla helgons dag', 3),
('2018-12-23', 'Jul', 4),
('2018-12-24', 'Jul', 5),
('2018-12-25', 'Jul', 5),
('2018-12-26', 'Jul', 5),
('2018-12-27', 'Jul', 6),
('2018-12-30', 'Nyår', 4),
('2018-12-31', 'Nyår', 5),
--2019
('2019-01-01', 'Nyår', 5),
('2019-01-02', 'Nyår', 6),
('2019-01-06', 'Trettondedag jul', 3),
('2019-04-18', 'Påsk', 4), --skärtorsdag
('2019-04-19', 'Påsk', 5), --långfredag
('2019-04-20', 'Påsk', 5), --påskafton (2018 lades den inte in)?
('2019-04-21', 'Påsk', 5), --påskdag
('2019-04-22', 'Påsk', 5), --annandag påsk
('2019-04-23', 'Påsk', 6),
('2019-05-01', 'Första maj', 3),
('2019-05-30', 'Kristi himmelfärdsdag', 3),
('2019-06-06', 'Sveriges nationaldag', 3),
('2019-06-07', 'Pingst', 4),
('2019-06-08', 'Pingst', 5),
('2019-06-09', 'Pingst', 5),
('2019-06-10', 'Pingst', 6),
('2019-06-20', 'Midsommar', 4), --var midsommar inlagt korrekt 2018? torsdag väl bör räknas som dagen före afton? den var inte inlagd
('2019-06-21', 'Midsommar', 5),
('2019-06-22', 'Midsommar', 5),
('2019-06-23', 'Midsommar', 5),
('2019-06-24', 'Midsommar', 6),
('2019-11-02', 'Alla helgons dag', 3),
('2019-12-23', 'Jul', 4),
('2019-12-24', 'Jul', 5),
('2019-12-25', 'Jul', 5),
('2019-12-26', 'Jul', 5),
('2019-12-27', 'Jul', 6),
('2019-12-30', 'Nyår', 4),
('2019-12-31', 'Nyår', 5)

MERGE Holidays dst
USING #Holidays src
ON (src.Date = dst.Date)
WHEN MATCHED THEN
UPDATE SET dst.Name = src.Name, dst.DateType = src.DateType
WHEN NOT MATCHED THEN
INSERT (Date, Name, DateType)
VALUES (src.Date, src.Name, src.DateType)
WHEN NOT MATCHED BY SOURCE THEN
DELETE;

DROP TABLE #Holidays


