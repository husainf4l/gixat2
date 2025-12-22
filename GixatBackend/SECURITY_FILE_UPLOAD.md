# File Upload Security Implementation

## Overview

This document describes the comprehensive security measures implemented for file uploads in the Gixat Backend application. The implementation follows a defense-in-depth strategy with multiple layers of security checks.

## Security Architecture

### Three-Layer Security Approach

1. **Validation Layer** - `FileValidationService`
2. **Temporary Storage Layer** - `TempFileStorageService`
3. **Scanning Layer** - `IVirusScanService` / `ClamAvScanService`

### Upload Workflow

```
User Upload
    ↓
1. File Validation (whitelist, size, content-type)
    ↓
2. Save to Temporary Storage (isolated directory)
    ↓
3. Virus/Malware Scan (ClamAV)
    ↓
4. Upload to S3 (permanent storage)
    ↓
5. Delete from Temporary Storage
```

## Layer 1: File Validation

**Implementation:** `Modules/Common/Services/FileValidationService.cs`

### Security Checks

1. **Extension Whitelist**
   - Images: `.jpg`, `.jpeg`, `.png`, `.gif`, `.webp`, `.bmp`, `.svg`
   - Videos: `.mp4`, `.webm`, `.mov`, `.avi`, `.mkv`, `.m4v`
   - Rejects all other extensions (including `.exe`, `.dll`, `.sh`, etc.)

2. **Content-Type Validation**
   - Images: `image/jpeg`, `image/png`, `image/gif`, `image/webp`, `image/bmp`, `image/svg+xml`
   - Videos: `video/mp4`, `video/webm`, `video/quicktime`, `video/x-msvideo`, `video/x-matroska`, `video/x-m4v`
   - Prevents MIME type spoofing

3. **Extension/Content-Type Consistency**
   - Validates that file extension matches declared content type
   - Example: `.jpg` file must have `image/jpeg` content type

4. **File Size Limits**
   - Images: 10 MB maximum
   - Videos: 50 MB maximum
   - Prevents denial-of-service attacks via large files

5. **Path Traversal Prevention**
   - Rejects filenames containing: `..`, `/`, `\`, `:`, `*`, `?`, `"`, `<`, `>`, `|`
   - Sanitizes filenames by removing invalid characters
   - Adds timestamp to prevent filename collisions

## Layer 2: Temporary Storage

**Implementation:** `Modules/Common/Services/TempFileStorageService.cs`

### Security Features

1. **Isolated Storage**
   - Creates unique temporary directory: `{SystemTemp}/gixat-uploads/{GUID}`
   - Each request gets isolated storage
   - Prevents access to other application files

2. **Secure File Naming**
   - Uses SHA256 hash of content + timestamp
   - Format: `{hash}_{timestamp}_{originalName}`
   - Prevents filename collisions and path traversal

3. **Automatic Cleanup**
   - Implements `IDisposable` pattern
   - Deletes entire temp directory on disposal
   - Prevents temp file accumulation

4. **Lifecycle Management**
   ```csharp
   using var tempStorage = new TempFileStorageService(logger);
   try {
       var tempPath = await tempStorage.SaveTempFileAsync(stream, filename);
       // scan and process
   }
   finally {
       tempStorage.DeleteTempFile(tempPath); // explicit cleanup
   }
   // tempStorage.Dispose() called automatically - removes entire directory
   ```

## Layer 3: Virus Scanning

**Implementation:** 
- Interface: `Modules/Common/Services/IVirusScanService.cs`
- ClamAV: `Modules/Common/Services/ClamAvScanService.cs`

### Security Features

1. **Fail-Closed Design**
   - If scanning fails, upload is rejected
   - No files uploaded without successful scan
   - Better safe than sorry approach

2. **Configurable Scanning**
   ```json
   "ClamAV": {
     "Enabled": false,  // Set to true in production
     "Host": "localhost",
     "Port": 3310
   }
   ```

3. **Detailed Scan Results**
   ```csharp
   public class ScanResult
   {
       public bool IsClean { get; set; }
       public string? ThreatName { get; set; }
       public string Message { get; set; }
   }
   ```

4. **Production Integration Required**
   - Current implementation is a stub
   - Production requires ClamAV daemon (clamd)
   - Recommended: Use NuGet package `nClam` or `ClamAV.Net`

## Integration Points

### 1. Generic Media Upload
**Endpoint:** `uploadMedia` mutation  
**File:** `Modules/Common/GraphQL/MediaMutations.cs`

```graphql
mutation {
  uploadMedia(file: Upload!, alt: String) {
    url
    alt
    type
  }
}
```

### 2. Session Media Upload
**Endpoint:** `uploadMediaToSession` mutation  
**File:** `Modules/Sessions/GraphQL/SessionMutations.cs`

```graphql
mutation {
  uploadMediaToSession(
    sessionId: UUID!
    file: Upload!
    stage: SessionStage!
    alt: String
  ) {
    id
    url
    stage
  }
}
```

## Configuration

