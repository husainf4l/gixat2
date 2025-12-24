# JobCard System - Frontend Implementation Report

## System Overview

The JobCard module is a comprehensive job management system for a garage/automotive service business. It tracks work orders from creation through completion, with support for multi-item jobs, technician assignments, customer approvals, and media documentation.

---

## Core Data Models

### 1. **JobCard** (Main Entity)
The parent work order containing:

**Identifiers & Relationships:**
- `id`: Guid - Unique identifier
- `sessionId`: Guid (optional) - Links to inspection session
- `carId`: Guid - Vehicle being serviced
- `customerId`: Guid - Vehicle owner
- `organizationId`: Guid - Service provider
- `assignedTechnicianId`: string (optional) - Lead technician

**Status & Workflow:**
- `status`: Enum (Pending, InProgress, Completed, Cancelled)
- `isApprovedByCustomer`: boolean
- `approvedAt`: DateTime (nullable)

**Financial Tracking (all decimals):**
- `totalEstimatedCost` / `totalActualCost`
- `totalEstimatedLabor` / `totalActualLabor`
- `totalEstimatedParts` / `totalActualParts`

**Additional:**
- `internalNotes`: string (max 5000 chars) - Staff-only notes
- `items`: Array of JobItem
- `media`: Array of JobCardMedia
- `createdAt` / `updatedAt`: DateTime

---

### 2. **JobItem** (Individual Tasks)
Granular work items within a JobCard:

**Core Fields:**
- `id`: Guid
- `jobCardId`: Guid - Parent reference
- `description`: string (required, max 500 chars)
- `status`: Enum (Pending, InProgress, Completed, Cancelled)
- `assignedTechnicianId`: string (optional)

**Costs (decimals):**
- `estimatedLaborCost` / `actualLaborCost`
- `estimatedPartsCost` / `actualPartsCost`
- `estimatedCost` (computed): labor + parts estimate
- `actualCost` (computed): labor + parts actual

**Customer Interaction:**
- `isApprovedByCustomer`: boolean
- `approvedAt`: DateTime (nullable)
- `technicianNotes`: string (max 2000 chars)

**Media:**
- `media`: Array of JobItemMedia

---

### 3. **Media Types**
Both JobCard and JobItem support media with types:
- `BeforeWork` (0)
- `DuringWork` (1)
- `AfterWork` (2)
- `Documentation` (3)

Each media object includes:
- `url`: string - S3 file location
- `alt`: string (optional) - Accessibility text
- `type`: MediaType (Image/Video)

---

## GraphQL API Operations

### Queries

#### 1. **getJobCards**
```graphql
query GetJobCards($first: Int, $after: String) {
  jobCards(first: $first, after: $after) {
    pageInfo {
      hasNextPage
      endCursor
    }
    nodes {
      id
      status
      car { make model licensePlate }
      customer { firstName lastName }
      totalEstimatedCost
      totalActualCost
      isApprovedByCustomer
      createdAt
      assignedTechnician { fullName }
    }
  }
}
```
**Features:** Pagination, filtering, sorting supported

---

#### 2. **searchJobCards**
```graphql
query SearchJobCards($query: String, $status: JobCardStatus) {
  searchJobCards(query: $query, status: $status) {
    nodes {
      id
      status
      car { make model licensePlate }
      customer { firstName lastName }
    }
  }
}
```
**Searchable Fields:** Customer name, car make/model, license plate, job card ID

---

#### 3. **getJobCardById**
```graphql
query GetJobCardById($id: UUID!) {
  jobCardById(id: $id) {
    id
    status
    internalNotes
    car { id make model year licensePlate }
    customer { id firstName lastName email phone }
    session { id mileage }
    assignedTechnician { id fullName }
    items {
      id
      description
      status
      estimatedLaborCost
      estimatedPartsCost
      actualLaborCost
      actualPartsCost
      isApprovedByCustomer
      technicianNotes
      assignedTechnician { fullName }
      media {
        media { url alt type }
        type
      }
    }
    media {
      media { url alt type }
      type
    }
    totalEstimatedCost
    totalActualCost
    totalEstimatedLabor
    totalActualLabor
    totalEstimatedParts
    totalActualParts
    isApprovedByCustomer
    approvedAt
    createdAt
    updatedAt
  }
}
```

---

#### 4. **getJobCardsByCustomer**
```graphql
query GetJobCardsByCustomer($customerId: UUID!) {
  jobCardsByCustomer(customerId: $customerId) {
    nodes {
      id
      status
      car { make model }
      totalEstimatedCost
      createdAt
    }
  }
}
```

