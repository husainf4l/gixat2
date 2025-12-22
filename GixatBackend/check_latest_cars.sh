#!/bin/bash

# PostgreSQL connection details
PGPASSWORD='TT%%oo77' psql -h 31.97.217.73 -p 5432 -U husain -d gixatnet -c "
SELECT 
    c.\"Id\",
    c.\"CustomerId\",
    c.\"OrganizationId\",
    c.\"Make\",
    c.\"Model\",
    c.\"LicensePlate\",
    c.\"CreatedAt\"
FROM \"Cars\" c
ORDER BY c.\"CreatedAt\" DESC
LIMIT 5;
"
