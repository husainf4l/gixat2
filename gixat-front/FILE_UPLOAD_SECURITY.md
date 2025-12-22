# File Upload Security Requirements

## Frontend Validations Implemented ✅

### 1. File Size Limit
- **Max Size**: 50MB
- **Validation**: Checks `file.size` before upload
- **Error Message**: "File size exceeds 50MB limit"

### 2. MIME Type Whitelist
**Allowed Image Types:**
- `image/jpeg`, `image/jpg`
- `image/png`
- `image/gif`
- `image/webp`
- `image/heic`, `image/heif`

**Allowed Video Types:**
- `video/mp4`
- `video/mpeg`
- `video/quicktime` (.mov)
- `video/x-msvideo` (.avi)
- `video/webm`

### 3. File Extension Whitelist
**Images:** `.jpg`, `.jpeg`, `.png`, `.gif`, `.webp`, `.heic`, `.heif`  
**Videos:** `.mp4`, `.mov`, `.avi`, `.mpeg`, `.webm`

### 4. Filename Security
- ❌ Blocks path traversal: `../`, `./`
- ❌ Blocks directory separators: `/`, `\`
- ❌ Blocks double extensions: `file.php.jpg`
- ❌ Blocks files with more than 2 parts (name + extension)

### 5. Empty File Check
- Rejects files with 0 bytes

---

## Backend Security Requirements (CRITICAL) ⚠️

### 1. Server-Side File Validation (MUST IMPLEMENT)
**Never trust client-side validation alone!**

```typescript
// Pseudo-code for backend validation
function validateUploadedFile(file) {
  // 1. Check file size
  if (file.size > 50 * 1024 * 1024) {
    throw new Error('File too large');
  }

  // 2. Verify MIME type from file headers (magic numbers)
  const actualMimeType = detectMimeTypeFromContent(file);
  const allowedTypes = ['image/jpeg', 'image/png', ...];
  if (!allowedTypes.includes(actualMimeType)) {
    throw new Error('Invalid file type');
  }

  // 3. Validate file extension
  const ext = getFileExtension(file.name);
  const allowedExts = ['.jpg', '.png', ...];
  if (!allowedExts.includes(ext)) {
    throw new Error('Invalid extension');
  }

  // 4. Scan for malware (if possible)
  scanForVirus(file);

  return true;
}
```

### 2. File Storage Security

#### Option A: Object Storage (Recommended)
**Use AWS S3, Cloudflare R2, or similar:**
```typescript
// Example with S3
{
  bucket: 'garage-session-media',
  key: `sessions/${sessionId}/${uuid}-${sanitizedFilename}`,
  contentType: validatedMimeType,
  acl: 'private', // Serve via signed URLs
  serverSideEncryption: 'AES256'
}
```

**Benefits:**
- ✅ Isolated from application server
- ✅ Built-in virus scanning (AWS Macie)
- ✅ CDN integration
- ✅ Automatic backups

#### Option B: Local File System
**If storing locally, implement these safeguards:**

```typescript
// 1. Store outside web root
const UPLOAD_DIR = '/var/app/uploads/session-media/'; // NOT /public/

// 2. Generate random filenames
const safeFilename = `${uuid()}-${Date.now()}${validExtension}`;

// 3. Set restrictive permissions
fs.chmodSync(filepath, 0o600); // Read/write for owner only

// 4. Serve via application route (not direct access)
app.get('/api/media/:id', authenticate, authorizeMedia, (req, res) => {
  const filepath = getSecureFilepath(req.params.id);
  res.sendFile(filepath);
});
```

### 3. Content Security

#### Remove EXIF Data
```typescript
// Use sharp or similar library
import sharp from 'sharp';

await sharp(inputFile)
  .rotate() // Auto-rotate based on EXIF
  .withMetadata(false) // Strip EXIF data
  .toFile(outputFile);
```

**Why?** EXIF data can contain:
- GPS coordinates
- Camera serial numbers
- Personal information

#### Image Processing
```typescript
// Re-encode images to sanitize
await sharp(inputFile)
  .jpeg({ quality: 85 }) // or .png()
  .toFile(outputFile);
```

**Why?** Destroys potential embedded malicious code

### 4. Access Control

#### Authentication Required
```graphql
mutation UploadMediaToSession(
  $sessionId: UUID!
  $file: Upload!
  $stage: String
  $alt: String
) @authenticated @authorize(role: "MECHANIC")
```

#### Authorization Checks
```typescript
// Verify user has access to this session
const session = await getSession(sessionId);
if (session.organizationId !== user.organizationId) {
  throw new ForbiddenError('Access denied');
}
```

#### Media Access Control
```typescript
// When serving media
app.get('/api/media/:mediaId', async (req, res) => {
  const media = await getMedia(req.params.mediaId);
  const session = await getSession(media.sessionId);
  
  // Check user has access to session's organization
  if (session.organizationId !== req.user.organizationId) {
    return res.status(403).send('Forbidden');
  }
  
  res.sendFile(media.filepath);
});
```

### 5. Rate Limiting

```typescript
// Limit uploads per user/IP
const uploadLimiter = rateLimit({
  windowMs: 15 * 60 * 1000, // 15 minutes
  max: 50, // 50 uploads per window
  message: 'Too many uploads, please try again later'
});

app.post('/graphql', uploadLimiter, graphqlHandler);
```

### 6. Virus Scanning

**Option A: ClamAV (Open Source)**
```typescript
import clamscan from 'clamscan';