---

#### 5. **getJobCardsByStatus**
```graphql
query GetJobCardsByStatus($status: JobCardStatus!) {
  jobCardsByStatus(status: $status) {
    nodes {
      id
      car { make model licensePlate }
      customer { firstName lastName }
      createdAt
    }
  }
}
```

---

### Mutations

#### 1. **createJobCardFromSession**
```graphql
mutation CreateJobCard($sessionId: UUID!) {
  createJobCardFromSession(sessionId: $sessionId) {
    id
    status
    items { id description }
  }
}
```
**Business Logic:**
- Session must have status `ReportGenerated`
- Auto-creates JobItems from session requests (customer/inspection/test drive)
- Populates internal notes with session data
- Changes session status to `JobCardCreated`

---

#### 2. **addJobItem**
```graphql
mutation AddJobItem($input: AddJobItemInput!) {
  addJobItem(
    jobCardId: $input.jobCardId
    description: $input.description
    estimatedLaborCost: $input.estimatedLaborCost
    estimatedPartsCost: $input.estimatedPartsCost
    assignedTechnicianId: $input.assignedTechnicianId
  ) {
    id
    totalEstimatedCost
    items { id description }
  }
}
```
**Auto-calculations:** Updates JobCard totals

---

#### 3. **updateJobItemStatus**
```graphql
mutation UpdateJobItemStatus($input: UpdateJobItemStatusInput!) {
  updateJobItemStatus(
    itemId: $input.itemId
    status: $input.status
    actualLaborCost: $input.actualLaborCost
    actualPartsCost: $input.actualPartsCost
    technicianNotes: $input.technicianNotes
  ) {
    id
    status
    actualCost
    jobCard { totalActualCost }
  }
}
```
**Validations:**
- Cannot mark `Completed` without actual costs
- Cannot start (`InProgress`) if not approved by customer
- Recalculates JobCard totals

---

#### 4. **updateJobCardStatus**
```graphql
mutation UpdateJobCardStatus($jobCardId: UUID!, $status: JobCardStatus!) {
  updateJobCardStatus(jobCardId: $jobCardId, status: $status) {
    id
    status
  }
}
```
**Validation:** Cannot complete if any items are Pending/InProgress

---

#### 5. **assignTechnicianToJobCard**
```graphql
mutation AssignTechnician($jobCardId: UUID!, $technicianId: String!) {
  assignTechnicianToJobCard(jobCardId: $jobCardId, technicianId: $technicianId) {
    id
    assignedTechnician { id fullName }
  }
}
```

---

#### 6. **assignTechnicianToJobItem**
```graphql
mutation AssignTechnicianToItem($itemId: UUID!, $technicianId: String!) {
  assignTechnicianToJobItem(itemId: $itemId, technicianId: $technicianId) {
    id
    assignedTechnician { fullName }
  }
}
```

---

#### 7. **approveJobCard**
```graphql
mutation ApproveJobCard($jobCardId: UUID!) {
  approveJobCard(jobCardId: $jobCardId) {
    id
    isApprovedByCustomer
    approvedAt
    items { isApprovedByCustomer }
  }
}
```
**Auto-behavior:** Approves all items within the JobCard

---

#### 8. **approveJobItem**
```graphql
mutation ApproveJobItem($itemId: UUID!) {
  approveJobItem(itemId: $itemId) {
    id
    isApprovedByCustomer
    approvedAt
  }
}
```

---

#### 9. **uploadMediaToJobCard**
```graphql
mutation UploadJobCardMedia($input: UploadJobCardMediaInput!) {
  uploadMediaToJobCard(
    jobCardId: $input.jobCardId
    file: $input.file
    type: $input.type
    alt: $input.alt
  ) {
    media { url alt }
    type
  }
}
```
**File Processing:**
- Validates file type/size
- Scans for viruses
- Uploads to S3
- Supports images and videos

---

#### 10. **uploadMediaToJobItem**
```graphql
mutation UploadJobItemMedia($input: UploadJobItemMediaInput!) {
  uploadMediaToJobItem(
    itemId: $input.itemId
    file: $input.file
    type: $input.type
    alt: $input.alt
  ) {
    media { url alt }
    type
  }
}
```

---

## Frontend Implementation Guide

### 1. **Type Definitions** (TypeScript)

