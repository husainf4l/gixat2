# S3 Security Best Practices

## Overview
This document outlines the security implementation for AWS S3 file storage in the Gixat Backend.

## Security Model

### 1. **Private Files by Default**
- All uploaded files are **private** (no public ACLs)
- Files are NOT accessible via direct URLs
- Access is granted only through **presigned URLs** with expiration times

### 2. **Presigned URLs for Secure Access**

#### Upload Flow (Client → S3 Direct Upload)
```
1. Frontend requests presigned upload URL from backend
2. Backend validates request and generates presigned URL (15 min expiry)
3. Frontend uploads directly to S3 using presigned URL
4. Frontend notifies backend to process the uploaded file
5. Backend downloads, scans, compresses, and re-uploads
```

#### Download Flow (Secure File Access)
```
1. Frontend requests file URL from backend
2. Backend generates presigned download URL (24 hour expiry)
3. Frontend uses presigned URL to access file
4. URL expires after 24 hours
```

### 3. **S3 Bucket Configuration**

#### Required Settings:
- **Block Public Access**: ALL enabled
- **Object Ownership**: Bucket owner enforced (ACLs disabled)
- **Versioning**: Recommended for production
- **Encryption**: Server-side encryption (SSE-S3 or SSE-KMS)

#### CORS Configuration:
```json
[
    {
        "AllowedHeaders": ["*"],
        "AllowedMethods": ["GET", "PUT", "POST"],
        "AllowedOrigins": [
            "http://localhost:4200",
            "http://localhost:3000",
            "http://localhost:3002",
            "https://gixat.com",
            "https://*.gixat.com",
            "https://next.aqlaan.com"
        ],
        "ExposeHeaders": ["ETag"],
        "MaxAgeSeconds": 3000
    }
]
```

## Implementation Details

### S3Service Methods

#### 1. **UploadFileAsync** (Backend Upload)
```csharp
// Files are private by default - NO ACL setting
var uploadRequest = new TransferUtilityUploadRequest
{
    InputStream = fileStream,
    Key = fileKey,
    BucketName = _bucketName,
    ContentType = contentType
    // No CannedACL property - defaults to private
};
```

#### 2. **GetFileUrl** (Generate Presigned Download URL)
```csharp
// Returns a presigned URL valid for 24 hours
public Uri GetFileUrl(string fileKey)
{
    var request = new GetPreSignedUrlRequest
    {
        BucketName = _bucketName,
        Key = fileKey,
        Verb = HttpVerb.GET,
        Expires = DateTime.UtcNow.AddHours(24)
    };
    return new Uri(_s3Client.GetPreSignedURL(request));
}
```

#### 3. **GeneratePresignedUploadUrlAsync** (Client Direct Upload)
```csharp
// For direct upload from frontend to S3
// URL valid for 15 minutes
public async Task<string> GeneratePresignedUploadUrlAsync(
    string fileKey, 
    string contentType, 
    int expiresInMinutes = 15)
```

#### 4. **GeneratePresignedDownloadUrlAsync** (Custom Expiry)
```csharp
// For custom expiration times
public async Task<string> GeneratePresignedDownloadUrlAsync(
    string fileKey, 
    int expiresInHours = 24)
```

## Frontend Integration

### Upload Flow
```typescript
// Step 1: Get presigned upload URL
const { uploadUrl, fileKey } = await getPresignedUploadUrl(fileName, contentType);

// Step 2: Upload directly to S3
await fetch(uploadUrl, {
  method: 'PUT',
  body: file,
  headers: {
    'Content-Type': contentType
  }
});

// Step 3: Notify backend to process
await processUploadedFile(fileKey, alt);
```

### Download Flow
```typescript
// Get presigned download URL from backend
const media = await getMedia(mediaId);
const secureUrl = media.url; // Presigned URL valid for 24h

// Use URL directly in img/video tags
<img src={secureUrl} alt={media.alt} />
```

## Security Benefits

1. **No Public Access**: Files cannot be accessed without valid presigned URLs
2. **Time-Limited Access**: URLs expire automatically
3. **Auditable**: All access goes through backend, can be logged
4. **Credential Management**: AWS credentials never exposed to frontend
5. **Fine-grained Control**: Backend can validate and authorize each request
6. **DDoS Protection**: Frontend can't directly upload unlimited files

## URL Expiration Times

| Operation | Default Expiry | Rationale |
|-----------|---------------|-----------|
| Upload URL | 15 minutes | Short window for upload completion |
| Download URL (GetFileUrl) | 24 hours | Balance between security and UX |
| Custom Download URL | Configurable | For special cases (email links, etc.) |

## Monitoring & Logging

### Recommended CloudWatch Alarms:
- Failed S3 GetObject requests
- Unusual upload patterns
- Presigned URL generation rate
- Storage size growth

### Audit Trail:
- All presigned URL generations are logged
- File processing results stored in database
- Failed uploads/downloads captured in logs

## Migration from Public ACLs

If migrating from public ACLs:
1. Remove `CannedACL = S3CannedACL.PublicRead` from uploads ✅
2. Update GetFileUrl to return presigned URLs ✅
3. Update S3 bucket to disable ACLs ✅
4. Update frontend to use presigned URLs ✅
5. Test upload/download flows ⏳

## Troubleshooting

### "The bucket does not allow ACLs" Error
**Cause**: Bucket has ACLs disabled (recommended setting)  
**Solution**: Remove ACL settings from upload requests (already implemented)

### "Access Denied" on GetObject
**Cause**: IAM user lacks s3:GetObject permission  
**Solution**: Add permission to IAM policy

### Frontend CORS Errors
**Cause**: Domain not in CORS AllowedOrigins  
**Solution**: Update CORS configuration in S3 bucket

## IAM Permissions Required

```json
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Effect": "Allow",
            "Action": [
                "s3:PutObject",
                "s3:GetObject",
                "s3:DeleteObject"
            ],
            "Resource": "arn:aws:s3:::gixat/*"
        },
        {
            "Effect": "Allow",
            "Action": [
                "s3:ListBucket"
            ],
            "Resource": "arn:aws:s3:::gixat"
        }
    ]
}
```

## References

- [AWS S3 Security Best Practices](https://docs.aws.amazon.com/AmazonS3/latest/userguide/security-best-practices.html)
- [Presigned URLs Documentation](https://docs.aws.amazon.com/AmazonS3/latest/userguide/PresignedUrlUploadObject.html)
- [S3 Block Public Access](https://docs.aws.amazon.com/AmazonS3/latest/userguide/access-control-block-public-access.html)
