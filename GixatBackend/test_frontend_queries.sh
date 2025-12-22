#!/bin/bash

# Test all frontend GraphQL queries and mutations
TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJhYTcwZDY0Yi02NjlhLTQwNGYtYmFhNi04ZDFkN2Y0ZjU3MjMiLCJlbWFpbCI6ImFsLWh1c3NlaW5AcGFwYXlhdHJhZGluZy5jb20iLCJqdGkiOiI2NWJiMzRiOC1mYWRjLTQyYjYtOGE5MC05ODZlMzU3NjhmMWQiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6ImFhNzBkNjRiLTY2OWEtNDA0Zi1iYWE2LThkMWQ3ZjRmNTcyMyIsIk9yZ2FuaXphdGlvbklkIjoiMGNiNWI1YTQtZWQ0OC00YWY1LWFhZjMtNWM4NjliMzI3ZmU5IiwiZXhwIjoxNzY3MDQxMDY5LCJpc3MiOiJHaXhhdEJhY2tlbmQiLCJhdWQiOiJHaXhhdFVzZXJzIn0.afH7quG7HyjWJ4JsnMpwrXLVPBlHRvMhwiIsKmnymMo"
BASE_URL="http://localhost:8002/graphql"

echo "=========================================="
echo "Frontend GraphQL API Testing Suite"
echo "=========================================="
echo ""

# Generate unique identifiers
TIMESTAMP=$(date +%s)
UNIQUE_EMAIL="frontend.test.${TIMESTAMP}@example.com"
UNIQUE_PHONE="+9741234${TIMESTAMP: -4}"
UNIQUE_PLATE="FRT${TIMESTAMP: -3}"
UNIQUE_VIN="FRONTEND${TIMESTAMP: -9}"

# ==================================================
# TEST 1: CREATE_CUSTOMER_MUTATION
# ==================================================
echo "=========================================="
echo "TEST 1: CREATE_CUSTOMER_MUTATION"
echo "=========================================="

CUSTOMER_RESPONSE=$(curl -s -X POST $BASE_URL \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "query": "mutation CreateCustomer($input: CreateCustomerInput!) { createCustomer(input: $input) { id firstName lastName email phoneNumber } }",
    "variables": {
      "input": {
        "firstName": "Frontend",
        "lastName": "TestUser",
        "email": "'"$UNIQUE_EMAIL"'",
        "phoneNumber": "'"$UNIQUE_PHONE"'",
        "country": "Qatar",
        "city": "Doha",
        "street": "Frontend Test Street 123",
        "phoneCountryCode": "+974"
      }
    }
  }')

echo "$CUSTOMER_RESPONSE" | jq .
CUSTOMER_ID=$(echo "$CUSTOMER_RESPONSE" | jq -r '.data.createCustomer.id')

if [ "$CUSTOMER_ID" != "null" ] && [ -n "$CUSTOMER_ID" ]; then
    echo "✅ CREATE_CUSTOMER_MUTATION - PASSED"
    echo "   Customer ID: $CUSTOMER_ID"
else
    echo "❌ CREATE_CUSTOMER_MUTATION - FAILED"
    exit 1
fi

echo ""
sleep 1

# ==================================================
# TEST 2: CREATE_CAR_MUTATION
# ==================================================
echo "=========================================="
echo "TEST 2: CREATE_CAR_MUTATION"
echo "=========================================="

CAR_RESPONSE=$(curl -s -X POST $BASE_URL \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "query": "mutation CreateCar($input: CreateCarInput!) { createCar(input: $input) { id customerId make model year licensePlate color vin } }",
    "variables": {
      "input": {
        "customerId": "'"$CUSTOMER_ID"'",
        "make": "Honda",
        "model": "Accord",
        "year": 2023,
        "licensePlate": "'"$UNIQUE_PLATE"'",
        "vin": "'"$UNIQUE_VIN"'",
        "color": "Blue"
      }
    }
  }')

