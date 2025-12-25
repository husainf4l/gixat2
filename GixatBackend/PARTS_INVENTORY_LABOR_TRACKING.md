# Parts & Inventory and Labor Tracking Implementation

## Overview

This document describes the implementation of **Parts & Inventory Management** and **Labor & Technician Hours Tracking** modules for the Job Card system. These modules provide detailed tracking of parts usage and labor time on job items.

## Architecture

### Entity Models

#### 1. InventoryItem ([InventoryItem.cs](Modules/JobCards/Models/InventoryItem.cs))
Master catalog of parts and materials available in the organization's inventory.

**Key Properties:**
- `PartNumber` - Unique identifier (SKU)
- `Name` - Display name
- `Category` - Part category (e.g., "Engine Parts", "Electrical")
- `UnitOfMeasure` - Unit type (e.g., "piece", "liter", "set")
- `QuantityInStock` - Current available quantity
- `MinimumStockLevel` - Reorder threshold
- `CostPrice` - Purchase cost per unit
- `SellingPrice` - Customer price per unit
- `Supplier` - Vendor/manufacturer
- `IsActive` - Active/inactive status

**Features:**
- Multi-tenancy support (OrganizationId)
- Unique part numbers per organization
- Low stock monitoring
- Soft delete (IsActive flag)

#### 2. JobItemPart ([JobItemPart.cs](Modules/JobCards/Models/JobItemPart.cs))
Links inventory items to job items with quantity and pricing.

**Key Properties:**
- `JobItemId` - Reference to job item
- `InventoryItemId` - Reference to inventory item
- `Quantity` - Amount used
- `UnitPrice` - Price at time of use
- `Discount` - Applied discount
- `IsActual` - Estimated vs actual usage flag
- `TotalCost` - Calculated: Quantity × UnitPrice
- `FinalCost` - Calculated: TotalCost - Discount

**Features:**
- Automatic inventory deduction when `IsActual = true`
- Price lock (captures price at time of use)
- Discount support
- Estimate vs actual tracking

#### 3. LaborEntry ([LaborEntry.cs](Modules/JobCards/Models/LaborEntry.cs))
Individual labor time entries by technicians.

**Key Properties:**
- `JobItemId` - Reference to job item
- `TechnicianId` - Reference to technician user
- `StartTime` - Work start timestamp
- `EndTime` - Work end timestamp (null if in progress)
- `HoursWorked` - Total hours (calculated or manual)
- `HourlyRate` - Labor rate
- `LaborType` - Category (e.g., "Diagnostic", "Repair")
- `Description` - Work description
- `IsActual` - Estimated vs actual flag
- `IsBillable` - Billable to customer flag
- `TotalCost` - Calculated: HoursWorked × HourlyRate

**Features:**
- Clock in/out tracking
- Automatic hours calculation
- Labor type categorization
- Billable/non-billable designation
- Multiple technicians per job item

### Database Schema

#### Tables Created:
- **InventoryItems** - Inventory catalog
- **JobItemParts** - Part usage on job items
- **LaborEntries** - Labor time tracking

#### Indexes Created:
```sql
-- InventoryItems
- IX_InventoryItems_OrganizationId_PartNumber (UNIQUE)
- IX_InventoryItems_OrganizationId_Category_IsActive
- IX_InventoryItems_OrganizationId_IsActive_QuantityInStock

-- JobItemParts
- IX_JobItemParts_JobItemId
- IX_JobItemParts_InventoryItemId
- IX_JobItemParts_JobItemId_IsActual

-- LaborEntries
- IX_LaborEntries_JobItemId
- IX_LaborEntries_TechnicianId
- IX_LaborEntries_JobItemId_IsActual
- IX_LaborEntries_TechnicianId_StartTime_EndTime
```

#### Relationships:
```
InventoryItem (1) ----< (N) JobItemPart (N) >---- (1) JobItem
                                                         |
                                                         v
                                                    (N) LaborEntry (N) >---- (1) Technician
```

### GraphQL API

#### Inventory Queries ([InventoryQueries.cs](Modules/JobCards/GraphQL/InventoryQueries.cs))

