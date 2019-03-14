CREATE TABLE #Holidays(
	[Date] [date] NOT NULL,
	[Name] [nvarchar](255) NOT NULL,
	[DateType] [int] NOT NULL)

GO

INSERT INTO #Holidays (Date, Name, DateType)
	VALUES

--2019
('2019-01-01', 'Nyår', 5),
('2019-01-02', 'Nyår', 6),
('2019-01-06', 'Trettondedag jul', 3),
('2019-04-18', 'Påsk', 4), --skärtorsdag
('2019-04-19', 'Påsk', 5), --långfredag
('2019-04-20', 'Påsk', 5), --påskafton 
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
('2019-06-20', 'Midsommar', 4),
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
('2019-12-31', 'Nyår', 5),

--2020
('2020-01-01', 'Nyår', 5),
('2020-01-02', 'Nyår', 6),
('2020-01-06', 'Trettondedag jul', 3),
('2020-04-09', 'Påsk', 4), --skärtorsdag
('2020-04-10', 'Påsk', 5), --långfredag
('2020-04-11', 'Påsk', 5), --påskafton
('2020-04-12', 'Påsk', 5), --påskdag
('2020-04-13', 'Påsk', 5), --annandag påsk
('2020-04-14', 'Påsk', 6),
('2020-05-01', 'Första maj', 3),
('2020-05-21', 'Kristi himmelfärdsdag', 3),
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
('2020-12-30', 'Nyår', 4),
('2020-12-31', 'Nyår', 5),

--2021
('2021-01-01', 'Nyår', 5),
('2021-01-02', 'Nyår', 6),
('2021-01-06', 'Trettondedag jul', 3),
('2021-04-01', 'Påsk', 4), --skärtorsdag
('2021-04-02', 'Påsk', 5), --långfredag
('2021-04-03', 'Påsk', 5), --påskafton
('2021-04-04', 'Påsk', 5), --påskdag
('2021-04-05', 'Påsk', 5), --annandag påsk
('2021-04-06', 'Påsk', 6),
('2021-05-01', 'Första maj', 3),
('2021-05-13', 'Kristi himmelfärdsdag', 3),
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
('2021-12-30', 'Nyår', 4),
('2021-12-31', 'Nyår', 5),

--2022
('2022-01-01', 'Nyår', 5),
('2022-01-02', 'Nyår', 6),
('2022-01-06', 'Trettondedag jul', 3),
('2022-04-14', 'Påsk', 4), --skärtorsdag
('2022-04-15', 'Påsk', 5), --långfredag
('2022-04-16', 'Påsk', 5), --påskafton
('2022-04-17', 'Påsk', 5), --påskdag
('2022-04-18', 'Påsk', 5), --annandag påsk
('2022-04-19', 'Påsk', 6),
('2022-05-01', 'Första maj', 3),
('2022-05-26', 'Kristi himmelfärdsdag', 3),
('2022-06-03', 'Pingst', 4),
('2022-06-04', 'Pingst', 5),
('2022-06-05', 'Pingst', 5),
('2022-06-06', 'Pingst', 6), --6 juni inlagt dubbelt bör testas av
('2022-06-06', 'Sveriges nationaldag', 3),
('2022-06-23', 'Midsommar', 4),
('2022-06-24', 'Midsommar', 5),
('2022-06-25', 'Midsommar', 5),
('2022-06-26', 'Midsommar', 5),
('2022-06-27', 'Midsommar', 6),
('2022-11-05', 'Alla helgons dag', 3),
('2022-12-23', 'Jul', 4),
('2022-12-24', 'Jul', 5),
('2022-12-25', 'Jul', 5),
('2022-12-26', 'Jul', 5),
('2022-12-27', 'Jul', 6),
('2022-12-30', 'Nyår', 4),
('2022-12-31', 'Nyår', 5),

--2023
('2023-01-01', 'Nyår', 5),
('2023-01-02', 'Nyår', 6),
('2023-01-06', 'Trettondedag jul', 3),
('2023-04-06', 'Påsk', 4), --skärtorsdag
('2023-04-07', 'Påsk', 5), --långfredag
('2023-04-08', 'Påsk', 5), --påskafton
('2023-04-09', 'Påsk', 5), --påskdag
('2023-04-10', 'Påsk', 5), --annandag påsk
('2023-04-11', 'Påsk', 6),
('2023-05-01', 'Första maj', 3),
('2023-05-18', 'Kristi himmelfärdsdag', 3),
('2023-05-26', 'Pingst', 4),
('2023-05-27', 'Pingst', 5),
('2023-05-28', 'Pingst', 5),
('2023-05-29', 'Pingst', 6), 
('2023-06-06', 'Sveriges nationaldag', 3),
('2023-06-22', 'Midsommar', 4),
('2023-06-23', 'Midsommar', 5),
('2023-06-24', 'Midsommar', 5),
('2023-06-25', 'Midsommar', 5),
('2023-06-26', 'Midsommar', 6),
('2023-11-04', 'Alla helgons dag', 3),
('2023-12-23', 'Jul', 4),
('2023-12-24', 'Jul', 5),
('2023-12-25', 'Jul', 5),
('2023-12-26', 'Jul', 5),
('2023-12-27', 'Jul', 6),
('2023-12-30', 'Nyår', 4),
('2023-12-31', 'Nyår', 5)

MERGE Holidays dst
USING #Holidays src
ON (src.Date = dst.Date AND dst.DateType = src.DateType)
WHEN MATCHED THEN
UPDATE SET dst.Name = src.Name, dst.DateType = src.DateType
WHEN NOT MATCHED THEN
INSERT (Date, Name, DateType)
VALUES (src.Date, src.Name, src.DateType)
WHEN NOT MATCHED BY SOURCE THEN
DELETE;

DROP TABLE #Holidays




