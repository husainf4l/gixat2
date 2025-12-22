#!/bin/bash

TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJhYTcwZDY0Yi02NjlhLTQwNGYtYmFhNi04ZDFkN2Y0ZjU3MjMiLCJlbWFpbCI6ImFsLWh1c3NlaW5AcGFwYXlhdHJhZGluZy5jb20iLCJqdGkiOiI2NWJiMzRiOC1mYWRjLTQyYjYtOGE5MC05ODZlMzU3NjhmMWQiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6ImFhNzBkNjRiLTY2OWEtNDA0Zi1iYWE2LThkMWQ3ZjRmNTcyMyIsIk9yZ2FuaXphdGlvbklkIjoiMGNiNWI1YTQtZWQ0OC00YWY1LWFhZjMtNWM4NjliMzI3ZmU5IiwiZXhwIjoxNzY3MDQxMDY5LCJpc3MiOiJHaXhhdEJhY2tlbmQiLCJhdWQiOiJHaXhhdFVzZXJzIn0.afH7quG7HyjWJ4JsnMpwrXLVPBlHRvMhwiIsKmnymMo"

# Use the car ID from the last test: 3bb223e3-c2cd-4006-8e53-92b83bdc6566
CAR_ID="3bb223e3-c2cd-4006-8e53-92b83bdc6566"

echo "Querying for car: $CAR_ID"
echo ""

curl -s -X POST http://localhost:8002/graphql \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "query": "query GetCarById($id: UUID!) { carById(id: $id) { id customerId make model year licensePlate vin color createdAt customer { id firstName lastName organizationId } } }",
    "variables": {
      "id": "'"$CAR_ID"'"
    }
  }' | jq .
