# Appointments Module - Frontend Integration Guide

This document provides the technical details for integrating the Appointments module into the frontend (Angular/Flutter).

## Overview

The Appointments module handles scheduling, availability checking, and status tracking for vehicle services. It is fully multi-tenant and requires authentication.

## Enums

### AppointmentStatus
| Value | Name | Description |
| :--- | :--- | :--- |
| 0 | `SCHEDULED` | Initial state when created |
| 1 | `CONFIRMED` | Customer or staff confirmed |
| 2 | `CHECKED_IN` | Vehicle arrived at garage |
| 3 | `IN_PROGRESS` | Service is being performed |
| 4 | `COMPLETED` | Service finished |
| 5 | `NO_SHOW` | Customer did not arrive |
| 6 | `CANCELLED` | Appointment was cancelled |

### AppointmentType
| Value | Name |
| :--- | :--- |
| 0 | `GENERAL_SERVICE` |
| 1 | `OIL_CHANGE` |
| 2 | `BRAKE_SERVICE` |
| 3 | `TIRE_CHANGE` |
| 4 | `INSPECTION` |
| 5 | `DIAGNOSIS` |
| 6 | `REPAIR` |
| 7 | `CONSULTATION` |
| 8 | `AIR_CONDITIONING_SERVICE` |
| 9 | `BATTERY_REPLACEMENT` |
| 10 | `ENGINE_REPAIR` |
| 11 | `TRANSMISSION_SERVICE` |
| 99 | `OTHER` |

---

## Queries

### 1. Get All Appointments
Supports filtering and sorting via HotChocolate `UseFiltering` and `UseSorting`.

```graphql
query GetAppointments($where: AppointmentFilterInput, $order: [AppointmentSortInput!]) {
  appointments(where: $where, order: $order) {
    id
    scheduledStartTime
    scheduledEndTime
    status
    type
    serviceRequested
    customer {
      id
      name
    }
    car {
      id
      licensePlate
      make
      model
    }
    assignedTechnician {
      id
      fullName
    }
  }
}
```

### 2. Get Appointment By ID
```graphql
query GetAppointment($id: UUID!) {
  appointmentById(id: $id) {
    id
    scheduledStartTime
    scheduledEndTime
    status
    type
    serviceRequested
    customerNotes
    internalNotes
    contactPhone
    contactEmail
    customer {
      id
      name
    }
    car {
      id
      licensePlate
    }
    assignedTechnician {
      id
      fullName
    }
    session {
      id
      status
    }
  }
}
```

### 3. Get Available Slots
Used to find free time for a specific date.

```graphql
query GetAvailableSlots($date: DateTime!, $durationMinutes: Int!, $organizationId: UUID!, $technicianId: String) {
  availableSlots(
    date: $date, 
    durationMinutes: $durationMinutes, 
    organizationId: $organizationId, 
    technicianId: $technicianId
  )
}
```

### 4. Get Customer Upcoming Appointments
```graphql
query GetCustomerUpcoming($customerId: UUID!) {
  customerUpcomingAppointments(customerId: $customerId) {
    id
    scheduledStartTime
    type
    status
  }
}
```

---

## Mutations

### 1. Create Appointment
```graphql
mutation CreateAppointment($input: CreateAppointmentInput!) {
  createAppointment(input: $input) {
    appointment {
      id
      scheduledStartTime
    }
    error
  }
}

# Input Example:
# {
#   "input": {
#     "customerId": "...",
#     "carId": "...",
#     "scheduledStartTime": "2025-01-15T10:00:00Z",
#     "scheduledEndTime": "2025-01-15T11:00:00Z",
#     "type": "OIL_CHANGE",
#     "serviceRequested": "Full synthetic oil change",
#     "estimatedDurationMinutes": 60
#   }
# }
```

### 2. Update Appointment
```graphql
mutation UpdateAppointment($id: UUID!, $input: UpdateAppointmentInput!) {
  updateAppointment(id: $id, input: $input) {
    appointment {
      id
      scheduledStartTime
      serviceRequested
    }
    error
  }
}
```

### 3. Update Appointment Status
Used for confirming, checking in, or cancelling.

```graphql
mutation UpdateStatus($id: UUID!, $status: AppointmentStatus!, $reason: String) {
  updateAppointmentStatus(id: $id, status: $status, cancellationReason: $reason) {
    appointment {
      id
      status
    }
    error
  }
}
```

### 4. Convert to Session
When a vehicle arrives, convert the appointment into an active garage session.

```graphql
mutation ConvertToSession($id: UUID!) {
  convertToSession(id: $id) {
    appointment {
      id
      status
      session {
        id
      }
    }
    error
  }
}
```

### 5. Delete Appointment
Requires `OrgAdmin` or `OrgManager` role.

```graphql
mutation DeleteAppointment($id: UUID!) {
  deleteAppointment(id: $id)
}
```

---

## Implementation Notes

1. **Timezones**: All `DateTime` fields are handled in UTC. Ensure the frontend converts to local time for display.
2. **Validation**: 
   - Start time must be in the future.
   - Start time must be before end time.
   - The system automatically checks for technician availability and customer conflicts.
3. **Multi-tenancy**: The `organizationId` is automatically handled by the backend based on the authenticated user's context, except for `availableSlots` which requires an explicit `organizationId` (useful for public booking pages if implemented).
4. **Navigation**: Appointments are linked to `Customer`, `Car`, and `ApplicationUser` (Technician). Always include the necessary fields in your GraphQL selection set to avoid extra round-trips.
