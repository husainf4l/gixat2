# 🎨 Frontend Implementation Report - Estimate Approval Workflow

**System:** Gixat Garage Management  
**Date:** December 24, 2025  
**Target:** React/Next.js + GraphQL Client

---

## 📋 Executive Summary

The estimate approval workflow is a **critical customer-facing feature** that allows customers to review and approve repair estimates before work begins. This feature bridges the gap between trust and transparency, which is essential in the automotive repair industry.

### Business Value:
- ✅ **Increases customer trust** - Transparency in pricing
- ✅ **Reduces disputes** - Written approval on file
- ✅ **Improves cash flow** - Pre-authorization before work
- ✅ **Legal protection** - Documented consent
- ✅ **Better UX** - Modern digital approval vs phone/paper

---

## 🏗️ Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                     FRONTEND ARCHITECTURE                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌────────────────┐  ┌────────────────┐  ┌──────────────────┐ │
│  │  Public Link   │  │  Customer      │  │   Technician     │ │
│  │  (No Auth)     │  │  Portal        │  │   Dashboard      │ │
│  │                │  │  (Auth)        │  │   (Auth)         │ │
│  └───────┬────────┘  └───────┬────────┘  └────────┬─────────┘ │
│          │                    │                     │            │
│          └────────────────────┴─────────────────────┘            │
│                               │                                   │
│                    ┌──────────▼─────────┐                        │
│                    │   React Query      │                        │
│                    │   State Management │                        │
│                    └──────────┬─────────┘                        │
└────────────────────────────────┼───────────────────────────────┘
                                 │
                       GraphQL API (HTTP)
                                 │
┌────────────────────────────────▼───────────────────────────────┐
│                      BACKEND (.NET 10 API)                      │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌──────────────────┐  ┌──────────────────┐  ┌───────────────┐│
│  │   Queries        │  │   Mutations      │  │   Extensions  ││
│  │                  │  │                  │  │               ││
│  │ • getJobCard     │  │ • approveJobCard │  │ • DataLoaders ││
│  │ • getJobItems    │  │ • approveJobItem │  │               ││
│  │ • getEstimate    │  │ • rejectEstimate │  │               ││
│  └──────────────────┘  └──────────────────┘  └───────────────┘│
└─────────────────────────────────────────────────────────────────┘
```

---

## 🎯 User Stories & Workflows

### 1. Customer Receives Estimate (Primary Flow)

**Story:** *As a customer, I want to receive a shareable estimate link so I can review and approve repairs online.*

**Flow:**
```
1. Technician completes inspection → Creates JobCard with items
2. System generates unique shareable link
3. Customer receives link via SMS/Email
4. Customer opens link (no auth required)
5. Customer reviews estimate breakdown
6. Customer approves/rejects items or full estimate
7. System notifies technician
8. Work begins on approved items only
```

---

## 💻 Frontend Components to Build

### Component Hierarchy

```
src/
├── features/
│   └── estimates/
│       ├── components/
│       │   ├── EstimateView.tsx              # Main container
│       │   ├── EstimateSummary.tsx           # Cost breakdown
│       │   ├── JobItemCard.tsx               # Individual service card
│       │   ├── ApprovalButtons.tsx           # Approve/Reject actions
│       │   ├── EstimateTimeline.tsx          # Approval history
│       │   └── ShareEstimateDialog.tsx       # Generate shareable link
│       ├── hooks/
│       │   ├── useEstimate.ts                # Fetch estimate data
│       │   ├── useApproveEstimate.ts         # Approval mutations
│       │   └── useEstimateSubscription.ts    # Real-time updates
│       └── pages/
│           ├── EstimateDetailPage.tsx        # Authenticated view
│           └── PublicEstimatePage.tsx        # Public shareable link
│
├── pages/
│   ├── estimates/
│   │   └── [jobCardId]/
│   │       └── index.tsx                     # /estimates/[id]
│   └── e/
│       └── [shareToken]/
│           └── index.tsx                     # /e/[token] (public link)
│
└── graphql/
    ├── queries/
    │   ├── getJobCardEstimate.graphql
    │   └── getEstimateByShareToken.graphql
    └── mutations/
        ├── approveJobCard.graphql
        ├── approveJobItem.graphql
        ├── generateEstimateShareLink.graphql
        └── revokeEstimateShareLink.graphql