```typescript
// Enums
export enum JobCardStatus {
  Pending = 'PENDING',
  InProgress = 'IN_PROGRESS',
  Completed = 'COMPLETED',
  Cancelled = 'CANCELLED'
}

export enum JobItemStatus {
  Pending = 'PENDING',
  InProgress = 'IN_PROGRESS',
  Completed = 'COMPLETED',
  Cancelled = 'CANCELLED'
}

export enum JobCardMediaType {
  BeforeWork = 'BEFORE_WORK',
  DuringWork = 'DURING_WORK',
  AfterWork = 'AFTER_WORK',
  Documentation = 'DOCUMENTATION'
}

// Models
export interface JobCard {
  id: string;
  sessionId?: string;
  carId: string;
  car?: Car;
  customerId: string;
  customer?: Customer;
  organizationId: string;
  assignedTechnicianId?: string;
  assignedTechnician?: ApplicationUser;
  status: JobCardStatus;
  internalNotes?: string;
  items: JobItem[];
  media: JobCardMedia[];
  totalEstimatedCost: number;
  totalActualCost: number;
  totalEstimatedLabor: number;
  totalActualLabor: number;
  totalEstimatedParts: number;
  totalActualParts: number;
  isApprovedByCustomer: boolean;
  approvedAt?: string;
  createdAt: string;
  updatedAt: string;
}

export interface JobItem {
  id: string;
  jobCardId: string;
  assignedTechnicianId?: string;
  assignedTechnician?: ApplicationUser;
  description: string;
  status: JobItemStatus;
  estimatedLaborCost: number;
  estimatedPartsCost: number;
  actualLaborCost: number;
  actualPartsCost: number;
  estimatedCost: number; // computed
  actualCost: number; // computed
  isApprovedByCustomer: boolean;
  approvedAt?: string;
  technicianNotes?: string;
  media: JobItemMedia[];
  createdAt: string;
  updatedAt: string;
}

export interface JobCardMedia {
  jobCardId: string;
  media: AppMedia;
  type: JobCardMediaType;
  createdAt: string;
}

export interface JobItemMedia {
  jobItemId: string;
  media: AppMedia;
  type: JobCardMediaType;
  createdAt: string;
}
```

---

### 2. **React Component Structure**

```
/pages
  /job-cards
    index.tsx           # List view with search/filter
    [id].tsx            # Detail view
    create.tsx          # Create from session

/components
  /job-cards
    JobCardList.tsx     # Table/grid of job cards
    JobCardDetail.tsx   # Full job card display
    JobCardStatusBadge.tsx
    JobItemsList.tsx    # Items within a job card
    JobItemCard.tsx     # Single item display
    JobItemForm.tsx     # Add/edit item
    CostSummary.tsx     # Financial breakdown
    ApprovalSection.tsx # Customer approval UI
    TechnicianAssignment.tsx
    MediaGallery.tsx    # Before/during/after photos
    MediaUpload.tsx     # File upload component
    JobCardFilters.tsx  # Status/search filters
```

---

### 3. **Key UI/UX Flows**

#### **Flow A: Create Job Card from Session**
1. User selects a session with status `ReportGenerated`
2. Click "Create Job Card"
3. System auto-creates JobCard with items from session requests
4. Navigate to job card detail view
5. Review auto-generated items
6. Add cost estimates to each item
7. Send for customer approval

#### **Flow B: Manage Job Card Lifecycle**
1. **Pending Stage:**
   - Add/remove job items
   - Assign lead technician
   - Add cost estimates
   - Upload "Before Work" photos
   - Send to customer for approval

2. **Customer Approval:**
   - Display approval page (can be separate portal)
   - Show all items with estimates
   - Customer approves entire job or individual items
   - System records approval timestamp

3. **In Progress Stage:**
   - Assign technicians to specific items
   - Update item status (Pending → InProgress → Completed)
   - Add actual costs as work completes
   - Upload "During Work" photos
   - Add technician notes

4. **Completion:**
   - All items marked Completed
   - Upload "After Work" photos
   - Add final documentation
   - Update job card status to Completed
   - Generate invoice (if integrated)

---

### 4. **Critical Business Rules to Implement**

