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

# Extract the token
TOKEN=$(echo $LOGIN_RESPONSE | jq -r '.data.login.token')

if [ "$TOKEN" == "null" ] || [ -z "$TOKEN" ]; then
  echo "Login failed!"
  exit 1
fi

echo "Token obtained successfully"
echo ""

# Test Me query 5 times to get average
echo "Testing Me query performance (5 iterations)..."
echo ""

for i in {1..5}; do
  echo "Test $i:"
  curl -s -X POST http://localhost:8002/graphql \
    -H "Content-Type: application/json" \
    -H "Authorization: Bearer $TOKEN" \
    -w "Time: %{time_total}s\n\n" \
    -o /dev/null \
    -d '{
      "operationName": "Me",
      "query": "query Me { me { id email fullName roles organizationId organization { id name __typename } __typename } }",
      "variables": {}
    }'
done
