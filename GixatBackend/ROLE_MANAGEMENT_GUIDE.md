# Role Management Guide

## Overview
Your system has a role-based access control (RBAC) system with different permission levels. Roles are automatically created when needed and assigned during user registration or invitation.

---

## Available Roles

### System Roles (Highest Privilege)
- **SuperAdmin** (0) - Full system access across all organizations
- **SystemAdmin** (1) - System-level administration

### Organizational Roles (Organization-Scoped)
- **OrgAdmin** (10) - Full administrative access within their organization
  - Can manage all organization resources
  - Can invite users
  - Can view/manage appointments, job cards, sessions
  - Can manage customers and cars
  
- **OrgManager** (11) - Management access within their organization
  - Can manage day-to-day operations
  - Can invite users
  - Can view/manage appointments, job cards, sessions
  - Can manage customers and cars
  
- **OrgUser** (12) - Standard user access within their organization
  - Limited access based on specific permissions
  - Can work on assigned tasks

### Customer Roles
- **Customer** (20) - External customers
  - Can view their own appointments
  - Can view their own cars and service history

---

## Who Should Have Which Role?

### OrgAdmin
**Recommended for:**
- Garage owner
- Business manager
- System administrator for the organization

**Permissions:**
- ✅ View/Create/Edit/Delete all invites
- ✅ Full access to appointments
- ✅ Full access to job cards and estimates
- ✅ Full access to garage sessions
- ✅ Manage organization settings
- ✅ Manage all users in organization
- ✅ View all reports and analytics

**Use Cases:**
- Setting up the organization
- Managing staff
- Overseeing all operations
- Financial oversight

### OrgManager
**Recommended for:**
- Service manager
- Workshop supervisor
- Senior technician with management duties

**Permissions:**
- ✅ View/Create/Edit/Delete all invites
- ✅ Full access to appointments
- ✅ Full access to job cards and estimates
- ✅ Full access to garage sessions
- ✅ Assign technicians
- ✅ View reports

**Use Cases:**
- Daily operations management
- Scheduling appointments
- Managing work orders
- Staff coordination

### OrgUser
**Recommended for:**
- Technicians
- Service advisors
- Front desk staff

**Permissions:**
- ✅ View assigned appointments
- ✅ Work on assigned job cards
- ✅ Update session progress
- ⚠️ Limited invite access (implementation dependent)

**Use Cases:**
- Performing service work
- Updating job status
- Recording work completed

### Customer
**Recommended for:**
- Vehicle owners
- Fleet managers (external)

**Permissions:**
- ✅ View own appointments
- ✅ View own service history
- ✅ View own vehicles
- ❌ No access to internal operations

---

## How to Assign Roles

### Method 1: During User Registration (Self-Service)

When a user registers **without an invite code**, they specify their role:

```graphql
mutation Register {
  register(input: {
    email: "user@example.com"
    password: "SecurePassword123!"
    fullName: "John Doe"
    role: "OrgUser"  # Specify role here
    userType: ORGANIZATIONAL
  }) {
    token
    user {
      id
      email
      fullName
    }
    error
  }
}
```

**Available role values:**
- `"SuperAdmin"`
- `"SystemAdmin"`
- `"OrgAdmin"`
- `"OrgManager"`
- `"OrgUser"`
- `"Customer"`

### Method 2: Through Invitation System (Recommended)

**Step 1: Create an invite as OrgAdmin/OrgManager**
```graphql
mutation CreateInvite {
  createInvite(input: {
    email: "newuser@example.com"
    role: "OrgManager"  # Role is set in the invite
    expiryDate: "2025-12-31T23:59:59Z"
  }) {
    invite {
      id
      email
      role
      inviteCode
      expiryDate
    }
    error
  }
}
```

**Step 2: New user registers with invite code**
```graphql
mutation RegisterWithInvite {
  register(input: {
    email: "newuser@example.com"
    password: "SecurePassword123!"
    fullName: "Jane Smith"
    inviteCode: "ABC123XYZ"  # From invite
    # role is ignored when invite code is present
  }) {
    token
    user {
      id
      email
      fullName
    }
    error
  }
}
```

