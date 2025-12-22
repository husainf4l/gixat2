# Backend Requirements for Session Workflow

## Current Status

### ✅ Available Mutations
- `updateIntake(sessionId: UUID!, intakeNotes: String, customerRequests: String)` - Updates intake notes and customer requests
- `updateInspection(sessionId: UUID!, inspectionNotes: String)` - Updates inspection notes
- `updateTestDrive(sessionId: UUID!, testDriveNotes: String)` - Updates test drive notes
- `generateInitialReport(sessionId: UUID!, report: String!)` - Generates initial report (required field)
- `createJobCardFromSession(sessionId: UUID!)` - Creates job card from completed session
- `uploadMedia(file: Upload!, alt: String)` - Uploads media file

### ✅ Available Queries
- `sessionById(id: UUID!)` - Returns full session details
- `sessions(first: Int, after: String, where: GarageSessionFilterInput, order: [GarageSessionSortInput!])` - Returns paginated sessions

---

## ⚠️ Issues & Required Changes

### 1. Media-Session Association
**Problem:** `uploadMedia` mutation doesn't accept `sessionId` parameter, so uploaded media cannot be linked to a specific session.

**Required Backend Changes:**
```graphql
# Option A: Add sessionId to uploadMedia mutation
mutation UploadMedia($file: Upload!, $sessionId: UUID, $alt: String) {
  uploadMedia(file: $file, sessionId: $sessionId, alt: $alt) {
    id
    url
    alt
    sessionId
  }
}

# Option B: Add separate mutation to link existing media
mutation AddMediaToSession($sessionId: UUID!, $mediaId: UUID!) {
  addMediaToSession(sessionId: $sessionId, mediaId: $mediaId) {
    id
    media {
      id
      url
      alt
    }
  }
}
```

**Frontend Impact:**
- Cannot display session-specific media in session detail page
- Media uploads are orphaned (not linked to any session)

---

### 2. Customer Requests Separation
**Problem:** `updateIntake` mutation handles both `intakeNotes` and `customerRequests` fields. When updating customer requests separately, we must preserve existing intake notes.

**Current Workaround:**
Frontend stores the current `intakeNotes` value and passes it along when updating `customerRequests` to avoid overwriting.

**Recommended Backend Change:**
```graphql
# Add separate mutation for customer requests
mutation UpdateCustomerRequests($sessionId: UUID!, $customerRequests: String) {
  updateCustomerRequests(sessionId: $sessionId, customerRequests: $customerRequests) {
    id
    customerRequests
  }
}
```

**Alternative:** Allow null values in `updateIntake` to only update provided fields without affecting others.

---

### 3. Media Field Type
**Problem:** GarageSession.media returns `[Media!]!` but the Media type structure is unknown.

**Required Information:**
```graphql
type Media {
  id: UUID!
  url: String!
  alt: String
  # ... other fields?
}
```

**Frontend Needs:**
- Media ID for deletion/management
- Media URL for display
- Media alt text for accessibility
- Creation timestamp (optional)
- File size/type (optional)

---

### 4. Session Status Workflow
**Current:** Frontend assumes workflow: INTAKE → IN_PROGRESS → QUALITY_CHECK → READY_FOR_PICKUP → COMPLETED

**Required Clarification:**
- Should completing all workflow steps (intake, requests, inspection, test drive, report) automatically change status?
- Or should status remain independent and be updated via `updateSessionStatus` mutation?
- What status should a session have when job card is generated?

---

## 📋 Frontend Workarounds Currently Implemented

1. **Intake + Customer Requests:** When updating customer requests, frontend fetches current intakeNotes and includes it in the mutation
2. **Media Display:** Media section shows placeholder since we can't query session-specific media
3. **Separate Step Completion:** Frontend treats intake and customer requests as separate steps even though they share a mutation

---

## 🎯 Priority Recommendations

### High Priority
1. **Add `sessionId` to `uploadMedia` mutation** - Critical for associating media with sessions
2. **Define Media type structure** - Needed to display uploaded media

### Medium Priority
3. **Separate customer requests mutation** - Improves data integrity and simplifies frontend logic
4. **Clarify status workflow** - Ensures frontend and backend status changes are synchronized

### Low Priority
5. **Add media management mutations** - Delete media, update alt text, reorder media

---

## 🔄 Current Frontend Implementation

The frontend currently:
- ✅ Handles 5 workflow steps (intake, customer requests, inspection, test drive, initial report)
- ✅ Uses correct mutations for each step
- ✅ Validates all steps complete before allowing job card generation
- ✅ Displays session logs/timeline
- ⚠️ Has placeholder for media (waiting for backend support)
- ⚠️ Preserves intakeNotes when updating customerRequests (workaround)

---

## 📝 Testing Queries

### Test session update flow:
```graphql
# 1. Update intake
mutation {
  updateIntake(sessionId: "uuid-here", intakeNotes: "Vehicle received in good condition") {
    id
    intakeNotes
  }
}

# 2. Update customer requests
mutation {
  updateIntake(sessionId: "uuid-here", customerRequests: "Customer reports engine noise") {
    id
    customerRequests
  }
}

# 3. Update inspection
mutation {
  updateInspection(sessionId: "uuid-here", inspectionNotes: "Found loose belt") {
    id
    inspectionNotes
  }
}

# 4. Update test drive
mutation {
  updateTestDrive(sessionId: "uuid-here", testDriveNotes: "Confirmed noise during acceleration") {
    id
    testDriveNotes
  }
}

# 5. Generate report
mutation {
  generateInitialReport(sessionId: "uuid-here", report: "Requires belt replacement and tension adjustment") {
    id
    initialReport
  }
}

# 6. Create job card
mutation {
  createJobCardFromSession(sessionId: "uuid-here") {
    id
    status
  }
}
```

### Test media upload:
```graphql
# Current (no session link)
mutation {
  uploadMedia(file: $file, alt: "Engine photo") {
    id
    url
    alt
  }
}

# Desired (with session link)
mutation {
  uploadMedia(file: $file, sessionId: "uuid-here", alt: "Engine photo") {
    id
    url
    alt
    sessionId
  }
}
```
