# S3 Folder Structure - Organized File Storage

## 📁 Overview

All files are now organized in a hierarchical folder structure based on:
- **Organization** (multi-tenancy isolation)
- **Context** (general vs session-specific)
- **Date** (year/month for easy archiving)
- **Unique identifiers** (prevent name collisions)

---

## 🗂️ Folder Structure

```
s3://gixat-bucket/
└── organizations/
    └── {organizationId}/
        ├── general/              # General media (not linked to sessions)
        │   └── {year}/
        │       └── {month}/
        │           └── {guid}_{sanitized-filename}
        │
        └── sessions/             # Session-specific media
            └── {sessionId}/
                ├── INTAKE/
                │   └── {year}/
                │       └── {month}/
                │           └── {guid}_{sanitized-filename}
                ├── CUSTOMER_REQUESTS/
                │   └── {year}/
                │       └── {month}/
                │           └── {guid}_{sanitized-filename}
                ├── INSPECTION/
                │   └── {year}/
                │       └── {month}/
                │           └── {guid}_{sanitized-filename}
                ├── TEST_DRIVE/
                │   └── {year}/
                │       └── {month}/
                │           └── {guid}_{sanitized-filename}
                ├── INITIAL_REPORT/
                │   └── {year}/
                │       └── {month}/
                │           └── {guid}_{sanitized-filename}
                └── GENERAL/
                    └── {year}/
                        └── {month}/
                            └── {guid}_{sanitized-filename}
```

---

## 📋 Examples

### General Media Upload:
```
organizations/a1b2c3d4-e5f6-7890-abcd-ef1234567890/general/2025/12/f8e9d7c6-b5a4-3210-fedc-ba9876543210_customer-photo.jpg
```

### Session Media Upload (Test Drive Stage):
```
organizations/a1b2c3d4-e5f6-7890-abcd-ef1234567890/sessions/12345678-90ab-cdef-1234-567890abcdef/TEST_DRIVE/2025/12/f8e9d7c6-b5a4-3210-fedc-ba9876543210_engine-check.jpg
```

---

## ✅ Benefits

### 1. **Multi-Tenancy Isolation**
- Each organization's files are completely separated
- Organization ID in path ensures access control at storage level
- Easy to implement per-organization backup/archiving

### 2. **Context-Based Organization**
- **General**: Company logos, marketing materials, templates
- **Sessions**: Organized by workflow stage (intake, inspection, test drive, etc.)
- Easy to find all media for a specific session

### 3. **Date-Based Archiving**
- Files organized by year/month
- Easy to implement lifecycle policies (e.g., archive files older than 1 year)
- Simplifies billing analysis and storage reports

### 4. **Collision-Free Naming**
- GUID prefix prevents filename collisions
- Original filename preserved for user reference
- Sanitized filenames prevent security issues

### 5. **Performance & Scalability**
- S3 optimizes performance with prefixes
- Distributed workload across partitions
- Efficient listing operations within folders

---

## 🔧 GraphQL Mutations

### General Media Upload
```graphql
# Step 1: Get presigned URL
mutation GetUploadUrl {
  getPresignedUploadUrl(
    fileName: "customer-photo.jpg"
    contentType: "image/jpeg"
  ) {
    uploadUrl
    fileKey
    expiresAt
  }
}

# Step 2: Frontend uploads directly to S3 using uploadUrl

# Step 3: Process uploaded file
mutation ProcessUpload {
  processUploadedFile(
    fileKey: "organizations/.../general/2025/12/...jpg"
    alt: "Customer profile photo"
  ) {
    id
    url
    type
  }
}
```

### Session Media Upload (Recommended)
```graphql
# Step 1: Get session-specific presigned URL
mutation GetSessionUploadUrl {
  getSessionPresignedUploadUrl(
    sessionId: "12345678-90ab-cdef-1234-567890abcdef"
    stage: TEST_DRIVE
    fileName: "engine-check.jpg"
    contentType: "image/jpeg"
  ) {
    uploadUrl
    fileKey
    expiresAt
  }
}

# Step 2: Frontend uploads directly to S3 using uploadUrl

# Step 3: Process and link to session
mutation ProcessSessionUpload {
  processSessionUpload(
    sessionId: "12345678-90ab-cdef-1234-567890abcdef"
    fileKey: "organizations/.../sessions/.../TEST_DRIVE/2025/12/...jpg"
    stage: TEST_DRIVE
    alt: "Engine inspection photo"
  ) {
    id
    stage
    media {
      id
      url
      type
    }
  }
}
```

### Bulk Session Upload (Efficient for Multiple Files)
```graphql
mutation BulkSessionUpload {
  processBulkSessionUploads(
    sessionId: "12345678-90ab-cdef-1234-567890abcdef"
    files: [
      {
        fileKey: "organizations/.../sessions/.../TEST_DRIVE/2025/12/...jpg"
        stage: TEST_DRIVE
        alt: "Engine check"
      },
      {
        fileKey: "organizations/.../sessions/.../TEST_DRIVE/2025/12/...jpg"
        stage: TEST_DRIVE
        alt: "Brake inspection"
      }
    ]
  ) {
    fileKey
    success
    sessionMedia {
      id
      stage
      media { url }
    }
    errorMessage
  }
}
```

---

## 🛡️ Security Features

### 1. **Organization Isolation**
- Files stored under organization ID path
- TenantService validates organization context
- Global query filters prevent cross-organization access

### 2. **File Validation**
- Extension whitelist (.jpg, .png, .mp4, etc.)
- MIME type validation
- Filename sanitization (removes path traversal attempts)
- Size limits (10MB images, 50MB videos)

