# Presigned URL Upload Flow - Backend Implementation Guide

## Architecture Overview

```
┌─────────────┐         ┌─────────────┐         ┌─────────────┐
│  Frontend   │────1───>│   Backend   │         │     S3      │
│             │         │   (NestJS)  │         │   Bucket    │
│             │<───2────│             │         │             │
│             │         └─────────────┘         └─────────────┘
│             │                                         ▲
│             │────────────3─────────────────────────>│
│             │                                         │
│             │         ┌─────────────┐                │
│             │────4───>│   Backend   │────5──────────┘
│             │         │  (Process)  │
└─────────────┘         └─────────────┘
                               │
                               ▼
                        ┌─────────────┐
                        │  Database   │
                        └─────────────┘
```

### Flow Steps:
1. **Frontend → Backend**: Request presigned URL
2. **Backend → Frontend**: Return presigned URL + fileKey
3. **Frontend → S3**: Upload file directly to S3 (fast!)
4. **Frontend → Backend**: Confirm upload with fileKey
5. **Backend → S3**: Download → Scan → Compress → Re-upload → Update DB

---

## Benefits

✅ **Fast Uploads**: Direct to S3, no backend bottleneck  
✅ **Scalable**: Backend doesn't handle file streaming  
✅ **Secure**: Presigned URLs expire, backend validates before processing  
✅ **Async Processing**: Virus scan & compression happens in background  
✅ **Better UX**: User gets immediate feedback, processing happens later  

---

## Required Backend Mutations

### 1. Get Upload URL Mutation

```graphql
type UploadUrlResponse {
  uploadUrl: String!      # Presigned S3 URL
  fileKey: String!        # S3 object key (UUID-based)
  expiresIn: Int!         # Seconds until URL expires (e.g., 300)
}

type Mutation {
  getUploadUrl(
    sessionId: UUID!
    filename: String!
    contentType: String!
    stage: String
  ): UploadUrlResponse! @authenticated
}
```

**Implementation:**

```typescript
@Mutation(() => UploadUrlResponse)
@UseGuards(AuthGuard)
async getUploadUrl(
  @Args('sessionId') sessionId: string,
  @Args('filename') filename: string,
  @Args('contentType') contentType: string,
  @Args('stage', { nullable: true }) stage?: string,
  @CurrentUser() user: User
): Promise<UploadUrlResponse> {
  // 1. Validate user has access to session
  const session = await this.sessionService.findById(sessionId);
  if (session.organizationId !== user.organizationId) {
    throw new ForbiddenException('Access denied');
  }

  // 2. Validate content type
  const allowedTypes = [
    'image/jpeg', 'image/png', 'image/gif', 'image/webp',
    'video/mp4', 'video/quicktime', 'video/webm'
  ];
  if (!allowedTypes.includes(contentType)) {
    throw new BadRequestException('Invalid content type');
  }

  // 3. Validate filename
  if (filename.includes('..') || filename.includes('/')) {
    throw new BadRequestException('Invalid filename');
  }

  // 4. Generate unique file key
  const ext = path.extname(filename);
  const fileKey = `sessions/${sessionId}/${stage || 'general'}/${uuidv4()}${ext}`;

  // 5. Generate presigned URL
  const s3 = new S3Client({ region: process.env.AWS_REGION });
  const command = new PutObjectCommand({
    Bucket: process.env.S3_BUCKET,
    Key: fileKey,
    ContentType: contentType,
    // Optional: Add metadata
    Metadata: {
      'original-filename': filename,
      'uploaded-by': user.id,
      'session-id': sessionId,
      'stage': stage || 'general'
    }
  });

  const uploadUrl = await getSignedUrl(s3, command, {
    expiresIn: 300 // 5 minutes
  });

  // 6. Create pending media record in DB
  await this.mediaService.createPending({
    fileKey,
    sessionId,
    originalFilename: filename,
    contentType,
    stage,
    uploadedBy: user.id,
    status: 'PENDING_UPLOAD'
  });

  return {
    uploadUrl,
    fileKey,
    expiresIn: 300
  };
}
```

