#!/bin/bash

# First, login to get the token
LOGIN_RESPONSE=$(curl -s -X POST http://localhost:8002/graphql \
  -H "Content-Type: application/json" \
  -d '{
    "query": "mutation Login($email: String!, $password: String!) { login(input: { email: $email, password: $password }) { token user { id email fullName } error } }",
    "variables": {
      "email": "al-hussein@papayatrading.com",
      "password": "TT%%oo77"
    }
  }')

echo "Login Response:"
echo $LOGIN_RESPONSE | jq '.'

# Extract the token
TOKEN=$(echo $LOGIN_RESPONSE | jq -r '.data.login.token')

if [ "$TOKEN" == "null" ] || [ -z "$TOKEN" ]; then
  echo "Login failed!"
  exit 1
fi

echo -e "\n\nToken obtained: ${TOKEN:0:50}...\n"

# Now query customers with timing - without cars array, just totalCars count
echo "Querying customers..."
TIME_OUTPUT=$(curl -s -X POST http://localhost:8002/graphql \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -w "\n%{time_total}" \
  -d '{
    "query": "query GetCustomers { customers(first: 10) { pageInfo { hasNextPage hasPreviousPage startCursor endCursor } totalCount edges { cursor node { id firstName lastName email phoneNumber address { city } lastSessionDate totalVisits totalSpent activeJobCards totalCars } } } }"
  }')

# Split output and timing
JSON_OUTPUT=$(echo "$TIME_OUTPUT" | head -n -1)
TIME_TAKEN=$(echo "$TIME_OUTPUT" | tail -n 1)

echo "$JSON_OUTPUT" | jq '.'
echo ""
echo "⏱️  Total Time: ${TIME_TAKEN}s"
