# Logout Mutation Documentation

## Overview

The logout mutation provides secure server-side session termination by clearing HTTP-only authentication cookies. This ensures proper logout functionality that complements the existing login/register mutations.

## GraphQL Mutation

### Mutation Definition
```graphql
mutation {
  logoutAsync {
    success
    message
  }
}
```

### Response Type
```csharp
public sealed class LogoutResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}
```

## Implementation Details

### Location
- **File**: `/Modules/Users/GraphQL/AuthMutations.cs`
- **Class**: `AuthMutations`
- **Method**: `LogoutAsync`

### Security
- **Authorization**: Requires `[Authorize]` attribute
- **Access**: Only authenticated users can call this mutation
- **Cookie Management**: Securely clears HTTP-only authentication cookies

### Code Implementation
```csharp
[Authorize]
public static LogoutResponse LogoutAsync(
    [Service] IHttpContextAccessor httpContextAccessor)
{
    try
    {
        ClearAuthCookie(httpContextAccessor);
        
        return new LogoutResponse
        {
            Success = true,
            Message = "Logout successful"
        };
    }
    catch (Exception ex)
    {
        return new LogoutResponse
        {
            Success = false,
            Message = $"Logout failed: {ex.Message}"
        };
    }
}

private static void ClearAuthCookie(IHttpContextAccessor httpContextAccessor)
{
    var httpContext = httpContextAccessor.HttpContext;
    if (httpContext != null)
    {
        httpContext.Response.Cookies.Append("access_token", "", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTime.UtcNow.AddDays(-1) // Set to past date to expire the cookie
        });
    }
}
```

## Usage Examples

### Frontend Integration (Angular/TypeScript)
```typescript
// GraphQL mutation
const LOGOUT_MUTATION = gql`
  mutation Logout {
    logoutAsync {
      success
      message
    }
  }
`;

// Service method
async logout(): Promise<void> {
  try {
    const result = await this.apollo.mutate({
      mutation: LOGOUT_MUTATION
    }).toPromise();
    
    if (result?.data?.logoutAsync?.success) {
      // Clear local state
      this.clearUserData();
      // Redirect to login
      this.router.navigate(['/login']);
    } else {
      console.error('Logout failed:', result?.data?.logoutAsync?.message);
    }
  } catch (error) {
    console.error('Logout error:', error);
  }
}
```

### cURL Testing
```bash
# Test with authentication
curl -X POST http://localhost:8002/graphql \
  -H "Content-Type: application/json" \
  -H "Cookie: access_token=your_jwt_token" \
  -d '{"query": "mutation { logoutAsync { success message } }"}'

# Expected response (success)
{
  "data": {
    "logoutAsync": {
      "success": true,
      "message": "Logout successful"
    }
  }
}

# Test without authentication
curl -X POST http://localhost:8002/graphql \
  -H "Content-Type: application/json" \
  -d '{"query": "mutation { logoutAsync { success message } }"}'

# Expected response (unauthorized)
{
  "errors": [
    {
      "message": "The current user is not authorized to access this resource.",
      "extensions": {
        "code": "AUTH_NOT_AUTHENTICATED"
      }
    }
  ]
}
```

## Cookie Management

### Authentication Cookie Details
- **Name**: `access_token`
- **Type**: HTTP-only (cannot be accessed via JavaScript)
- **Security**: Secure flag set to `true`
- **SameSite**: `None` (for cross-origin support)
- **Expiration**: Set to past date (`DateTime.UtcNow.AddDays(-1)`) to immediately expire

### Cookie Lifecycle
1. **Login**: Cookie set with 7-day expiration
2. **Request**: Cookie automatically sent with GraphQL requests
3. **Logout**: Cookie expired by setting past date
4. **Browser**: Automatically removes expired cookie

## Error Handling

### Possible Error Scenarios
1. **Not Authenticated**: Returns `AUTH_NOT_AUTHENTICATED` error
2. **Server Error**: Returns `LogoutResponse` with `Success = false`
3. **Network Error**: Standard HTTP/GraphQL error responses

### Error Response Example
```json
{
  "data": {
    "logoutAsync": {
      "success": false,
      "message": "Logout failed: HttpContext is null"
    }
  }
}
```

## Security Considerations

### Best Practices
- ✅ **Server-Side Invalidation**: Properly clears HTTP-only cookies
- ✅ **Authorization Required**: Only authenticated users can logout
- ✅ **Secure Cookie Handling**: Maintains security flags during clear operation
- ✅ **Error Handling**: Graceful failure with meaningful messages

### Security Benefits
- **Session Termination**: Ensures complete logout from server perspective
- **Cookie Security**: Prevents client-side cookie manipulation
- **Cross-Site Protection**: SameSite settings protect against CSRF
- **HTTPS Enforcement**: Secure flag ensures HTTPS-only transmission

## Integration with Existing Auth System

### Related Components
- **Login Mutation**: Sets authentication cookie
- **Register Mutation**: Sets authentication cookie after registration
- **Google Auth**: Sets authentication cookie after OAuth
- **JWT Middleware**: Validates cookie on subsequent requests

