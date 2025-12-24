# User Invitation System - Complete Guide

## Overview
Secure, role-based user invitation system for multi-tenant organizations with invite code validation and expiration.

---

## Architecture

### Flow Diagram
```
Admin/Manager → Create Invite → Generate Code → Send Link → User Registers → Auto-assign Organization & Role
```

### Key Components
1. **Invite Creation** - Admins/Managers generate invite codes
2. **Code Validation** - 24-hour expiration with status tracking
3. **User Registration** - Automatic organization assignment via invite code
4. **Role Assignment** - Pre-defined roles assigned upon acceptance

---

## GraphQL Endpoints

### 1. **Create Invite** (Mutation)

**Authorization:** `OrgAdmin` or `OrgManager` roles required

```graphql
mutation InviteUser($input: InviteUserInput!) {
  inviteUser(input: $input) {
    invite {
      id
      email
      role
      inviteCode
      expiryDate
      status
      createdAt
      organizationId
      inviter {
        id
        fullName
        email
      }
    }
    link
    error
  }
}
```

**Input Variables:**
```json
{
  "input": {
    "email": "newuser@example.com",
    "role": "OrgUser"
  }
}
```

**Response Example:**
```json
{
  "data": {
    "inviteUser": {
      "invite": {
        "id": "550e8400-e29b-41d4-a716-446655440000",
        "email": "newuser@example.com",
        "role": "OrgUser",
        "inviteCode": "A1B2C3D4E5F6",
        "expiryDate": "2025-12-26T10:30:00Z",
        "status": "PENDING",
        "createdAt": "2025-12-25T10:30:00Z",
        "organizationId": "org-uuid-here",
        "inviter": {
          "id": "user-id",
          "fullName": "Admin User",
          "email": "admin@example.com"
        }
      },
      "link": "http://localhost:4200/register?code=A1B2C3D4E5F6",
      "error": null
    }
  }
}
```

**Features:**
- ✅ Generates secure 12-character alphanumeric code
- ✅ 24-hour expiration by default
- ✅ Automatically captures inviter information
- ✅ Multi-tenant aware (inherits organization from inviter's context)
- ✅ Returns ready-to-send registration link

---

### 2. **Get All Invites** (Query)

**Authorization:** `OrgAdmin` or `OrgManager` roles required

```graphql
query GetInvites(
  $where: UserInviteFilterInput
  $order: [UserInviteSortInput!]
) {
  invites(where: $where, order: $order) {
    id
    email
    role
    inviteCode
    expiryDate
    status
    createdAt
    organizationId
    inviter {
      id
      fullName
      email
    }
  }
}
```

**Filtering Examples:**

**a) Get Pending Invites:**
```json
{
  "where": {
    "status": { "eq": "PENDING" }
  }
}
```

**b) Get Invites by Email:**
```json
{
  "where": {
    "email": { "contains": "example.com" }
  }
}
```

**c) Get Expired Invites:**
```json
{
  "where": {
    "status": { "eq": "EXPIRED" }
  }
}
```

**Sorting Example:**
```json
{
  "order": [
    { "createdAt": "DESC" }
  ]
}
```

---

### 3. **Get Invite by Code** (Query)

**Authorization:** `AllowAnonymous` (for registration flow)

```graphql
query GetInviteByCode($code: String!) {
  inviteByCode(code: $code) {
    id
    email
    role
    inviteCode
    expiryDate
    status
    organizationId
    organization {
      id
      name
    }
  }
}
```

**Input Variables:**
```json
{
  "code": "A1B2C3D4E5F6"
}
```

**Response Example:**
```json
{
  "data": {
    "inviteByCode": {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "email": "newuser@example.com",
      "role": "OrgUser",
      "inviteCode": "A1B2C3D4E5F6",
      "expiryDate": "2025-12-26T10:30:00Z",
      "status": "PENDING",
      "organizationId": "org-uuid-here",
      "organization": {
        "id": "org-uuid-here",
        "name": "Acme Corporation"
      }
    }
  }
}
```

**Features:**
- ✅ Public endpoint (no auth required)
- ✅ Only returns valid invites (Pending status + not expired)
- ✅ Returns organization details for display
- ✅ Returns `null` if invite is invalid/expired