```

---

## 🔧 Implementation Details

### 1. GraphQL Queries

#### Get Estimate for Customer Review

```graphql
# queries/getJobCardEstimate.graphql
query GetJobCardEstimate($jobCardId: UUID!) {
  jobCard(where: { id: { eq: $jobCardId } }) {
    id
    status
    isApprovedByCustomer
    approvedAt
    totalEstimatedCost
    totalEstimatedLabor
    totalEstimatedParts
    createdAt
    
    customer {
      id
      firstName
      lastName
      email
      phoneNumber
    }
    
    car {
      id
      make
      model
      year
      licensePlate
      color
    }
    
    items {
      id
      description
      estimatedLaborCost
      estimatedPartsCost
      estimatedCost
      isApprovedByCustomer
      approvedAt
      status
      technicianNotes
      createdAt
    }
    
    assignedTechnician {
      id
      fullName
      email
    }
    
    organization {
      id
      name
      logo {
        url
      }
    }
  }
}
```

#### Get Estimate by Public Share Token

```graphql
# queries/getEstimateByShareToken.graphql
query GetEstimateByShareToken($shareToken: String!) {
  estimateByShareToken(token: $shareToken) {
    id
    jobCardId
    expiresAt
    isActive
    
    jobCard {
      id
      totalEstimatedCost
      totalEstimatedLabor
      totalEstimatedParts
      isApprovedByCustomer
      
      customer {
        firstName
        lastName
      }
      
      car {
        make
        model
        year
        licensePlate
      }
      
      items {
        id
        description
        estimatedLaborCost
        estimatedPartsCost
        estimatedCost
        isApprovedByCustomer
        technicianNotes
      }
      
      organization {
        name
        logo {
          url
        }
      }
    }
  }
}
```

---

### 2. GraphQL Mutations

#### Approve Full Estimate

```graphql
# mutations/approveJobCard.graphql
mutation ApproveJobCard($jobCardId: UUID!) {
  approveJobCard(jobCardId: $jobCardId) {
    id
    isApprovedByCustomer
    approvedAt
    items {
      id
      isApprovedByCustomer
      approvedAt
    }
  }
}
```

#### Approve Individual Job Item

```graphql
# mutations/approveJobItem.graphql
mutation ApproveJobItem($itemId: UUID!) {
  approveJobItem(itemId: $itemId) {
    id
    isApprovedByCustomer
    approvedAt
    jobCard {
      id
      items {
        id
        isApprovedByCustomer
      }
    }
  }
}
```

#### Generate Shareable Link

```graphql
# mutations/generateEstimateShareLink.graphql
mutation GenerateEstimateShareLink($jobCardId: UUID!, $expiresInHours: Int) {
  generateEstimateShareLink(jobCardId: $jobCardId, expiresInHours: $expiresInHours) {
    id
    shareToken
    shareUrl
    expiresAt
    isActive
  }
}
```

---

### 3. React Components

#### A. EstimateView.tsx (Main Container)

```typescript
// features/estimates/components/EstimateView.tsx
import React, { useState } from 'react';
import { useEstimate } from '../hooks/useEstimate';
import { EstimateSummary } from './EstimateSummary';
import { JobItemCard } from './JobItemCard';
import { ApprovalButtons } from './ApprovalButtons';
import { EstimateTimeline } from './EstimateTimeline';
import { Skeleton } from '@/components/ui/skeleton';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { CheckCircle, Clock, AlertTriangle } from 'lucide-react';

interface EstimateViewProps {
  jobCardId: string;
  readOnly?: boolean;
}