```typescript
// Validation Functions

function canStartJobItem(item: JobItem): boolean {
  return item.isApprovedByCustomer;
}

function canCompleteJobItem(item: JobItem): boolean {
  return item.actualLaborCost > 0 || item.actualPartsCost > 0;
}

function canCompleteJobCard(jobCard: JobCard): boolean {
  return jobCard.items.every(
    item => item.status === JobItemStatus.Completed ||
            item.status === JobItemStatus.Cancelled
  );
}

function calculateJobCardTotals(items: JobItem[]): {
  totalEstimatedCost: number;
  totalActualCost: number;
  totalEstimatedLabor: number;
  totalActualLabor: number;
  totalEstimatedParts: number;
  totalActualParts: number;
} {
  return items.reduce((acc, item) => ({
    totalEstimatedCost: acc.totalEstimatedCost + item.estimatedCost,
    totalActualCost: acc.totalActualCost + item.actualCost,
    totalEstimatedLabor: acc.totalEstimatedLabor + item.estimatedLaborCost,
    totalActualLabor: acc.totalActualLabor + item.actualLaborCost,
    totalEstimatedParts: acc.totalEstimatedParts + item.estimatedPartsCost,
    totalActualParts: acc.totalActualParts + item.actualPartsCost,
  }), {
    totalEstimatedCost: 0,
    totalActualCost: 0,
    totalEstimatedLabor: 0,
    totalActualLabor: 0,
    totalEstimatedParts: 0,
    totalActualParts: 0,
  });
}
```

---

### 5. **State Management Recommendations**

#### **Using React Query/TanStack Query:**

```typescript
// Queries
const useJobCards = (filters?: { status?: JobCardStatus; query?: string }) => {
  return useQuery({
    queryKey: ['jobCards', filters],
    queryFn: () => fetchJobCards(filters),
  });
};

const useJobCard = (id: string) => {
  return useQuery({
    queryKey: ['jobCard', id],
    queryFn: () => fetchJobCardById(id),
  });
};

// Mutations
const useCreateJobCard = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (sessionId: string) => createJobCardFromSession(sessionId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['jobCards'] });
    },
  });
};

const useUpdateJobItemStatus = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: UpdateJobItemStatusInput) => updateJobItemStatus(input),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['jobCard', variables.jobCardId] });
    },
  });
};

const useApproveJobCard = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (jobCardId: string) => approveJobCard(jobCardId),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['jobCard', data.id] });
    },
  });
};
```

---

### 6. **UI Components Examples**

#### **Status Badge Component:**
```tsx
export const JobCardStatusBadge: React.FC<{ status: JobCardStatus }> = ({ status }) => {
  const config = {
    [JobCardStatus.Pending]: { color: 'yellow', label: 'Pending' },
    [JobCardStatus.InProgress]: { color: 'blue', label: 'In Progress' },
    [JobCardStatus.Completed]: { color: 'green', label: 'Completed' },
    [JobCardStatus.Cancelled]: { color: 'red', label: 'Cancelled' },
  };

  return (
    <Badge colorScheme={config[status].color}>
      {config[status].label}
    </Badge>
  );
};
```

#### **Cost Summary Component:**
```tsx
export const CostSummary: React.FC<{ jobCard: JobCard }> = ({ jobCard }) => {
  const variance = jobCard.totalActualCost - jobCard.totalEstimatedCost;

  return (
    <Box>
      <Heading size="md">Cost Summary</Heading>
      <SimpleGrid columns={3} spacing={4}>
        <Stat>
          <StatLabel>Estimated Total</StatLabel>
          <StatNumber>${jobCard.totalEstimatedCost.toFixed(2)}</StatNumber>
          <StatHelpText>
            Labor: ${jobCard.totalEstimatedLabor.toFixed(2)} |
            Parts: ${jobCard.totalEstimatedParts.toFixed(2)}
          </StatHelpText>
        </Stat>

        <Stat>
          <StatLabel>Actual Total</StatLabel>
          <StatNumber>${jobCard.totalActualCost.toFixed(2)}</StatNumber>
          <StatHelpText>
            Labor: ${jobCard.totalActualLabor.toFixed(2)} |
            Parts: ${jobCard.totalActualParts.toFixed(2)}
          </StatHelpText>
        </Stat>

        <Stat>
          <StatLabel>Variance</StatLabel>
          <StatNumber color={variance > 0 ? 'red.500' : 'green.500'}>
            {variance > 0 ? '+' : ''}{variance.toFixed(2)}
          </StatNumber>
          <StatHelpText>
            {Math.abs((variance / jobCard.totalEstimatedCost) * 100).toFixed(1)}%
          </StatHelpText>
        </Stat>
      </SimpleGrid>
    </Box>
  );
};
```

---

### 7. **Media Upload Implementation**

