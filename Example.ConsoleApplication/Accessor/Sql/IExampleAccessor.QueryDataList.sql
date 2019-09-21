SELECT * FROM Data
/*% if (!String.IsNullOrEmpty(type)) { */
WHERE Type = /*@ type */'A'
/*% } */
/*% if (!String.IsNullOrEmpty(order)) { */
ORDER BY /*# order */Id
/*% } */