export const EstimateView: React.FC<EstimateViewProps> = ({ 
  jobCardId, 
  readOnly = false 
}) => {
  const { data: estimate, isLoading, error } = useEstimate(jobCardId);
  const [selectedItems, setSelectedItems] = useState<Set<string>>(new Set());

  if (isLoading) {
    return <EstimateSkeleton />;
  }

  if (error) {
    return (
      <Alert variant="destructive">
        <AlertTriangle className="h-4 w-4" />
        <AlertDescription>
          Failed to load estimate. Please try again.
        </AlertDescription>
      </Alert>
    );
  }

  if (!estimate) {
    return <div>Estimate not found</div>;
  }

  const handleItemToggle = (itemId: string) => {
    setSelectedItems(prev => {
      const newSet = new Set(prev);
      if (newSet.has(itemId)) {
        newSet.delete(itemId);
      } else {
        newSet.add(itemId);
      }
      return newSet;
    });
  };

  const handleSelectAll = () => {
    setSelectedItems(new Set(estimate.items.map(item => item.id)));
  };

  const handleDeselectAll = () => {
    setSelectedItems(new Set());
  };

  return (
    <div className="max-w-4xl mx-auto space-y-6 p-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Repair Estimate</h1>
          <p className="text-muted-foreground">
            {estimate.car.year} {estimate.car.make} {estimate.car.model}
          </p>
          <p className="text-sm text-muted-foreground">
            License Plate: {estimate.car.licensePlate}
          </p>
        </div>
        
        {/* Status Badge */}
        {estimate.isApprovedByCustomer ? (
          <div className="flex items-center gap-2 text-green-600 bg-green-50 px-4 py-2 rounded-full">
            <CheckCircle className="h-5 w-5" />
            <span className="font-semibold">Approved</span>
          </div>
        ) : (
          <div className="flex items-center gap-2 text-amber-600 bg-amber-50 px-4 py-2 rounded-full">
            <Clock className="h-5 w-5" />
            <span className="font-semibold">Awaiting Approval</span>
          </div>
        )}
      </div>

      {/* Organization Info */}
      <div className="flex items-center gap-4 p-4 bg-gray-50 rounded-lg">
        {estimate.organization.logo?.url && (
          <img 
            src={estimate.organization.logo.url} 
            alt={estimate.organization.name}
            className="h-12 w-12 object-contain"
          />
        )}
        <div>
          <p className="font-semibold">{estimate.organization.name}</p>
          {estimate.assignedTechnician && (
            <p className="text-sm text-muted-foreground">
              Technician: {estimate.assignedTechnician.fullName}
            </p>
          )}
        </div>
      </div>

      {/* Cost Summary */}
      <EstimateSummary 
        totalCost={estimate.totalEstimatedCost}
        laborCost={estimate.totalEstimatedLabor}
        partsCost={estimate.totalEstimatedParts}
        selectedItems={Array.from(selectedItems)}
        allItems={estimate.items}
      />

      {/* Selection Controls */}
      {!readOnly && !estimate.isApprovedByCustomer && (
        <div className="flex gap-2 justify-end">
          <button
            onClick={handleSelectAll}
            className="text-sm text-blue-600 hover:text-blue-800"
          >
            Select All
          </button>
          <span className="text-gray-400">|</span>
          <button
            onClick={handleDeselectAll}
            className="text-sm text-blue-600 hover:text-blue-800"
          >
            Deselect All
          </button>
        </div>
      )}

      {/* Job Items List */}
      <div className="space-y-4">
        <h2 className="text-xl font-semibold">Services & Repairs</h2>
        {estimate.items.map((item) => (
          <JobItemCard
            key={item.id}
            item={item}
            isSelected={selectedItems.has(item.id)}
            onToggle={handleItemToggle}
            readOnly={readOnly || estimate.isApprovedByCustomer}
          />
        ))}
      </div>

      {/* Approval Section */}
      {!readOnly && !estimate.isApprovedByCustomer && (
        <ApprovalButtons
          jobCardId={jobCardId}
          selectedItems={Array.from(selectedItems)}
          allItems={estimate.items.map(item => item.id)}
          totalCost={estimate.totalEstimatedCost}
        />
      )}

      {/* Timeline */}
      {estimate.isApprovedByCustomer && (
        <EstimateTimeline
          approvedAt={estimate.approvedAt}
          items={estimate.items}
        />
      )}
    </div>
  );
};

const EstimateSkeleton = () => (
  <div className="max-w-4xl mx-auto space-y-6 p-6">
    <Skeleton className="h-12 w-3/4" />
    <Skeleton className="h-32 w-full" />
    <Skeleton className="h-64 w-full" />
    <Skeleton className="h-64 w-full" />
  </div>
);
```

---

#### B. JobItemCard.tsx (Individual Service)

```typescript
// features/estimates/components/JobItemCard.tsx
import React from 'react';
import { Card, CardContent } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import { CheckCircle, Wrench, Package } from 'lucide-react';
import { formatCurrency } from '@/lib/utils';

interface JobItem {
  id: string;
  description: string;
  estimatedLaborCost: number;
  estimatedPartsCost: number;
  estimatedCost: number;
  isApprovedByCustomer: boolean;
  technicianNotes?: string;
}

interface JobItemCardProps {
  item: JobItem;
  isSelected: boolean;
  onToggle: (itemId: string) => void;
  readOnly?: boolean;
}

export const JobItemCard: React.FC<JobItemCardProps> = ({
  item,
  isSelected,
  onToggle,
  readOnly = false
}) => {
  const isApproved = item.isApprovedByCustomer;

  return (
    <Card className={`
      transition-all duration-200
      ${isSelected && !isApproved ? 'border-blue-500 border-2' : ''}
      ${isApproved ? 'border-green-500 border-2 bg-green-50/50' : ''}
      ${!readOnly && !isApproved ? 'cursor-pointer hover:shadow-md' : ''}
    `}
      onClick={() => !readOnly && !isApproved && onToggle(item.id)}
    >
      <CardContent className="p-6">
        <div className="flex items-start gap-4">
          {/* Checkbox */}
          {!readOnly && !isApproved && (
            <Checkbox
              checked={isSelected}
              onCheckedChange={() => onToggle(item.id)}
              className="mt-1"
            />
          )}

          {/* Approved Icon */}
          {isApproved && (
            <div className="text-green-600 mt-1">
              <CheckCircle className="h-6 w-6" />
            </div>
          )}

          {/* Content */}
          <div className="flex-1 space-y-3">
            {/* Description */}
            <div>
              <h3 className="font-semibold text-lg">{item.description}</h3>
              {item.technicianNotes && (
                <p className="text-sm text-muted-foreground mt-1">
                  {item.technicianNotes}
                </p>
              )}
            </div>

            {/* Cost Breakdown */}
            <div className="grid grid-cols-2 gap-4 text-sm">
              <div className="flex items-center gap-2">
                <Wrench className="h-4 w-4 text-gray-500" />
                <span className="text-muted-foreground">Labor:</span>
                <span className="font-medium">
                  {formatCurrency(item.estimatedLaborCost)}
                </span>
              </div>
              <div className="flex items-center gap-2">
                <Package className="h-4 w-4 text-gray-500" />
                <span className="text-muted-foreground">Parts:</span>
                <span className="font-medium">
                  {formatCurrency(item.estimatedPartsCost)}
                </span>
              </div>
            </div>
          </div>

          {/* Total Cost */}
          <div className="text-right">
            <p className="text-sm text-muted-foreground">Total</p>
            <p className="text-2xl font-bold text-blue-600">
              {formatCurrency(item.estimatedCost)}
            </p>
          </div>
        </div>
      </CardContent>
    </Card>
  );
};
```

---

#### C. EstimateSummary.tsx (Cost Breakdown)

```typescript
// features/estimates/components/EstimateSummary.tsx
import React, { useMemo } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Separator } from '@/components/ui/separator';
import { formatCurrency } from '@/lib/utils';

