SELECT Id, Name, Type, Kind FROM Data
/*% if (id != null) { */
WHERE Id >= /*@ id */0
/*% } */
