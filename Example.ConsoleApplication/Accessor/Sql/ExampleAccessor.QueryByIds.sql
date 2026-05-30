SELECT Id, Name, Type, Kind FROM Data
WHERE Id IN /*@ ids */(1, 2)
ORDER BY Id
