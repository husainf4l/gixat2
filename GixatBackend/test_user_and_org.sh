#!/bin/bash

API_URL="http://localhost:8002/graphql"
TEST_EMAIL="testuser_$(date +%s)@example.com"
TEST_PASSWORD="Test123!"
TEST_NAME="Test User"
ORG_NAME="Test Company Ltd"

echo "🔐 Step 1: Registering new user..."
echo "Email: $TEST_EMAIL"

REGISTER_RESPONSE=$(curl -s -X POST "$API_URL" \
  -H "Content-Type: application/json" \
  -d "{
    \"query\": \"mutation Register { register(input: { email: \\\"$TEST_EMAIL\\\", password: \\\"$TEST_PASSWORD\\\", fullName: \\\"$TEST_NAME\\\", role: \\\"Admin\\\", userType: ORGANIZATIONAL }) { token user { id email fullName organizationId } error } }\"
  }")

echo "$REGISTER_RESPONSE" | python3 -m json.tool

# Extract token
TOKEN=$(echo "$REGISTER_RESPONSE" | python3 -c "import sys, json; data=json.load(sys.stdin); print(data['data']['register']['token'] if data.get('data', {}).get('register', {}).get('token') else '')")

if [ -z "$TOKEN" ] || [ "$TOKEN" == "null" ]; then
  echo "❌ Registration failed!"
  exit 1
fi

echo -e "\n✅ User registered successfully!"
echo "🎫 Token: ${TOKEN:0:50}..."

echo -e "\n👤 Step 2: Checking user profile (should have NO organization)..."
ME_RESPONSE=$(curl -s -X POST "$API_URL" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "query": "query { me { id email fullName organizationId organization { id name } } }"
  }')

echo "$ME_RESPONSE" | python3 -m json.tool

echo -e "\n🏢 Step 3: Creating organization..."
CREATE_ORG_RESPONSE=$(curl -s -X POST "$API_URL" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d "{
    \"query\": \"mutation { createOrganization(input: { name: \\\"$ORG_NAME\\\", address: { street: \\\"123 Test St\\\", city: \\\"Test City\\\", country: \\\"Test Country\\\", phoneCountryCode: \\\"+1\\\" } }) { token user { id email organizationId organization { id name } } } }\"
  }")

echo "$CREATE_ORG_RESPONSE" | python3 -m json.tool

if echo "$CREATE_ORG_RESPONSE" | grep -q '"errors"'; then
  echo -e "\n❌ Organization creation failed!"
  exit 1
fi

echo -e "\n✅ Organization created successfully!"

echo -e "\n👤 Step 4: Checking user profile again (should now have organization)..."
ME_RESPONSE_2=$(curl -s -X POST "$API_URL" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "query": "query { me { id email fullName organizationId organization { id name } } }"
  }')

echo "$ME_RESPONSE_2" | python3 -m json.tool

echo -e "\n✅ Test completed successfully!"
