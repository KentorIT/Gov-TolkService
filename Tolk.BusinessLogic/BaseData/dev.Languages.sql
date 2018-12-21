﻿
CREATE TABLE #Languages(
	[LanguageId] [int] NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
	[Active] [bit] NOT NULL DEFAULT ((0)),
	[ISO_639_Code] [nvarchar](3) NULL,
	[TellusName] [nvarchar](100) NULL
	)

INSERT #Languages (LanguageId, Name, ISO_639_Code, TellusName, Active)
	VALUES 
(1, 'Abchaziska', 'abk', '', 1),
(2, 'Oromo', 'orm', '', 1),
(3, 'Afar', 'aar', '', 1),
(4, 'Afrikaans', 'afr', '', 1),
(5, 'Albanska', 'alb', 'albanska', 1),
(6, 'Amhariska', 'amh', '', 1),
(7, 'Arabiska', 'ara', 'arabiska', 1),
(8, 'Armeniska', 'arm', 'armeniska', 1),
(9, 'Assamesiska', 'asm', '', 1),
(10, 'Aymara', 'aym', '', 1),
(11, 'Azerbajdzjanska', 'aze', 'azerbajdzjanska', 1),
(12, 'Basjkiriska', 'bak', '', 1),
(13, 'Baskiska', 'eus', '', 1),
(14, 'Bengali', 'ben', '', 1),
(15, 'Bihari', 'bih', '', 1),
(16, 'Bislama', 'bis', '', 1),
(17, 'Bosniska', 'bos', 'bosniska, kroatiska, serbiska', 1),
(18, 'Bretonska', 'bre', '', 1),
(19, 'Bulgariska', 'bul', 'bulgariska', 1),
(20, 'Burmesiska', 'mya', '', 1),
(21, 'Centralkurdiska', 'ckb', '', 1),
(22, 'Danska', 'dan', 'danska', 1),
(23, 'Dari', 'prs', 'dari', 1),
(24, 'Dzongkha', 'dzo', '', 1),
(25, 'Engelsk', 'eng', 'engelska', 1),
(26, 'Esperanto', 'epo', '', 1),
(27, 'Estniska', 'est', 'estniska', 1),
(28, 'Fijianska', 'fij', '', 1),
(29, 'Finska', 'fin', 'finska', 1),
(30, 'Franska', 'fre', 'franska', 1),
(31, 'Frisiska', 'fry', '', 1),
(32, 'Färöiska', 'fao', '', 1),
(33, 'Gaeliska', 'gla', '', 1),
(34, 'Galiciska', 'glg', '', 1),
(35, 'Georgiska', 'kat', 'georgiska', 1),
(36, 'Grekiska', 'gre', 'grekiska', 1),
(37, 'Grönländska', 'kal', '', 1),
(38, 'Guarani', 'grn', '', 1),
(39, 'Gujarati', 'guj', '', 1),
(40, 'Hausa', 'hau', '', 1),
(41, 'Hebreiska', 'heb', 'hebreiska', 1),
(42, 'Hindi', 'hin', 'hindi', 1),
(43, 'Indonesiska', 'ind', 'indonesiska', 1),
(44, 'Interlingua', 'ina', '', 1),
(45, 'Inuktitut', 'iku', '', 1),
(46, 'Iñupiaq', 'ipk', '', 1),
(47, 'Iriska', 'gle', '', 1),
(48, 'Isländska', 'isl', 'isländska', 1),
(49, 'Italienska', 'ita', 'italienska', 1),
(50, 'Japanska', 'jpn', 'japanska', 1),
(51, 'Javanesiska', 'jav', '', 1),
(52, 'Jiddisch', 'yid', '', 1),
(53, 'Kambodjanska', 'khm', '', 1),
(54, 'Kannada', 'kan', '', 1),
(55, 'Kantonesiska', 'yue', 'kantonesiska', 1),
(56, 'Kashmiri', 'kas', '', 1),
(57, 'Katalanska', 'cat', '', 1),
(58, 'Kazakiska', 'kaz', '', 1),
(59, 'Kinesiska', 'zho', 'kinesiska', 1),
(60, 'Kinyarwanda', 'kin', '', 1),
(61, 'Kirgiziska', 'kir', '', 1),
(62, 'Kirundi', 'run', 'kirundi', 1),
(63, 'Koreanska', 'kor', 'koreanska', 1),
(64, 'Korsikanska', 'cos', '', 1),
(65, 'Kymriska', 'wel', '', 1),
(66, 'Laotiska', 'lao', '', 1),
(67, 'Latin', 'lat', '', 1),
(68, 'Lettiska', 'lav', 'lettiska', 1),
(69, 'Lingala', 'lin', '', 1),
(70, 'Litauiska', 'lit', 'litauiska', 1),
(71, 'Madagaskiska', 'mlg', '', 1),
(72, 'Makedonska', 'mkd', 'makedonska', 1),
(73, 'Malajiska', 'msa', '', 1),
(74, 'Malayalam', 'mal', '', 1),
(75, 'Maltesiska', 'mlt', '', 1),
(76, 'Maori', 'mri', '', 1),
(77, 'Marathi', 'mar', '', 1),
(78, 'Meänkieli', 'fit', 'meänkieli', 1),
(79, 'Moldaviska', 'ron', '', 1),
(80, 'Mongoliska', 'mon', 'mongoliska', 1),
(81, 'Montenegrinska', 'cnr', '', 1),
(82, 'Nauriska', 'nau', '', 1),
(83, 'Nederländska', 'nld', 'nederländska', 1),
(84, 'Nepali', 'nep', 'nepali', 1),
(85, 'Nordkurdiska', 'kmr', 'nordkurdiska', 1),
(86, 'Norska', 'nor', 'norska', 1),
(87, 'Nyanja', 'bnt', '', 1),
(88, 'Occitanska', 'oci', '', 1),
(89, 'Oriya', 'ori', '', 1),
(90, 'Pashto', 'pus', 'pashto', 1),
(91, 'Persiska', 'fas', 'persiska', 1),
(92, 'Polska', 'pol', 'polska', 1),
(93, 'Portugisiska', 'por', 'portugisiska', 1),
(94, 'Punjabi', 'pan', '', 1),
(95, 'Quechua', 'que', '', 1),
(96, 'Rikskinesiska', 'cmn', 'rikskinesiska', 1),
(97, 'Romska', 'rom', 'romska', 1),
(98, 'Rumantsch', 'roh', '', 1),
(99, 'Rumänska', 'rum', 'rumänska', 1),
(100, 'Ryska', 'rus', 'ryska', 1),
(101, 'Samiska (nordsamiska)', 'sme', 'samiska (nordamiska)', 1),
(102, 'Samoanska', 'smo', '', 1),
(103, 'Sangho', 'sag', '', 1),
(104, 'Sanskrit', 'san', '', 1),
(105, 'Sardiska', 'srd', '', 1),
(106, 'Serbiska', 'srp', '', 1),
(107, 'Serbo-croation', 'hbs', '', 1),
(108, 'Sesotho', 'sot', '', 1),
(109, 'Setswana', 'tsn', '', 1),
(110, 'Shona', 'sna', '', 1),
(111, 'Sindhi', 'snd', '', 1),
(112, 'Singalesiska', 'sin', '', 1),
(113, 'Siswati', 'ssw', '', 1),
(114, 'Slovakiska', 'slk', 'slovakiska', 1),
(115, 'Slovenska', 'slv', 'slovenska', 1),
(116, 'Somaliska', 'som', 'somaliska', 1),
(117, 'Spanska', 'spa', 'spanska', 1),
(118, 'Sundanesiska', 'sun', '', 1),
(119, 'Swahili', 'swa', 'swahili', 1),
(120, 'Sydkurdiska', 'sdh', 'sydkurdiska', 1),
(121, 'Tadzjikiska', 'tgk', '', 1),
(122, 'Tagalog', 'tgl', 'tagalog', 1),
(123, 'Tamil', 'tam', '', 1),
(124, 'Tatariska', 'tat', '', 1),
(125, 'Telugu', 'tel', '', 1),
(126, 'Thailändska', 'tha', 'thai', 1),
(127, 'Tibetanska', 'bod', '', 1),
(128, 'Tigrinska', 'tir', 'tigrinska', 1),
(129, 'Tjeckiska', 'ces', 'tjeckiska', 1),
(130, 'Tjuvasjiska', 'chv', '', 1),
(131, 'Tonganska', 'ton', '', 1),
(132, 'Tsonga', 'tso', '', 1),
(133, 'Turkiska', 'tur', 'turkiska', 1),
(134, 'Turkmenska', 'tuk', '', 1),
(135, 'Twi', 'twi', '', 1),
(136, 'Tyska', 'ger', 'tyska', 1),
(137, 'Uiguriska', 'uig', '', 1),
(138, 'Ukrainska', 'ukr', 'ukrainska', 1),
(139, 'Ungerska', 'hun', 'ungerska', 1),
(140, 'Urdu', 'urd', 'urdu', 1),
(141, 'Uzbekiska', 'uzb', 'uzbekiska', 1),
(142, 'Vietnamesiska', 'vie', 'vietnamesiska', 1),
(143, 'Vitryska', 'bel', 'vitryska', 1),
(144, 'Volapük', 'vol', '', 1),
(145, 'Wolof', 'wol', '', 1),
(146, 'Xhosa', 'xho', '', 1),
(147, 'Yoruba', 'yor', '', 1),
(148, 'Zhuang', 'zha', '', 1),
(149, 'Zulu', 'zul', '', 1),
(1000, 'Övrigt', Null, '', 1)

SET IDENTITY_INSERT Languages ON

MERGE Languages dst
USING #Languages src
ON (src.LanguageId = dst.LanguageId)
WHEN MATCHED THEN
UPDATE SET dst.Name = src.Name, dst.Active = src.Active, dst.TellusName = src.Tellusname, dst.ISO_639_Code = src.ISO_639_Code
WHEN NOT MATCHED THEN
INSERT (LanguageId, Name, ISO_639_Code, TellusName, Active)
VALUES (src.LanguageId, src.Name, src.ISO_639_Code, src.TellusName, src.Active);

SET IDENTITY_INSERT Languages OFF

DROP TABLE #Languages