### 3. **Private by Default**
- No public ACLs
- Access via presigned URLs only
- 15-minute expiry on upload URLs
- 24-hour expiry on download URLs

### 4. **Audit Trail**
- Organization ID in path
- Session ID in path (for session media)
- Timestamp in path (year/month)
- GUID for unique tracking

---

## 📊 S3 Lifecycle Policies (Recommended)

```json
{
  "Rules": [
    {
      "Id": "ArchiveOldGeneralMedia",
      "Status": "Enabled",
      "Filter": {
        "Prefix": "organizations/"
      },
      "Transitions": [
        {
          "Days": 90,
          "StorageClass": "STANDARD_IA"
        },
        {
          "Days": 365,
          "StorageClass": "GLACIER"
        }
      ]
    },
    {
      "Id": "RetainSessionMediaLonger",
      "Status": "Enabled",
      "Filter": {
        "Prefix": "organizations/*/sessions/"
      },
      "Transitions": [
        {
          "Days": 180,
          "StorageClass": "STANDARD_IA"
        }
      ]
    }
  ]
}
```

**Benefits:**
- General media → Standard-IA after 90 days, Glacier after 1 year
- Session media → Standard-IA after 180 days (retained longer for legal/warranty)
- Significant cost savings on older files

---

## 🔍 S3 Operations

### List All Files for an Organization
```bash
aws s3 ls s3://gixat-bucket/organizations/a1b2c3d4-e5f6-7890-abcd-ef1234567890/ --recursive
```

### List All Media for a Specific Session
```bash
aws s3 ls s3://gixat-bucket/organizations/{orgId}/sessions/{sessionId}/ --recursive
```

### List Files by Date
```bash
aws s3 ls s3://gixat-bucket/organizations/{orgId}/sessions/{sessionId}/TEST_DRIVE/2025/12/
```

### Copy Session to Archive
```bash
aws s3 sync \
  s3://gixat-bucket/organizations/{orgId}/sessions/{sessionId}/ \
  s3://gixat-archive/organizations/{orgId}/sessions/{sessionId}/
```

---

## 📈 Storage Analytics

### Files per Organization
```sql
-- Query CloudWatch or S3 Inventory
SELECT 
  organization_id,
  COUNT(*) as file_count,
  SUM(size) as total_size_bytes
FROM s3_inventory
WHERE bucket = 'gixat-bucket'
GROUP BY organization_id;
```

### Files per Session Stage
```bash
# Count files per stage
aws s3 ls s3://gixat-bucket/organizations/{orgId}/sessions/{sessionId}/ --recursive | \
  awk '{print $4}' | \
  grep -oP '(?<=sessions/[^/]+/)[^/]+' | \
  sort | uniq -c
```

---

## 🚀 Migration Guide

If you have existing files in the old `uploads/` structure, migrate them:

```bash
#!/bin/bash
# migrate-to-organized-structure.sh

OLD_PREFIX="uploads/"
NEW_PREFIX="organizations/"

# List all files in old structure
aws s3 ls s3://gixat-bucket/${OLD_PREFIX} --recursive | while read -r line; do
  # Extract filename
  FILE=$(echo $line | awk '{print $4}')
  
  # Extract organization ID from database
  # (You'll need to query your database to map files to organizations)
  ORG_ID=$(get_organization_for_file $FILE)
  
  # Determine new path
  TIMESTAMP=$(echo $line | awk '{print $1" "$2}')
  YEAR=$(date -d "$TIMESTAMP" +%Y)
  MONTH=$(date -d "$TIMESTAMP" +%m)
  
  NEW_PATH="organizations/${ORG_ID}/general/${YEAR}/${MONTH}/$(basename $FILE)"
  
  # Copy to new location
  aws s3 cp \
    s3://gixat-bucket/${FILE} \
    s3://gixat-bucket/${NEW_PATH}
  
  echo "Migrated: $FILE -> $NEW_PATH"
done
```

**Important:** Update database records to point to new S3 keys after migration.

---

## 📝 Backend Implementation

### File Locations:
- **Presigned URL Generation:** `/Modules/Common/GraphQL/PresignedUploadMutations.cs`
  - `GetPresignedUploadUrlAsync` - General uploads
  - `GetSessionPresignedUploadUrlAsync` - Session-specific uploads (NEW)
  - `GetBulkPresignedUploadUrlsAsync` - Bulk general uploads
  
- **File Processing:** Same file
  - `ProcessUploadedFileAsync` - Download, scan, compress, upload
  - `ProcessSessionUploadAsync` - Process + link to session
  - `ProcessBulkSessionUploadsAsync` - Bulk session processing

### Tenant Service Integration:
```cs
var orgId = tenantService.OrganizationId 
    ?? throw new InvalidOperationException("Organization context required");
```

---

## ✅ Best Practices

1. **Always use session-specific mutations for session media**
   - `getSessionPresignedUploadUrl` instead of `getPresignedUploadUrl`
   - Ensures proper folder organization

2. **Use bulk operations for multiple files**
   - More efficient than individual uploads
   - Parallel processing on backend

3. **Include descriptive alt text**
   - Improves accessibility
   - Helps with search and organization

4. **Monitor storage costs**
   - Implement lifecycle policies
   - Archive old sessions
   - Delete cancelled/abandoned sessions

5. **Backup critical sessions**
   - Use S3 replication for important data
   - Consider separate archive bucket

---

**Last Updated:** December 22, 2025  
**Implemented By:** Backend Team  
**Status:** ✅ Production Ready