---

### 4. **Cancel Invite** (Mutation)

**Authorization:** `OrgAdmin` or `OrgManager` roles required

```graphql
mutation CancelInvite($id: UUID!) {
  cancelInvite(id: $id)
}
```

**Input Variables:**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000"
}
```

**Response:**
```json
{
  "data": {
    "cancelInvite": true
  }
}
```

---

### 5. **Register with Invite Code** (Mutation)

**Authorization:** `AllowAnonymous`

```graphql
mutation Register($input: RegisterInput!) {
  register(input: $input) {
    token
    user {
      id
      email
      fullName
      userType
      organizationId
      isActive
    }
    error
  }
}
```

**Input Variables (With Invite Code):**
```json
{
  "input": {
    "email": "newuser@example.com",
    "password": "SecurePass123!",
    "fullName": "John Doe",
    "role": "OrgUser",
    "userType": "ORGANIZATIONAL",
    "inviteCode": "A1B2C3D4E5F6"
  }
}
```

**Input Variables (Without Invite Code - Regular Registration):**
```json
{
  "input": {
    "email": "newuser@example.com",
    "password": "SecurePass123!",
    "fullName": "John Doe",
    "role": "Public",
    "userType": "PUBLIC"
  }
}
```

**Response Example:**
```json
{
  "data": {
    "register": {
      "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
      "user": {
        "id": "user-uuid",
        "email": "newuser@example.com",
        "fullName": "John Doe",
        "userType": "ORGANIZATIONAL",
        "organizationId": "org-uuid-here",
        "isActive": true
      },
      "error": null
    }
  }
}
```

**Features:**
- ✅ Validates invite code automatically
- ✅ Checks expiration (24-hour window)
- ✅ Assigns user to organization from invite
- ✅ Assigns role from invite (overrides input role)
- ✅ Marks invite as "Accepted" upon success
- ✅ Returns JWT token for immediate login

---

## Data Model

### UserInvite Entity

```csharp
public sealed class UserInvite : IMustHaveOrganization
{
    public Guid Id { get; set; }
    
    public string Email { get; set; }              // Invitee email
    public string Role { get; set; }               // Role to assign (OrgUser, OrgManager, etc.)
    
    public string InviteCode { get; set; }         // 12-char alphanumeric code
    public DateTime ExpiryDate { get; set; }       // 24 hours from creation
    
    public InviteStatus Status { get; set; }       // Pending, Accepted, Expired, Canceled
    
    public Guid OrganizationId { get; set; }       // Target organization
    public Organization? Organization { get; set; }
    
    public string? InviterId { get; set; }         // Who sent the invite
    public ApplicationUser? Inviter { get; set; }
    
    public DateTime CreatedAt { get; set; }
}
```

### InviteStatus Enum

```csharp
public enum InviteStatus
{
    Pending = 0,    // Invite created, waiting for acceptance
    Accepted = 1,   // User registered successfully
    Expired = 2,    // 24 hours passed
    Canceled = 3    // Admin/Manager canceled invite
}
```

---

## Best Practices & Implementation Guide

### 1. **Frontend Registration Flow**

```typescript
// Step 1: User clicks invite link with code
// URL: http://yourapp.com/register?code=A1B2C3D4E5F6

// Step 2: Validate code before showing form
const { data } = await apolloClient.query({
  query: GET_INVITE_BY_CODE,
  variables: { code: 'A1B2C3D4E5F6' }
});

if (!data.inviteByCode) {
  // Show "Invalid or expired invite" message
  return;
}

// Step 3: Pre-fill email, show organization name
const invite = data.inviteByCode;
setEmail(invite.email); // Pre-filled, read-only
setOrganizationName(invite.organization.name);

// Step 4: User fills password and name, submits
const { data: authData } = await apolloClient.mutate({
  mutation: REGISTER,
  variables: {
    input: {
      email: invite.email,
      password: userPassword,
      fullName: userFullName,
      role: invite.role, // From invite
      userType: 'ORGANIZATIONAL',
      inviteCode: invite.inviteCode
    }
  }
});

