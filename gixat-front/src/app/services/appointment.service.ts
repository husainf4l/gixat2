import { Injectable, inject } from '@angular/core';
import { Apollo, gql } from 'apollo-angular';
import { Observable, map } from 'rxjs';

// Enums - Matching documentation
export enum AppointmentStatus {
  SCHEDULED = 0,
  CONFIRMED = 1,
  CHECKED_IN = 2,
  IN_PROGRESS = 3,
  COMPLETED = 4,
  NO_SHOW = 5,
  CANCELLED = 6
}

export enum AppointmentType {
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

export enum ReminderType {
  SMS = 'SMS',
  EMAIL = 'EMAIL',
  PUSH = 'PUSH'
}

// Interfaces
export interface AppointmentCustomer {
  id: string;
  name: string; // Documentation uses 'name' instead of firstName/lastName
}

export interface AppointmentCar {
  id: string;
  licensePlate: string;
  make: string;
  model: string;
}

export interface AppointmentTechnician {
  id: string;
  fullName: string;
}

export interface Session {
  id: string;
  status: string;
}

export interface Appointment {
  id: string;
  scheduledStartTime: string; // DateTime ISO 8601
  scheduledEndTime: string;   // DateTime ISO 8601
  status: AppointmentStatus;
  type: AppointmentType;
  serviceRequested?: string | null;
  customerNotes?: string | null;
  internalNotes?: string | null;
  contactPhone?: string | null;
  contactEmail?: string | null;
  customer: AppointmentCustomer;
  car: AppointmentCar;
  assignedTechnician?: AppointmentTechnician | null;
  session?: Session | null;
  createdAt: string;
  updatedAt: string;
}

export interface TimeSlot {
  startTime: string; // DateTime ISO 8601
  endTime: string;   // DateTime ISO 8601
  isAvailable: boolean;
}

export interface AppointmentsConnection {
  edges: {
    node: Appointment;
    cursor: string;
  }[];
  pageInfo: {
    hasNextPage: boolean;
    hasPreviousPage: boolean;
    startCursor: string | null;
    endCursor: string | null;
  };
  totalCount: number;
}

export interface CreateAppointmentInput {
  customerId: string;
  carId: string;
  scheduledStartTime: string; // DateTime ISO 8601
  scheduledEndTime: string;   // DateTime ISO 8601
  type: AppointmentType;
  serviceRequested?: string;
  estimatedDurationMinutes: number;
  assignedTechnicianId?: string;
  customerNotes?: string;
  internalNotes?: string;
  contactPhone?: string;
  contactEmail?: string;
}

export interface UpdateAppointmentInput {
  scheduledStartTime?: string;
  scheduledEndTime?: string;
  type?: AppointmentType;
  serviceRequested?: string;
  assignedTechnicianId?: string;
  customerNotes?: string;
  internalNotes?: string;
  contactPhone?: string;
  contactEmail?: string;
}

// GraphQL Fragments
const APPOINTMENT_FRAGMENT = gql`
  fragment AppointmentDetails on Appointment {
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
      make
      model
    }
    assignedTechnician {
      id
      fullName
    }
    session {
      id
      status
    }
    createdAt
    updatedAt
  }
`;

// GraphQL Queries - Matching documentation
const APPOINTMENTS_QUERY = gql`
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
`;

const APPOINTMENT_BY_ID_QUERY = gql`
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
`;

const AVAILABLE_SLOTS_QUERY = gql`
  query GetAvailableSlots($date: DateTime!, $durationMinutes: Int!, $organizationId: UUID!, $technicianId: String) {
    availableSlots(
      date: $date
      durationMinutes: $durationMinutes
      organizationId: $organizationId
      technicianId: $technicianId
    )
  }
`;

const CUSTOMER_UPCOMING_APPOINTMENTS_QUERY = gql`
  query GetCustomerUpcoming($customerId: UUID!) {
    customerUpcomingAppointments(customerId: $customerId) {
      id
      scheduledStartTime
      type
      status
    }
  }
`;

// GraphQL Mutations - Matching documentation
const CREATE_APPOINTMENT_MUTATION = gql`
  mutation CreateAppointment($input: CreateAppointmentInput!) {
    createAppointment(input: $input) {
      appointment {
        id
        scheduledStartTime
      }
      error
    }
  }
`;

const UPDATE_APPOINTMENT_MUTATION = gql`
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
`;

const UPDATE_APPOINTMENT_STATUS_MUTATION = gql`
  mutation UpdateStatus($id: UUID!, $status: AppointmentStatus!, $reason: String) {
    updateAppointmentStatus(id: $id, status: $status, cancellationReason: $reason) {
      appointment {
        id
        status
      }
      error
    }
  }
`;

const CONVERT_TO_SESSION_MUTATION = gql`
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
`;

const DELETE_APPOINTMENT_MUTATION = gql`
  mutation DeleteAppointment($id: UUID!) {
    deleteAppointment(id: $id)
  }
`;

@Injectable({ providedIn: 'root' })
export class AppointmentService {
  private apollo = inject(Apollo);

