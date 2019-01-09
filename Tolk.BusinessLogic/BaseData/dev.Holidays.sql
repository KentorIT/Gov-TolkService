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
('2019-12-31', 'Ny�r', 5),

--2020
('2020-01-01', 'Ny�r', 5),
('2020-01-02', 'Ny�r', 6),
('2020-01-06', 'Trettondedag jul', 3),
('2020-04-09', 'P�sk', 4), --sk�rtorsdag
('2020-04-10', 'P�sk', 5), --l�ngfredag
('2020-04-11', 'P�sk', 5), --p�skafton
('2020-04-12', 'P�sk', 5), --p�skdag
('2020-04-13', 'P�sk', 5), --annandag p�sk
('2020-04-14', 'P�sk', 6),
('2020-05-01', 'F�rsta maj', 3),
('2020-05-21', 'Kristi himmelf�rdsdag', 3),
('2020-05-29', 'Pingst', 4),
('2020-05-30', 'Pingst', 5),
('2020-05-31', 'Pingst', 5),
('2020-06-01', 'Pingst', 6),
('2020-06-06', 'Sveriges nationaldag', 3),
('2020-06-18', 'Midsommar', 4),
('2020-06-19', 'Midsommar', 5),
('2020-06-20', 'Midsommar', 5),
('2020-06-21', 'Midsommar', 5),
('2020-06-22', 'Midsommar', 6),
('2020-10-31', 'Alla helgons dag', 3),
('2020-12-23', 'Jul', 4),
('2020-12-24', 'Jul', 5),
('2020-12-25', 'Jul', 5),
('2020-12-26', 'Jul', 5),
('2020-12-27', 'Jul', 6),
('2020-12-30', 'Ny�r', 4),
('2020-12-31', 'Ny�r', 5),

--2021
('2021-01-01', 'Ny�r', 5),
('2021-01-02', 'Ny�r', 6),
('2021-01-06', 'Trettondedag jul', 3),
('2021-04-01', 'P�sk', 4), --sk�rtorsdag
('2021-04-02', 'P�sk', 5), --l�ngfredag
('2021-04-03', 'P�sk', 5), --p�skafton
('2021-04-04', 'P�sk', 5), --p�skdag
('2021-04-05', 'P�sk', 5), --annandag p�sk
('2021-04-06', 'P�sk', 6),
('2021-05-01', 'F�rsta maj', 3),
('2021-05-13', 'Kristi himmelf�rdsdag', 3),
('2021-05-21', 'Pingst', 4),
('2021-05-22', 'Pingst', 5),
('2021-05-23', 'Pingst', 5),
('2021-05-24', 'Pingst', 6),
('2021-06-06', 'Sveriges nationaldag', 3),
('2021-06-24', 'Midsommar', 4), 
('2021-06-25', 'Midsommar', 5),
('2021-06-26', 'Midsommar', 5),
('2021-06-27', 'Midsommar', 5),
('2021-06-28', 'Midsommar', 6),
('2021-11-06', 'Alla helgons dag', 3),
('2021-12-23', 'Jul', 4),
('2021-12-24', 'Jul', 5),
('2021-12-25', 'Jul', 5),
('2021-12-26', 'Jul', 5),
('2021-12-27', 'Jul', 6),
('2021-12-30', 'Ny�r', 4),
('2021-12-31', 'Ny�r', 5)

----2022
--('2022-01-01', 'Ny�r', 5),
--('2022-01-02', 'Ny�r', 6),
--('2022-01-06', 'Trettondedag jul', 3),
--('2022-04-14', 'P�sk', 4), --sk�rtorsdag
--('2022-04-15', 'P�sk', 5), --l�ngfredag
--('2022-04-16', 'P�sk', 5), --p�skafton
--('2022-04-17', 'P�sk', 5), --p�skdag
--('2022-04-18', 'P�sk', 5), --annandag p�sk
--('2022-04-19', 'P�sk', 6),
--('2022-05-01', 'F�rsta maj', 3),
--('2022-05-26', 'Kristi himmelf�rdsdag', 3),
--('2022-06-03', 'Pingst', 4),
--('2022-06-04', 'Pingst', 5),
--('2022-06-05', 'Pingst', 5),
--('2022-06-06', 'Pingst', 6), --6 juni inlagt dubbelt b�r testas av
--('2022-06-06', 'Sveriges nationaldag', 3),
--('2022-06-23', 'Midsommar', 4),
--('2022-06-24', 'Midsommar', 5),
--('2022-06-25', 'Midsommar', 5),
--('2022-06-26', 'Midsommar', 5),
--('2022-06-27', 'Midsommar', 6),
--('2022-11-05', 'Alla helgons dag', 3),
--('2022-12-23', 'Jul', 4),
--('2022-12-24', 'Jul', 5),
--('2022-12-25', 'Jul', 5),
--('2022-12-26', 'Jul', 5),
--('2022-12-27', 'Jul', 6),
--('2022-12-30', 'Ny�r', 4),
--('2022-12-31', 'Ny�r', 5),

----2023
--('2023-01-01', 'Ny�r', 5),
--('2023-01-02', 'Ny�r', 6),
--('2023-01-06', 'Trettondedag jul', 3),
--('2023-04-06', 'P�sk', 4), --sk�rtorsdag
--('2023-04-07', 'P�sk', 5), --l�ngfredag
--('2023-04-08', 'P�sk', 5), --p�skafton
--('2023-04-09', 'P�sk', 5), --p�skdag
--('2023-04-10', 'P�sk', 5), --annandag p�sk
--('2023-04-11', 'P�sk', 6),
--('2023-05-01', 'F�rsta maj', 3),
--('2023-05-18', 'Kristi himmelf�rdsdag', 3),
--('2023-05-26', 'Pingst', 4),
--('2023-05-27', 'Pingst', 5),
--('2023-05-28', 'Pingst', 5),
--('2023-05-29', 'Pingst', 6), 
--('2023-06-06', 'Sveriges nationaldag', 3),
--('2023-06-22', 'Midsommar', 4),
--('2023-06-23', 'Midsommar', 5),
--('2023-06-24', 'Midsommar', 5),
--('2023-06-25', 'Midsommar', 5),
--('2023-06-26', 'Midsommar', 6),
--('2023-11-04', 'Alla helgons dag', 3),
--('2023-12-23', 'Jul', 4),
--('2023-12-24', 'Jul', 5),
--('2023-12-25', 'Jul', 5),
--('2023-12-26', 'Jul', 5),
--('2023-12-27', 'Jul', 6),
--('2023-12-30', 'Ny�r', 4),
--('2023-12-31', 'Ny�r', 5)

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