// Step 5: Store token and redirect
localStorage.setItem('token', authData.register.token);
router.push('/dashboard');
```

### 2. **Email Notification Integration**

**Recommended: Add Email Service**

```csharp
// Add to InviteMutations.cs after creating invite:

// Option A: Using SendGrid (Recommended)
public interface IEmailService
{
    Task SendInviteEmailAsync(string toEmail, string inviteLink, string organizationName);
}

// In InviteUserAsync after creating invite:
var emailService = configuration.GetService<IEmailService>();
if (emailService != null)
{
    await emailService.SendInviteEmailAsync(
        input.Email, 
        link, 
        context.Organizations.Find(organizationId)?.Name ?? "Your Organization"
    );
}

// Option B: Using Background Job (Hangfire/Quartz)
BackgroundJob.Enqueue(() => SendInviteEmail(input.Email, link));

// Option C: Queue-based (RabbitMQ/Azure Service Bus)
await messageQueue.PublishAsync(new InviteCreatedEvent {
    Email = input.Email,
    InviteLink = link,
    OrganizationId = organizationId
});
```

**Email Template Example:**

```html
Subject: You've been invited to join [Organization Name]

Hi there,

[Inviter Name] has invited you to join [Organization Name] as a [Role].

Click the link below to create your account:
[Registration Link]

This invitation expires in 24 hours.

If you have any questions, please contact [Organization Email].

Best regards,
The [App Name] Team
```

### 3. **Security Best Practices**

**a) Rate Limiting (Recommended)**
```csharp
// Add rate limiting to prevent invite spam
[RateLimit(MaxAttempts = 10, WindowMinutes = 60)] // 10 invites per hour
public static async Task<InvitePayload> InviteUserAsync(...)
```

**b) Duplicate Prevention**
```csharp
// Check if user already invited or exists
var existingInvite = await context.UserInvites
    .Where(i => i.Email == input.Email && i.Status == InviteStatus.Pending)
    .FirstOrDefaultAsync();

if (existingInvite != null)
{
    return new InvitePayload(null, null, "User already has a pending invitation");
}

var existingUser = await userManager.FindByEmailAsync(input.Email);
if (existingUser != null)
{
    return new InvitePayload(null, null, "User already exists in the system");
}
```

**c) Secure Code Generation**
```csharp
// Current implementation uses cryptographically secure random:
const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
var code = new string(Enumerable.Range(0, 12)
    .Select(_ => chars[RandomNumberGenerator.GetInt32(chars.Length)]).ToArray());

// ✅ This is secure - uses System.Security.Cryptography.RandomNumberGenerator
```

### 4. **Expiration Handling**

**Background Job for Auto-Expiration (Optional):**
```csharp
// Add a scheduled job to mark expired invites
public class InviteExpirationJob
{
    public async Task ExpireOldInvitesAsync()
    {
        var expiredInvites = await context.UserInvites
            .Where(i => i.Status == InviteStatus.Pending && i.ExpiryDate < DateTime.UtcNow)
            .ToListAsync();
        
        foreach (var invite in expiredInvites)
        {
            invite.Status = InviteStatus.Expired;
        }
        
        await context.SaveChangesAsync();
    }
}

// Schedule: Run every hour
RecurringJob.AddOrUpdate("expire-invites", 
    () => job.ExpireOldInvitesAsync(), 
    Cron.Hourly);
