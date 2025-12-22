#!/bin/bash

# PostgreSQL connection details from your configuration
PGPASSWORD='TT%%oo77' psql -h 31.97.217.73 -p 5432 -U husain -d gixatnet -c "
SELECT 
    c.\"Id\",
    c.\"CustomerId\",
    c.\"OrganizationId\",
    c.\"Make\",
    c.\"Model\",
    c.\"LicensePlate\",
    c.\"CreatedAt\",
    cust.\"OrganizationId\" as CustomerOrgId
FROM \"Cars\" c
LEFT JOIN \"Customers\" cust ON c.\"CustomerId\" = cust.\"Id\"
WHERE c.\"Id\" = '3bb223e3-c2cd-4006-8e53-92b83bdc6566'::uuid;
"