```typescript
const useUploadJobCardMedia = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (input: {
      jobCardId: string;
      file: File;
      type: JobCardMediaType;
      alt?: string;
    }) => {
      // GraphQL file upload using apollo-upload-client or similar
      return uploadMediaToJobCard(input);
    },
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['jobCard', variables.jobCardId] });
    },
  });
};

// Component usage
const MediaUploadButton: React.FC<{ jobCardId: string; type: JobCardMediaType }> = ({
  jobCardId,
  type,
}) => {
  const uploadMutation = useUploadJobCardMedia();
  const fileInputRef = useRef<HTMLInputElement>(null);

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    await uploadMutation.mutateAsync({
      jobCardId,
      file,
      type,
    });
  };

  return (
    <>
      <input
        ref={fileInputRef}
        type="file"
        accept="image/*,video/*"
        style={{ display: 'none' }}
        onChange={handleFileChange}
      />
      <Button onClick={() => fileInputRef.current?.click()}>
        Upload {type} Photo
      </Button>
    </>
  );
};
```

---

### 8. **Real-time Updates Considerations**

For multi-user environments (technicians updating job items), consider:

- **GraphQL Subscriptions** for real-time job card updates
- **Polling** with React Query's `refetchInterval`
- **Optimistic Updates** for better UX

```typescript
const useJobCardSubscription = (jobCardId: string) => {
  const queryClient = useQueryClient();

  useSubscription({
    subscription: JOB_CARD_UPDATED_SUBSCRIPTION,
    variables: { jobCardId },
    onData: ({ data }) => {
      queryClient.setQueryData(['jobCard', jobCardId], data.jobCardUpdated);
    },
  });
};
```

---

### 9. **Permission Considerations**

Based on the `[Authorize]` attributes, implement role-based access:

- **Admin/Manager:** Full access to all operations
- **Technician:** Can update assigned items, add notes, upload media
- **Customer:** View-only + approve job cards/items
- **Receptionist:** Create job cards, assign technicians

---

### 10. **Recommended Features to Add**

1. **Notifications:**
   - Email customer when job card needs approval
   - Notify technician when assigned
   - Alert when job card completed

2. **Timeline View:**
   - Show status change history
   - Track who did what and when

3. **Reports:**
   - Estimated vs. actual cost analysis
   - Technician performance
   - Average job completion time

4. **Mobile App:**
   - Technicians can update job items from garage floor
   - Photo upload directly from phone camera

5. **Customer Portal:**
   - View job card status
   - Approve estimates
   - View before/after photos

---

## Summary

Your JobCard system is well-structured with clear separation of concerns, proper validation, and comprehensive features. The frontend should focus on:

1. **Clear workflow visualization** (Kanban board for job cards)
2. **Easy cost management** (inline editing, real-time totals)
3. **Mobile-first design** for technicians
4. **Customer-friendly approval flow**
5. **Rich media support** (photo galleries, video playback)
6. **Real-time updates** for multi-user scenarios

The GraphQL API provides all necessary operations with proper authorization. Consider implementing optimistic updates and caching strategies for the best user experience.

---

## Backend Code Reference

- **Models:** [Modules/JobCards/Models/](Modules/JobCards/Models/)
- **GraphQL Queries:** [Modules/JobCards/GraphQL/JobCardQueries.cs](Modules/JobCards/GraphQL/JobCardQueries.cs)
- **GraphQL Mutations:** [Modules/JobCards/GraphQL/JobCardMutations.cs](Modules/JobCards/GraphQL/JobCardMutations.cs)
- **Services:** [Modules/JobCards/Services/JobCardService.cs](Modules/JobCards/Services/JobCardService.cs)
- **Extensions:** [Modules/JobCards/GraphQL/JobCardExtensions.cs](Modules/JobCards/GraphQL/JobCardExtensions.cs)

---

## Team Collaboration & Chat System

### NEW FEATURE: JobCard Team Chat

The JobCard system now includes a **real-time team collaboration feature** - an open chat/discussion system for each JobCard where team members can communicate, mention colleagues, and have threaded conversations.

### 4. **JobCardComment** (Chat/Discussion Messages)

**Core Fields:**
- `id`: Guid - Unique identifier
- `jobCardId`: Guid (required) - Parent JobCard
- `jobItemId`: Guid (optional) - Link comment to specific item
- `authorId`: string (required) - User who wrote the comment
- `content`: string (required, max 5000 chars) - Message content
- `parentCommentId`: Guid (optional) - For threaded replies

**Threading & Mentions:**
- `replies`: Array of JobCardComment - Threaded conversations
- `mentions`: Array of JobCardCommentMention - @mentioned users

**Metadata:**
- `isEdited`: boolean - Track if comment was edited
- `editedAt`: DateTime (nullable) - When it was edited
- `isDeleted`: boolean - Soft delete
- `deletedAt`: DateTime (nullable)
- `createdAt` / `updatedAt`: DateTime