interface JobItem {
  id: string;
  estimatedCost: number;
  estimatedLaborCost: number;
  estimatedPartsCost: number;
}

interface EstimateSummaryProps {
  totalCost: number;
  laborCost: number;
  partsCost: number;
  selectedItems: string[];
  allItems: JobItem[];
}

export const EstimateSummary: React.FC<EstimateSummaryProps> = ({
  totalCost,
  laborCost,
  partsCost,
  selectedItems,
  allItems
}) => {
  const selectedCosts = useMemo(() => {
    if (selectedItems.length === 0) {
      return {
        total: totalCost,
        labor: laborCost,
        parts: partsCost
      };
    }

    const selected = allItems.filter(item => selectedItems.includes(item.id));
    
    return {
      total: selected.reduce((sum, item) => sum + item.estimatedCost, 0),
      labor: selected.reduce((sum, item) => sum + item.estimatedLaborCost, 0),
      parts: selected.reduce((sum, item) => sum + item.estimatedPartsCost, 0)
    };
  }, [selectedItems, allItems, totalCost, laborCost, partsCost]);

  const isPartialSelection = selectedItems.length > 0 && selectedItems.length < allItems.length;

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center justify-between">
          <span>Estimate Summary</span>
          {isPartialSelection && (
            <span className="text-sm font-normal text-muted-foreground">
              {selectedItems.length} of {allItems.length} items selected
            </span>
          )}
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        {/* Labor Cost */}
        <div className="flex justify-between items-center">
          <span className="text-muted-foreground">Labor</span>
          <span className="font-semibold">{formatCurrency(selectedCosts.labor)}</span>
        </div>

        {/* Parts Cost */}
        <div className="flex justify-between items-center">
          <span className="text-muted-foreground">Parts</span>
          <span className="font-semibold">{formatCurrency(selectedCosts.parts)}</span>
        </div>

        <Separator />

        {/* Total */}
        <div className="flex justify-between items-center">
          <span className="text-lg font-semibold">Total</span>
          <span className="text-2xl font-bold text-blue-600">
            {formatCurrency(selectedCosts.total)}
          </span>
        </div>

        {/* Tax Notice */}
        <p className="text-xs text-muted-foreground text-center">
          * Tax and additional fees may apply
        </p>

        {/* Partial Selection Warning */}
        {isPartialSelection && (
          <div className="bg-amber-50 border border-amber-200 rounded-lg p-3">
            <p className="text-sm text-amber-800">
              You've selected only some services. The total shown reflects your selection.
            </p>
          </div>
        )}
      </CardContent>
    </Card>
  );
};
```

---

#### D. ApprovalButtons.tsx (Action Buttons)

```typescript
// features/estimates/components/ApprovalButtons.tsx
import React, { useState } from 'react';
import { Button } from '@/components/ui/button';
import { useApproveEstimate } from '../hooks/useApproveEstimate';
import { CheckCircle, X, Loader2 } from 'lucide-react';
import { formatCurrency } from '@/lib/utils';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";

interface ApprovalButtonsProps {
  jobCardId: string;
  selectedItems: string[];
  allItems: string[];
  totalCost: number;
}

