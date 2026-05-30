/*!helper System.Math */
/*!using System.Globalization */
SELECT Id, Name, Type, Kind FROM Data
WHERE Type = /*@ type */1
ORDER BY Id