### Authentication Flow
```
1. User calls login/register → JWT cookie set
2. User makes authenticated requests → Cookie validated
3. User calls logout → Cookie cleared/expired
4. Subsequent requests → Return unauthorized
```

## Testing

### Unit Test Example
```csharp
[Test]
public void LogoutAsync_WithValidContext_ReturnSuccessResponse()
{
    // Arrange
    var httpContext = new DefaultHttpContext();
    var httpContextAccessor = new Mock<IHttpContextAccessor>();
    httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
    
    // Act
    var result = AuthMutations.LogoutAsync(httpContextAccessor.Object);
    
    // Assert
    Assert.IsTrue(result.Success);
    Assert.AreEqual("Logout successful", result.Message);
}
```

### Integration Test Verification
1. **Login** → Verify cookie is set
2. **Make authenticated request** → Verify success
3. **Logout** → Verify success response
4. **Make authenticated request** → Verify unauthorized

## Deployment Notes

### Environment Configuration
- No additional configuration required
- Uses existing HTTP context and cookie infrastructure
- Compatible with existing JWT authentication setup

### Monitoring
- Monitor logout success/failure rates
- Track authentication errors after logout
- Verify cookie clearing in browser developer tools

## Troubleshooting

### Common Issues

#### Issue: "AUTH_NOT_AUTHENTICATED" Error
**Cause**: User not logged in or invalid JWT token
**Solution**: Ensure user is properly authenticated before calling logout

#### Issue: Logout appears successful but user still authenticated
**Cause**: Frontend not clearing local state or making cached requests
**Solution**: Clear local storage/state and refresh authentication status

#### Issue: Cookie not cleared in browser
**Cause**: Browser security settings or SameSite configuration
**Solution**: Verify browser settings and cookie configuration match

### Debug Steps
1. Check browser developer tools → Application → Cookies
2. Verify JWT token validity before logout call
3. Confirm GraphQL response indicates success
4. Test subsequent authenticated requests return unauthorized

## Migration from Frontend-Only Logout

### Previous Behavior
- Frontend cleared local state only
- HTTP-only cookies remained valid
- Security gap: server-side sessions not invalidated

### New Behavior
- Frontend calls logout mutation
- Server clears HTTP-only cookies
- Complete session termination
- Enhanced security posture

### Migration Steps
1. Update frontend logout functions to call `logoutAsync` mutation
2. Keep existing local state clearing logic
3. Add error handling for logout mutation failures
4. Test end-to-end logout flow

## Google Authentication Fix

### Issue Resolved
Fixed a database constraint violation in Google authentication that occurred when:
- A user already existed in the database with the same email
- The user tried to authenticate with Google for the first time
- This caused a duplicate key violation on the `UserNameIndex`
- **Multi-tenant Issue**: Global query filters were preventing proper user lookup across organizations

### Root Cause
The application uses multi-tenant architecture with global query filters:
```csharp
builder.Entity<ApplicationUser>().HasQueryFilter(u => u.OrganizationId == _tenantService.OrganizationId);
builder.Entity<Account>().HasQueryFilter(a => a.User!.OrganizationId == _tenantService.OrganizationId);
```

During Google authentication, the tenant context might not be properly established, causing:
- `FindByEmailAsync()` to not find existing users from other organizations
- Attempts to create duplicate users
- Database constraint violations

### Solution Implemented
- **Query Filter Bypass**: Added `IgnoreQueryFilters()` for Google authentication lookups
- **Cross-Tenant User Detection**: Proper checking for existing users regardless of organization
- **Improved Error Handling**: Specific error messages for different failure scenarios
- **Enhanced Flow Logic**: Better handling of multi-tenant scenarios

### Technical Details
```csharp
// OLD: Tenant-filtered lookup (problematic)
var existingUser = await userManager.FindByEmailAsync(email).ConfigureAwait(false);

// NEW: Bypass tenant filters for Google auth
var existingUser = await context.Users
    .IgnoreQueryFilters()
    .FirstOrDefaultAsync(u => u.Email == email)
    .ConfigureAwait(false);

// Also bypass filters for account lookups
var account = await context.Accounts
    .IgnoreQueryFilters()
    .Include(a => a.User)
    .FirstOrDefaultAsync(a => a.Provider == "Google" && a.ProviderAccountId == googleId)
    .ConfigureAwait(false);
```

### Security Considerations
- **Cross-Tenant Access**: The fix allows Google authentication to work across organizations
- **Data Isolation**: Once authenticated, normal tenant filters still apply
- **Account Linking**: Users can link Google accounts regardless of organization context
- **Proper Organization Assignment**: Users maintain their original organization memberships

### Error Messages
- `Failed to create user: [details]` - When user creation fails
- `Failed to create Google account link: [details]` - When linking Google account fails
- `Failed to update account: [details]` - When updating existing account fails
- `Failed to update Google account: [details]` - When updating Google account fails

### Multi-Tenant Workflow
1. **Google Token Validation** → Bypasses tenant filters
2. **User Lookup** → Searches across all organizations for email
3. **Account Association** → Links Google account to existing user
4. **JWT Generation** → Creates token with proper organization context
5. **Subsequent Requests** → Normal tenant filtering applies