export const ApprovalButtons: React.FC<ApprovalButtonsProps> = ({
  jobCardId,
  selectedItems,
  allItems,
  totalCost
}) => {
  const [showConfirmDialog, setShowConfirmDialog] = useState(false);
  const { approveJobCard, approveJobItems, isLoading } = useApproveEstimate();

  const handleApprove = async () => {
    if (selectedItems.length === 0) {
      alert('Please select at least one service to approve');
      return;
    }

    setShowConfirmDialog(true);
  };

  const confirmApproval = async () => {
    try {
      if (selectedItems.length === allItems.length) {
        // Approve entire job card
        await approveJobCard({ jobCardId });
      } else {
        // Approve individual items
        await approveJobItems({ 
          jobCardId,
          itemIds: selectedItems 
        });
      }

      // Show success message
      alert('Estimate approved successfully! We will begin work shortly.');
      
      // Optionally redirect or refresh
      window.location.reload();
    } catch (error) {
      alert('Failed to approve estimate. Please try again.');
    } finally {
      setShowConfirmDialog(false);
    }
  };

  const selectedCount = selectedItems.length;
  const isFullApproval = selectedCount === allItems.length;

  return (
    <div className="sticky bottom-0 bg-white border-t shadow-lg p-6 space-y-4">
      {/* Selection Info */}
      <div className="flex items-center justify-between">
        <div>
          <p className="text-sm text-muted-foreground">
            {selectedCount} of {allItems.length} services selected
          </p>
          <p className="text-lg font-semibold">
            Estimated Total: {formatCurrency(totalCost)}
          </p>
        </div>
      </div>

      {/* Action Buttons */}
      <div className="flex gap-4">
        <Button
          onClick={handleApprove}
          disabled={selectedCount === 0 || isLoading}
          className="flex-1"
          size="lg"
        >
          {isLoading ? (
            <>
              <Loader2 className="mr-2 h-5 w-5 animate-spin" />
              Approving...
            </>
          ) : (
            <>
              <CheckCircle className="mr-2 h-5 w-5" />
              {isFullApproval ? 'Approve All Services' : `Approve ${selectedCount} Service${selectedCount > 1 ? 's' : ''}`}
            </>
          )}
        </Button>

        <Button
          variant="outline"
          size="lg"
          onClick={() => alert('Contact us to discuss modifications')}
        >
          <X className="mr-2 h-5 w-5" />
          Request Changes
        </Button>
      </div>

      {/* Confirmation Dialog */}
      <AlertDialog open={showConfirmDialog} onOpenChange={setShowConfirmDialog}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Confirm Estimate Approval</AlertDialogTitle>
            <AlertDialogDescription>
              You are about to approve {isFullApproval ? 'all services' : `${selectedCount} selected service${selectedCount > 1 ? 's' : ''}`} 
              for a total of <strong>{formatCurrency(totalCost)}</strong>.
              <br /><br />
              By approving, you authorize us to begin work on the selected services.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction onClick={confirmApproval}>
              Confirm Approval
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
};
```

---

### 4. Custom Hooks

#### useEstimate.ts (Fetch Estimate Data)

```typescript
// features/estimates/hooks/useEstimate.ts
import { useQuery } from '@tanstack/react-query';
import { graphQLClient } from '@/lib/graphql-client';
import { GET_JOB_CARD_ESTIMATE } from '../graphql/queries';

export interface Estimate {
  id: string;
  status: string;
  isApprovedByCustomer: boolean;
  approvedAt?: string;
  totalEstimatedCost: number;
  totalEstimatedLabor: number;
  totalEstimatedParts: number;
  customer: {
    firstName: string;
    lastName: string;
    email: string;
    phoneNumber: string;
  };
  car: {
    make: string;
    model: string;
    year: number;
    licensePlate: string;
    color: string;
  };
  items: Array<{
    id: string;
    description: string;
    estimatedLaborCost: number;
    estimatedPartsCost: number;
    estimatedCost: number;
    isApprovedByCustomer: boolean;
    approvedAt?: string;
    technicianNotes?: string;
  }>;
  assignedTechnician?: {
    fullName: string;
    email: string;
  };
  organization: {
    name: string;
    logo?: {
      url: string;
    };
  };
}

export const useEstimate = (jobCardId: string) => {
  return useQuery<Estimate>({
    queryKey: ['estimate', jobCardId],
    queryFn: async () => {
      const data = await graphQLClient.request(GET_JOB_CARD_ESTIMATE, {
        jobCardId
      });
      return data.jobCard;
    },
    staleTime: 1000 * 60 * 5, // 5 minutes
    refetchInterval: 1000 * 30, // Refetch every 30 seconds for live updates
  });
};
```

---

#### useApproveEstimate.ts (Approval Mutations)

```typescript
// features/estimates/hooks/useApproveEstimate.ts
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { graphQLClient } from '@/lib/graphql-client';
import { APPROVE_JOB_CARD, APPROVE_JOB_ITEM } from '../graphql/mutations';

interface ApproveJobCardInput {
  jobCardId: string;
}

interface ApproveJobItemsInput {
  jobCardId: string;
  itemIds: string[];
}

export const useApproveEstimate = () => {
  const queryClient = useQueryClient();

  const approveJobCardMutation = useMutation({
    mutationFn: async (input: ApproveJobCardInput) => {
      const data = await graphQLClient.request(APPROVE_JOB_CARD, {
        jobCardId: input.jobCardId
      });
      return data.approveJobCard;
    },
    onSuccess: (data, variables) => {
      // Invalidate and refetch estimate
      queryClient.invalidateQueries({ queryKey: ['estimate', variables.jobCardId] });
    },
  });

  const approveJobItemsMutation = useMutation({
    mutationFn: async (input: ApproveJobItemsInput) => {
      // Approve each item individually
      const promises = input.itemIds.map(itemId =>
        graphQLClient.request(APPROVE_JOB_ITEM, { itemId })
      );
      return await Promise.all(promises);
    },
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: ['estimate', variables.jobCardId] });
    },
  });

  return {
    approveJobCard: approveJobCardMutation.mutateAsync,
    approveJobItems: approveJobItemsMutation.mutateAsync,
    isLoading: approveJobCardMutation.isPending || approveJobItemsMutation.isPending,
  };
};
```

---

### 5. Public Shareable Link Page

```typescript
// pages/e/[shareToken]/index.tsx
import { useRouter } from 'next/router';
import { useQuery } from '@tanstack/react-query';
import { EstimateView } from '@/features/estimates/components/EstimateView';
import { graphQLClient } from '@/lib/graphql-client';
import { GET_ESTIMATE_BY_SHARE_TOKEN } from '@/features/estimates/graphql/queries';
import { Loader2, AlertTriangle } from 'lucide-react';