```

### 5. **Multi-Tenant Isolation**

The system automatically handles multi-tenancy:

```csharp
// ✅ Invite inherits OrganizationId from authenticated user's context
// ✅ Global query filters ensure users only see their org's invites
// ✅ User registration automatically assigns correct OrganizationId from invite
```

### 6. **Role Configuration**

**Available Roles:**
```csharp
// Standard roles in the system:
- "OrgAdmin"    // Full organization access
- "OrgManager"  // Can manage users and invites
- "OrgUser"     // Standard user access
- "Mechanic"    // Workshop-specific role
- "Accountant"  // Finance-specific role
- "Public"      // Public users (no organization)
```

**Role Hierarchy:**
```
OrgAdmin > OrgManager > OrgUser/Mechanic/Accountant
```

### 7. **Frontend Components**

**Invite Management Dashboard:**
```typescript
const InviteList = () => {
  const { data, refetch } = useQuery(GET_INVITES, {
    variables: {
      where: { status: { eq: 'PENDING' } },
      order: [{ createdAt: 'DESC' }]
    }
  });

  const [cancelInvite] = useMutation(CANCEL_INVITE);

  const handleCancel = async (id: string) => {
    await cancelInvite({ variables: { id } });
    refetch();
  };

  return (
    <Table>
      {data?.invites.map(invite => (
        <Row key={invite.id}>
          <Cell>{invite.email}</Cell>
          <Cell>{invite.role}</Cell>
          <Cell>{formatDate(invite.expiryDate)}</Cell>
          <Cell>
            <Badge status={invite.status} />
          </Cell>
          <Cell>
            <Button onClick={() => handleCancel(invite.id)}>
              Cancel
            </Button>
          </Cell>
        </Row>
      ))}
    </Table>
  );
};
```

**Create Invite Form:**
```typescript
const InviteUserForm = () => {
  const [inviteUser] = useMutation(INVITE_USER);
  
  const handleSubmit = async (values: { email: string; role: string }) => {
    const { data } = await inviteUser({
      variables: { input: values }
    });

    if (data?.inviteUser.link) {
      // Option 1: Copy link to clipboard
      navigator.clipboard.writeText(data.inviteUser.link);
      toast.success('Invite link copied to clipboard!');
      
      // Option 2: Send email (if implemented)
      // await sendEmail(values.email, data.inviteUser.link);
      
      // Option 3: Show link in modal
      // openModal({ link: data.inviteUser.link });
    }
  };

  return (
    <Form onSubmit={handleSubmit}>
      <Input name="email" type="email" required />
      <Select name="role">
        <option value="OrgUser">User</option>
        <option value="OrgManager">Manager</option>
        <option value="Mechanic">Mechanic</option>
      </Select>
      <Button type="submit">Send Invite</Button>
    </Form>
  );
};
```

---

## Error Handling

### Common Error Scenarios

1. **Invalid Invite Code**
```json
{
  "data": {
    "register": {
      "token": null,
      "user": null,
      "error": "Invalid or expired invite code"
    }
  }
}
```

2. **Expired Invite**
```json
{
  "data": {
    "inviteByCode": null
  }
}
```

3. **Duplicate Email**
```csharp
// Add this validation to InviteUserAsync:
var existingUser = await userManager.FindByEmailAsync(input.Email);
if (existingUser != null)
{
    return new InvitePayload(null, null, "User already exists");
}
```

4. **Unauthorized Access**
```json
{
  "errors": [
    {
      "message": "The current user is not authorized to access this resource.",
      "extensions": {
        "code": "AUTH_NOT_AUTHORIZED"
      }
    }
  ]
}
```

---

## Testing

### Integration Tests

```csharp
[Fact]
public async Task InviteUser_ShouldCreateInviteWithCode()
{
    // Arrange
    var input = new InviteUserInput("test@example.com", "OrgUser");
    
    // Act
    var result = await InviteMutations.InviteUserAsync(
        input, claimsPrincipal, context, configuration);
    
    // Assert
    Assert.NotNull(result.Invite);
    Assert.Equal(12, result.Invite.InviteCode.Length);
    Assert.Equal("test@example.com", result.Invite.Email);
    Assert.Equal(InviteStatus.Pending, result.Invite.Status);
    Assert.True(result.Invite.ExpiryDate > DateTime.UtcNow);
}

[Fact]
public async Task RegisterWithInvite_ShouldAssignOrganizationAndRole()
{
    // Arrange
    var invite = CreateTestInvite();
    var registerInput = new RegisterInput(
        invite.Email, "Password123!", "Test User", 
        "OrgUser", UserType.Organizational, null, invite.InviteCode);
    
    // Act
    var result = await authService.RegisterAsync(registerInput);
    
    // Assert
    Assert.NotNull(result.User);
    Assert.Equal(invite.OrganizationId, result.User.OrganizationId);
    var roles = await userManager.GetRolesAsync(result.User);
    Assert.Contains(invite.Role, roles);
}
```

---

## Monitoring & Analytics

### Metrics to Track

1. **Invite Conversion Rate**
```sql
SELECT 
    COUNT(CASE WHEN Status = 1 THEN 1 END) * 100.0 / COUNT(*) as ConversionRate