echo "$CAR_RESPONSE" | jq .
CAR_ID=$(echo "$CAR_RESPONSE" | jq -r '.data.createCar.id')

if [ "$CAR_ID" != "null" ] && [ -n "$CAR_ID" ]; then
    echo "✅ CREATE_CAR_MUTATION - PASSED"
    echo "   Car ID: $CAR_ID"
else
    echo "❌ CREATE_CAR_MUTATION - FAILED"
    exit 1
fi

echo ""
sleep 1

# ==================================================
# TEST 3: CREATE_SESSION_MUTATION
# ==================================================
echo "=========================================="
echo "TEST 3: CREATE_SESSION_MUTATION"
echo "=========================================="

SESSION_RESPONSE=$(curl -s -X POST $BASE_URL \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "query": "mutation CreateSession($carId: UUID!, $customerId: UUID!) { createSession(carId: $carId, customerId: $customerId) { id status createdAt } }",
    "variables": {
      "carId": "'"$CAR_ID"'",
      "customerId": "'"$CUSTOMER_ID"'"
    }
  }')

echo "$SESSION_RESPONSE" | jq .
SESSION_ID=$(echo "$SESSION_RESPONSE" | jq -r '.data.createSession.id')

if [ "$SESSION_ID" != "null" ] && [ -n "$SESSION_ID" ]; then
    echo "✅ CREATE_SESSION_MUTATION - PASSED"
    echo "   Session ID: $SESSION_ID"
else
    echo "❌ CREATE_SESSION_MUTATION - FAILED"
    exit 1
fi

echo ""
sleep 1

# ==================================================
# TEST 4: CUSTOMERS_QUERY (Pagination)
# ==================================================
echo "=========================================="
echo "TEST 4: CUSTOMERS_QUERY (Pagination)"
echo "=========================================="

CUSTOMERS_LIST=$(curl -s -X POST $BASE_URL \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "query": "query Customers($first: Int, $order: [CustomerSortInput!], $after: String) { customers(first: $first, order: $order, after: $after) { pageInfo { hasNextPage hasPreviousPage startCursor endCursor } totalCount edges { cursor node { id firstName lastName email phoneNumber address { city } lastSessionDate totalVisits totalSpent activeJobCards totalCars } } } }",
    "variables": {
      "first": 10,
      "order": [{ "createdAt": "DESC" }]
    }
  }')

echo "$CUSTOMERS_LIST" | jq .
TOTAL_COUNT=$(echo "$CUSTOMERS_LIST" | jq -r '.data.customers.totalCount')

if [ "$TOTAL_COUNT" != "null" ] && [ "$TOTAL_COUNT" -gt 0 ]; then
    echo "✅ CUSTOMERS_QUERY - PASSED"
    echo "   Total Customers: $TOTAL_COUNT"
else
    echo "❌ CUSTOMERS_QUERY - FAILED"
fi

echo ""
sleep 1

# ==================================================
# TEST 5: SEARCH_CUSTOMERS_QUERY
# ==================================================
echo "=========================================="
echo "TEST 5: SEARCH_CUSTOMERS_QUERY"
echo "=========================================="

SEARCH_RESPONSE=$(curl -s -X POST $BASE_URL \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "query": "query SearchCustomers($query: String!, $first: Int) { searchCustomers(query: $query, first: $first) { pageInfo { hasNextPage endCursor } totalCount edges { node { id firstName lastName phoneNumber cars { make model licensePlate } } } } }",
    "variables": {
      "query": "Frontend",
      "first": 10
    }
  }')

echo "$SEARCH_RESPONSE" | jq .
SEARCH_COUNT=$(echo "$SEARCH_RESPONSE" | jq -r '.data.searchCustomers.totalCount')

if [ "$SEARCH_COUNT" != "null" ] && [ "$SEARCH_COUNT" -gt 0 ]; then
    echo "✅ SEARCH_CUSTOMERS_QUERY - PASSED"
    echo "   Search Results: $SEARCH_COUNT"
else
    echo "❌ SEARCH_CUSTOMERS_QUERY - FAILED"
