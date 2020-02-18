Select * from PriceListRows

update PriceListRows 
Set EndDate = '20191231'
Where EndDate = '99991231'

SET IDENTITY_INSERT PriceListRows ON
INSERT PriceListRows (PriceListRowId, PriceListType, StartDate, EndDate, MaxMinutes, Price, CompetenceLevel, PriceListRowType)

Select 257, 1, '20200101', '99991231', 60, 366, 1, 1
UNION ALL		  
Select 258, 1, '20200101', '99991231', 60, 426, 2, 1
UNION ALL		  
Select 259, 1, '20200101', '99991231', 60, 501, 3, 1
UNION ALL		  
Select 260, 1, '20200101', '99991231', 60, 632, 4, 1

UNION ALL

Select 261, 1, '20200101', '99991231', 90, 498, 1, 1
UNION ALL
Select 262, 1, '20200101', '99991231', 90, 591, 2, 1
UNION ALL
Select 263, 1, '20200101', '99991231', 90, 693, 3, 1
UNION ALL
Select 264, 1, '20200101', '99991231', 90, 881, 4, 1

UNION ALL

Select 265, 1, '20200101', '99991231', 120, 630, 1, 1
UNION ALL
Select 266, 1, '20200101', '99991231', 120, 756, 2, 1
UNION ALL
Select 267, 1, '20200101', '99991231', 120, 885, 3, 1
UNION ALL
Select 268, 1, '20200101', '99991231', 120, 1130, 4, 1

UNION ALL

Select 269, 1, '20200101', '99991231', 150, 762, 1, 1
UNION ALL
Select 270, 1, '20200101', '99991231', 150, 921, 2, 1
UNION ALL
Select 271, 1, '20200101', '99991231', 150, 1077, 3, 1
UNION ALL
Select 272, 1, '20200101', '99991231', 150, 1379, 4, 1

UNION ALL

Select 273, 1, '20200101', '99991231', 180, 894, 1, 1
UNION ALL
Select 274, 1, '20200101', '99991231', 180, 1086, 2, 1
UNION ALL
Select 275, 1, '20200101', '99991231', 180, 1269, 3, 1
UNION ALL
Select 276, 1, '20200101', '99991231', 180, 1628, 4, 1

UNION ALL

Select 277, 1, '20200101', '99991231', 210, 1026, 1, 1
UNION ALL
Select 278, 1, '20200101', '99991231', 210, 1251, 2, 1
UNION ALL
Select 279, 1, '20200101', '99991231', 210, 1461, 3, 1
UNION ALL
Select 280, 1, '20200101', '99991231', 210, 1877, 4, 1

UNION ALL

Select 281, 1, '20200101', '99991231', 240, 1158, 1, 1
UNION ALL
Select 282, 1, '20200101', '99991231', 240, 1416, 2, 1
UNION ALL
Select 283, 1, '20200101', '99991231', 240, 1653, 3, 1
UNION ALL
Select 284, 1, '20200101', '99991231', 240, 2126, 4, 1

UNION ALL

Select 285, 1, '20200101', '99991231', 270, 1290, 1, 1
UNION ALL
Select 286, 1, '20200101', '99991231', 270, 1581, 2, 1
UNION ALL
Select 287, 1, '20200101', '99991231', 270, 1845, 3, 1
UNION ALL
Select 288, 1, '20200101', '99991231', 270, 2375, 4, 1

UNION ALL

Select 289, 1, '20200101', '99991231', 300, 1422, 1, 1
UNION ALL
Select 290, 1, '20200101', '99991231', 300, 1746, 2, 1
UNION ALL
Select 291, 1, '20200101', '99991231', 300, 2037, 3, 1
UNION ALL
Select 292, 1, '20200101', '99991231', 300, 2624, 4, 1

UNION ALL

Select 293, 1, '20200101', '99991231', 330, 1554, 1, 1
UNION ALL
Select 294, 1, '20200101', '99991231', 330, 1911, 2, 1
UNION ALL
Select 295, 1, '20200101', '99991231', 330, 2229, 3, 1
UNION ALL
Select 296, 1, '20200101', '99991231', 330, 2873, 4, 1

UNION ALL

