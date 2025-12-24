import { Injectable, inject } from '@angular/core';
import { Apollo, gql } from 'apollo-angular';
import { Observable, map } from 'rxjs';

// Enums
export enum AppointmentStatus {
  SCHEDULED = 'SCHEDULED',
  CONFIRMED = 'CONFIRMED',
  IN_PROGRESS = 'IN_PROGRESS',
  COMPLETED = 'COMPLETED',
  CANCELLED = 'CANCELLED',
  NO_SHOW = 'NO_SHOW'
}

export enum ReminderType {
  SMS = 'SMS',
  EMAIL = 'EMAIL',
  PUSH = 'PUSH'
}

export enum ReminderStatus {
  PENDING = 'PENDING',
  SENT = 'SENT',
  FAILED = 'FAILED'
}

export enum RecurrenceType {
  NONE = 'NONE',
  DAILY = 'DAILY',
  WEEKLY = 'WEEKLY',
  MONTHLY = 'MONTHLY'
}

// Interfaces
export interface AppointmentCustomer {
  id: string;
  firstName: string;
  lastName: string;
  email?: string | null;
  phoneNumber: string;
}

export interface AppointmentCar {
  id: string;
  make: string;
  model: string;
  year: number;
  licensePlate: string;
}

export interface AppointmentTechnician {
  id: string;
  fullName: string;
  email: string;
}

