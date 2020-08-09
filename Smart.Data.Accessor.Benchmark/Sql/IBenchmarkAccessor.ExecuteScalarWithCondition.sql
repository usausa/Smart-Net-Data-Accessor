SELECT COUNT(*) FROM Data
/*% if (flag != null) { */
WHERE Flag = /*@ flag */'1'
/*% } */