Select 297, 2, '20200101', '99991231', 60, 305, 1, 1
UNION ALL
Select 298, 2, '20200101', '99991231', 60, 354, 2, 1
UNION ALL
Select 299, 2, '20200101', '99991231', 60, 416, 3, 1
UNION ALL
Select 300, 2, '20200101', '99991231', 60, 532, 4, 1

UNION ALL

Select 301, 2, '20200101', '99991231', 90, 437, 1, 1
UNION ALL
Select 302, 2, '20200101', '99991231', 90, 513, 2, 1
UNION ALL
Select 303, 2, '20200101', '99991231', 90, 601, 3, 1
UNION ALL
Select 304, 2, '20200101', '99991231', 90, 768, 4, 1

UNION ALL

Select 305, 2, '20200101', '99991231', 120, 569, 1, 1
UNION ALL
Select 306, 2, '20200101', '99991231', 120, 672, 2, 1
UNION ALL
Select 307, 2, '20200101', '99991231', 120, 786, 3, 1
UNION ALL
Select 308, 2, '20200101', '99991231', 120, 1004, 4, 1

UNION ALL

Select 309, 2, '20200101', '99991231', 150, 701, 1, 1
UNION ALL
Select 310, 2, '20200101', '99991231', 150, 831, 2, 1
UNION ALL
Select 311, 2, '20200101', '99991231', 150, 971, 3, 1
UNION ALL
Select 312, 2, '20200101', '99991231', 150, 1240, 4, 1

UNION ALL

Select 313, 2, '20200101', '99991231', 180, 833, 1, 1
UNION ALL
Select 314, 2, '20200101', '99991231', 180, 990, 2, 1
UNION ALL
Select 315, 2, '20200101', '99991231', 180, 1156, 3, 1
UNION ALL
Select 316, 2, '20200101', '99991231', 180, 1476, 4, 1

UNION ALL

Select 317, 2, '20200101', '99991231', 210, 965, 1, 1
UNION ALL
Select 318, 2, '20200101', '99991231', 210, 1149, 2, 1
UNION ALL
Select 319, 2, '20200101', '99991231', 210, 1341, 3, 1
UNION ALL
Select 320, 2, '20200101', '99991231', 210, 1712, 4, 1

UNION ALL

Select 321, 2, '20200101', '99991231', 240, 1097, 1, 1
UNION ALL
Select 322, 2, '20200101', '99991231', 240, 1308, 2, 1
UNION ALL
Select 323, 2, '20200101', '99991231', 240, 1526, 3, 1
UNION ALL
Select 324, 2, '20200101', '99991231', 240, 1948, 4, 1

UNION ALL

Select 325, 2, '20200101', '99991231', 270, 1229, 1, 1
UNION ALL
Select 326, 2, '20200101', '99991231', 270, 1467, 2, 1
UNION ALL
Select 327, 2, '20200101', '99991231', 270, 1711, 3, 1
UNION ALL
Select 328, 2, '20200101', '99991231', 270, 2184, 4, 1

UNION ALL

Select 329, 2, '20200101', '99991231', 300, 1361, 1, 1
UNION ALL
Select 330, 2, '20200101', '99991231', 300, 1626, 2, 1
UNION ALL
Select 331, 2, '20200101', '99991231', 300, 1896, 3, 1
UNION ALL
Select 332, 2, '20200101', '99991231', 300, 2420, 4, 1

UNION ALL

Select 333, 2, '20200101', '99991231', 330, 1493, 1, 1
UNION ALL
Select 334, 2, '20200101', '99991231', 330, 1785, 2, 1
UNION ALL
Select 335, 2, '20200101', '99991231', 330, 2081, 3, 1
UNION ALL
Select 336, 2, '20200101', '99991231', 330, 2656, 4, 1

--PriceOverMaxTime,	Court
UNION ALL

Select 337, 1, '20200101', '99991231', 30, 132, 1, 2
UNION ALL
Select 338, 1, '20200101', '99991231', 30, 165, 2, 2
UNION ALL
Select 339, 1, '20200101', '99991231', 30, 192, 3, 2
UNION ALL
Select 340, 1, '20200101', '99991231', 30, 249, 4, 2

--PriceOverMaxTime,	Other
UNION ALL

Select 341, 2, '20200101', '99991231', 30, 132, 1, 2
UNION ALL
Select 342, 2, '20200101', '99991231', 30, 159, 2, 2
UNION ALL
Select 343, 2, '20200101', '99991231', 30, 185, 3, 2
UNION ALL
Select 344, 2, '20200101', '99991231', 30, 236, 4, 2

