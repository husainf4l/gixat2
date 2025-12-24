# Appointments Module - Frontend Implementation Guide

## Overview
Complete guide for implementing the appointment scheduling system in your frontend application. This covers booking appointments, managing schedules, technician assignment, and converting appointments to garage sessions.

---

## Table of Contents
1. [GraphQL Schema & Types](#graphql-schema--types)
2. [Core Queries](#core-queries)
3. [Core Mutations](#core-mutations)
4. [UI Implementation Patterns](#ui-implementation-patterns)
5. [State Management](#state-management)
6. [Component Architecture](#component-architecture)
7. [Best Practices](#best-practices)

---

## GraphQL Schema & Types

### Enums

```typescript
enum AppointmentStatus {
  SCHEDULED = 0,
  CONFIRMED = 1,
  CHECKED_IN = 2,
  IN_PROGRESS = 3,
  COMPLETED = 4,
  NO_SHOW = 5,
  CANCELLED = 6
}

enum AppointmentType {
  GENERAL_SERVICE = 0,
  OIL_CHANGE = 1,
  BRAKE_SERVICE = 2,
  TIRE_CHANGE = 3,
  INSPECTION = 4,
  DIAGNOSIS = 5,
  REPAIR = 6,
  CONSULTATION = 7,
  AIR_CONDITIONING_SERVICE = 8,
  BATTERY_REPLACEMENT = 9,
  ENGINE_REPAIR = 10,
  TRANSMISSION_SERVICE = 11,
  OTHER = 99
}
```

### Core Types

```typescript
interface Appointment {
  id: string;
  organizationId: string;
  customerId: string;
  customer: Customer;
  carId: string;
  car: Car;
  scheduledStartTime: string; // ISO 8601
  scheduledEndTime: string;   // ISO 8601
  assignedTechnicianId?: string;
  assignedTechnician?: User;
  status: AppointmentStatus;
  type: AppointmentType;
  serviceRequested?: string;
  customerNotes?: string;
  internalNotes?: string;
  sessionId?: string;
  session?: GarageSession;
  reminderSent: boolean;
  reminderSentAt?: string;
  contactPhone?: string;
  contactEmail?: string;
  estimatedDurationMinutes: number;
  cancelledAt?: string;
  cancellationReason?: string;
  createdAt: string;
  updatedAt: string;
  createdById?: string;
  createdBy?: User;
}
```

---

## Core Queries

### 1. Get All Appointments (with Filtering)

```graphql
query GetAppointments(
  $where: AppointmentFilterInput
  $order: [AppointmentSortInput!]
) {
  appointments(where: $where, order: $order) {
    id
    scheduledStartTime
    scheduledEndTime
    status
    type
    serviceRequested
    estimatedDurationMinutes
    customer {
      id
      firstName
      lastName
      phoneNumber
      email
    }
    car {
      id
      make
      model
      year
      plateNumber
    }
    assignedTechnician {
      id
      fullName
      email
    }
  }
}
```

**TypeScript Implementation:**

```typescript
import { useQuery, gql } from '@apollo/client';

const GET_APPOINTMENTS = gql`
  query GetAppointments($where: AppointmentFilterInput, $order: [AppointmentSortInput!]) {
    appointments(where: $where, order: $order) {
      id
      scheduledStartTime
      scheduledEndTime
      status
      type
      serviceRequested
      estimatedDurationMinutes
      customer {
        id
        firstName
        lastName
        phoneNumber
      }
      car {
        id
        make
        model
        plateNumber
      }
      assignedTechnician {
        id
        fullName
      }
    }
  }
`;

function AppointmentsList() {
  const { data, loading, error } = useQuery(GET_APPOINTMENTS, {
    variables: {
      where: {
        status: { eq: 'SCHEDULED' }
      },
      order: [{ scheduledStartTime: 'ASC' }]
    }
  });

  if (loading) return <LoadingSpinner />;
  if (error) return <ErrorMessage error={error} />;

  return (
    <div>
      {data.appointments.map(appointment => (
        <AppointmentCard key={appointment.id} appointment={appointment} />
      ))}
    </div>
  );
}
```

### 2. Get Appointments by Date Range

```graphql
query GetAppointmentsByDateRange(
  $startDate: DateTime!
  $endDate: DateTime!
) {
  appointmentsByDateRange(startDate: $startDate, endDate: $endDate) {
    id
    scheduledStartTime
    scheduledEndTime
    status
    type
    customer {
      id
      firstName
      lastName
    }
    car {
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

**TypeScript Implementation (Calendar View):**

```typescript
const GET_APPOINTMENTS_BY_DATE_RANGE = gql`
  query GetAppointmentsByDateRange($startDate: DateTime!, $endDate: DateTime!) {
    appointmentsByDateRange(startDate: $startDate, endDate: $endDate) {
      id
      scheduledStartTime
      scheduledEndTime
      status
      type
      customer {
        firstName
        lastName
      }
      car {
        make
        model
      }
      assignedTechnician {
        fullName
      }
    }
  }
`;

function AppointmentCalendar() {
  const [selectedDate, setSelectedDate] = useState(new Date());
  
  const startDate = startOfWeek(selectedDate);
  const endDate = endOfWeek(selectedDate);

  const { data, loading } = useQuery(GET_APPOINTMENTS_BY_DATE_RANGE, {
    variables: {
      startDate: startDate.toISOString(),
      endDate: endDate.toISOString()
    },
    pollInterval: 30000 // Refresh every 30 seconds
  });

  return (
    <Calendar
      events={data?.appointmentsByDateRange || []}
      onDateChange={setSelectedDate}
    />
  );
}
```

### 3. Get Appointments by Customer

```graphql
query GetCustomerAppointments($customerId: UUID!) {
  appointmentsByCustomer(customerId: $customerId) {
    id
    scheduledStartTime
    scheduledEndTime
    status
    type
    serviceRequested
    car {
      make
      model
      plateNumber
    }
    assignedTechnician {
      fullName
    }
  }
}
```

### 4. Get Available Time Slots

```graphql
query GetAvailableSlots(
  $date: DateTime!
  $durationMinutes: Int!
  $technicianId: String
  $organizationId: UUID!
) {
  availableSlots(
    date: $date
    durationMinutes: $durationMinutes
    technicianId: $technicianId
    organizationId: $organizationId
  )
}
```

**TypeScript Implementation:**

```typescript
const GET_AVAILABLE_SLOTS = gql`
  query GetAvailableSlots(
    $date: DateTime!
    $durationMinutes: Int!
    $technicianId: String
    $organizationId: UUID!
  ) {
    availableSlots(
      date: $date
      durationMinutes: $durationMinutes
      technicianId: $technicianId
      organizationId: $organizationId
    )
  }
`;

function TimeSlotPicker({ 
  selectedDate, 
  duration, 
  technicianId, 
  organizationId,
  onSelectSlot 
}: TimeSlotPickerProps) {
  const { data, loading } = useQuery(GET_AVAILABLE_SLOTS, {
    variables: {
      date: selectedDate.toISOString(),
      durationMinutes: duration,
      technicianId,
      organizationId
    },
    skip: !selectedDate
  });

  if (loading) return <Skeleton />;

  return (
    <div className="grid grid-cols-4 gap-2">
      {data?.availableSlots.map((slot: string) => (
        <button
          key={slot}
          onClick={() => onSelectSlot(new Date(slot))}
          className="p-2 border rounded hover:bg-blue-50"
        >
          {format(new Date(slot), 'HH:mm')}
        </button>
      ))}
    </div>
  );
}
```

### 5. Get Single Appointment by ID

```graphql
query GetAppointmentById($id: UUID!) {
  appointmentById(id: $id) {
    id
    scheduledStartTime
    scheduledEndTime
    status
    type
    serviceRequested
    customerNotes
    internalNotes
    estimatedDurationMinutes
    contactPhone
    contactEmail
    reminderSent
    reminderSentAt
    cancelledAt
    cancellationReason
    customer {
      id
      firstName
      lastName
      phoneNumber
      email
    }
    car {
      id
      make
      model
      year
      plateNumber
      vin
    }
    assignedTechnician {
      id
      fullName
      email
    }
    session {
      id
      status
    }
    createdAt
    updatedAt
  }
}
```

---

## Core Mutations

### 1. Create Appointment

```graphql
mutation CreateAppointment($input: CreateAppointmentInput!) {
  createAppointment(input: $input) {
    appointment {
      id
      scheduledStartTime
      scheduledEndTime
      status
      customer {
        firstName
        lastName
      }
      car {
        make
        model
      }
    }
    error
  }
}
```

**Input Type:**

```typescript
interface CreateAppointmentInput {
  customerId: string;
  carId: string;
  scheduledStartTime: string; // ISO 8601
  scheduledEndTime: string;   // ISO 8601
  assignedTechnicianId?: string;
  type: AppointmentType;
  serviceRequested?: string;
  customerNotes?: string;
  internalNotes?: string;
  estimatedDurationMinutes: number;
  contactPhone?: string;
  contactEmail?: string;
}
```

**TypeScript Implementation:**

```typescript
const CREATE_APPOINTMENT = gql`
  mutation CreateAppointment($input: CreateAppointmentInput!) {
    createAppointment(input: $input) {
      appointment {
        id
        scheduledStartTime
        scheduledEndTime
        status
        customer {
          firstName
          lastName
        }
        car {
          make
          model
        }
      }
      error
    }
  }
`;

function BookAppointmentForm() {
  const [createAppointment, { loading }] = useMutation(CREATE_APPOINTMENT, {
    refetchQueries: ['GetAppointments'],
    onCompleted: (data) => {
      if (data.createAppointment.error) {
        toast.error(data.createAppointment.error);
      } else {
        toast.success('Appointment booked successfully!');
        router.push(`/appointments/${data.createAppointment.appointment.id}`);
      }
    }
  });

  const handleSubmit = async (formData: AppointmentFormData) => {
    await createAppointment({
      variables: {
        input: {
          customerId: formData.customerId,
          carId: formData.carId,
          scheduledStartTime: formData.startTime.toISOString(),
          scheduledEndTime: formData.endTime.toISOString(),
          assignedTechnicianId: formData.technicianId,
          type: formData.type,
          serviceRequested: formData.service,
          customerNotes: formData.notes,
          estimatedDurationMinutes: formData.duration,
          contactPhone: formData.phone,
          contactEmail: formData.email
        }
      }
    });
  };

  return <AppointmentForm onSubmit={handleSubmit} loading={loading} />;
}
```

### 2. Update Appointment

```graphql
mutation UpdateAppointment($input: UpdateAppointmentInput!) {
  updateAppointment(input: $input) {
    appointment {
      id
      scheduledStartTime
      scheduledEndTime
      status
      type
      serviceRequested
    }
    error
  }
}
```

**Input Type:**

```typescript
interface UpdateAppointmentInput {
  id: string;
  scheduledStartTime?: string;
  scheduledEndTime?: string;
  assignedTechnicianId?: string;
  type?: AppointmentType;
  serviceRequested?: string;
  customerNotes?: string;
  internalNotes?: string;
  estimatedDurationMinutes?: number;
  contactPhone?: string;
  contactEmail?: string;
}
```

### 3. Update Appointment Status

```graphql
mutation UpdateAppointmentStatus($input: UpdateAppointmentStatusInput!) {
  updateAppointmentStatus(input: $input) {
    appointment {
      id
      status
      updatedAt
    }
    error
  }
}
```

**TypeScript Implementation (Status Workflow):**

```typescript
const UPDATE_APPOINTMENT_STATUS = gql`
  mutation UpdateAppointmentStatus($input: UpdateAppointmentStatusInput!) {
    updateAppointmentStatus(input: $input) {
      appointment {
        id
        status
        updatedAt
      }
      error
    }
  }
`;

function AppointmentStatusFlow({ appointment }: { appointment: Appointment }) {
  const [updateStatus] = useMutation(UPDATE_APPOINTMENT_STATUS);

  const handleStatusChange = async (newStatus: AppointmentStatus) => {
    const { data } = await updateStatus({
      variables: {
        input: {
          appointmentId: appointment.id,
          status: newStatus
        }
      }
    });

    if (data.updateAppointmentStatus.error) {
      toast.error(data.updateAppointmentStatus.error);
    }
  };

  // Status workflow buttons
  const getNextActions = () => {
    switch (appointment.status) {
      case AppointmentStatus.SCHEDULED:
        return [
          { label: 'Confirm', status: AppointmentStatus.CONFIRMED, color: 'green' },
          { label: 'Cancel', status: AppointmentStatus.CANCELLED, color: 'red' }
        ];
      case AppointmentStatus.CONFIRMED:
        return [
          { label: 'Check In', status: AppointmentStatus.CHECKED_IN, color: 'blue' },
          { label: 'No Show', status: AppointmentStatus.NO_SHOW, color: 'orange' }
        ];
      case AppointmentStatus.CHECKED_IN:
        return [
          { label: 'Start Service', status: AppointmentStatus.IN_PROGRESS, color: 'purple' }
        ];
      case AppointmentStatus.IN_PROGRESS:
        return [
          { label: 'Complete', status: AppointmentStatus.COMPLETED, color: 'green' }
        ];
      default:
        return [];
    }
  };

  return (
    <div className="flex gap-2">
      {getNextActions().map(action => (
        <button
          key={action.status}
          onClick={() => handleStatusChange(action.status)}
          className={`px-4 py-2 rounded bg-${action.color}-500 text-white`}
        >
          {action.label}
        </button>
      ))}
    </div>
  );
}
```

### 4. Convert Appointment to Session

```graphql
mutation ConvertToSession($appointmentId: UUID!) {
  convertToSession(appointmentId: $appointmentId) {
    appointment {
      id
      status
      sessionId
      session {
        id
        status
      }
    }
    error
  }
}
```

**TypeScript Implementation:**

```typescript
const CONVERT_TO_SESSION = gql`
  mutation ConvertToSession($appointmentId: UUID!) {
    convertToSession(appointmentId: $appointmentId) {
      appointment {
        id
        status
        sessionId
      }
      error
    }
  }
`;

function ConvertToSessionButton({ appointmentId }: { appointmentId: string }) {
  const [convertToSession, { loading }] = useMutation(CONVERT_TO_SESSION, {
    onCompleted: (data) => {
      if (data.convertToSession.error) {
        toast.error(data.convertToSession.error);
      } else {
        toast.success('Converted to garage session!');
        router.push(`/sessions/${data.convertToSession.appointment.sessionId}`);
      }
    }
  });

  return (
    <button
      onClick={() => convertToSession({ variables: { appointmentId } })}
      disabled={loading}
      className="px-4 py-2 bg-blue-600 text-white rounded"
    >
      {loading ? 'Converting...' : 'Start Garage Session'}
    </button>
  );
}
```

### 5. Delete Appointment

```graphql
mutation DeleteAppointment($id: UUID!) {
  deleteAppointment(id: $id) {
    success
    error
  }
}
```

---

## UI Implementation Patterns

### 1. Appointment Booking Flow

```typescript
// components/appointments/BookingWizard.tsx
import { useState } from 'react';

type Step = 'customer' | 'car' | 'service' | 'datetime' | 'confirm';

export function BookingWizard() {
  const [step, setStep] = useState<Step>('customer');
  const [formData, setFormData] = useState({
    customerId: '',
    carId: '',
    type: AppointmentType.GENERAL_SERVICE,
    serviceRequested: '',
    scheduledStartTime: null as Date | null,
    scheduledEndTime: null as Date | null,
    assignedTechnicianId: '',
    duration: 60,
    notes: ''
  });

  const steps = {
    customer: <CustomerSelector 
      onSelect={(id) => {
        setFormData(prev => ({ ...prev, customerId: id }));
        setStep('car');
      }} 
    />,
    car: <CarSelector 
      customerId={formData.customerId}
      onSelect={(id) => {
        setFormData(prev => ({ ...prev, carId: id }));
        setStep('service');
      }}
    />,
    service: <ServiceTypeSelector
      onSelect={(type, service, duration) => {
        setFormData(prev => ({ ...prev, type, serviceRequested: service, duration }));
        setStep('datetime');
      }}
    />,
    datetime: <DateTimeSlotPicker
      duration={formData.duration}
      onSelect={(start, end, techId) => {
        setFormData(prev => ({
          ...prev,
          scheduledStartTime: start,
          scheduledEndTime: end,
          assignedTechnicianId: techId
        }));
        setStep('confirm');
      }}
    />,
    confirm: <ConfirmationStep
      data={formData}
      onConfirm={handleBooking}
    />
  };

  const handleBooking = async () => {
    // Call createAppointment mutation
  };

  return (
    <div className="max-w-2xl mx-auto">
      <StepIndicator currentStep={step} />
      {steps[step]}
    </div>
  );
}
```

### 2. Calendar View with Drag & Drop

```typescript
// components/appointments/AppointmentCalendar.tsx
import FullCalendar from '@fullcalendar/react';
import timeGridPlugin from '@fullcalendar/timegrid';
import interactionPlugin from '@fullcalendar/interaction';

export function AppointmentCalendar() {
  const { data } = useQuery(GET_APPOINTMENTS_BY_DATE_RANGE, {
    variables: {
      startDate: startOfWeek(new Date()).toISOString(),
      endDate: endOfWeek(new Date()).toISOString()
    }
  });

  const [updateAppointment] = useMutation(UPDATE_APPOINTMENT);

  const events = data?.appointmentsByDateRange.map(apt => ({
    id: apt.id,
    title: `${apt.customer.firstName} ${apt.customer.lastName} - ${apt.car.make} ${apt.car.model}`,
    start: apt.scheduledStartTime,
    end: apt.scheduledEndTime,
    backgroundColor: getStatusColor(apt.status),
    extendedProps: {
      status: apt.status,
      type: apt.type,
      technicianId: apt.assignedTechnician?.id
    }
  }));

  const handleEventDrop = async (info: any) => {
    await updateAppointment({
      variables: {
        input: {
          id: info.event.id,
          scheduledStartTime: info.event.start.toISOString(),
          scheduledEndTime: info.event.end.toISOString()
        }
      }
    });
  };

  return (
    <FullCalendar
      plugins={[timeGridPlugin, interactionPlugin]}
      initialView="timeGridWeek"
      events={events}
      editable={true}
      eventDrop={handleEventDrop}
      eventClick={(info) => router.push(`/appointments/${info.event.id}`)}
      slotMinTime="08:00:00"
      slotMaxTime="18:00:00"
      allDaySlot={false}
      headerToolbar={{
        left: 'prev,next today',
        center: 'title',
        right: 'timeGridWeek,timeGridDay'
      }}
    />
  );
}

function getStatusColor(status: AppointmentStatus): string {
  const colors = {
    [AppointmentStatus.SCHEDULED]: '#3B82F6',
    [AppointmentStatus.CONFIRMED]: '#10B981',
    [AppointmentStatus.CHECKED_IN]: '#8B5CF6',
    [AppointmentStatus.IN_PROGRESS]: '#F59E0B',
    [AppointmentStatus.COMPLETED]: '#6B7280',
    [AppointmentStatus.NO_SHOW]: '#EF4444',
    [AppointmentStatus.CANCELLED]: '#9CA3AF'
  };
  return colors[status];
}
```

### 3. Real-time Dashboard

```typescript
// components/appointments/Dashboard.tsx
export function AppointmentDashboard() {
  const today = startOfDay(new Date());
  const tomorrow = addDays(today, 1);

  const { data: todayAppointments } = useQuery(GET_APPOINTMENTS_BY_DATE_RANGE, {
    variables: {
      startDate: today.toISOString(),
      endDate: tomorrow.toISOString()
    },
    pollInterval: 10000 // Refresh every 10 seconds
  });

  const stats = useMemo(() => {
    const appointments = todayAppointments?.appointmentsByDateRange || [];
    return {
      total: appointments.length,
      confirmed: appointments.filter(a => a.status === AppointmentStatus.CONFIRMED).length,
      checkedIn: appointments.filter(a => a.status === AppointmentStatus.CHECKED_IN).length,
      inProgress: appointments.filter(a => a.status === AppointmentStatus.IN_PROGRESS).length,
      completed: appointments.filter(a => a.status === AppointmentStatus.COMPLETED).length,
      noShow: appointments.filter(a => a.status === AppointmentStatus.NO_SHOW).length
    };
  }, [todayAppointments]);

  return (
    <div className="grid grid-cols-3 gap-6">
      <StatCard label="Total Today" value={stats.total} icon={<CalendarIcon />} />
      <StatCard label="Confirmed" value={stats.confirmed} icon={<CheckIcon />} color="green" />
      <StatCard label="In Progress" value={stats.inProgress} icon={<WrenchIcon />} color="blue" />
      
      <div className="col-span-3">
        <TodaySchedule appointments={todayAppointments?.appointmentsByDateRange} />
      </div>
    </div>
  );
}
```

### 4. Mobile-Friendly Time Slot Picker

```typescript
// components/appointments/MobileSlotPicker.tsx
export function MobileSlotPicker({ 
  date, 
  duration, 
  onSelect 
}: MobileSlotPickerProps) {
  const { data } = useQuery(GET_AVAILABLE_SLOTS, {
    variables: {
      date: date.toISOString(),
      durationMinutes: duration,
      organizationId: currentOrg.id
    }
  });

  const groupedSlots = useMemo(() => {
    const slots = data?.availableSlots || [];
    return {
      morning: slots.filter(s => new Date(s).getHours() < 12),
      afternoon: slots.filter(s => {
        const hour = new Date(s).getHours();
        return hour >= 12 && hour < 17;
      }),
      evening: slots.filter(s => new Date(s).getHours() >= 17)
    };
  }, [data]);

  return (
    <div className="space-y-6">
      <SlotGroup 
        title="Morning" 
        icon="🌅" 
        slots={groupedSlots.morning} 
        onSelect={onSelect} 
      />
      <SlotGroup 
        title="Afternoon" 
        icon="☀️" 
        slots={groupedSlots.afternoon} 
        onSelect={onSelect} 
      />
      <SlotGroup 
        title="Evening" 
        icon="🌙" 
        slots={groupedSlots.evening} 
        onSelect={onSelect} 
      />
    </div>
  );
}

function SlotGroup({ title, icon, slots, onSelect }) {
  if (slots.length === 0) return null;

  return (
    <div>
      <h3 className="text-lg font-semibold mb-3">
        {icon} {title}
      </h3>
      <div className="grid grid-cols-3 gap-2">
        {slots.map((slot: string) => (
          <button
            key={slot}
            onClick={() => onSelect(slot)}
            className="p-3 border-2 rounded-lg active:scale-95 transition"
          >
            {format(new Date(slot), 'HH:mm')}
          </button>
        ))}
      </div>
    </div>
  );
}
```

---

## State Management

### Redux Toolkit Example

```typescript
// store/appointmentsSlice.ts
import { createSlice, PayloadAction } from '@reduxjs/toolkit';

interface AppointmentsState {
  selectedDate: string;
  filterStatus: AppointmentStatus | 'ALL';
  filterTechnician: string | null;
  viewMode: 'calendar' | 'list' | 'board';
}

const initialState: AppointmentsState = {
  selectedDate: new Date().toISOString(),
  filterStatus: 'ALL',
  filterTechnician: null,
  viewMode: 'calendar'
};

export const appointmentsSlice = createSlice({
  name: 'appointments',
  initialState,
  reducers: {
    setSelectedDate: (state, action: PayloadAction<string>) => {
      state.selectedDate = action.payload;
    },
    setFilterStatus: (state, action: PayloadAction<AppointmentStatus | 'ALL'>) => {
      state.filterStatus = action.payload;
    },
    setFilterTechnician: (state, action: PayloadAction<string | null>) => {
      state.filterTechnician = action.payload;
    },
    setViewMode: (state, action: PayloadAction<'calendar' | 'list' | 'board'>) => {
      state.viewMode = action.payload;
    }
  }
});

export const { 
  setSelectedDate, 
  setFilterStatus, 
  setFilterTechnician, 
  setViewMode 
} = appointmentsSlice.actions;
```

### Zustand Example (Lighter Alternative)

```typescript
// store/useAppointmentStore.ts
import create from 'zustand';

interface AppointmentStore {
  selectedAppointment: Appointment | null;
  isBookingModalOpen: boolean;
  setSelectedAppointment: (apt: Appointment | null) => void;
  openBookingModal: () => void;
  closeBookingModal: () => void;
}

export const useAppointmentStore = create<AppointmentStore>((set) => ({
  selectedAppointment: null,
  isBookingModalOpen: false,
  setSelectedAppointment: (apt) => set({ selectedAppointment: apt }),
  openBookingModal: () => set({ isBookingModalOpen: true }),
  closeBookingModal: () => set({ isBookingModalOpen: false })
}));
```

---

## Component Architecture

### Recommended Folder Structure

```
src/
├── features/
│   └── appointments/
│       ├── components/
│       │   ├── AppointmentCard.tsx
│       │   ├── AppointmentCalendar.tsx
│       │   ├── AppointmentList.tsx
│       │   ├── BookingWizard/
│       │   │   ├── index.tsx
│       │   │   ├── CustomerStep.tsx
│       │   │   ├── CarStep.tsx
│       │   │   ├── ServiceStep.tsx
│       │   │   ├── DateTimeStep.tsx
│       │   │   └── ConfirmationStep.tsx
│       │   ├── TimeSlotPicker.tsx
│       │   ├── StatusBadge.tsx
│       │   └── AppointmentDetails.tsx
│       ├── hooks/
│       │   ├── useAppointments.ts
│       │   ├── useAvailableSlots.ts
│       │   ├── useCreateAppointment.ts
│       │   └── useUpdateAppointment.ts
│       ├── graphql/
│       │   ├── queries.ts
│       │   ├── mutations.ts
│       │   └── fragments.ts
│       ├── types/
│       │   └── index.ts
│       └── utils/
│           ├── statusHelpers.ts
│           ├── dateHelpers.ts
│           └── validators.ts
```

### Custom Hooks

```typescript
// hooks/useAppointments.ts
export function useAppointments(filters?: AppointmentFilters) {
  const { data, loading, error, refetch } = useQuery(GET_APPOINTMENTS, {
    variables: {
      where: buildWhereClause(filters),
      order: [{ scheduledStartTime: 'ASC' }]
    }
  });

  return {
    appointments: data?.appointments || [],
    loading,
    error,
    refetch
  };
}

// hooks/useCreateAppointment.ts
export function useCreateAppointment() {
  const [mutate, { loading }] = useMutation(CREATE_APPOINTMENT, {
    refetchQueries: ['GetAppointments'],
    onCompleted: (data) => {
      if (data.createAppointment.error) {
        toast.error(data.createAppointment.error);
      } else {
        toast.success('Appointment created!');
      }
    }
  });

  const createAppointment = useCallback(async (input: CreateAppointmentInput) => {
    return await mutate({ variables: { input } });
  }, [mutate]);

  return { createAppointment, loading };
}

// hooks/useAvailableSlots.ts
export function useAvailableSlots(
  date: Date | null,
  duration: number,
  technicianId?: string
) {
  const { organizationId } = useAuth();

  return useQuery(GET_AVAILABLE_SLOTS, {
    variables: {
      date: date?.toISOString(),
      durationMinutes: duration,
      technicianId,
      organizationId
    },
    skip: !date || !organizationId
  });
}
```

---

## Best Practices

### 1. Optimistic UI Updates

```typescript
const [updateStatus] = useMutation(UPDATE_APPOINTMENT_STATUS, {
  optimisticResponse: (vars) => ({
    updateAppointmentStatus: {
      __typename: 'AppointmentPayload',
      appointment: {
        __typename: 'Appointment',
        id: vars.input.appointmentId,
        status: vars.input.status,
        updatedAt: new Date().toISOString()
      },
      error: null
    }
  }),
  update: (cache, { data }) => {
    if (data?.updateAppointmentStatus.appointment) {
      cache.modify({
        id: cache.identify({
          __typename: 'Appointment',
          id: data.updateAppointmentStatus.appointment.id
        }),
        fields: {
          status: () => data.updateAppointmentStatus.appointment.status
        }
      });
    }
  }
});
```

### 2. Error Handling

```typescript
function AppointmentForm() {
  const [createAppointment, { loading, error }] = useMutation(CREATE_APPOINTMENT);

  const handleSubmit = async (data: FormData) => {
    try {
      const result = await createAppointment({ variables: { input: data } });
      
      if (result.data?.createAppointment.error) {
        // Business logic error from backend
        toast.error(result.data.createAppointment.error);
        return;
      }

      toast.success('Appointment created!');
      router.push('/appointments');
    } catch (err) {
      // Network or GraphQL error
      console.error('Failed to create appointment:', err);
      toast.error('Failed to create appointment. Please try again.');
    }
  };

  return (
    <form onSubmit={handleSubmit}>
      {/* form fields */}
      {error && <ErrorAlert error={error} />}
      <button type="submit" disabled={loading}>
        {loading ? 'Creating...' : 'Create Appointment'}
      </button>
    </form>
  );
}
```

### 3. Real-time Updates with Subscriptions (If Implemented)

```typescript
// If you add GraphQL subscriptions later
const APPOINTMENT_UPDATED_SUBSCRIPTION = gql`
  subscription OnAppointmentUpdated($organizationId: UUID!) {
    appointmentUpdated(organizationId: $organizationId) {
      id
      status
      updatedAt
    }
  }
`;

function useAppointmentSubscription() {
  const { organizationId } = useAuth();
  
  useSubscription(APPOINTMENT_UPDATED_SUBSCRIPTION, {
    variables: { organizationId },
    onSubscriptionData: ({ client, subscriptionData }) => {
      const updated = subscriptionData.data?.appointmentUpdated;
      if (updated) {
        // Update cache
        client.cache.modify({
          id: client.cache.identify({ __typename: 'Appointment', id: updated.id }),
          fields: {
            status: () => updated.status,
            updatedAt: () => updated.updatedAt
          }
        });
        
        toast.info(`Appointment ${updated.id} was updated`);
      }
    }
  });
}
```

### 4. Accessibility

```typescript
// Ensure keyboard navigation and screen reader support
function TimeSlotButton({ slot, onSelect }: TimeSlotButtonProps) {
  return (
    <button
      onClick={() => onSelect(slot)}
      onKeyDown={(e) => {
        if (e.key === 'Enter' || e.key === ' ') {
          onSelect(slot);
        }
      }}
      aria-label={`Book appointment at ${format(new Date(slot), 'h:mm a')}`}
      className="p-2 border rounded focus:ring-2 focus:ring-blue-500"
    >
      {format(new Date(slot), 'HH:mm')}
    </button>
  );
}
```

### 5. Form Validation

```typescript
import { z } from 'zod';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';

const appointmentSchema = z.object({
  customerId: z.string().uuid('Select a customer'),
  carId: z.string().uuid('Select a car'),
  scheduledStartTime: z.date({
    required_error: 'Select start time'
  }).refine(date => date > new Date(), 'Cannot book in the past'),
  scheduledEndTime: z.date(),
  type: z.nativeEnum(AppointmentType),
  serviceRequested: z.string().max(1000).optional(),
  customerNotes: z.string().max(2000).optional(),
  estimatedDurationMinutes: z.number().min(15).max(480),
  contactPhone: z.string().regex(/^\+?[1-9]\d{1,14}$/).optional(),
  contactEmail: z.string().email().optional()
}).refine(
  data => data.scheduledEndTime > data.scheduledStartTime,
  { message: 'End time must be after start time', path: ['scheduledEndTime'] }
);

function AppointmentForm() {
  const { register, handleSubmit, formState: { errors } } = useForm({
    resolver: zodResolver(appointmentSchema)
  });

  return (
    <form onSubmit={handleSubmit(onSubmit)}>
      {/* form fields with error messages */}
      {errors.customerId && <span>{errors.customerId.message}</span>}
    </form>
  );
}
```

### 6. Performance Optimization

```typescript
// Pagination for large lists
function AppointmentsList() {
  const [page, setPage] = useState(0);
  const pageSize = 20;

  const { data, fetchMore } = useQuery(GET_APPOINTMENTS, {
    variables: {
      skip: page * pageSize,
      take: pageSize
    }
  });

  const loadMore = () => {
    fetchMore({
      variables: {
        skip: data.appointments.length
      },
      updateQuery: (prev, { fetchMoreResult }) => {
        if (!fetchMoreResult) return prev;
        return {
          appointments: [...prev.appointments, ...fetchMoreResult.appointments]
        };
      }
    });
  };

  return (
    <InfiniteScroll loadMore={loadMore}>
      {data?.appointments.map(apt => (
        <AppointmentCard key={apt.id} appointment={apt} />
      ))}
    </InfiniteScroll>
  );
}
```

### 7. Testing

```typescript
// __tests__/appointments/BookingWizard.test.tsx
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MockedProvider } from '@apollo/client/testing';
import { BookingWizard } from '../BookingWizard';

const mocks = [
  {
    request: {
      query: GET_AVAILABLE_SLOTS,
      variables: {
        date: '2025-12-26T00:00:00.000Z',
        durationMinutes: 60,
        organizationId: 'test-org-id'
      }
    },
    result: {
      data: {
        availableSlots: [
          '2025-12-26T09:00:00.000Z',
          '2025-12-26T10:00:00.000Z',
          '2025-12-26T11:00:00.000Z'
        ]
      }
    }
  }
];

test('booking wizard shows available slots', async () => {
  render(
    <MockedProvider mocks={mocks}>
      <BookingWizard />
    </MockedProvider>
  );

  // Navigate to datetime step
  fireEvent.click(screen.getByText('Next'));
  fireEvent.click(screen.getByText('Next'));
  fireEvent.click(screen.getByText('Next'));

  await waitFor(() => {
    expect(screen.getByText('09:00')).toBeInTheDocument();
    expect(screen.getByText('10:00')).toBeInTheDocument();
  });
});
```

---

## Common UI Patterns

### Status Badge Component

```typescript
export function StatusBadge({ status }: { status: AppointmentStatus }) {
  const config = {
    [AppointmentStatus.SCHEDULED]: { 
      label: 'Scheduled', 
      color: 'bg-blue-100 text-blue-800' 
    },
    [AppointmentStatus.CONFIRMED]: { 
      label: 'Confirmed', 
      color: 'bg-green-100 text-green-800' 
    },
    [AppointmentStatus.CHECKED_IN]: { 
      label: 'Checked In', 
      color: 'bg-purple-100 text-purple-800' 
    },
    [AppointmentStatus.IN_PROGRESS]: { 
      label: 'In Progress', 
      color: 'bg-yellow-100 text-yellow-800' 
    },
    [AppointmentStatus.COMPLETED]: { 
      label: 'Completed', 
      color: 'bg-gray-100 text-gray-800' 
    },
    [AppointmentStatus.NO_SHOW]: { 
      label: 'No Show', 
      color: 'bg-red-100 text-red-800' 
    },
    [AppointmentStatus.CANCELLED]: { 
      label: 'Cancelled', 
      color: 'bg-gray-100 text-gray-600' 
    }
  };

  const { label, color } = config[status];

  return (
    <span className={`px-2 py-1 rounded-full text-xs font-medium ${color}`}>
      {label}
    </span>
  );
}
```

### Appointment Type Icon

```typescript
export function AppointmentTypeIcon({ type }: { type: AppointmentType }) {
  const icons = {
    [AppointmentType.OIL_CHANGE]: '🛢️',
    [AppointmentType.BRAKE_SERVICE]: '🛑',
    [AppointmentType.TIRE_CHANGE]: '🛞',
    [AppointmentType.INSPECTION]: '🔍',
    [AppointmentType.DIAGNOSIS]: '🔧',
    [AppointmentType.REPAIR]: '⚙️',
    [AppointmentType.CONSULTATION]: '💬',
    [AppointmentType.AIR_CONDITIONING_SERVICE]: '❄️',
    [AppointmentType.BATTERY_REPLACEMENT]: '🔋',
    [AppointmentType.ENGINE_REPAIR]: '🏎️',
    [AppointmentType.TRANSMISSION_SERVICE]: '⚙️',
    [AppointmentType.GENERAL_SERVICE]: '🔧',
    [AppointmentType.OTHER]: '📝'
  };

  return <span className="text-2xl">{icons[type]}</span>;
}
```

---

## Summary

This implementation guide provides everything you need to integrate the Appointments module into your frontend:

1. **GraphQL Queries & Mutations** - Complete API reference
2. **TypeScript Types** - Type-safe implementation
3. **UI Components** - Calendar, forms, dashboards
4. **State Management** - Redux & Zustand examples
5. **Best Practices** - Error handling, validation, testing
6. **Performance** - Optimistic updates, pagination
7. **Accessibility** - Keyboard navigation, ARIA labels

**Key Features Implemented:**
- ✅ Booking appointments with conflict detection
- ✅ Time slot availability checking
- ✅ Technician assignment
- ✅ Status workflow (Scheduled → Confirmed → CheckedIn → InProgress → Completed)
- ✅ Calendar views with drag & drop
- ✅ Mobile-responsive design
- ✅ Real-time updates
- ✅ Convert appointments to garage sessions

**Next Steps:**
1. Implement booking wizard for customer portal
2. Create admin dashboard for scheduling
3. Add SMS/email reminder system (backend integration)
4. Build mobile app views
5. Add analytics and reporting

The backend is production-ready. Follow this guide to build a complete frontend experience!