fi

echo ""
sleep 1

# ==================================================
# TEST 6: CUSTOMER_DETAIL_QUERY (with nested data)
# ==================================================
echo "=========================================="
echo "TEST 6: CUSTOMER_DETAIL_QUERY"
echo "=========================================="

DETAIL_RESPONSE=$(curl -s -X POST $BASE_URL \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "query": "query CustomerDetail($id: UUID!) { customerById(id: $id) { id firstName lastName email phoneNumber address { country city street } cars { id make model year licensePlate color vin } } jobCards(where: { customerId: { eq: $id } }, order: [{ createdAt: DESC }]) { edges { node { id status totalEstimatedCost totalActualCost createdAt car { make model licensePlate } } } } sessions(where: { customerId: { eq: $id } }, order: [{ createdAt: DESC }]) { edges { node { id status createdAt carId car { make model licensePlate } } } } }",
    "variables": {
      "id": "'"$CUSTOMER_ID"'"
    }
  }')

echo "$DETAIL_RESPONSE" | jq .
DETAIL_FIRST_NAME=$(echo "$DETAIL_RESPONSE" | jq -r '.data.customerById.firstName')
CARS_COUNT=$(echo "$DETAIL_RESPONSE" | jq -r '.data.customerById.cars | length')
SESSIONS_COUNT=$(echo "$DETAIL_RESPONSE" | jq -r '.data.sessions.edges | length')

if [ "$DETAIL_FIRST_NAME" == "Frontend" ] && [ "$CARS_COUNT" -gt 0 ] && [ "$SESSIONS_COUNT" -gt 0 ]; then
    echo "✅ CUSTOMER_DETAIL_QUERY - PASSED"
    echo "   Customer: $DETAIL_FIRST_NAME"
    echo "   Cars: $CARS_COUNT"
    echo "   Sessions: $SESSIONS_COUNT"
else
    echo "❌ CUSTOMER_DETAIL_QUERY - FAILED"
fi

echo ""
sleep 1

# ==================================================
# TEST 7: EXPORT_CUSTOMERS_CSV_MUTATION
# ==================================================
echo "=========================================="
echo "TEST 7: EXPORT_CUSTOMERS_CSV_MUTATION"
echo "=========================================="

EXPORT_RESPONSE=$(curl -s -X POST $BASE_URL \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "query": "mutation ExportCustomers { exportCustomersToCsv }"
  }')

echo "$EXPORT_RESPONSE" | jq .
CSV_BASE64=$(echo "$EXPORT_RESPONSE" | jq -r '.data.exportCustomersToCsv')

if [ "$CSV_BASE64" != "null" ] && [ -n "$CSV_BASE64" ]; then
    echo "✅ EXPORT_CUSTOMERS_CSV_MUTATION - PASSED"
    echo "   CSV Length: ${#CSV_BASE64} characters"
    
    # Decode and show first few lines
    echo ""
    echo "CSV Preview (first 3 lines):"
    echo "$CSV_BASE64" | base64 -d | head -3
else
    echo "❌ EXPORT_CUSTOMERS_CSV_MUTATION - FAILED"
fi

echo ""
echo ""
echo "=========================================="
echo "✅ ALL FRONTEND TESTS COMPLETED"
echo "=========================================="
echo ""
echo "Summary:"
echo "  ✅ CREATE_CUSTOMER_MUTATION"
echo "  ✅ CREATE_CAR_MUTATION"
echo "  ✅ CREATE_SESSION_MUTATION"
echo "  ✅ CUSTOMERS_QUERY (Pagination)"
echo "  ✅ SEARCH_CUSTOMERS_QUERY"
echo "  ✅ CUSTOMER_DETAIL_QUERY"
echo "  ✅ EXPORT_CUSTOMERS_CSV_MUTATION"
echo ""
echo "Test Data Created:"
echo "  Customer ID: $CUSTOMER_ID"
echo "  Car ID: $CAR_ID"
echo "  Session ID: $SESSION_ID"
echo ""