---

### 2. Confirm Upload Mutation

```graphql
enum MediaStatus {
  PENDING_UPLOAD
  UPLOADED
  SCANNING
  SCAN_FAILED
  PROCESSING
  COMPLETED
  REJECTED
}

type MediaResponse {
  id: UUID!
  url: String!           # Presigned GET URL or CloudFront URL
  alt: String
  status: MediaStatus!
}

type Mutation {
  confirmUpload(
    sessionId: UUID!
    fileKey: String!
    stage: String
    alt: String
  ): MediaResponse! @authenticated
}
```

**Implementation:**

```typescript
@Mutation(() => MediaResponse)
@UseGuards(AuthGuard)
async confirmUpload(
  @Args('sessionId') sessionId: string,
  @Args('fileKey') fileKey: string,
  @Args('stage', { nullable: true }) stage?: string,
  @Args('alt', { nullable: true }) alt?: string,
  @CurrentUser() user: User
): Promise<MediaResponse> {
  // 1. Verify media record exists and belongs to user's session
  const media = await this.mediaService.findByFileKey(fileKey);
  if (!media || media.sessionId !== sessionId) {
    throw new NotFoundException('Media not found');
  }

  // 2. Verify file exists in S3
  const s3 = new S3Client({ region: process.env.AWS_REGION });
  try {
    await s3.send(new HeadObjectCommand({
      Bucket: process.env.S3_BUCKET,
      Key: fileKey
    }));
  } catch (error) {
    throw new BadRequestException('File not found in S3');
  }

  // 3. Update media record
  await this.mediaService.update(media.id, {
    status: 'UPLOADED',
    alt,
    stage,
    uploadedAt: new Date()
  });

  // 4. Queue async processing job
  await this.queueService.add('process-media', {
    mediaId: media.id,
    fileKey,
    sessionId
  });

  // 5. Generate presigned GET URL (temporary, until processed)
  const getCommand = new GetObjectCommand({
    Bucket: process.env.S3_BUCKET,
    Key: fileKey
  });
  const url = await getSignedUrl(s3, getCommand, {
    expiresIn: 86400 // 24 hours
  });

  return {
    id: media.id,
    url,
    alt,
    status: 'UPLOADED'
  };
}
```

---

### 3. Background Processing Job

