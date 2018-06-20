
CREATE TABLE #Languages(
	[LanguageId] [int] NOT NULL,
	[Name] [nvarchar](100) NOT NULL
	)

insert #Languages(LanguageId, [Name])
Values
(1, 'Albanska'),
(2, 'Arabiska'),
(3, 'Arameiska/Syriska'),
(4, 'Armeniska'),
(5, 'Azerbajdzjanska'),
(6, 'Bosniska, Kroatiska, Serbiska'),
(7, 'Bulgariska'),
(8, 'Danska'),
(9, 'Dari'),
(10, 'Engelska'),
(11, 'Estniska'),
(12, 'Finska'),
(13, 'Franska'),
(14, 'Georgiska'),
(15, 'Grekiska'),
(16, 'Hebreiska'),
(17, 'Hindi'),
(18, 'Indonesiska'),
(19, 'Isländska'),
(20, 'Italienska'),
(21, 'Japanska'),
(22, 'Kantonesiska'),
(23, 'Kinesiska'),
(24, 'Kirundi'),
(25, 'Koreanska'),
(26, 'Lettiska'),
(27, 'Litauiska'),
(28, 'Makedonska'),
(29, 'Meänkieli'),
(30, 'Mongoliska'),
(31, 'Nederländska'),
(32, 'Nepali'),
(33, 'Nordkurdiska'),
(34, 'Norska'),
(35, 'Pashto'),
(36, 'Persiska'),
(37, 'Polska'),
(38, 'Portugisiska'),
(39, 'Rikskinesiska'),
(40, 'Romska'),
(41, 'Rumänska'),
(42, 'Ryska'),
(43, 'Samiska (Nordsamiska)'),
(44, 'Slovakiska'),
(45, 'Slovenska'),
(46, 'Somaliska'),
(47, 'Spanska'),
(48, 'Swahili'),
(49, 'Sydkurdiska'),
(50, 'Tagalog'),
(51, 'Thai'),
(52, 'Tigrinska'),
(53, 'Tjeckiska'),
(54, 'Turkiska'),
(55, 'Tyska'),
(56, 'Ukrainska'),
(57, 'Ungerska'),
(58, 'Urdu'),
(59, 'Uzbekiska'),
(60, 'Vietnamesiska'),
(61, 'Vitryska'),
(62, 'Övrigt språk')

Set IDENTITY_INSERT Languages ON

MERGE Languages dst
USING #Languages src
ON (src.LanguageId = dst.LanguageId)
WHEN MATCHED THEN
UPDATE SET dst.Name = src.Name
WHEN NOT MATCHED THEN
INSERT (LanguageId, Name)
VALUES (src.LanguageId, src.Name);

Set IDENTITY_INSERT Languages OFF

DROP TABLE #Languages