FROM UserInvites
WHERE CreatedAt > DATEADD(day, -30, GETUTCDATE());
```

2. **Average Time to Accept**
```sql
SELECT 
    AVG(DATEDIFF(minute, CreatedAt, UpdatedAt)) as AvgMinutesToAccept
FROM UserInvites
WHERE Status = 1; -- Accepted
```

3. **Expiration Rate**
```sql
SELECT 
    COUNT(CASE WHEN Status = 2 THEN 1 END) * 100.0 / COUNT(*) as ExpirationRate
FROM UserInvites;
```

---

## Migration Guide

### Database Migration Required

The invite system uses the existing migration:
```
20251222203949_InitialCreate.cs
```

If you need to add custom fields:

```csharp
migrationBuilder.CreateTable(
    name: "UserInvites",
    columns: table => new
    {
        Id = table.Column<Guid>(nullable: false),
        Email = table.Column<string>(nullable: false),
        Role = table.Column<string>(nullable: false),
        InviteCode = table.Column<string>(maxLength: 12, nullable: false),
        ExpiryDate = table.Column<DateTime>(nullable: false),
        Status = table.Column<int>(nullable: false),
        OrganizationId = table.Column<Guid>(nullable: false),
        InviterId = table.Column<string>(nullable: true),
        CreatedAt = table.Column<DateTime>(nullable: false)
    });

migrationBuilder.CreateIndex(
    name: "IX_UserInvites_InviteCode",
    table: "UserInvites",
    column: "InviteCode",
    unique: true);
```

---

## Configuration

### Required Settings (appsettings.json)

```json
{
  "FrontendUrl": "http://localhost:4200",
  "Jwt": {
    "Key": "your-secret-key-here",
    "Issuer": "GixatBackend",
    "Audience": "GixatFrontend",
    "ExpireDays": "7"
  },
  "Email": {
    "Provider": "SendGrid",
    "ApiKey": "your-sendgrid-api-key",
    "FromEmail": "noreply@yourapp.com",
    "FromName": "Your App Name"
  }
}
```

---

## Summary

### ✅ Current Features
- Secure invite code generation (12-char alphanumeric)
- 24-hour expiration
- Role-based access control
- Multi-tenant isolation
- Automatic organization assignment
- Status tracking (Pending/Accepted/Expired/Canceled)
- GraphQL API with filtering/sorting

### 🚀 Recommended Enhancements
1. **Email Service Integration** (SendGrid/AWS SES)
2. **Rate Limiting** (prevent invite spam)
3. **Duplicate Detection** (check existing users/invites)
4. **Background Jobs** (auto-expire old invites)
5. **Invite Analytics Dashboard**
6. **Resend Invite Functionality**
7. **Custom Expiration Period** (make 24h configurable)
8. **Bulk Invite Import** (CSV upload)

### 📊 Production Checklist
- [ ] Add email sending service
- [ ] Implement rate limiting
- [ ] Add duplicate detection
- [ ] Set up monitoring/logging
- [ ] Configure frontend invite pages
- [ ] Test all error scenarios
- [ ] Set up background job for expiration
- [ ] Add invite analytics
- [ ] Configure email templates
- [ ] Test multi-tenant isolation

---

## Quick Reference

### GraphQL Operations
| Operation | Authorization | Purpose |
|-----------|--------------|---------|
| `inviteUser` | OrgAdmin/Manager | Create new invite |
| `invites` | OrgAdmin/Manager | List all invites |
| `inviteByCode` | Anonymous | Validate invite code |
| `cancelInvite` | OrgAdmin/Manager | Cancel pending invite |
| `register` | Anonymous | Register with invite code |

### Status Flow
```
Pending → Accepted  (user registered)
        → Expired   (24h passed)
        → Canceled  (admin action)
```

### Code Locations
- Mutations: `/Modules/Invites/GraphQL/InviteMutations.cs`
- Queries: `/Modules/Invites/GraphQL/InviteQueries.cs`
- Model: `/Modules/Invites/Models/UserInvite.cs`
- Registration: `/Modules/Users/Services/AuthService.cs`
