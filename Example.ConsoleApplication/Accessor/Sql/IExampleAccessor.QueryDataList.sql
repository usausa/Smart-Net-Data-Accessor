SELECT * FROM Data
/*% if (!String.IsNullOrEmpty(type)) { */
WHERE Type = /*@ type */'A'
/*% } */