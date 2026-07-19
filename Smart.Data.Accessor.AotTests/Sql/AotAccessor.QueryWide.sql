SELECT
    "col_дата",
    t2_value,
    last_modified_by_x AS LAST_MODIFIED_BY_X,
    1,
    extra_unmapped,
    t1_value,
    "user_名前" AS "USER_名前",
    id AS ID,
    age,
    city,
    email,
    status,
    item_code,
    created_at,
    status_code,
    display_name,
    department_id,
    address_line_1,
    manager_user_id,
    organization_cd1,
    registration_date
FROM Wide
ORDER BY id