export interface Appointment {
  id: string;
  customerId: string;
  customer: AppointmentCustomer;
  carId: string;
  car: AppointmentCar;
  scheduledDate: string;
  scheduledTime: string; // HH:mm format
  duration: number; // in minutes
  serviceType: string;
  serviceDescription?: string | null;
  status: AppointmentStatus;
  technicianId?: string | null;
  technician?: AppointmentTechnician | null;
  notes?: string | null;
  reminderSent: boolean;
  isRecurring: boolean;
  recurrenceType?: RecurrenceType | null;
  recurrenceEndDate?: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface TimeSlot {
  id: string;
  date: string;
  startTime: string; // HH:mm format
  endTime: string;
  technicianId?: string | null;
  technician?: AppointmentTechnician | null;
  isAvailable: boolean;
  isBlocked: boolean;
  blockReason?: string | null;
}

export interface AppointmentReminder {
  id: string;
  appointmentId: string;
  reminderType: ReminderType;
  scheduledFor: string;
  status: ReminderStatus;
  sentAt?: string | null;
  errorMessage?: string | null;
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
  scheduledDate: string;
  scheduledTime: string;
  duration: number;
  serviceType: string;
  serviceDescription?: string;
  technicianId?: string;
  notes?: string;
  isRecurring?: boolean;
  recurrenceType?: RecurrenceType;
  recurrenceEndDate?: string;
}

export interface UpdateAppointmentInput {
  scheduledDate?: string;
  scheduledTime?: string;
  duration?: number;
  serviceType?: string;
  serviceDescription?: string;
  technicianId?: string;
  notes?: string;
  status?: AppointmentStatus;
}

// GraphQL Fragments
const APPOINTMENT_FRAGMENT = gql`
  fragment AppointmentDetails on Appointment {
    id
    scheduledDate
    scheduledTime
    duration
    serviceType
    serviceDescription
    status
    notes
    reminderSent
    isRecurring
    recurrenceType
    recurrenceEndDate
    createdAt
    updatedAt
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
    }
    technician {
      id
      fullName
      email
    }
  }
`;

// GraphQL Queries
const APPOINTMENTS_QUERY = gql`
  query GetAppointments($first: Int, $after: String, $where: AppointmentFilterInput, $order: [AppointmentSortInput!]) {
    appointments(first: $first, after: $after, where: $where, order: $order) {
      edges {
        node {
          ...AppointmentDetails
        }
        cursor
      }
      pageInfo {
        hasNextPage
        hasPreviousPage
        startCursor
        endCursor
      }
      totalCount
    }
  }
  ${APPOINTMENT_FRAGMENT}
`;

const APPOINTMENT_BY_ID_QUERY = gql`
  query GetAppointmentById($id: UUID!) {
    appointmentById(id: $id) {
      ...AppointmentDetails
    }
  }
  ${APPOINTMENT_FRAGMENT}
`;

const AVAILABLE_TIME_SLOTS_QUERY = gql`
  query GetAvailableTimeSlots($date: Date!, $technicianId: String, $duration: Int!) {
    availableTimeSlots(date: $date, technicianId: $technicianId, duration: $duration) {
      id
      date
      startTime
      endTime
      isAvailable
      isBlocked
      blockReason
      technician {
        id
        fullName
      }
    }
  }
`;

const TECHNICIAN_AVAILABILITY_QUERY = gql`
  query GetTechnicianAvailability($technicianId: String!, $startDate: Date!, $endDate: Date!) {
    technicianAvailability(technicianId: $technicianId, startDate: $startDate, endDate: $endDate) {
      date
      availableSlots {
        startTime
        endTime
      }
      bookedSlots {
        startTime
        endTime
        appointmentId
      }
    }
  }
`;

const TODAY_APPOINTMENTS_QUERY = gql`
  query GetTodayAppointments {
    todayAppointments {
      ...AppointmentDetails
    }
  }
  ${APPOINTMENT_FRAGMENT}
`;

const UPCOMING_APPOINTMENTS_QUERY = gql`
  query GetUpcomingAppointments($days: Int!) {
    upcomingAppointments(days: $days) {
      ...AppointmentDetails
    }
  }
  ${APPOINTMENT_FRAGMENT}
`;

// GraphQL Mutations
const CREATE_APPOINTMENT_MUTATION = gql`
  mutation CreateAppointment($input: CreateAppointmentInput!) {
    createAppointment(input: $input) {
      ...AppointmentDetails
    }
  }
  ${APPOINTMENT_FRAGMENT}
`;

const UPDATE_APPOINTMENT_MUTATION = gql`
  mutation UpdateAppointment($id: UUID!, $input: UpdateAppointmentInput!) {
    updateAppointment(id: $id, input: $input) {
      ...AppointmentDetails
    }
  }
  ${APPOINTMENT_FRAGMENT}
`;

const CANCEL_APPOINTMENT_MUTATION = gql`
  mutation CancelAppointment($id: UUID!, $reason: String) {
    cancelAppointment(id: $id, reason: $reason) {
      ...AppointmentDetails
    }
  }
  ${APPOINTMENT_FRAGMENT}
`;

const CONFIRM_APPOINTMENT_MUTATION = gql`
  mutation ConfirmAppointment($id: UUID!) {
    confirmAppointment(id: $id) {
      ...AppointmentDetails
    }
  }
  ${APPOINTMENT_FRAGMENT}
`;

const MARK_NO_SHOW_MUTATION = gql`
  mutation MarkNoShow($id: UUID!) {
    markNoShow(id: $id) {
      ...AppointmentDetails
    }
  }
  ${APPOINTMENT_FRAGMENT}
`;

const START_APPOINTMENT_MUTATION = gql`
  mutation StartAppointment($id: UUID!) {
    startAppointment(id: $id) {
      ...AppointmentDetails
    }
  }
  ${APPOINTMENT_FRAGMENT}
`;

const COMPLETE_APPOINTMENT_MUTATION = gql`
  mutation CompleteAppointment($id: UUID!) {
    completeAppointment(id: $id) {
      ...AppointmentDetails
    }
  }
  ${APPOINTMENT_FRAGMENT}
`;

const RESCHEDULE_APPOINTMENT_MUTATION = gql`
  mutation RescheduleAppointment($id: UUID!, $newDate: Date!, $newTime: String!) {
    rescheduleAppointment(id: $id, newDate: $newDate, newTime: $newTime) {
      ...AppointmentDetails
    }
  }
  ${APPOINTMENT_FRAGMENT}
`;

const SEND_REMINDER_MUTATION = gql`
  mutation SendAppointmentReminder($id: UUID!, $type: ReminderType!) {
    sendAppointmentReminder(id: $id, type: $type) {
      success
      message
    }
  }
`;

const BLOCK_TIME_SLOT_MUTATION = gql`
  mutation BlockTimeSlot($date: Date!, $startTime: String!, $endTime: String!, $technicianId: String, $reason: String!) {
    blockTimeSlot(date: $date, startTime: $startTime, endTime: $endTime, technicianId: $technicianId, reason: $reason) {
      id
      date
      startTime
      endTime
      isBlocked
      blockReason
    }
  }
`;

const UNBLOCK_TIME_SLOT_MUTATION = gql`
  mutation UnblockTimeSlot($id: UUID!) {
    unblockTimeSlot(id: $id) {
      id
      isBlocked
    }
  }
`;

@Injectable({ providedIn: 'root' })
export class AppointmentService {
  private apollo = inject(Apollo);