export default function PublicEstimatePage() {
  const router = useRouter();
  const { shareToken } = router.query;

  const { data, isLoading, error } = useQuery({
    queryKey: ['public-estimate', shareToken],
    queryFn: async () => {
      const response = await graphQLClient.request(GET_ESTIMATE_BY_SHARE_TOKEN, {
        shareToken: shareToken as string
      });
      return response.estimateByShareToken;
    },
    enabled: !!shareToken,
  });

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <Loader2 className="h-12 w-12 animate-spin text-blue-600" />
      </div>
    );
  }

  if (error || !data || !data.isActive) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <div className="text-center space-y-4">
          <AlertTriangle className="h-16 w-16 text-amber-500 mx-auto" />
          <h1 className="text-2xl font-bold">Link Expired or Invalid</h1>
          <p className="text-muted-foreground max-w-md">
            This estimate link is no longer valid or has expired. 
            Please contact the garage for a new link.
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 py-8">
      <EstimateView 
        jobCardId={data.jobCardId} 
        readOnly={false}
      />
    </div>
  );
}
```

---

## 🎨 UI/UX Best Practices

### 1. Mobile-First Design
- **Responsive layouts** - Works on all screen sizes
- **Touch-friendly buttons** - Minimum 44x44px tap targets
- **Readable fonts** - Minimum 16px for body text
- **Clear CTAs** - Prominent approval buttons

### 2. Progressive Disclosure
- **Summary first** - Show total cost immediately
- **Expandable details** - Click to see full breakdown
- **Optional notes** - Technician comments available but not intrusive

### 3. Trust & Transparency
- **Clear pricing** - No hidden costs
- **Itemized breakdown** - See what you're paying for
- **Business branding** - Logo and contact info visible
- **Secure link** - HTTPS with expiring tokens

### 4. Accessibility
- **Screen reader support** - Proper ARIA labels
- **Keyboard navigation** - All actions accessible via keyboard
- **High contrast** - WCAG AA compliant colors
- **Clear focus states** - Visible focus indicators

---

## 📱 Notification Strategy

### SMS/Email Templates

#### 1. Estimate Ready Notification

**SMS:**
```
Hi {CustomerName}, your repair estimate for {CarMake} {CarModel} 
is ready! Total: ${TotalCost}

View & approve: {ShareURL}

Questions? Call us at {PhoneNumber}
- {GarageName}
```

**Email:**
```html
Subject: Your Repair Estimate is Ready - {CarMake} {CarModel}

Hi {CustomerName},

We've completed the inspection of your {Year} {CarMake} {CarModel} 
({LicensePlate}) and prepared your repair estimate.

Estimated Total: ${TotalCost}
- Labor: ${LaborCost}
- Parts: ${PartsCost}

[VIEW & APPROVE ESTIMATE BUTTON]

You can review each service in detail and approve what you'd like us to proceed with.

Questions? Reply to this email or call us at {PhoneNumber}

Best regards,
{TechnicianName}
{GarageName}
```

#### 2. Approval Confirmation

**SMS:**
```
Thank you for approving your estimate! We'll begin work on your 
{CarMake} {CarModel} shortly. 

Track progress: {ProgressURL}
```

**Email:**
```html
Subject: Estimate Approved - Work Starting Soon

Hi {CustomerName},

Thank you for approving your repair estimate!

Approved Services: {ApprovedCount}
Total Approved: ${ApprovedTotal}

We'll begin work immediately and keep you updated on progress.