### 5. **JobCardCommentMention** (@Mentions)

**Core Fields:**
- `id`: Guid
- `commentId`: Guid - Parent comment
- `mentionedUserId`: string (required) - User who was mentioned
- `isRead`: boolean - Track if user has seen the mention
- `readAt`: DateTime (nullable) - When mention was read
- `createdAt`: DateTime

---

## Chat/Collaboration GraphQL API

### Chat Queries

#### 1. **getJobCardComments** (Get all chat messages)
```graphql
query GetJobCardComments($jobCardId: UUID!, $first: Int) {
  jobCardComments(jobCardId: $jobCardId, first: $first) {
    pageInfo {
      hasNextPage
      endCursor
    }
    nodes {
      id
      content
      author {
        id
        fullName
        avatar
      }
      jobItemId  # If comment is about specific item
      parentCommentId  # If this is a reply
      replies {
        id
        content
        author { fullName }
        createdAt
      }
      mentions {
        mentionedUser { id fullName }
        isRead
      }
      isEdited
      editedAt
      createdAt
    }
  }
}
```

---

#### 2. **getJobItemComments** (Comments for specific item)
```graphql
query GetJobItemComments($jobItemId: UUID!) {
  jobItemComments(jobItemId: $jobItemId) {
    nodes {
      id
      content
      author { fullName }
      createdAt
    }
  }
}
```

---

#### 3. **getMyUnreadMentions** (Notifications)
```graphql
query GetMyUnreadMentions {
  myUnreadMentions {
    nodes {
      id
      comment {
        id
        content
        author { fullName avatar }
        jobCard {
          id
          car { make model licensePlate }
        }
        jobItem {
          id
          description
        }
        createdAt
      }
      isRead
      createdAt
    }
  }
}
```

---

#### 4. **getUnreadMentionCount** (Badge count)
```graphql
query GetUnreadMentionCount {
  unreadMentionCount
}
```

---

#### 5. **getRecentJobCardActivity** (Activity feed)
```graphql
query GetRecentJobCardActivity($organizationId: UUID!, $first: Int) {
  recentJobCardActivity(organizationId: $organizationId, first: $first) {
    nodes {
      id
      content
      author { fullName avatar }
      jobCard {
        id
        car { make model }
        customer { firstName lastName }
      }
      createdAt
    }
  }
}
```

---

### Chat Mutations

#### 1. **addJobCardComment** (Send message)
```graphql
mutation AddJobCardComment($input: AddJobCardCommentInput!) {
  addJobCardComment(
    jobCardId: $input.jobCardId
    content: $input.content
    jobItemId: $input.jobItemId  # optional, for item-specific comments
    parentCommentId: $input.parentCommentId  # optional, for replies
  ) {
    id
    content
    author { fullName }
    mentions {
      mentionedUser { id fullName }
    }
    createdAt
  }
}
```

**Features:**
- Auto-extracts @mentions from content (e.g., "@john please check this")
- Validates mentioned users exist in organization
- Creates mention records for notifications
- Supports threaded replies

**Example Input:**
```javascript
{
  jobCardId: "abc-123",
  content: "@john Can you take a look at the brake pads? They look worn out.",
  jobItemId: "item-456"  // Optional: links comment to specific job item
}
```

---

#### 2. **editJobCardComment** (Edit own message)
```graphql
mutation EditJobCardComment($commentId: UUID!, $content: String!) {
  editJobCardComment(commentId: $commentId, content: $content) {
    id
    content
    isEdited
    editedAt
  }
}
```

**Validation:**
- Only author can edit their own comments
- Updates mention records if @mentions changed
- Marks comment as edited with timestamp

---

#### 3. **deleteJobCardComment** (Soft delete)
```graphql
mutation DeleteJobCardComment($commentId: UUID!) {
  deleteJobCardComment(commentId: $commentId) {
    id
    isDeleted
    deletedAt
  }
}
```

**Validation:**
- Only author can delete their own comments
- Soft delete (not removed from database)
- Hidden from queries but preserved for audit trail

---

#### 4. **markMentionsAsRead** (Clear notifications)
```graphql
mutation MarkMentionsAsRead($mentionIds: [UUID!]!) {
  markMentionsAsRead(mentionIds: $mentionIds)
}
```

**Usage:**
- Mark mentions as read when user views them
- Updates notification badge count

---

## Frontend Implementation - Chat Feature

### Updated TypeScript Types