The user automatically:
- Gets assigned the role from the invite (e.g., OrgManager)
- Joins the organization from the invite
- The invite status changes to "Accepted"

### Method 3: Direct Database Assignment (For Existing Users)

If you need to upgrade an existing user's role:

**Step 1: Connect to your database**
```bash
# Using psql
psql -h your-database-host -U your-username -d your-database
```

**Step 2: Find the role IDs**
```sql
SELECT * FROM "AspNetRoles";
```

Example output:
```
Id                                   | Name        | NormalizedName
-------------------------------------|-------------|----------------
role-id-1                            | OrgAdmin    | ORGADMIN
role-id-2                            | OrgManager  | ORGMANAGER
role-id-3                            | OrgUser     | ORGUSER
```

**Step 3: Check current user roles**
```sql
SELECT u."Email", r."Name" 
FROM "AspNetUsers" u
LEFT JOIN "AspNetUserRoles" ur ON u."Id" = ur."UserId"
LEFT JOIN "AspNetRoles" r ON ur."RoleId" = r."Id"
WHERE u."Email" = 'al-hussein@papayatrading.com';
```

**Step 4: Assign role to user**
```sql
-- First, get the user ID
SELECT "Id" FROM "AspNetUsers" WHERE "Email" = 'al-hussein@papayatrading.com';

-- Then assign the role
INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
VALUES (
  'aa70d64b-669a-404f-baa6-8d1d7f4f5723',  -- Your user ID
  (SELECT "Id" FROM "AspNetRoles" WHERE "Name" = 'OrgAdmin')
);
```

**Step 5: User must log out and log back in**
The new role will be included in the JWT token on next login.

---

## Testing Role Assignment

### Verify Your Current Role
```bash
# Login and save the token
curl -X POST http://localhost:8002/graphql \
  -H "Content-Type: application/json" \
  -d '{"query":"mutation { login(email: \"your-email@example.com\", password: \"your-password\") { token user { id email fullName } error } }"}'
```

### Decode JWT to See Roles
Visit https://jwt.io and paste your token, or use this command:

```bash
# Install jwt-cli
brew install mike-engel/jwt-cli/jwt-cli

# Decode token
jwt decode YOUR_TOKEN_HERE
```

Look for the `role` claim in the payload:
```json
{
  "sub": "user-id",
  "email": "user@example.com",
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": "OrgAdmin",
  "OrganizationId": "org-id",
  "exp": 1767215470
}
```

### Test Access to Protected Endpoints

**Test Invites (Requires OrgAdmin or OrgManager):**
```bash
curl -X POST http://localhost:8002/graphql \
  -H "Content-Type: application/json" \
  -H "Cookie: access_token=YOUR_TOKEN" \
  -d '{"query":"query { invites { id email role status } }"}'
```

**Expected Results:**
- ✅ **OrgAdmin/OrgManager**: Returns list of invites
- ❌ **OrgUser/Customer/No Role**: `AUTH_NOT_AUTHORIZED` error

---

## Quick Fix for Your Current User

Based on your error, here's how to fix it right now:

```sql
-- Connect to your database
psql -h your-host -U your-user -d your-database

-- Assign OrgAdmin role to your user
INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
VALUES (
  'aa70d64b-669a-404f-baa6-8d1d7f4f5723',
  (SELECT "Id" FROM "AspNetRoles" WHERE "Name" = 'OrgAdmin')
)
ON CONFLICT DO NOTHING;
```

Then **log out and log back in** to get a new JWT token with the role.

---

## Role-Based Access Control Matrix