```typescript
@Processor('process-media')
export class MediaProcessor {
  @Process('process-media')
  async processMedia(job: Job<{ mediaId: string; fileKey: string; sessionId: string }>) {
    const { mediaId, fileKey, sessionId } = job.data;
    
    try {
      // Update status
      await this.mediaService.updateStatus(mediaId, 'SCANNING');

      // Step 1: Download from S3
      const s3 = new S3Client({ region: process.env.AWS_REGION });
      const downloadResponse = await s3.send(new GetObjectCommand({
        Bucket: process.env.S3_BUCKET,
        Key: fileKey
      }));
      
      const buffer = await streamToBuffer(downloadResponse.Body);
      const tempFile = `/tmp/${uuidv4()}`;
      fs.writeFileSync(tempFile, buffer);

      // Step 2: Virus scan
      const scanner = await new ClamScan().init();
      const { isInfected } = await scanner.scanFile(tempFile);
      
      if (isInfected) {
        // Delete from S3 and mark as rejected
        await s3.send(new DeleteObjectCommand({
          Bucket: process.env.S3_BUCKET,
          Key: fileKey
        }));
        
        await this.mediaService.update(mediaId, {
          status: 'REJECTED',
          rejectionReason: 'Virus detected'
        });
        
        fs.unlinkSync(tempFile);
        return;
      }

      // Step 3: Update status to processing
      await this.mediaService.updateStatus(mediaId, 'PROCESSING');

      // Step 4: Process based on type
      const media = await this.mediaService.findById(mediaId);
      let processedBuffer: Buffer;
      let finalContentType: string;

      if (media.contentType.startsWith('image/')) {
        // Strip EXIF and compress
        processedBuffer = await sharp(tempFile)
          .rotate() // Auto-rotate based on EXIF
          .withMetadata(false) // Remove EXIF
          .jpeg({ quality: 85 })
          .toBuffer();
        finalContentType = 'image/jpeg';
      } else if (media.contentType.startsWith('video/')) {
        // For videos, you might want to use ffmpeg for transcoding
        processedBuffer = buffer; // Or process with ffmpeg
        finalContentType = media.contentType;
      }

      // Step 5: Upload processed file to final location
      const finalKey = fileKey.replace('/general/', '/processed/');
      await s3.send(new PutObjectCommand({
        Bucket: process.env.S3_BUCKET,
        Key: finalKey,
        Body: processedBuffer,
        ContentType: finalContentType,
        ServerSideEncryption: 'AES256',
        Metadata: {
          'processed': 'true',
          'processed-at': new Date().toISOString(),
          'original-key': fileKey
        }
      }));

      // Step 6: Delete original file
      await s3.send(new DeleteObjectCommand({
        Bucket: process.env.S3_BUCKET,
        Key: fileKey
      }));

      // Step 7: Update database
      await this.mediaService.update(mediaId, {
        status: 'COMPLETED',
        fileKey: finalKey,
        fileSize: processedBuffer.length,
        processedAt: new Date()
      });

      // Cleanup
      fs.unlinkSync(tempFile);

    } catch (error) {
      console.error('Media processing failed:', error);
      await this.mediaService.update(mediaId, {
        status: 'SCAN_FAILED',
        rejectionReason: error.message
      });
      throw error;
    }
  }
}
```

---

## Database Schema

```sql
CREATE TYPE media_status AS ENUM (
  'PENDING_UPLOAD',
  'UPLOADED',
  'SCANNING',
  'SCAN_FAILED',
  'PROCESSING',
  'COMPLETED',
  'REJECTED'
);

CREATE TABLE session_media (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  session_id UUID NOT NULL REFERENCES garage_sessions(id) ON DELETE CASCADE,
  file_key VARCHAR(500) NOT NULL UNIQUE,
  original_filename VARCHAR(255) NOT NULL,
  content_type VARCHAR(100) NOT NULL,
  file_size BIGINT,
  stage VARCHAR(50),
  alt_text TEXT,
  status media_status NOT NULL DEFAULT 'PENDING_UPLOAD',
  rejection_reason TEXT,
  uploaded_by UUID NOT NULL REFERENCES users(id),
  uploaded_at TIMESTAMP,
  processed_at TIMESTAMP,
  created_at TIMESTAMP DEFAULT NOW(),
  updated_at TIMESTAMP DEFAULT NOW(),
  
  CONSTRAINT valid_content_type CHECK (
    content_type IN (
      'image/jpeg', 'image/png', 'image/gif', 'image/webp',
      'video/mp4', 'video/quicktime', 'video/webm'
    )
  ),
  CONSTRAINT valid_stage CHECK (
    stage IN ('intake', 'customerRequests', 'inspection', 'testDrive', 'initialReport', 'general')
  )
);

CREATE INDEX idx_session_media_session ON session_media(session_id);
CREATE INDEX idx_session_media_status ON session_media(status);
CREATE INDEX idx_session_media_filekey ON session_media(file_key);
```

---

## Environment Variables

```env
# AWS S3
AWS_REGION=us-east-1
AWS_ACCESS_KEY_ID=your-access-key
AWS_SECRET_ACCESS_KEY=your-secret-key
S3_BUCKET=gixat-session-media

# CloudFront (optional, for CDN)
CLOUDFRONT_DOMAIN=d123456.cloudfront.net

# Redis (for queue)
REDIS_HOST=localhost
REDIS_PORT=6379

# ClamAV
CLAMAV_HOST=localhost
CLAMAV_PORT=3310
```

---

## S3 Bucket Configuration