[TRACK PROGRESS BUTTON]

Estimated completion: {EstimatedCompletion}
```

---

## 🔒 Security Considerations

### 1. Share Token Security
```typescript
// Generate secure, time-limited tokens
interface EstimateShareToken {
  token: string;           // UUID or cryptographic hash
  jobCardId: string;
  expiresAt: Date;        // Default: 72 hours
  isActive: boolean;
  createdBy: string;
  usageCount: number;     // Track how many times accessed
  maxUsageCount?: number;  // Optional: limit to X views
}

// Backend validation
const validateShareToken = (token: string) => {
  const shareToken = await db.shareTokens.findOne({ token });
  
  if (!shareToken) throw new Error('Invalid token');
  if (!shareToken.isActive) throw new Error('Token revoked');
  if (shareToken.expiresAt < new Date()) throw new Error('Token expired');
  if (shareToken.maxUsageCount && shareToken.usageCount >= shareToken.maxUsageCount) {
    throw new Error('Token usage limit reached');
  }
  
  // Increment usage count
  await db.shareTokens.update(token, { usageCount: shareToken.usageCount + 1 });
  
  return shareToken;
};
```

### 2. Rate Limiting
```typescript
// Prevent abuse of public links
const rateLimiter = rateLimit({
  windowMs: 15 * 60 * 1000, // 15 minutes
  max: 10, // 10 requests per window
  message: 'Too many requests, please try again later'
});

app.use('/api/public/estimates', rateLimiter);
```

### 3. Audit Trail
```typescript
// Log all approval actions
interface ApprovalAuditLog {
  jobCardId: string;
  itemId?: string;
  action: 'approved' | 'rejected' | 'viewed';
  ipAddress: string;
  userAgent: string;
  shareToken?: string;
  timestamp: Date;
}
```

---

## 📊 Analytics & Metrics to Track

### 1. Conversion Metrics
- **View Rate** - % of customers who view the estimate
- **Approval Rate** - % of views that result in approval
- **Partial Approval Rate** - % of customers who approve some items
- **Time to Approval** - Average time from send to approval
- **Rejection Rate** - % of estimates not approved

### 2. Financial Metrics
- **Average Estimate Value** - Mean estimate amount
- **Approval Value** - Total approved vs rejected amounts
- **Item Approval Patterns** - Which services get approved most

### 3. Engagement Metrics
- **Link Open Rate** - % of sent links that are opened
- **Session Duration** - Time spent reviewing estimate
- **Device Usage** - Mobile vs desktop breakdown

---

## 🚀 Advanced Features (Future Enhancements)

### 1. Real-Time Collaboration
```typescript
// WebSocket updates when technician modifies estimate
const useEstimateSubscription = (jobCardId: string) => {
  useEffect(() => {
    const subscription = graphQLClient.subscribe({
      query: ESTIMATE_UPDATED_SUBSCRIPTION,
      variables: { jobCardId }
    });

    subscription.subscribe({
      next: (data) => {
        // Update local state with new estimate data
        queryClient.setQueryData(['estimate', jobCardId], data);
      }
    });

    return () => subscription.unsubscribe();
  }, [jobCardId]);
};
```

### 2. Payment Integration
```typescript
// Allow immediate payment upon approval
interface PaymentOption {
  provider: 'stripe' | 'square' | 'paypal';
  amount: number;
  depositRequired?: number; // e.g., 50% deposit
}

const handleApproveAndPay = async () => {
  // 1. Approve estimate
  await approveEstimate();
  
  // 2. Initialize payment
  const paymentIntent = await createPaymentIntent({
    amount: totalCost,
    jobCardId
  });
  
  // 3. Show payment form
  showPaymentDialog(paymentIntent);
};
```

### 3. Appointment Scheduling
```typescript
// Book drop-off time after approval
const handleApproveAndSchedule = async () => {
  await approveEstimate();
  router.push(`/book-appointment?jobCardId=${jobCardId}`);
};
```

### 4. Video Explanation
```typescript
// Technician can record video explaining estimate
interface EstimateVideo {
  url: string;
  thumbnail: string;
  duration: number;
  createdAt: Date;
}