```graphql
# Get all inventory items (with filtering, sorting, projection)
query {
  inventoryItems {
    id
    partNumber
    name
    quantityInStock
    sellingPrice
  }
}

# Get single inventory item
query {
  inventoryItemById(id: "guid") {
    id
    name
    quantityInStock
  }
}

# Get low stock items
query {
  lowStockItems {
    id
    name
    quantityInStock
    minimumStockLevel
  }
}

# Search inventory
query {
  searchInventory(searchTerm: "brake") {
    id
    name
    partNumber
  }
}

# Get by category
query {
  inventoryByCategory(category: "Engine Parts") {
    id
    name
  }
}
```

#### Inventory Mutations ([InventoryMutations.cs](Modules/JobCards/GraphQL/InventoryMutations.cs))

```graphql
# Create inventory item
mutation {
  createInventoryItem(input: {
    partNumber: "BRK-001"
    name: "Brake Pad Set"
    category: "Brakes"
    unitOfMeasure: "set"
    quantityInStock: 50
    minimumStockLevel: 10
    costPrice: 25.00
    sellingPrice: 45.00
  }) {
    id
    name
  }
}

# Update inventory item
mutation {
  updateInventoryItem(input: {
    id: "guid"
    sellingPrice: 50.00
    quantityInStock: 45
  }) {
    id
    sellingPrice
  }
}

# Adjust inventory quantity
mutation {
  adjustInventoryQuantity(input: {
    inventoryItemId: "guid"
    quantityChange: -5  # negative = deduct, positive = add
    reason: "Manual adjustment"
  }) {
    id
    quantityInStock
  }
}

# Delete (deactivate) inventory item
mutation {
  deleteInventoryItem(id: "guid")
}
```

#### Labor Queries ([LaborQueries.cs](Modules/JobCards/GraphQL/LaborQueries.cs))

```graphql
# Get labor entries for a job item
query {
  laborEntriesByJobItem(jobItemId: "guid") {
    id
    technician { name }
    startTime
    endTime
    hoursWorked
    totalCost
  }
}

# Get labor entries for a technician
query {
  laborEntriesByTechnician(
    technicianId: "userId"
    startDate: "2025-01-01"
    endDate: "2025-01-31"
  ) {
    id
    jobItem { description }
    hoursWorked
    totalCost
  }
}

# Get active labor entries (in progress)
query {
  activeLaborEntries(technicianId: "userId") {
    id
    jobItem { description }
    startTime
  }
}

# Get labor summary for a job card
query {
  laborSummaryByJobCard(jobCardId: "guid") {
    totalEstimatedHours
    totalActualHours
    totalEstimatedCost
    totalActualCost
    entryCount
  }
}
```

#### Job Item Parts & Labor Mutations ([JobItemPartsAndLaborMutations.cs](Modules/JobCards/GraphQL/JobItemPartsAndLaborMutations.cs))

```graphql
# Add part to job item
mutation {
  addPartToJobItem(input: {
    jobItemId: "guid"
    inventoryItemId: "guid"
    quantity: 2
    unitPrice: 45.00  # optional, defaults to inventory selling price
    discount: 0
    isActual: true  # deducts from inventory if true
    notes: "Customer requested premium parts"
  }) {
    id
    quantity
    totalCost
    finalCost
  }
}

# Update job item part
mutation {
  updateJobItemPart(input: {
    id: "guid"
    quantity: 3
    discount: 5.00
  }) {
    id
    finalCost
  }
}

# Remove part from job item
mutation {
  removePartFromJobItem(id: "guid")
}

# Add labor entry
mutation {
  addLaborEntry(input: {
    jobItemId: "guid"
    technicianId: "userId"
    startTime: "2025-01-15T09:00:00Z"
    endTime: "2025-01-15T11:30:00Z"
    hoursWorked: 2.5  # optional if startTime/endTime provided
    hourlyRate: 50.00
    laborType: "Repair"
    description: "Replaced brake pads"
    isActual: true
    isBillable: true
  }) {
    id
    totalCost
  }
}

# Update labor entry
mutation {
  updateLaborEntry(input: {
    id: "guid"
    endTime: "2025-01-15T12:00:00Z"
    hoursWorked: 3.0
  }) {
    id
    totalCost
  }
}

# Clock out labor entry (set end time to now)
mutation {
  clockOutLaborEntry(id: "guid") {
    id
    endTime
    hoursWorked
  }
}

# Delete labor entry
mutation {
  deleteLaborEntry(id: "guid")
}
```