const scanner = await new ClamScan().init();
const { isInfected } = await scanner.scanFile(filepath);

if (isInfected) {
  fs.unlinkSync(filepath);
  throw new Error('File contains malware');
}
```

**Option B: Cloud Service**
- AWS GuardDuty
- VirusTotal API
- Cloudflare Gateway

### 7. Database Security

```sql
-- Store media metadata
CREATE TABLE session_media (
  id UUID PRIMARY KEY,
  session_id UUID NOT NULL REFERENCES garage_sessions(id) ON DELETE CASCADE,
  filename VARCHAR(255) NOT NULL, -- Original name (sanitized)
  stored_filename VARCHAR(255) NOT NULL, -- Random UUID-based name
  mime_type VARCHAR(100) NOT NULL,
  file_size BIGINT NOT NULL,
  stage VARCHAR(50), -- intake, inspection, etc.
  alt_text TEXT,
  uploaded_by UUID NOT NULL REFERENCES users(id),
  uploaded_at TIMESTAMP DEFAULT NOW(),
  virus_scanned BOOLEAN DEFAULT false,
  virus_scan_result VARCHAR(50),
  CONSTRAINT valid_mime_type CHECK (mime_type IN ('image/jpeg', 'image/png', ...))
);

-- Index for fast lookups
CREATE INDEX idx_session_media_session ON session_media(session_id);
```

### 8. Content-Type Headers

```typescript
// When serving files
res.setHeader('Content-Type', media.mimeType); // Use validated MIME
res.setHeader('X-Content-Type-Options', 'nosniff'); // Prevent MIME sniffing
res.setHeader('Content-Disposition', 'inline'); // or 'attachment' for downloads
```

### 9. CSP Headers

```typescript
// Content Security Policy
res.setHeader('Content-Security-Policy', 
  "default-src 'self'; " +
  "img-src 'self' data: https:; " +
  "media-src 'self'; " +
  "object-src 'none';"
);
```

### 10. Logging & Monitoring

```typescript
// Log all upload attempts
logger.info('File upload', {
  userId: user.id,
  sessionId,
  filename: file.name,
  size: file.size,
  mimeType: file.type,
  ip: req.ip,
  userAgent: req.headers['user-agent'],
  timestamp: new Date()
});

// Alert on suspicious activity
if (file.size > 45 * 1024 * 1024) { // Close to limit
  alertSecurityTeam('Large file upload attempt', { userId, file });
}
```

---

## Security Checklist for Backend Team

### Critical (Must Have) ✅
- [ ] Server-side file size validation (50MB max)
- [ ] Server-side MIME type validation (magic number check)
- [ ] File extension whitelist validation
- [ ] Store files outside web root OR use object storage
- [ ] Random UUID-based filenames
- [ ] Authentication required for uploads
- [ ] Authorization check (user has access to session)
- [ ] Strip EXIF data from images
- [ ] Set secure Content-Type headers
- [ ] Database constraints on mime_type field

### High Priority ⚠️
- [ ] Rate limiting on uploads
- [ ] Virus scanning (ClamAV or cloud service)
- [ ] Re-encode images to sanitize
- [ ] Comprehensive logging
- [ ] Content-Disposition headers
- [ ] File permissions (0o600)

### Recommended 📋
- [ ] CSP headers
- [ ] Monitoring & alerting
- [ ] Automatic cleanup of orphaned files
- [ ] File retention policy
- [ ] Backup strategy
- [ ] CDN integration for media serving

---

## Testing Checklist

### Security Tests to Perform:
1. **Upload malicious file types:**
   - [ ] .exe, .sh, .bat files
   - [ ] .php, .jsp, .asp files
   - [ ] SVG with embedded JavaScript
   - [ ] Files with double extensions (.php.jpg)

2. **Upload oversized files:**
   - [ ] 51MB file
   - [ ] 100MB file

3. **Path traversal attempts:**
   - [ ] Filename: `../../etc/passwd`
   - [ ] Filename: `..\..\windows\system32`

4. **Unauthorized access:**
   - [ ] Upload to another organization's session
   - [ ] Access another organization's media

5. **MIME type spoofing:**
   - [ ] Rename .exe to .jpg
   - [ ] Modify Content-Type header

6. **Rate limiting:**
   - [ ] 100 rapid upload attempts

---

## Frontend-Backend Contract

**Frontend sends:**
```typescript
{
  sessionId: UUID,
  file: File,
  stage: 'intake' | 'inspection' | 'testDrive' | 'customerRequests' | 'initialReport',
  alt: string
}
```

**Backend validates:**
1. User is authenticated
2. User has access to session
3. File size ≤ 50MB
4. MIME type is in whitelist
5. File extension is valid
6. Filename is safe
7. (Optional) File passes virus scan

**Backend returns:**
```typescript
{
  id: UUID,
  url: string, // Signed URL or proxied route
  alt: string
}
```

---

## Incident Response

If malicious upload detected:
1. **Immediate:** Delete file from storage
2. **Log:** Record user, IP, file details
3. **Alert:** Notify security team
4. **Investigate:** Review user's other uploads
5. **Block:** Consider temporary account suspension
6. **Review:** Audit access logs for the session

---

## Additional Resources

- OWASP File Upload Cheat Sheet: https://cheatsheetseries.owasp.org/cheatsheets/File_Upload_Cheat_Sheet.html
- CWE-434: Unrestricted Upload of File with Dangerous Type
- SANS Secure File Upload Guidelines