### Required Services (Program.cs)

```csharp
builder.Services.AddScoped<IVirusScanService, ClamAvScanService>();
```

### ClamAV Configuration (appsettings.json)

```json
{
  "ClamAV": {
    "Enabled": false,
    "Host": "localhost",
    "Port": 3310
  }
}
```

### Production Setup

1. **Install ClamAV Daemon**
   ```bash
   # Ubuntu/Debian
   apt-get install clamav-daemon
   
   # macOS
   brew install clamav
   
   # Docker
   docker run -d -p 3310:3310 clamav/clamav
   ```

2. **Update Configuration**
   ```json
   "ClamAV": {
     "Enabled": true,
     "Host": "clamav-service",
     "Port": 3310
   }
   ```

3. **Implement ClamAV Client**
   - Replace stub in `ClamAvScanService.cs`
   - Use NuGet: `nClam` or `ClamAV.Net`
   - Example:
     ```csharp
     var clam = new ClamClient(_host, _port);
     var result = await clam.SendAndScanFileAsync(stream);
     return new ScanResult {
         IsClean = result.Result == ClamScanResults.Clean,
         ThreatName = result.InfectedFiles?.FirstOrDefault()?.VirusName,
         Message = result.RawResult
     };
     ```

## Testing

### Security Test Cases

1. **Whitelist Validation**
   - ✅ Upload valid image (.jpg, .png)
   - ✅ Upload valid video (.mp4)
   - ❌ Reject executable (.exe, .dll, .sh)
   - ❌ Reject script files (.js, .php, .py)

2. **Content-Type Validation**
   - ✅ Match extension and content-type
   - ❌ Reject mismatched pairs (e.g., .jpg with text/plain)

3. **Size Limits**
   - ✅ Upload 9MB image (under limit)
   - ❌ Reject 11MB image (over limit)
   - ✅ Upload 45MB video (under limit)
   - ❌ Reject 55MB video (over limit)

4. **Path Traversal**
   - ❌ Reject `../../etc/passwd`
   - ❌ Reject `..\..\..\windows\system32`
   - ❌ Reject `file:///etc/passwd`

5. **Malware Detection**
   - Test with EICAR test file: `X5O!P%@AP[4\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*`
   - Should be detected and rejected by ClamAV

### Test EICAR File

```bash
# Download EICAR test file
curl -o eicar.txt https://secure.eicar.org/eicar.com.txt
```

## Error Handling

### Validation Errors

```json
{
  "errors": [
    {
      "message": "Invalid file extension. Allowed: jpg, jpeg, png, gif, webp, bmp, svg, mp4, webm, mov, avi, mkv, m4v",
      "extensions": {
        "code": "VALIDATION_ERROR"
      }
    }
  ]
}
```

### Scan Failures

```json
{
  "errors": [
    {
      "message": "File failed security scan: Virus detected (Threat: EICAR-Test-File)",
      "extensions": {
        "code": "SECURITY_SCAN_FAILED"
      }
    }
  ]
}
```

## Performance Considerations

1. **Temporary Storage**
   - Files are streamed, not loaded into memory
   - Cleanup happens automatically per request
   - No disk space accumulation

2. **Virus Scanning**
   - Scanning adds latency (typically 100-500ms per file)
   - Consider async processing for large files
   - May need queue system for high-volume uploads

3. **Optimization Options**
   - Skip scanning for small images (<1MB)
   - Cache scan results by file hash
   - Parallel scanning for multiple files
   - Background scanning with quarantine area

## Security Best Practices

1. **Never trust user input**
   - Always validate file extensions
   - Always check content types
   - Always enforce size limits

2. **Defense in depth**
   - Multiple layers of validation
   - Temporary isolation before permanent storage
   - Virus scanning as final gate

3. **Fail closed**
   - Reject on any validation failure
   - Reject if scanning unavailable
   - Better to block legitimate uploads than allow malicious ones

4. **Logging and monitoring**
   - Log all rejected uploads
   - Monitor scan failures
   - Alert on unusual patterns

5. **Regular updates**
   - Keep ClamAV signatures updated
   - Review whitelist periodically
   - Update size limits as needed

## Future Enhancements

1. **Advanced Scanning**
   - Deep content inspection
   - Document macro detection
   - Archive scanning (zip, rar)

2. **Quarantine System**
   - Isolate suspicious files
   - Manual review workflow
   - Delayed processing

3. **Rate Limiting**
   - Per-user upload limits
   - IP-based throttling
   - Organization quotas

4. **Content Analysis**
   - Image content validation (actual dimensions, format)
   - Video codec verification
   - Metadata stripping (EXIF)

5. **CDN Integration**
   - Direct upload to CDN
   - Signed URLs for secure upload
   - Edge validation

## References

- ClamAV Documentation: https://docs.clamav.net/
- OWASP File Upload Security: https://owasp.org/www-community/vulnerabilities/Unrestricted_File_Upload
- EICAR Test File: https://www.eicar.org/download-anti-malware-testfile/
