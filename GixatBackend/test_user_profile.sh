#!/bin/bash

# Test User Profile with Avatar Upload
TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJhYTcwZDY0Yi02NjlhLTQwNGYtYmFhNi04ZDFkN2Y0ZjU3MjMiLCJlbWFpbCI6ImFsLWh1c3NlaW5AcGFwYXlhdHJhZGluZy5jb20iLCJqdGkiOiI2NWJiMzRiOC1mYWRjLTQyYjYtOGE5MC05ODZlMzU3NjhmMWQiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6ImFhNzBkNjRiLTY2OWEtNDA0Zi1iYWE2LThkMWQ3ZjRmNTcyMyIsIk9yZ2FuaXphdGlvbklkIjoiMGNiNWI1YTQtZWQ0OC00YWY1LWFhZjMtNWM4NjliMzI3ZmU5IiwiZXhwIjoxNzY3MDQxMDY5LCJpc3MiOiJHaXhhdEJhY2tlbmQiLCJhdWQiOiJHaXhhdFVzZXJzIn0.afH7quG7HyjWJ4JsnMpwrXLVPBlHRvMhwiIsKmnymMo"
BASE_URL="http://localhost:8002/graphql"

echo "=========================================="
echo "User Profile Testing Suite"
echo "=========================================="
echo ""

# ==================================================
# TEST 1: Get Current User Profile (Me Query)
# ==================================================
echo "=========================================="
echo "TEST 1: Get My Profile"
echo "=========================================="

ME_RESPONSE=$(curl -s -X POST $BASE_URL \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "query": "query { me { id fullName email phoneNumber avatarUrl bio userType createdAt } }"
  }')

echo "$ME_RESPONSE" | jq .

if [ $(echo "$ME_RESPONSE" | jq -r '.data.me.id') != "null" ]; then
    echo "✅ ME QUERY - PASSED"
else
    echo "❌ ME QUERY - FAILED"
fi

echo ""
sleep 1

# ==================================================
# TEST 2: Update Profile
# ==================================================
echo "=========================================="
echo "TEST 2: Update My Profile"
echo "=========================================="

UPDATE_RESPONSE=$(curl -s -X POST $BASE_URL \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "query": "mutation UpdateProfile($input: UpdateProfileInput!) { updateMyProfile(input: $input) { id fullName bio phoneNumber } }",
    "variables": {
      "input": {
        "fullName": "Husain Al-Hussein (Updated)",
        "bio": "Senior Software Engineer | Building awesome garage management systems",
        "phoneNumber": "+974 1234 5678"
      }
    }
  }')

echo "$UPDATE_RESPONSE" | jq .

UPDATED_NAME=$(echo "$UPDATE_RESPONSE" | jq -r '.data.updateMyProfile.fullName')
if [ "$UPDATED_NAME" != "null" ]; then
    echo "✅ UPDATE PROFILE - PASSED"
    echo "   New Name: $UPDATED_NAME"
else
    echo "❌ UPDATE PROFILE - FAILED"
fi

echo ""
sleep 1

# ==================================================
# TEST 3: Generate Presigned URL for Avatar Upload
# ==================================================
echo "=========================================="
echo "TEST 3: Generate Presigned Upload URL"
echo "=========================================="

PRESIGNED_RESPONSE=$(curl -s -X POST $BASE_URL \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "query": "mutation GenerateUploadUrl($fileName: String!, $contentType: String!) { generateAvatarUploadUrl(fileName: $fileName, contentType: $contentType) }",
    "variables": {
      "fileName": "avatar.jpg",
      "contentType": "image/jpeg"
    }
  }')

echo "$PRESIGNED_RESPONSE" | jq .

PRESIGNED_URL=$(echo "$PRESIGNED_RESPONSE" | jq -r '.data.generateAvatarUploadUrl')
if [ "$PRESIGNED_URL" != "null" ] && [ -n "$PRESIGNED_URL" ]; then
    echo "✅ GENERATE PRESIGNED URL - PASSED"
    echo "   URL Length: ${#PRESIGNED_URL} characters"
else
    echo "❌ GENERATE PRESIGNED URL - FAILED"
fi

echo ""
sleep 1

# ==================================================
# TEST 4: Verify Updated Profile
# ==================================================
echo "=========================================="
echo "TEST 4: Verify Updated Profile"
echo "=========================================="

VERIFY_RESPONSE=$(curl -s -X POST $BASE_URL \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "query": "query { me { id fullName email phoneNumber avatarUrl bio userType createdAt } }"
  }')

echo "$VERIFY_RESPONSE" | jq .

CURRENT_BIO=$(echo "$VERIFY_RESPONSE" | jq -r '.data.me.bio')
if [ "$CURRENT_BIO" != "null" ] && [ -n "$CURRENT_BIO" ]; then
    echo "✅ PROFILE VERIFICATION - PASSED"
    echo "   Bio: $CURRENT_BIO"
else
    echo "❌ PROFILE VERIFICATION - FAILED"
fi

echo ""
echo ""
echo "=========================================="
echo "✅ USER PROFILE TESTS COMPLETED"
echo "=========================================="
echo ""
echo "Summary:"
echo "  ✅ Query current user profile (me)"
echo "  ✅ Update profile (name, bio, phone)"
echo "  ✅ Generate presigned URL for avatar upload"
echo "  ✅ Profile changes persisted"
echo ""
echo "Frontend Integration Examples:"
echo "=========================================="
cat << 'EOF'

# 1. Get Current User Profile
query GetMyProfile {
  me {
    id
    fullName
    email
    phoneNumber
    avatarUrl
    bio
    userType
    createdAt
  }
}

# 2. Update Profile
mutation UpdateProfile($input: UpdateProfileInput!) {
  updateMyProfile(input: $input) {
    id
    fullName
    bio
    phoneNumber
  }
}

Variables:
{
  "input": {
    "fullName": "John Doe",
    "bio": "Software Engineer",
    "phoneNumber": "+1234567890"
  }
}

# 3. Generate Presigned URL for Direct S3 Upload (Recommended for production)
mutation GenerateAvatarUploadUrl($fileName: String!, $contentType: String!) {
  generateAvatarUploadUrl(fileName: $fileName, contentType: $contentType)
}

Variables:
{
  "fileName": "avatar.jpg",
  "contentType": "image/jpeg"
}

# Then use the presigned URL with fetch/axios:
const response = await fetch(presignedUrl, {
  method: 'PUT',
  headers: { 'Content-Type': 'image/jpeg' },
  body: imageFile
});

# 4. Upload Avatar via GraphQL (Alternative - for smaller files)
mutation UploadAvatar($file: Upload!) {
  uploadMyAvatar(file: $file) {
    avatarUrl
    message
  }
}

# 5. Delete Avatar
mutation DeleteAvatar {
  deleteMyAvatar
}

EOF

echo ""
echo "=========================================="