--InconvenientWorkingHours,	Court
UNION ALL

Select 345, 1, '20200101', '99991231', 30, 80, 1, 3
UNION ALL
Select 346, 1, '20200101', '99991231', 30, 105, 2, 3
UNION ALL
Select 347, 1, '20200101', '99991231', 30, 124, 3, 3
UNION ALL
Select 348, 1, '20200101', '99991231', 30, 141, 4, 3

--InconvenientWorkingHours,	Other
UNION ALL

Select 349, 2, '20200101', '99991231', 30, 80, 1, 3
UNION ALL
Select 350, 2, '20200101', '99991231', 30, 105, 2, 3
UNION ALL
Select 351, 2, '20200101', '99991231', 30, 124, 3, 3
UNION ALL
Select 352, 2, '20200101', '99991231', 30, 141, 4, 3

--WeekendIWH, Court
UNION ALL

Select 353, 1, '20200101', '99991231', 30, 132, 1, 4
UNION ALL
Select 354, 1, '20200101', '99991231', 30, 165, 2, 4
UNION ALL
Select 355, 1, '20200101', '99991231', 30, 192, 3, 4
UNION ALL
Select 356, 1, '20200101', '99991231', 30, 249, 4, 4

--WeekendIWH, Other
UNION ALL

Select 357, 2, '20200101', '99991231', 30, 132, 1, 4
UNION ALL
Select 358, 2, '20200101', '99991231', 30, 165, 2, 4
UNION ALL
Select 359, 2, '20200101', '99991231', 30, 192, 3, 4
UNION ALL
Select 360, 2, '20200101', '99991231', 30, 249, 4, 4

--BigHolidayWeekendIWH, Court
UNION ALL

Select 361, 1, '20200101', '99991231', 30, 160, 1, 5
UNION ALL
Select 362, 1, '20200101', '99991231', 30, 210, 2, 5
UNION ALL
Select 363, 1, '20200101', '99991231', 30, 248, 3, 5
UNION ALL
Select 364, 1, '20200101', '99991231', 30, 282, 4, 5

--BigHolidayWeekendIWH, Other
UNION ALL

Select 365, 2, '20200101', '99991231', 30, 160, 1, 5
UNION ALL
Select 366, 2, '20200101', '99991231', 30, 210, 2, 5
UNION ALL
Select 367, 2, '20200101', '99991231', 30, 248, 3, 5
UNION ALL
Select 368, 2, '20200101', '99991231', 30, 282, 4, 5

--LostTime, Court
UNION ALL

Select 369, 1, '20200101', '99991231', 60, 199, 1, 6
UNION ALL
Select 370, 1, '20200101', '99991231', 60, 233, 2, 6
UNION ALL
Select 371, 1, '20200101', '99991231', 60, 275, 3, 6
UNION ALL
Select 372, 1, '20200101', '99991231', 60, 364, 4, 6

--LostTime, Other
UNION ALL

Select 373, 2, '20200101', '99991231', 60, 199, 1, 6
UNION ALL
Select 374, 2, '20200101', '99991231', 60, 233, 2, 6
UNION ALL
Select 375, 2, '20200101', '99991231', 60, 275, 3, 6
UNION ALL
Select 376, 2, '20200101', '99991231', 60, 364, 4, 6

--LostTime, Court
UNION ALL

Select 377, 1, '20200101', '99991231', 30, 80, 1, 7
UNION ALL
Select 378, 1, '20200101', '99991231', 30, 105, 2, 7
UNION ALL
Select 379, 1, '20200101', '99991231', 30, 124, 3, 7
UNION ALL
Select 380, 1, '20200101', '99991231', 30, 141, 4, 7

--LostTime, Other
UNION ALL

Select 381, 2, '20200101', '99991231', 30, 80, 1, 7
UNION ALL
Select 382, 2, '20200101', '99991231', 30, 105, 2, 7
UNION ALL
Select 383, 2, '20200101', '99991231', 30, 124, 3, 7
UNION ALL
Select 384, 2, '20200101', '99991231', 30, 141, 4, 7

SET IDENTITY_INSERT PriceListRows Off
Select * from PriceListRows