  // Query Methods
  getAppointments(
    first: number = 20,
    after?: string | null,
    where?: any,
    order?: any[]
  ): Observable<AppointmentsConnection> {
    return this.apollo.query<{ appointments: AppointmentsConnection }>({
      query: APPOINTMENTS_QUERY,
      variables: { first, after, where, order },
      fetchPolicy: 'network-only'
    }).pipe(
      map(result => result.data?.appointments!)
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

  getAvailableTimeSlots(date: string, duration: number, technicianId?: string): Observable<TimeSlot[]> {
    return this.apollo.query<{ availableTimeSlots: TimeSlot[] }>({
      query: AVAILABLE_TIME_SLOTS_QUERY,
      variables: { date, duration, technicianId },
      fetchPolicy: 'network-only'
    }).pipe(
      map(result => result.data?.availableTimeSlots!)
    );
  }

  getTechnicianAvailability(technicianId: string, startDate: string, endDate: string): Observable<any> {
    return this.apollo.query<{ technicianAvailability: any }>({
      query: TECHNICIAN_AVAILABILITY_QUERY,
      variables: { technicianId, startDate, endDate },
      fetchPolicy: 'network-only'
    }).pipe(
      map(result => result.data?.technicianAvailability!)
    );
  }

  getTodayAppointments(): Observable<Appointment[]> {
    return this.apollo.query<{ todayAppointments: Appointment[] }>({
      query: TODAY_APPOINTMENTS_QUERY,
      fetchPolicy: 'network-only'
    }).pipe(
      map(result => result.data?.todayAppointments!)
    );
  }

  getUpcomingAppointments(days: number = 7): Observable<Appointment[]> {
    return this.apollo.query<{ upcomingAppointments: Appointment[] }>({
      query: UPCOMING_APPOINTMENTS_QUERY,
      variables: { days },
      fetchPolicy: 'network-only'
    }).pipe(
      map(result => result.data?.upcomingAppointments!)
    );
  }

  // Mutation Methods
  createAppointment(input: CreateAppointmentInput): Observable<Appointment> {
    return this.apollo.mutate<{ createAppointment: Appointment }>({
      mutation: CREATE_APPOINTMENT_MUTATION,
      variables: { input }
    }).pipe(
      map(result => {
        if (!result.data) throw new Error('Failed to create appointment');
        return result.data.createAppointment;
      })
    );
  }

  updateAppointment(id: string, input: UpdateAppointmentInput): Observable<Appointment> {
    return this.apollo.mutate<{ updateAppointment: Appointment }>({
      mutation: UPDATE_APPOINTMENT_MUTATION,
      variables: { id, input }
    }).pipe(
      map(result => {
        if (!result.data) throw new Error('Failed to update appointment');
        return result.data.updateAppointment;
      })
    );
  }

  cancelAppointment(id: string, reason?: string): Observable<Appointment> {
    return this.apollo.mutate<{ cancelAppointment: Appointment }>({
      mutation: CANCEL_APPOINTMENT_MUTATION,
      variables: { id, reason }
    }).pipe(
      map(result => {
        if (!result.data) throw new Error('Failed to cancel appointment');
        return result.data.cancelAppointment;
      })
    );
  }

  confirmAppointment(id: string): Observable<Appointment> {
    return this.apollo.mutate<{ confirmAppointment: Appointment }>({
      mutation: CONFIRM_APPOINTMENT_MUTATION,
      variables: { id }
    }).pipe(
      map(result => {
        if (!result.data) throw new Error('Failed to confirm appointment');
        return result.data.confirmAppointment;
      })
    );
  }

  markNoShow(id: string): Observable<Appointment> {
    return this.apollo.mutate<{ markNoShow: Appointment }>({
      mutation: MARK_NO_SHOW_MUTATION,
      variables: { id }
    }).pipe(
      map(result => {
        if (!result.data) throw new Error('Failed to mark as no-show');
        return result.data.markNoShow;
      })
    );
  }

  startAppointment(id: string): Observable<Appointment> {
    return this.apollo.mutate<{ startAppointment: Appointment }>({
      mutation: START_APPOINTMENT_MUTATION,
      variables: { id }
    }).pipe(
      map(result => {
        if (!result.data) throw new Error('Failed to start appointment');
        return result.data.startAppointment;
      })
    );
  }

  completeAppointment(id: string): Observable<Appointment> {
    return this.apollo.mutate<{ completeAppointment: Appointment }>({
      mutation: COMPLETE_APPOINTMENT_MUTATION,
      variables: { id }
    }).pipe(
      map(result => {
        if (!result.data) throw new Error('Failed to complete appointment');
        return result.data.completeAppointment;
      })
    );
  }

  rescheduleAppointment(id: string, newDate: string, newTime: string): Observable<Appointment> {
    return this.apollo.mutate<{ rescheduleAppointment: Appointment }>({
      mutation: RESCHEDULE_APPOINTMENT_MUTATION,
      variables: { id, newDate, newTime }
    }).pipe(
      map(result => {
        if (!result.data) throw new Error('Failed to reschedule appointment');
        return result.data.rescheduleAppointment;
      })
    );
  }

  sendReminder(id: string, type: ReminderType): Observable<{ success: boolean; message: string }> {
    return this.apollo.mutate<{ sendAppointmentReminder: { success: boolean; message: string } }>({
      mutation: SEND_REMINDER_MUTATION,
      variables: { id, type }
    }).pipe(
      map(result => {
        if (!result.data) throw new Error('Failed to send reminder');
        return result.data.sendAppointmentReminder;
      })
    );
  }

  blockTimeSlot(date: string, startTime: string, endTime: string, reason: string, technicianId?: string): Observable<TimeSlot> {
    return this.apollo.mutate<{ blockTimeSlot: TimeSlot }>({
      mutation: BLOCK_TIME_SLOT_MUTATION,
      variables: { date, startTime, endTime, reason, technicianId }
    }).pipe(
      map(result => {
        if (!result.data) throw new Error('Failed to block time slot');
        return result.data.blockTimeSlot;
      })
    );
  }

  unblockTimeSlot(id: string): Observable<TimeSlot> {
    return this.apollo.mutate<{ unblockTimeSlot: TimeSlot }>({
      mutation: UNBLOCK_TIME_SLOT_MUTATION,
      variables: { id }
    }).pipe(
      map(result => {
        if (!result.data) throw new Error('Failed to unblock time slot');
        return result.data.unblockTimeSlot;
      })
    );
  }

  // Helper Methods
  getStatusColor(status: AppointmentStatus): string {
    const colors: Record<AppointmentStatus, string> = {
      [AppointmentStatus.SCHEDULED]: 'bg-blue-100 text-blue-800 border-blue-200',
      [AppointmentStatus.CONFIRMED]: 'bg-emerald-100 text-emerald-800 border-emerald-200',
      [AppointmentStatus.IN_PROGRESS]: 'bg-amber-100 text-amber-800 border-amber-200',
      [AppointmentStatus.COMPLETED]: 'bg-slate-100 text-slate-800 border-slate-200',
      [AppointmentStatus.CANCELLED]: 'bg-red-100 text-red-800 border-red-200',
      [AppointmentStatus.NO_SHOW]: 'bg-orange-100 text-orange-800 border-orange-200'
    };
    return colors[status] || 'bg-slate-100 text-slate-800 border-slate-200';
  }

  formatStatus(status: string): string {
    return status.replace(/_/g, ' ').toLowerCase()
      .replace(/\b\w/g, c => c.toUpperCase());
  }

  formatTime(time: string): string {
    // Converts HH:mm to 12-hour format
    const [hours, minutes] = time.split(':').map(Number);
    const period = hours >= 12 ? 'PM' : 'AM';
    const displayHours = hours % 12 || 12;
    return `${displayHours}:${minutes.toString().padStart(2, '0')} ${period}`;
  }

  formatDuration(minutes: number): string {
    const hours = Math.floor(minutes / 60);
    const mins = minutes % 60;
    if (hours > 0 && mins > 0) return `${hours}h ${mins}m`;
    if (hours > 0) return `${hours}h`;
    return `${mins}m`;
  }

  isToday(dateString: string): boolean {
    const date = new Date(dateString);
    const today = new Date();
    return date.toDateString() === today.toDateString();
  }

  isTomorrow(dateString: string): boolean {
    const date = new Date(dateString);
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    return date.toDateString() === tomorrow.toDateString();
  }

  isPast(dateString: string, time: string): boolean {
    const appointmentDate = new Date(`${dateString}T${time}`);
    return appointmentDate < new Date();
  }

  canCancel(appointment: Appointment): boolean {
    return appointment.status === AppointmentStatus.SCHEDULED || 
           appointment.status === AppointmentStatus.CONFIRMED;
  }

  canConfirm(appointment: Appointment): boolean {
    return appointment.status === AppointmentStatus.SCHEDULED;
  }

  canStart(appointment: Appointment): boolean {
    return (appointment.status === AppointmentStatus.CONFIRMED || 
            appointment.status === AppointmentStatus.SCHEDULED) &&
           this.isToday(appointment.scheduledDate);
  }

  canComplete(appointment: Appointment): boolean {
    return appointment.status === AppointmentStatus.IN_PROGRESS;
  }

  canReschedule(appointment: Appointment): boolean {
    return appointment.status === AppointmentStatus.SCHEDULED || 
           appointment.status === AppointmentStatus.CONFIRMED;
  }
}