// Component
const VideoExplanation = ({ videoUrl }: { videoUrl: string }) => (
  <div className="aspect-video">
    <video src={videoUrl} controls className="w-full rounded-lg" />
  </div>
);
```

### 5. Financing Options
```typescript
// Show monthly payment calculator
const FinancingCalculator = ({ totalCost }: { totalCost: number }) => {
  const [term, setTerm] = useState(12); // months
  const monthlyPayment = (totalCost / term) * 1.05; // 5% interest

  return (
    <div className="bg-blue-50 p-4 rounded-lg">
      <h3>Financing Available</h3>
      <p>As low as ${monthlyPayment.toFixed(2)}/month</p>
      <input 
        type="range" 
        min="6" 
        max="24" 
        value={term} 
        onChange={(e) => setTerm(Number(e.target.value))} 
      />
      <Button>Apply Now</Button>
    </div>
  );
};
```

---

## ✅ Implementation Checklist

### Phase 1: Core Functionality (Week 1-2)
- [ ] Set up GraphQL queries and mutations
- [ ] Build EstimateView component
- [ ] Build JobItemCard component
- [ ] Build EstimateSummary component
- [ ] Build ApprovalButtons component
- [ ] Implement useEstimate hook
- [ ] Implement useApproveEstimate hook
- [ ] Create public estimate page route
- [ ] Add basic styling with Tailwind CSS

### Phase 2: Share Link Feature (Week 3)
- [ ] Backend: Implement share token generation
- [ ] Backend: Add token validation middleware
- [ ] Frontend: Build ShareEstimateDialog component
- [ ] Frontend: Create public link page
- [ ] Implement token expiration logic
- [ ] Add usage tracking

### Phase 3: Notifications (Week 4)
- [ ] Integrate SMS provider (Twilio)
- [ ] Integrate email provider (SendGrid)
- [ ] Create SMS templates
- [ ] Create email templates
- [ ] Implement notification triggers
- [ ] Add notification preferences

### Phase 4: Polish & Testing (Week 5)
- [ ] Mobile responsive testing
- [ ] Cross-browser testing
- [ ] Accessibility audit
- [ ] Performance optimization
- [ ] Analytics integration
- [ ] User acceptance testing

---

## 📚 Dependencies & Tech Stack

### Frontend Dependencies

```json
{
  "dependencies": {
    "react": "^18.3.0",
    "next": "^14.2.0",
    "@tanstack/react-query": "^5.0.0",
    "graphql": "^16.8.0",
    "graphql-request": "^6.1.0",
    "@radix-ui/react-checkbox": "^1.0.0",
    "@radix-ui/react-dialog": "^1.0.0",
    "@radix-ui/react-alert-dialog": "^1.0.0",
    "lucide-react": "^0.300.0",
    "tailwindcss": "^3.4.0",
    "clsx": "^2.0.0",
    "date-fns": "^3.0.0"
  }
}
```

### Backend Enhancements Needed

```csharp
// New models to add
public sealed class EstimateShareToken : IMustHaveOrganization
{
    public Guid Id { get; set; }
    public Guid JobCardId { get; set; }
    public JobCard? JobCard { get; set; }
    
    public string Token { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(72);
    public bool IsActive { get; set; } = true;
    
    public int UsageCount { get; set; }
    public int? MaxUsageCount { get; set; }
    
    public string CreatedById { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public Guid OrganizationId { get; set; }
}

// New mutations to add
public static async Task<EstimateShareToken> GenerateEstimateShareLinkAsync(
    Guid jobCardId,
    int expiresInHours,
    ApplicationDbContext context,
    ClaimsPrincipal claimsPrincipal)
{
    // Implementation
}

public static async Task<JobCard?> GetEstimateByShareTokenAsync(
    string token,
    ApplicationDbContext context)
{
    // Implementation - no auth required
}
```

---

## 🎓 Training Materials Needed

### 1. For Technicians
- How to create accurate estimates
- Using the estimate generation tool
- Sending estimate links to customers
- Handling customer questions about estimates

### 2. For Service Advisors
- Explaining estimates to customers
- Handling partial approvals
- Upselling additional services
- Managing customer expectations

### 3. For Customers (Help Center)
- How to view your estimate
- Understanding the breakdown
- Approving specific services
- Contacting us with questions

---

## 📈 Success Metrics (6 Months Post-Launch)

### Target KPIs:
- **80%+** estimate view rate (of links sent)
- **65%+** approval rate (of estimates viewed)
- **<24 hours** average time to approval
- **90%+** mobile usage (most customers view on mobile)
- **40%+** partial approval rate (customers selecting specific items)

---

## 🎉 Summary

The estimate approval workflow is a **high-value, customer-facing feature** that:

✅ **Improves trust** through transparency  
✅ **Increases efficiency** with digital approvals  
✅ **Reduces disputes** via documented consent  
✅ **Enhances UX** with modern, mobile-first design  
✅ **Protects legally** with audit trails  

### Implementation Timeline:
- **Core functionality**: 2 weeks
- **Share links & notifications**: 2 weeks
- **Polish & testing**: 1 week
- **Total**: ~5 weeks for production-ready feature

### Estimated Development Cost:
- **Frontend**: 80 hours @ $75/hr = $6,000
- **Backend enhancements**: 40 hours @ $100/hr = $4,000
- **Testing & QA**: 20 hours @ $60/hr = $1,200
- **Total**: ~$11,200

**ROI**: High - This feature directly impacts revenue by reducing friction in the approval process and increasing customer confidence.

---

**Ready to implement? Let's prioritize this alongside invoicing and inventory management! 🚀**