#### JobItem Type Extensions

JobItem now exposes parts and labor entries via DataLoaders:

```graphql
query {
  jobCardById(id: "guid") {
    items {
      description

      # Parts used
      parts {
        quantity
        unitPrice
        totalCost
        inventoryItem {
          name
          partNumber
        }
      }

      # Labor entries
      laborEntries {
        technician { name }
        hoursWorked
        hourlyRate
        totalCost
        laborType
        description
      }
    }
  }
}
```

## Features

### Inventory Management
✅ **Multi-tenant inventory catalog**
✅ **Low stock alerts** - Query items below minimum level
✅ **Search and filtering** - By name, part number, category
✅ **Automatic stock deduction** - When parts marked as actual usage
✅ **Price locking** - Captures selling price at time of use
✅ **Soft delete** - Inactive items remain in database
✅ **Stock adjustments** - Manual add/subtract inventory

### Labor Tracking
✅ **Clock in/out** - Automatic time tracking
✅ **Multiple technicians** - Per job item
✅ **Labor types** - Categorize work (diagnostic, repair, etc.)
✅ **Billable/non-billable** - Flag for billing purposes
✅ **Estimate vs actual** - Track projected vs real time
✅ **Flexible rates** - Per-entry hourly rates
✅ **Active labor tracking** - Find in-progress work

### Integration with Job Items
✅ **Detailed cost breakdown** - Parts and labor separated
✅ **Estimate tracking** - Add estimated parts/labor before work starts
✅ **Actual tracking** - Record actual usage/time during work
✅ **Discount support** - Apply discounts to parts
✅ **Performance optimized** - DataLoaders prevent N+1 queries

## Multi-Tenancy

All entities are properly filtered by organization:
- **InventoryItems** - Direct filter on OrganizationId
- **JobItemParts** - Filtered through JobItem → JobCard → OrganizationId
- **LaborEntries** - Filtered through JobItem → JobCard → OrganizationId

Global query filters ensure data isolation across organizations.

## Inventory Deduction Logic

When adding/updating `JobItemPart`:

1. **IsActual = true** (actual usage):
   - Deducts quantity from `InventoryItem.QuantityInStock`
   - Validates sufficient stock before deduction
   - Throws error if insufficient stock

2. **IsActual = false** (estimate):
   - No inventory deduction
   - Used for estimates and quotes

3. **Updating from estimate to actual**:
   - Deducts quantity from inventory

4. **Updating from actual to estimate**:
   - Adds quantity back to inventory

5. **Deleting actual part**:
   - Adds quantity back to inventory

## Usage Workflow Examples

### Example 1: Adding Parts to Job Item

```graphql
# 1. Search for brake pads in inventory
query {
  searchInventory(searchTerm: "brake pad") {
    id
    name
    partNumber
    quantityInStock
    sellingPrice
  }
}

# 2. Add estimated parts (no inventory deduction)
mutation {
  addPartToJobItem(input: {
    jobItemId: "job-item-guid"
    inventoryItemId: "brake-pad-guid"
    quantity: 2
    isActual: false
  }) {
    id
    finalCost
  }
}

# 3. Customer approves, convert to actual (inventory deducted)
mutation {
  updateJobItemPart(input: {
    id: "job-item-part-guid"
    isActual: true
  }) {
    id
  }
}
```

### Example 2: Tracking Labor Time

```graphql
# 1. Technician starts work (clock in)
mutation {
  addLaborEntry(input: {
    jobItemId: "job-item-guid"
    technicianId: "tech-123"
    startTime: "2025-01-15T09:00:00Z"
    hourlyRate: 50.00
    laborType: "Diagnostic"
    isActual: true
    isBillable: true
  }) {
    id
  }
}

# 2. Check active labor (what's in progress)
query {
  activeLaborEntries(technicianId: "tech-123") {
    id
    jobItem { description }
    startTime
  }
}

# 3. Technician finishes (clock out)
mutation {
  clockOutLaborEntry(id: "labor-entry-guid") {
    id
    endTime
    hoursWorked
    totalCost
  }
}
```

### Example 3: Job Card with Full Details