```typescript
// Add to existing types

export interface JobCard {
  // ... existing fields ...
  comments: JobCardComment[];  // NEW
}

export interface JobItem {
  // ... existing fields ...
  comments: JobCardComment[];  // NEW
}

export interface JobCardComment {
  id: string;
  jobCardId: string;
  jobItemId?: string;
  authorId: string;
  author: ApplicationUser;
  content: string;
  parentCommentId?: string;
  parentComment?: JobCardComment;
  replies: JobCardComment[];
  mentions: JobCardCommentMention[];
  isEdited: boolean;
  editedAt?: string;
  isDeleted: boolean;
  deletedAt?: string;
  createdAt: string;
  updatedAt: string;
}

export interface JobCardCommentMention {
  id: string;
  commentId: string;
  comment: JobCardComment;
  mentionedUserId: string;
  mentionedUser: ApplicationUser;
  isRead: boolean;
  readAt?: string;
  createdAt: string;
}
```

---

### Updated Component Structure

```
/components
  /job-cards
    // ... existing components ...
    
    // NEW: Chat components
    /chat
      ChatPanel.tsx            # Main chat container
      CommentThread.tsx        # Display threaded comments
      CommentItem.tsx          # Single comment message
      CommentForm.tsx          # Input for new comments/replies
      MentionInput.tsx         # @mention autocomplete
      UnreadMentionsBadge.tsx  # Notification indicator
      ActivityFeed.tsx         # Organization-wide activity
      CommentActions.tsx       # Edit/Delete actions
```

---

### React Query Hooks

```typescript
// Chat queries
const useJobCardComments = (jobCardId: string) => {
  return useQuery({
    queryKey: ['jobCardComments', jobCardId],
    queryFn: () => fetchJobCardComments(jobCardId),
    refetchInterval: 5000, // Poll every 5 seconds for new messages
  });
};

const useUnreadMentions = () => {
  return useQuery({
    queryKey: ['unreadMentions'],
    queryFn: () => fetchMyUnreadMentions(),
    refetchInterval: 10000, // Poll for new mentions
  });
};

const useUnreadMentionCount = () => {
  return useQuery({
    queryKey: ['unreadMentionCount'],
    queryFn: () => fetchUnreadMentionCount(),
    refetchInterval: 10000,
  });
};

// Chat mutations
const useAddComment = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: AddCommentInput) => addJobCardComment(input),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['jobCardComments', variables.jobCardId] });
      queryClient.invalidateQueries({ queryKey: ['jobCard', variables.jobCardId] });
    },
  });
};

const useEditComment = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: { commentId: string; content: string }) => editJobCardComment(input),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['jobCardComments'] });
    },
  });
};

const useDeleteComment = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (commentId: string) => deleteJobCardComment(commentId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['jobCardComments'] });
    },
  });
};

const useMarkMentionsRead = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (mentionIds: string[]) => markMentionsAsRead(mentionIds),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['unreadMentions'] });
      queryClient.invalidateQueries({ queryKey: ['unreadMentionCount'] });
    },
  });
};
```

---

### Chat Panel Component Example

```tsx
export const ChatPanel: React.FC<{ jobCardId: string }> = ({ jobCardId }) => {
  const { data: comments, isLoading } = useJobCardComments(jobCardId);
  const addCommentMutation = useAddComment();
  const [replyTo, setReplyTo] = useState<string | null>(null);

  const handleSendMessage = async (content: string) => {
    await addCommentMutation.mutateAsync({
      jobCardId,
      content,
      parentCommentId: replyTo ?? undefined,
    });
    setReplyTo(null);
  };

  if (isLoading) return <Spinner />;

  return (
    <Box>
      <Heading size="md">Team Chat</Heading>
      
      <VStack align="stretch" spacing={4} mb={4}>
        {comments?.nodes.map(comment => (
          <CommentItem
            key={comment.id}
            comment={comment}
            onReply={() => setReplyTo(comment.id)}
          />
        ))}
      </VStack>

      <CommentForm
        onSubmit={handleSendMessage}
        replyTo={replyTo}
        onCancelReply={() => setReplyTo(null)}
      />
    </Box>
  );
};
```

---

### Mention Input Component with Autocomplete