| Feature | SuperAdmin | SystemAdmin | OrgAdmin | OrgManager | OrgUser | Customer |
|---------|------------|-------------|----------|------------|---------|----------|
| **Invites** |
| View All Invites | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| Create Invite | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| Delete Invite | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| **Appointments** |
| View All | ✅ | ✅ | ✅ | ✅ | ✅* | ❌ |
| Create | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ |
| Update | ✅ | ✅ | ✅ | ✅ | ✅* | ❌ |
| Delete | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| Convert to Session | ✅ | ✅ | ✅ | ✅ | ✅* | ❌ |
| **Job Cards** |
| View All | ✅ | ✅ | ✅ | ✅ | ✅* | ❌ |
| Create | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ |
| Update | ✅ | ✅ | ✅ | ✅ | ✅* | ❌ |
| Approve Estimate | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| **Sessions** |
| View All | ✅ | ✅ | ✅ | ✅ | ✅* | ❌ |
| Update | ✅ | ✅ | ✅ | ✅ | ✅* | ❌ |
| **Customers** |
| View All | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ |
| Create/Update | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ |
| Delete | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| **Organization** |
| Manage Settings | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| View Reports | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |

*OrgUser can only access items assigned to them or within their scope

---

## Common Scenarios

### Scenario 1: First User Registration (Garage Owner)
```graphql
mutation {
  register(input: {
    email: "owner@garage.com"
    password: "SecurePass123!"
    fullName: "Garage Owner"
    role: "OrgAdmin"
    userType: ORGANIZATIONAL
  }) {
    token
    user { id email }
  }
}
```

### Scenario 2: Inviting a Manager
```graphql
# As OrgAdmin, create invite
mutation {
  createInvite(input: {
    email: "manager@garage.com"
    role: "OrgManager"
    expiryDate: "2025-12-31T23:59:59Z"
  }) {
    invite { inviteCode }
  }
}

# Manager receives email with code and registers
mutation {
  register(input: {
    email: "manager@garage.com"
    password: "SecurePass123!"
    fullName: "Service Manager"
    inviteCode: "ABC123"
  }) {
    token
  }
}
```

### Scenario 3: Adding Multiple Technicians
```graphql
# Create multiple invites with OrgUser role
mutation CreateTechInvite1 {
  createInvite(input: {
    email: "tech1@garage.com"
    role: "OrgUser"
    expiryDate: "2025-12-31T23:59:59Z"
  }) {
    invite { inviteCode }
  }
}

mutation CreateTechInvite2 {
  createInvite(input: {
    email: "tech2@garage.com"
    role: "OrgUser"
    expiryDate: "2025-12-31T23:59:59Z"
  }) {
    invite { inviteCode }
  }
}
```

---

## Troubleshooting

### Issue: "Current user is not authorized to access this resource"
**Cause**: User doesn't have the required role

**Solution**:
1. Check your current role (decode JWT)
2. Verify the endpoint's role requirements
3. Assign the correct role using database or re-invite
4. Log out and log back in

### Issue: Role not appearing in JWT token
**Cause**: Token was issued before role assignment

**Solution**: Log out and log back in to get a fresh token

### Issue: Cannot create invites
**Cause**: User doesn't have OrgAdmin or OrgManager role

**Solution**: Have an existing OrgAdmin assign you the role

### Issue: Multiple roles needed
**Note**: Currently the system supports one role per user. If you need multiple permission sets, use OrgAdmin (highest privilege within organization)

---

## Best Practices

1. **Start with OrgAdmin**: First user in an organization should be OrgAdmin
2. **Use Invites**: Always use the invitation system for adding users
3. **Principle of Least Privilege**: Assign the minimum role needed
4. **Regular Audits**: Periodically review user roles
5. **Role Transitions**: When promoting users, have them log out/in to refresh token
6. **Customer Separation**: Never give organizational roles to external customers

---

## Security Notes

- Roles are stored in JWT tokens - they persist until token expiry
- Token expiry is configurable (default: 7 days in `Jwt:ExpireDays`)
- Role changes require new login to take effect
- Invites expire and can only be used once
- SuperAdmin/SystemAdmin should be tightly controlled
- All mutations are protected by `[Authorize]` attributes

---

## Need Help?

Check these queries for role management:

```graphql
# List all roles in system (if you add this query)
query {
  roles {
    id
    name
  }
}

# Get current user info
query {
  me {
    id
    email
    fullName
  }
}

# List organization users (as OrgAdmin)
query {
  users {
    id
    email
    fullName
    # role would need to be added to UserProfile type
  }
}
```
