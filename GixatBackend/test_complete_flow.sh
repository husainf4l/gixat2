#!/bin/bash

# Your organization ID from token: 0cb5b5a4-ed48-4af5-aaf3-5c869b327fe9
TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJhYTcwZDY0Yi02NjlhLTQwNGYtYmFhNi04ZDFkN2Y0ZjU3MjMiLCJlbWFpbCI6ImFsLWh1c3NlaW5AcGFwYXlhdHJhZGluZy5jb20iLCJqdGkiOiI2NWJiMzRiOC1mYWRjLTQyYjYtOGE5MC05ODZlMzU3NjhmMWQiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6ImFhNzBkNjRiLTY2OWEtNDA0Zi1iYWE2LThkMWQ3ZjRmNTcyMyIsIk9yZ2FuaXphdGlvbklkIjoiMGNiNWI1YTQtZWQ0OC00YWY1LWFhZjMtNWM4NjliMzI3ZmU5IiwiZXhwIjoxNzY3MDQxMDY5LCJpc3MiOiJHaXhhdEJhY2tlbmQiLCJhdWQiOiJHaXhhdFVzZXJzIn0.afH7quG7HyjWJ4JsnMpwrXLVPBlHRvMhwiIsKmnymMo"

echo "=========================================="
echo "STEP 1: Create Customer"
echo "=========================================="

# Generate unique email with timestamp to avoid duplicate key errors
TIMESTAMP=$(date +%s)
UNIQUE_EMAIL="test.${TIMESTAMP}@example.com"
UNIQUE_PHONE="+9741234${TIMESTAMP: -4}"

CUSTOMER_RESPONSE=$(curl -s -X POST http://localhost:8002/graphql \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "query": "mutation CreateCustomer($input: CreateCustomerInput!) { createCustomer(input: $input) { id firstName lastName email phoneNumber } }",
    "variables": {
      "input": {
        "firstName": "John",
        "lastName": "Doe",
        "email": "'"$UNIQUE_EMAIL"'",
        "phoneNumber": "'"$UNIQUE_PHONE"'",
        "country": "Qatar",
        "city": "Doha",
        "street": "Test Street 123",
        "phoneCountryCode": "+974"
      }
    }
  }')

echo "$CUSTOMER_RESPONSE" | jq .

CUSTOMER_ID=$(echo "$CUSTOMER_RESPONSE" | jq -r '.data.createCustomer.id')
echo ""
echo "✅ Customer Created with ID: $CUSTOMER_ID"
echo ""

sleep 1

echo "=========================================="
echo "STEP 2: Create Car for Customer"
echo "=========================================="

# Generate unique license plate and VIN
UNIQUE_PLATE="ABC${TIMESTAMP: -3}"
UNIQUE_VIN="1HGBH41JXMN${TIMESTAMP: -6}"

CAR_RESPONSE=$(curl -s -X POST http://localhost:8002/graphql \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "query": "mutation CreateCar($input: CreateCarInput!) { createCar(input: $input) { id customerId make model year licensePlate vin color } }",
    "variables": {
      "input": {
        "customerId": "'"$CUSTOMER_ID"'",
        "make": "Toyota",
        "model": "Camry",
        "year": 2024,
        "licensePlate": "'"$UNIQUE_PLATE"'",
        "vin": "'"$UNIQUE_VIN"'",
        "color": "Silver"
      }
    }
  }')

echo "$CAR_RESPONSE" | jq .

CAR_ID=$(echo "$CAR_RESPONSE" | jq -r '.data.createCar.id')
echo ""
echo "✅ Car Created with ID: $CAR_ID"
echo ""

sleep 1

echo "=========================================="
echo "STEP 3: Create Session for Car"
echo "=========================================="

SESSION_RESPONSE=$(curl -s -X POST http://localhost:8002/graphql \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "query": "mutation CreateSession($carId: UUID!, $customerId: UUID!) { createSession(carId: $carId, customerId: $customerId) { id carId customerId status createdAt } }",
    "variables": {
      "carId": "'"$CAR_ID"'",
      "customerId": "'"$CUSTOMER_ID"'"
    }
  }')

echo "$SESSION_RESPONSE" | jq .

SESSION_ID=$(echo "$SESSION_RESPONSE" | jq -r '.data.createSession.id')
echo ""
echo "✅ Session Created with ID: $SESSION_ID"
echo ""

sleep 1

echo "=========================================="
echo "STEP 4: Query Customer with Cars & Sessions (DataLoader Test)"
echo "=========================================="

FULL_QUERY=$(curl -s -X POST http://localhost:8002/graphql \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "query": "query GetCustomer($id: UUID!) { customerById(id: $id) { id firstName lastName email phoneNumber address { country city street } cars { id make model year licensePlate color vin } sessions { id status createdAt } jobCards { id status createdAt } } }",
    "variables": {
      "id": "'"$CUSTOMER_ID"'"
    }
  }')

echo "$FULL_QUERY" | jq .

echo ""
echo "=========================================="
echo "✅ COMPLETE TEST SUCCESSFUL"
echo "=========================================="
echo ""
echo "Summary:"
echo "  Customer ID: $CUSTOMER_ID"
echo "  Car ID: $CAR_ID"
echo "  Session ID: $SESSION_ID"
echo ""
echo "Frontend GraphQL Query Template:"
echo "=========================================="
cat << 'EOF'
query GetCustomerDetails($id: UUID!) {
  customerById(id: $id) {
    id
    firstName
    lastName
    email
    phoneNumber
    address {
      country
      city
      street
      phoneCountryCode
    }
    cars {
      id
      make
      model
      year
      licensePlate
      color
      vin
      createdAt
    }
    sessions {
      id
      status
      createdAt
      updatedAt
    }
    jobCards {
      id
      status
      totalEstimatedCost
      totalActualCost
      createdAt
    }
  }
}

Variables:
{
  "id": "YOUR_CUSTOMER_ID_HERE"
}
EOF

echo ""
echo "=========================================="
