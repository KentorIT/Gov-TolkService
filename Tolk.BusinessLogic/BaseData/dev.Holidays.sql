USE TolkDev

CREATE TABLE #Holidays(
	[Date] [date] NOT NULL,
	[Name] [nvarchar](255) NOT NULL,
	[DateType] [int] NOT NULL)

GO

INSERT INTO #Holidays (Date, Name, DateType)
	VALUES
--2018
('2018-01-01', 'Ny�r', 5),
('2018-01-02', 'Ny�r', 6),
('2018-01-06', 'Trettondedag jul', 3),
('2018-03-29', 'P�sk', 4),
('2018-03-30', 'P�sk', 5),  --varf�r �r inte p�skafton med?
('2018-04-01', 'P�sk', 5),
('2018-04-02', 'P�sk', 5),
('2018-04-03', 'P�sk', 6),
('2018-05-01', 'F�rsta maj', 3),
('2018-05-10', 'Kristi himmelf�rdsdag', 3),
('2018-05-18', 'Pingst', 4),
('2018-05-19', 'Pingst', 5),
('2018-05-20', 'Pingst', 5),
('2018-05-21', 'Pingst', 6),
('2018-06-06', 'Sveriges nationaldag', 3),
('2018-06-22', 'Midsommar', 4),  --dan f�re midsommarafton b�r vara med och sj�lva midsommarafton ska vara 5 eller?
('2018-06-23', 'Midsommar', 5),
('2018-06-24', 'Midsommar', 5),
('2018-06-25', 'Midsommar', 6),
('2018-11-03', 'Alla helgons dag', 3),
('2018-12-23', 'Jul', 4),
('2018-12-24', 'Jul', 5),
('2018-12-25', 'Jul', 5),
('2018-12-26', 'Jul', 5),
('2018-12-27', 'Jul', 6),
('2018-12-30', 'Ny�r', 4),
('2018-12-31', 'Ny�r', 5),
--2019
('2019-01-01', 'Ny�r', 5),
('2019-01-02', 'Ny�r', 6),
('2019-01-06', 'Trettondedag jul', 3),
('2019-04-18', 'P�sk', 4), --sk�rtorsdag
('2019-04-19', 'P�sk', 5), --l�ngfredag
('2019-04-20', 'P�sk', 5), --p�skafton (2018 lades den inte in)?
('2019-04-21', 'P�sk', 5), --p�skdag
('2019-04-22', 'P�sk', 5), --annandag p�sk
('2019-04-23', 'P�sk', 6),
('2019-05-01', 'F�rsta maj', 3),
('2019-05-30', 'Kristi himmelf�rdsdag', 3),
('2019-06-06', 'Sveriges nationaldag', 3),
('2019-06-07', 'Pingst', 4),
('2019-06-08', 'Pingst', 5),
('2019-06-09', 'Pingst', 5),
('2019-06-10', 'Pingst', 6),
('2019-06-20', 'Midsommar', 4), --var midsommar inlagt korrekt 2018? torsdag v�l b�r r�knas som dagen f�re afton? den var inte inlagd
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
('2019-12-30', 'Ny�r', 4),
('2019-12-31', 'Ny�r', 5)

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


