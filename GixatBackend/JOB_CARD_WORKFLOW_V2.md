# Gixat Backend: Enhanced Job Card Workflow Documentation

## Summary of Improvements Implemented

✅ **Labor vs Parts Cost Tracking** - Split all costs into labor and parts  
✅ **Technician Assignment** - Assign technicians to job cards and individual items  
✅ **Customer Approval Workflow** - Approval required before starting work  
✅ **Auto-Generated Job Items** - Automatic parsing of requests into job items  
✅ **Media Upload** - Upload before/during/after photos to job cards and items  
✅ **Validation Rules** - Prevent completing cards with pending items, require costs, etc.  

---

## Complete Workflow

### 1. Session Phase

```graphql
# Create session
mutation { createSession(carId: "...", customerId: "...") { id status } }

# Add customer requests
mutation { updateCustomerRequests(sessionId: "...", customerRequests: "- Oil change\n- Check brakes") { id status } }

# Record inspection with mileage
mutation { updateInspection(sessionId: "...", mileage: 45000, inspectionRequests: "- Replace brake pads\n- Rotate tires") { id status } }

# Test drive
mutation { updateTestDrive(sessionId: "...", testDriveRequests: "- Wheel alignment needed") { id status } }

# Generate report (compiles everything)
mutation { generateInitialReport(sessionId: "...") { id initialReport } }
```

### 2. Job Card Phase

```graphql
# Create job card (auto-creates 3 job items from requests above)
mutation {
  createJobCardFromSession(sessionId: "...") {
    id
    items {
      id
      description  # "Oil change", "Replace brake pads", "Wheel alignment needed"
      status       # All start as PENDING
    }
  }
}

# Assign technician to entire job
mutation { assignTechnicianToJobCard(jobCardId: "...", technicianId: "tech123") { id } }

# OR assign different technicians per item
mutation { assignTechnicianToJobItem(itemId: "...", technicianId: "tech456") { id } }

# Add costs to items
mutation {
  addJobItem(
    jobCardId: "...",
    description: "Replace brake pads",
    estimatedLaborCost: 100.00,
    estimatedPartsCost: 150.00,
    assignedTechnicianId: "tech123"
  ) {
    id
    totalEstimatedLabor  # 100
    totalEstimatedParts  # 150
  }
}

# Customer approves entire job
mutation { approveJobCard(jobCardId: "...") { id isApprovedByCustomer } }

# Start work
mutation {
  updateJobItemStatus(
    itemId: "...",
    status: IN_PROGRESS,
    actualLaborCost: 0,
    actualPartsCost: 0,
    technicianNotes: "Starting work on brakes"
  ) { id status }
}

# Upload progress photo
mutation($file: Upload!) {
  uploadMediaToJobItem(
    itemId: "...",
    file: $file,
    type: DURING_WORK,
    alt: "Brake pad installation in progress"
  ) { media { url } }
}

# Complete item with actual costs
mutation {
  updateJobItemStatus(
    itemId: "...",
    status: COMPLETED,
    actualLaborCost: 95.00,
    actualPartsCost: 145.00,
    technicianNotes: "Replaced front brake pads successfully"
  ) {
    id
    actualLaborCost
    actualPartsCost
  }
}

# Complete entire job card (only works if all items are done)
mutation { updateJobCardStatus(jobCardId: "...", status: COMPLETED) { id } }
```

---

## New Query Examples

```graphql
# Get all jobs for a specific technician
query {
  jobCards(where: { assignedTechnicianId: { eq: "tech123" } }, first: 10) {
    edges {
      node {
        id
        status
        totalActualLabor
        totalActualParts
        items {
          description
          status
        }
      }
    }
  }
}

# Get unapproved job cards
query {
  jobCards(where: { isApprovedByCustomer: { eq: false } }) {
    edges { node { id customer { name } } }
  }
}

# Get jobs with date filtering
query {
  jobCards(
    where: { createdAt: { gte: "2025-01-01" } },
    order: { createdAt: DESC }
  ) {
    edges { node { id createdAt } }
  }
}
```

---

## Cost Structure

| Field | Description |
|-------|-------------|
| `estimatedLaborCost` | Estimated cost of labor for this item |
| `estimatedPartsCost` | Estimated cost of parts |
| `actualLaborCost` | Final labor cost after work completed |
| `actualPartsCost` | Final parts cost |
| `totalEstimatedLabor` | Sum of all item labor estimates (JobCard) |
| `totalEstimatedParts` | Sum of all item parts estimates (JobCard) |
| `totalActualLabor` | Sum of all item actual labor costs (JobCard) |
| `totalActualParts` | Sum of all item actual parts costs (JobCard) |

---

## Validation Rules

| Action | Validation |
|--------|-----------|
| Start work (`InProgress`) | Item must be approved |
| Complete item | Must provide actual costs > 0 |
| Complete job card | All items must be `COMPLETED` or `CANCELLED` |
| Upload media | File virus scanned automatically |

---

## Media Types

### JobCardMediaType / JobItemMediaType
- `BEFORE_WORK` (0) - Initial condition photos
- `DURING_WORK` (1) - Work in progress
- `AFTER_WORK` (2) - Completed work
- `DOCUMENTATION` (3) - Additional documentation