  // Query Methods
  getAppointments(where?: any, order?: any[]): Observable<Appointment[]> {
    return this.apollo.query<{ appointments: Appointment[] }>({
      query: APPOINTMENTS_QUERY,
      variables: { where, order },
      fetchPolicy: 'network-only'
    }).pipe(
      map(result => result.data?.appointments || [])
    );
  }

  getAppointmentById(id: string): Observable<Appointment> {
    return this.apollo.query<{ appointmentById: Appointment }>({
      query: APPOINTMENT_BY_ID_QUERY,
      variables: { id },
      fetchPolicy: 'network-only'
    }).pipe(
      map(result => result.data?.appointmentById!)
    );
  }

  getAvailableSlots(
    date: string,
    durationMinutes: number,
    organizationId: string,
    technicianId?: string
  ): Observable<string[]> {
    return this.apollo.query<{ availableSlots: string[] }>({
      query: AVAILABLE_SLOTS_QUERY,
      variables: { date, durationMinutes, organizationId, technicianId },
      fetchPolicy: 'network-only'
    }).pipe(
      map(result => result.data?.availableSlots || [])
    );
  }

  getCustomerUpcomingAppointments(customerId: string): Observable<Appointment[]> {
    return this.apollo.query<{ customerUpcomingAppointments: Appointment[] }>({
      query: CUSTOMER_UPCOMING_APPOINTMENTS_QUERY,
      variables: { customerId },
      fetchPolicy: 'network-only'
    }).pipe(
      map(result => result.data?.customerUpcomingAppointments || [])
    );
  }

  // Mutation Methods
  createAppointment(input: CreateAppointmentInput): Observable<{ appointment?: Appointment; error?: string }> {
    return this.apollo.mutate<{ createAppointment: { appointment?: Appointment; error?: string } }>({
      mutation: CREATE_APPOINTMENT_MUTATION,
      variables: { input }
    }).pipe(
      map(result => {
        if (!result.data) throw new Error('Failed to create appointment');
        return result.data.createAppointment;
      })
    );
  }

  updateAppointment(id: string, input: UpdateAppointmentInput): Observable<{ appointment?: Appointment; error?: string }> {
    return this.apollo.mutate<{ updateAppointment: { appointment?: Appointment; error?: string } }>({
      mutation: UPDATE_APPOINTMENT_MUTATION,
      variables: { id, input }
    }).pipe(
      map(result => {
        if (!result.data) throw new Error('Failed to update appointment');
        return result.data.updateAppointment;
      })
    );
  }

  updateAppointmentStatus(
    id: string,
    status: AppointmentStatus,
    cancellationReason?: string
  ): Observable<{ appointment?: Appointment; error?: string }> {
    return this.apollo.mutate<{ updateAppointmentStatus: { appointment?: Appointment; error?: string } }>({
      mutation: UPDATE_APPOINTMENT_STATUS_MUTATION,
      variables: { id, status, reason: cancellationReason }
    }).pipe(
      map(result => {
        if (!result.data) throw new Error('Failed to update appointment status');
        return result.data.updateAppointmentStatus;
      })
    );
  }

  convertToSession(id: string): Observable<{ appointment?: Appointment; error?: string }> {
    return this.apollo.mutate<{ convertToSession: { appointment?: Appointment; error?: string } }>({
      mutation: CONVERT_TO_SESSION_MUTATION,
      variables: { id }
    }).pipe(
      map(result => {
        if (!result.data) throw new Error('Failed to convert appointment to session');
        return result.data.convertToSession;
      })
    );
  }

  deleteAppointment(id: string): Observable<boolean> {
    return this.apollo.mutate<{ deleteAppointment: boolean }>({
      mutation: DELETE_APPOINTMENT_MUTATION,
      variables: { id }
    }).pipe(
      map(result => {
        if (!result.data) throw new Error('Failed to delete appointment');
        return result.data.deleteAppointment;
      })
    );
  }

  // Helper Methods - Status management
  confirmAppointment(id: string): Observable<{ appointment?: Appointment; error?: string }> {
    return this.updateAppointmentStatus(id, AppointmentStatus.CONFIRMED);
  }

  checkInAppointment(id: string): Observable<{ appointment?: Appointment; error?: string }> {
    return this.updateAppointmentStatus(id, AppointmentStatus.CHECKED_IN);
  }

  startAppointment(id: string): Observable<{ appointment?: Appointment; error?: string }> {
    return this.updateAppointmentStatus(id, AppointmentStatus.IN_PROGRESS);
  }

  completeAppointment(id: string): Observable<{ appointment?: Appointment; error?: string }> {
    return this.updateAppointmentStatus(id, AppointmentStatus.COMPLETED);
  }

  cancelAppointment(id: string, reason?: string): Observable<{ appointment?: Appointment; error?: string }> {
    return this.updateAppointmentStatus(id, AppointmentStatus.CANCELLED, reason);
  }

  markNoShow(id: string): Observable<{ appointment?: Appointment; error?: string }> {
    return this.updateAppointmentStatus(id, AppointmentStatus.NO_SHOW);
  }