```tsx
export const MentionInput: React.FC<{
  value: string;
  onChange: (value: string) => void;
  onSubmit: () => void;
}> = ({ value, onChange, onSubmit }) => {
  const [mentionSearch, setMentionSearch] = useState<string | null>(null);
  const { data: users } = useOrganizationUsers();

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === '@') {
      setMentionSearch('');
    }
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      onSubmit();
    }
  };

  const handleUserSelect = (username: string) => {
    const lastAtIndex = value.lastIndexOf('@');
    const newValue = value.substring(0, lastAtIndex + 1) + username + ' ';
    onChange(newValue);
    setMentionSearch(null);
  };

  const filteredUsers = mentionSearch !== null
    ? users?.filter(u => u.userName?.toLowerCase().includes(mentionSearch.toLowerCase()))
    : [];

  return (
    <Box position="relative">
      <Textarea
        value={value}
        onChange={(e) => {
          onChange(e.target.value);
          const lastWord = e.target.value.split(/\s/).pop();
          if (lastWord?.startsWith('@')) {
            setMentionSearch(lastWord.substring(1));
          } else {
            setMentionSearch(null);
          }
        }}
        onKeyDown={handleKeyDown}
        placeholder="Type a message... Use @ to mention someone"
      />
      
      {mentionSearch !== null && filteredUsers && filteredUsers.length > 0 && (
        <Box position="absolute" bottom="100%" bg="white" shadow="md" borderRadius="md">
          {filteredUsers.map(user => (
            <Button
              key={user.id}
              variant="ghost"
              onClick={() => handleUserSelect(user.userName!)}
            >
              @{user.userName}
            </Button>
          ))}
        </Box>
      )}
    </Box>
  );
};
```

---

### Unread Mentions Badge

```tsx
export const UnreadMentionsBadge: React.FC = () => {
  const { data: count } = useUnreadMentionCount();

  if (!count || count === 0) return null;

  return (
    <Badge colorScheme="red" borderRadius="full">
      {count > 99 ? '99+' : count}
    </Badge>
  );
};

// Usage in header
<IconButton icon={<BellIcon />} aria-label="Notifications">
  <UnreadMentionsBadge />
</IconButton>
```

---

## Chat Feature Benefits

### 1. **Real-Time Team Collaboration**
- Technicians can discuss issues directly on the JobCard
- No need for external chat apps (Slack, Teams, etc.)
- All communication is contextual and tied to the work

### 2. **@Mentions & Notifications**
- Tag specific team members for urgent issues
- Automatic notification system
- Read/unread tracking

### 3. **Threaded Conversations**
- Reply to specific comments
- Keep discussions organized
- Easy to follow complex conversations

### 4. **Audit Trail**
- Soft delete preserves history
- Edit tracking shows what changed
- Complete record of all decisions

### 5. **Context-Aware**
- Comments can be linked to specific JobItems
- See exactly what part of the job is being discussed
- Reduces confusion and miscommunication

### 6. **Activity Feed**
- See organization-wide updates
- Monitor team activity
- Stay informed on all projects

---

## Recommended UI/UX Patterns

### 1. **Inline Chat on JobCard Detail Page**
```
+------------------------------------------+
| JobCard #1234                     [Chat] |
|------------------------------------------|
| Items | Media | Costs | **CHAT**         |
|                                          |
| [Chat messages appear here]              |
| @john: Brake pads look worn              |
| @sarah: Confirmed, ordering new ones     |
|                                          |
| [Type a message... @mention]             |
+------------------------------------------+
```

### 2. **Floating Chat Widget**
- Collapsible chat panel
- Appears on right side of screen
- Can minimize/maximize
- Shows unread count when minimized

### 3. **Mobile Chat View**
- Full-screen chat interface
- Swipe to reply
- Quick @mention from team list
- Push notifications for mentions

### 4. **Desktop Notifications**
- Browser notifications for new mentions
- Sound alerts (optional)
- Desktop popup with message preview

---

## Security & Permissions

**Current Implementation:**
- All operations require `[Authorize]` attribute
- Multi-tenancy enforced via global query filters
- Users can only see comments from their organization
- Only comment author can edit/delete their own comments

**Recommended Additions:**
- Role-based comment visibility (admin-only comments)
- Customer-facing vs internal-only comments
- Comment moderation for sensitive information
- Export chat history for compliance

---

## Summary of Chat System

The JobCard chat/collaboration system transforms JobCards into **living, collaborative workspaces** where teams can:

1. **Communicate in real-time** without leaving the platform
2. **@Mention teammates** for instant notifications
3. **Thread discussions** for organized conversations
4. **Link comments to specific work items** for context
5. **Track all edits and deletions** for accountability
6. **See organization-wide activity** for transparency

This feature significantly enhances team coordination, reduces miscommunication, and keeps all job-related discussions in one place alongside the work itself.

---