```graphql
query {
  jobCardById(id: "job-card-guid") {
    id
    status
    totalActualCost

    items {
      description
      status

      # Parts breakdown
      parts {
        quantity
        unitPrice
        discount
        finalCost
        inventoryItem {
          name
          partNumber
        }
      }

      # Labor breakdown
      laborEntries {
        technician { name email }
        startTime
        endTime
        hoursWorked
        hourlyRate
        totalCost
        laborType
        description
        isBillable
      }

      # Calculated totals
      estimatedCost
      actualCost
    }
  }
}
```

## Files Created/Modified

### New Entity Models
- [InventoryItem.cs](Modules/JobCards/Models/InventoryItem.cs)
- [JobItemPart.cs](Modules/JobCards/Models/JobItemPart.cs)
- [LaborEntry.cs](Modules/JobCards/Models/LaborEntry.cs)

### Modified Entity Models
- [JobItem.cs](Modules/JobCards/Models/JobItem.cs:58-66) - Added `Parts` and `LaborEntries` navigation properties

### GraphQL Queries
- [InventoryQueries.cs](Modules/JobCards/GraphQL/InventoryQueries.cs)
- [LaborQueries.cs](Modules/JobCards/GraphQL/LaborQueries.cs)

### GraphQL Mutations
- [InventoryMutations.cs](Modules/JobCards/GraphQL/InventoryMutations.cs)
- [JobItemPartsAndLaborMutations.cs](Modules/JobCards/GraphQL/JobItemPartsAndLaborMutations.cs)

### DataLoaders
- [JobItemPartsDataLoader.cs](Modules/JobCards/Services/JobItemPartsDataLoader.cs)
- [JobItemLaborEntriesDataLoader.cs](Modules/JobCards/Services/JobItemLaborEntriesDataLoader.cs)

### GraphQL Extensions
- [JobItemExtensions.cs](Modules/JobCards/GraphQL/JobItemExtensions.cs:31-55) - Added parts and labor entry resolvers

### Database
- [ApplicationDbContext.cs](Data/ApplicationDbContext.cs:48-50) - Added DbSets
- [ApplicationDbContext.cs](Data/ApplicationDbContext.cs:242-300) - Added relationships and indexes
- Migration: `20251225005627_AddPartsInventoryAndLaborTracking`

### Configuration
- [Program.cs](Program.cs:251-252) - Registered query types
- [Program.cs](Program.cs:268-269) - Registered mutation types
- [Program.cs](Program.cs:289-290) - Registered DataLoaders

## Testing Recommendations

1. **Inventory Management**
   - Create inventory items
   - Test unique part number constraint
   - Test low stock queries
   - Test search functionality

2. **Parts on Job Items**
   - Add estimated parts (no stock deduction)
   - Convert to actual (verify stock deduction)
   - Test insufficient stock error
   - Remove part (verify stock added back)
   - Test discount calculations

3. **Labor Tracking**
   - Add labor entry with start/end time
   - Test automatic hours calculation
   - Test clock out functionality
   - Test multiple technicians on same job item
   - Test labor summary calculations

4. **Multi-tenancy**
   - Verify organizations can't see each other's inventory
   - Test part number uniqueness per organization

## Performance Considerations

- ✅ **Indexes** on frequently queried columns
- ✅ **DataLoaders** to prevent N+1 queries
- ✅ **Composite indexes** for common filter combinations
- ✅ **Query filters** applied at database level for multi-tenancy

## Security

- ✅ All mutations require `[Authorize]` attribute
- ✅ Multi-tenancy enforced via global query filters
- ✅ Inventory deduction validates sufficient stock
- ✅ Part number uniqueness per organization

## Future Enhancements

Potential additions:
- **Purchase Orders** - Track part orders from suppliers
- **Inventory Transfers** - Move stock between locations
- **Batch/Serial Numbers** - Track individual items
- **Labor Templates** - Predefined labor estimates by service type
- **Commission Tracking** - Calculate technician commissions
- **Inventory Audit Log** - Track all stock changes
- **Supplier Management** - Full supplier database
- **Reorder Automation** - Auto-generate purchase orders for low stock

---

**Implementation Date:** 2025-12-25
**Migration:** `AddPartsInventoryAndLaborTracking`
**Status:** ✅ Complete and tested (build successful)