  // Helper Methods - UI
  getStatusColor(status: AppointmentStatus): string {
    const colors: Record<AppointmentStatus, string> = {
      [AppointmentStatus.SCHEDULED]: 'bg-blue-100 text-blue-800 border-blue-200',
      [AppointmentStatus.CONFIRMED]: 'bg-emerald-100 text-emerald-800 border-emerald-200',
      [AppointmentStatus.CHECKED_IN]: 'bg-purple-100 text-purple-800 border-purple-200',
      [AppointmentStatus.IN_PROGRESS]: 'bg-amber-100 text-amber-800 border-amber-200',
      [AppointmentStatus.COMPLETED]: 'bg-slate-100 text-slate-800 border-slate-200',
      [AppointmentStatus.NO_SHOW]: 'bg-orange-100 text-orange-800 border-orange-200',
      [AppointmentStatus.CANCELLED]: 'bg-red-100 text-red-800 border-red-200'
    };
    return colors[status] || 'bg-slate-100 text-slate-800 border-slate-200';
  }

  formatStatus(status: AppointmentStatus | number): string {
    const statusMap: Record<AppointmentStatus, string> = {
      [AppointmentStatus.SCHEDULED]: 'Scheduled',
      [AppointmentStatus.CONFIRMED]: 'Confirmed',
      [AppointmentStatus.CHECKED_IN]: 'Checked In',
      [AppointmentStatus.IN_PROGRESS]: 'In Progress',
      [AppointmentStatus.COMPLETED]: 'Completed',
      [AppointmentStatus.NO_SHOW]: 'No Show',
      [AppointmentStatus.CANCELLED]: 'Cancelled'
    };
    return statusMap[status as AppointmentStatus] || 'Unknown';
  }

  formatType(type: AppointmentType | number): string {
    const typeMap: Record<AppointmentType, string> = {
      [AppointmentType.GENERAL_SERVICE]: 'General Service',
      [AppointmentType.OIL_CHANGE]: 'Oil Change',
      [AppointmentType.BRAKE_SERVICE]: 'Brake Service',
      [AppointmentType.TIRE_CHANGE]: 'Tire Change',
      [AppointmentType.INSPECTION]: 'Inspection',
      [AppointmentType.DIAGNOSIS]: 'Diagnosis',
      [AppointmentType.REPAIR]: 'Repair',
      [AppointmentType.CONSULTATION]: 'Consultation',
      [AppointmentType.AIR_CONDITIONING_SERVICE]: 'Air Conditioning Service',
      [AppointmentType.BATTERY_REPLACEMENT]: 'Battery Replacement',
      [AppointmentType.ENGINE_REPAIR]: 'Engine Repair',
      [AppointmentType.TRANSMISSION_SERVICE]: 'Transmission Service',
      [AppointmentType.OTHER]: 'Other'
    };
    return typeMap[type as AppointmentType] || 'Unknown';
  }

  formatDateTime(dateTime: string): string {
    const date = new Date(dateTime);
    return date.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: 'numeric',
      minute: '2-digit'
    });
  }

  formatTime(dateTime: string): string {
    const date = new Date(dateTime);
    return date.toLocaleTimeString('en-US', {
      hour: 'numeric',
      minute: '2-digit'
    });
  }

  formatDate(dateTime: string): string {
    const date = new Date(dateTime);
    return date.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }

  formatDuration(startTime: string, endTime: string): string {
    const start = new Date(startTime);
    const end = new Date(endTime);
    const diffMs = end.getTime() - start.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const hours = Math.floor(diffMins / 60);
    const mins = diffMins % 60;
    if (hours > 0 && mins > 0) return `${hours}h ${mins}m`;
    if (hours > 0) return `${hours}h`;
    return `${mins}m`;
  }

  isToday(dateTime: string): boolean {
    const date = new Date(dateTime);
    const today = new Date();
    return date.toDateString() === today.toDateString();
  }

  isPast(dateTime: string): boolean {
    return new Date(dateTime) < new Date();
  }

  canCancel(appointment: Appointment): boolean {
    return appointment.status === AppointmentStatus.SCHEDULED || 
           appointment.status === AppointmentStatus.CONFIRMED;
  }

  canConfirm(appointment: Appointment): boolean {
    return appointment.status === AppointmentStatus.SCHEDULED;
  }

  canCheckIn(appointment: Appointment): boolean {
    return appointment.status === AppointmentStatus.CONFIRMED || 
           appointment.status === AppointmentStatus.SCHEDULED;
  }

  canStart(appointment: Appointment): boolean {
    return appointment.status === AppointmentStatus.CHECKED_IN || 
           appointment.status === AppointmentStatus.CONFIRMED;
  }

  canComplete(appointment: Appointment): boolean {
    return appointment.status === AppointmentStatus.IN_PROGRESS;
  }

  canConvertToSession(appointment: Appointment): boolean {
    return appointment.status === AppointmentStatus.CHECKED_IN || 
           appointment.status === AppointmentStatus.CONFIRMED;
  }
}