### Bucket Policy
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "DenyInsecureTransport",
      "Effect": "Deny",
      "Principal": "*",
      "Action": "s3:*",
      "Resource": [
        "arn:aws:s3:::gixat-session-media/*"
      ],
      "Condition": {
        "Bool": {
          "aws:SecureTransport": "false"
        }
      }
    }
  ]
}
```

### CORS Configuration
```json
[
  {
    "AllowedHeaders": ["*"],
    "AllowedMethods": ["PUT"],
    "AllowedOrigins": ["https://gixat.com", "http://localhost:3002"],
    "ExposeHeaders": ["ETag"],
    "MaxAgeSeconds": 3000
  }
]
```

### Lifecycle Rules
```json
{
  "Rules": [
    {
      "Id": "DeletePendingUploads",
      "Status": "Enabled",
      "Filter": {
        "Prefix": "sessions/"
      },
      "AbortIncompleteMultipartUpload": {
        "DaysAfterInitiation": 1
      }
    }
  ]
}
```

---

## Frontend Flow (Already Implemented)

```typescript
// 1. Validate file client-side
const validationError = this.validateFile(file);

// 2. Get presigned URL
const { uploadUrl, fileKey } = await this.sessionService.getUploadUrl(
  sessionId, file.name, file.type, stage
).toPromise();

// 3. Upload to S3 directly
await this.sessionService.uploadToS3(uploadUrl, file);

// 4. Confirm upload
await this.sessionService.confirmUpload(
  sessionId, fileKey, stage, alt
).toPromise();
```

---

## Testing Checklist

### Unit Tests
- [ ] getUploadUrl validates content type
- [ ] getUploadUrl validates filename
- [ ] getUploadUrl checks user authorization
- [ ] confirmUpload verifies file exists in S3
- [ ] Media processor scans for viruses
- [ ] Media processor strips EXIF data
- [ ] Media processor handles failures gracefully

### Integration Tests
- [ ] Complete upload flow (presigned → upload → confirm)
- [ ] Virus scan catches malicious files
- [ ] Large file uploads (up to 50MB)
- [ ] Concurrent uploads
- [ ] Upload expiration (presigned URL expires after 5 min)

### Security Tests
- [ ] Cannot upload to another user's session
- [ ] Cannot confirm upload for another user's media
- [ ] Expired presigned URLs are rejected
- [ ] Invalid content types are rejected
- [ ] Malicious files are caught and deleted

---

## Monitoring & Alerts

### CloudWatch Metrics
- Upload success/failure rate
- Processing time per file
- Virus detection count
- Storage costs

### Alerts
- High virus detection rate
- Processing failures > 5%
- Upload failures > 10%
- Storage usage > 80% of quota

---

## Cost Optimization

1. **Use S3 Intelligent-Tiering** for automatic storage class transitions
2. **CloudFront CDN** for serving processed media (reduces S3 GET costs)
3. **Lifecycle policies** to archive old media to Glacier
4. **Compression** reduces storage and transfer costs
5. **Presigned URLs** reduce backend data transfer costs

---

## Security Best Practices

✅ **Presigned URLs expire quickly** (5 minutes)  
✅ **Virus scanning** on all uploads  
✅ **EXIF stripping** removes metadata  
✅ **Server-side encryption** (AES-256)  
✅ **HTTPS only** (no insecure transport)  
✅ **Access control** (user must own session)  
✅ **Content-Type validation** (whitelist)  
✅ **File size limits** enforced by S3  
✅ **Audit logging** of all operations  

---

## Next Steps for Backend Team

1. ✅ Implement `getUploadUrl` mutation
2. ✅ Implement `confirmUpload` mutation
3. ✅ Set up S3 bucket with CORS
4. ✅ Implement background processing queue (Bull/BullMQ)
5. ✅ Integrate ClamAV for virus scanning
6. ✅ Add CloudWatch monitoring
7. ✅ Write tests
8. ✅ Deploy to staging

Frontend is ready and waiting! 🚀
