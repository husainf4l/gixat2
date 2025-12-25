import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { AppointmentService, Appointment, AppointmentStatus, AppointmentType, CreateAppointmentInput } from '../../services/appointment.service';
import { CustomerService, Customer } from '../../services/customer.service';
import { ProfileService } from '../../services/profile.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-appointments',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './appointments.component.html'
})
export class AppointmentsComponent implements OnInit {
  private appointmentService = inject(AppointmentService);
  private customerService = inject(CustomerService);
  private profileService = inject(ProfileService);
  private authService = inject(AuthService);
  private router = inject(Router);

  // State
  appointments = signal<Appointment[]>([]);
  filteredAppointments = signal<Appointment[]>([]);
  loading = signal(false);
  viewMode = signal<'calendar' | 'list'>('calendar');
  organizationId = signal<string | null>(null);
  
  // Filters
  selectedDate = signal<Date>(new Date());
  selectedStatus = signal<AppointmentStatus | 'ALL'>('ALL');
  selectedTechnician = signal<string | 'ALL'>('ALL');
  searchQuery = signal('');

  // Calendar state
  currentMonth = signal(new Date());
  calendarDays = signal<CalendarDay[]>([]);
  
  // Modal state
  showCreateModal = signal(false);
  showDetailsModal = signal(false);
  showRescheduleModal = signal(false);
  selectedAppointment = signal<Appointment | null>(null);

  // Create appointment form - using new structure
  createForm = signal<{
    customerId: string;
    carId: string;
    scheduledDate: string; // Date picker value (YYYY-MM-DD)
    scheduledTime: string; // Time picker value (HH:mm)
    durationMinutes: number;
    type: AppointmentType | null;
    serviceRequested: string;
    assignedTechnicianId?: string;
    customerNotes?: string;
    internalNotes?: string;
    contactPhone?: string;
    contactEmail?: string;
  }>({
    customerId: '',
    carId: '',
    scheduledDate: '',
    scheduledTime: '',
    durationMinutes: 60,
    type: null,
    serviceRequested: '',
    assignedTechnicianId: undefined,
    customerNotes: undefined,
    internalNotes: undefined,
    contactPhone: undefined,
    contactEmail: undefined
  });

  customers = signal<Customer[]>([]);
  technicians = signal<any[]>([]);
  availableSlots = signal<string[]>([]); // Array of DateTime strings
  customerCars = signal<any[]>([]);

  // Reschedule form
  rescheduleDate = signal('');
  rescheduleTime = signal('');

  // Enum access for template
  AppointmentStatus = AppointmentStatus;
  AppointmentType = AppointmentType;
  statuses = Object.values(AppointmentStatus).filter(v => typeof v === 'number') as AppointmentStatus[];
  types = Object.values(AppointmentType).filter(v => typeof v === 'number') as AppointmentType[];

  ngOnInit() {
    // Load organization ID
    this.authService.me().subscribe({
      next: (data) => {
        if (data.me?.organizationId) {
          this.organizationId.set(data.me.organizationId);
        }
      }
    });

    this.loadAppointments();
    this.loadTechnicians();
    this.loadCustomers();
    this.generateCalendar();
  }

  loadAppointments() {
    this.loading.set(true);
    this.appointmentService.getAppointments().subscribe({
      next: (apps) => {
        this.appointments.set(apps);
        this.applyFilters();
        this.generateCalendar();
        this.loading.set(false);
      },
      error: (err: any) => {
        console.error('Failed to load appointments:', err);
        this.loading.set(false);
      }
    });
  }

  // Helper methods for form updates
  updateCarId(event: Event) {
    const value = (event.target as HTMLSelectElement).value;
    this.createForm.update(f => ({ ...f, carId: value }));
  }

  updateDuration(event: Event) {
    const value = +(event.target as HTMLSelectElement).value;
    this.createForm.update(f => ({ ...f, durationMinutes: value }));
    this.loadAvailableSlots();
  }

  updateScheduledTime(slotDateTime: string) {
    // slotDateTime is a full DateTime string from availableSlots
    const date = new Date(slotDateTime);
    this.createForm.update(f => ({
      ...f,
      scheduledDate: date.toISOString().split('T')[0],
      scheduledTime: date.toTimeString().slice(0, 5)
    }));
  }

  updateType(event: Event) {
    const value = +(event.target as HTMLSelectElement).value;
    this.createForm.update(f => ({ ...f, type: value as AppointmentType }));
  }

  updateServiceRequested(event: Event) {
    const value = (event.target as HTMLTextAreaElement).value;
    this.createForm.update(f => ({ ...f, serviceRequested: value }));
  }

  updateTechnicianId(event: Event) {
    const value = (event.target as HTMLSelectElement).value || undefined;
    this.createForm.update(f => ({ ...f, assignedTechnicianId: value }));
    this.loadAvailableSlots();
  }

  updateCustomerNotes(event: Event) {
    const value = (event.target as HTMLTextAreaElement).value;
    this.createForm.update(f => ({ ...f, customerNotes: value || undefined }));
  }

  updateInternalNotes(event: Event) {
    const value = (event.target as HTMLTextAreaElement).value;
    this.createForm.update(f => ({ ...f, internalNotes: value || undefined }));
  }

  updateContactPhone(event: Event) {
    const value = (event.target as HTMLInputElement).value;
    this.createForm.update(f => ({ ...f, contactPhone: value || undefined }));
  }

  updateContactEmail(event: Event) {
    const value = (event.target as HTMLInputElement).value;
    this.createForm.update(f => ({ ...f, contactEmail: value || undefined }));
  }

  getTodayDate(): string {
    return new Date().toISOString().split('T')[0];
  }

  loadTechnicians() {
    this.profileService.getOrganizationUsers().subscribe({
      next: (members: any[]) => {
        this.technicians.set(members);
      },
      error: (err: any) => console.error('Failed to load technicians:', err)
    });
  }

  loadCustomers() {
    this.customerService.getCustomers(100).subscribe({
      next: (result: any) => {
        this.customers.set(result);
      },
      error: (err: any) => console.error('Failed to load customers:', err)
    });
  }

  applyFilters() {
    let filtered = [...this.appointments()];

    // Status filter
    if (this.selectedStatus() !== 'ALL') {
      filtered = filtered.filter(a => a.status === this.selectedStatus());
    }

    // Technician filter
    if (this.selectedTechnician() !== 'ALL') {
      filtered = filtered.filter(a => a.assignedTechnician?.id === this.selectedTechnician());
    }

    // Search filter
    const query = this.searchQuery().toLowerCase();
    if (query) {
      filtered = filtered.filter(a => 
        a.customer.name.toLowerCase().includes(query) ||
        a.car.make.toLowerCase().includes(query) ||
        a.car.model.toLowerCase().includes(query) ||
        this.appointmentService.formatType(a.type).toLowerCase().includes(query)
      );
    }

    this.filteredAppointments.set(filtered);
  }

  onStatusFilterChange(status: string) {
    this.selectedStatus.set(status === 'ALL' ? 'ALL' : +status as AppointmentStatus);
    this.applyFilters();
  }

  onTechnicianFilterChange(techId: string) {
    this.selectedTechnician.set(techId);
    this.applyFilters();
  }

  onSearchChange(query: string) {
    this.searchQuery.set(query);
    this.applyFilters();
  }

  // Calendar methods
  generateCalendar() {
    const month = this.currentMonth();
    const year = month.getFullYear();
    const monthIndex = month.getMonth();
    
    const firstDay = new Date(year, monthIndex, 1);
    const lastDay = new Date(year, monthIndex + 1, 0);
    const daysInMonth = lastDay.getDate();
    const startDayOfWeek = firstDay.getDay();

    const days: CalendarDay[] = [];
    
    // Previous month days
    const prevMonthLastDay = new Date(year, monthIndex, 0).getDate();
    for (let i = startDayOfWeek - 1; i >= 0; i--) {
      days.push({
        date: new Date(year, monthIndex - 1, prevMonthLastDay - i),
        isCurrentMonth: false,
        isToday: false,
        appointments: []
      });
    }

    // Current month days
    const today = new Date();
    for (let i = 1; i <= daysInMonth; i++) {
      const date = new Date(year, monthIndex, i);
      const dateStr = this.formatDateForComparison(date);
      days.push({
        date,
        isCurrentMonth: true,
        isToday: this.isSameDay(date, today),
        appointments: this.appointments().filter(a => {
          const appointmentDate = new Date(a.scheduledStartTime);
          return this.formatDateForComparison(appointmentDate) === dateStr;
        })
      });
    }

    // Next month days
    const remainingDays = 42 - days.length; // 6 rows * 7 days
    for (let i = 1; i <= remainingDays; i++) {
      days.push({
        date: new Date(year, monthIndex + 1, i),
        isCurrentMonth: false,
        isToday: false,
        appointments: []
      });
    }

    this.calendarDays.set(days);
  }

  previousMonth() {
    const current = this.currentMonth();
    this.currentMonth.set(new Date(current.getFullYear(), current.getMonth() - 1));
    this.generateCalendar();
  }

  nextMonth() {
    const current = this.currentMonth();
    this.currentMonth.set(new Date(current.getFullYear(), current.getMonth() + 1));
    this.generateCalendar();
  }

  goToToday() {
    this.currentMonth.set(new Date());
    this.generateCalendar();
  }

  selectDate(day: CalendarDay) {
    this.selectedDate.set(day.date);
    if (day.appointments.length === 1) {
      this.viewAppointment(day.appointments[0]);
    }
  }

  // CRUD Operations
  openCreateModal() {
    this.showCreateModal.set(true);
  }

  closeCreateModal() {
    this.showCreateModal.set(false);
    this.resetCreateForm();
  }

  resetCreateForm() {
    this.createForm.set({
      customerId: '',
      carId: '',
      scheduledDate: '',
      scheduledTime: '',
      durationMinutes: 60,
      type: null,
      serviceRequested: '',
      assignedTechnicianId: undefined,
      customerNotes: undefined,
      internalNotes: undefined,
      contactPhone: undefined,
      contactEmail: undefined
    });
    this.customerCars.set([]);
    this.availableSlots.set([]);
  }

  onCustomerSelect(customerId: string) {
    const customer = this.customers().find(c => c.id === customerId);
    if (customer) {
      this.customerCars.set(customer.cars || []);
      this.createForm.update(f => ({ ...f, customerId, carId: '' }));
    }
  }

  onDateSelect(date: string) {
    this.createForm.update(f => ({ ...f, scheduledDate: date }));
    this.loadAvailableSlots();
  }

  loadAvailableSlots() {
    const form = this.createForm();
    const orgId = this.organizationId();
    
    if (form.scheduledDate && form.durationMinutes && orgId) {
      // Convert date to DateTime for the query
      const dateTime = new Date(`${form.scheduledDate}T00:00:00`).toISOString();
      
      this.appointmentService.getAvailableSlots(
        dateTime,
        form.durationMinutes,
        orgId,
        form.assignedTechnicianId
      ).subscribe({
        next: (slots) => this.availableSlots.set(slots),
        error: (err) => console.error('Failed to load slots:', err)
      });
    }
  }

  createAppointment() {
    const form = this.createForm();
    if (!this.validateCreateForm()) return;

    // Build DateTime strings from date and time
    const scheduledStartTime = new Date(`${form.scheduledDate}T${form.scheduledTime}:00`).toISOString();
    const scheduledEndTime = new Date(new Date(scheduledStartTime).getTime() + form.durationMinutes * 60000).toISOString();

    const input: CreateAppointmentInput = {
      customerId: form.customerId,
      carId: form.carId,
      scheduledStartTime,
      scheduledEndTime,
      type: form.type!,
      serviceRequested: form.serviceRequested,
      estimatedDurationMinutes: form.durationMinutes,
      assignedTechnicianId: form.assignedTechnicianId,
      customerNotes: form.customerNotes,
      internalNotes: form.internalNotes,
      contactPhone: form.contactPhone,
      contactEmail: form.contactEmail
    };

    this.loading.set(true);
    this.appointmentService.createAppointment(input).subscribe({
      next: (result) => {
        if (result.error) {
          alert(`Failed to create appointment: ${result.error}`);
          this.loading.set(false);
          return;
        }
        if (result.appointment) {
          this.appointments.update(apps => [...apps, result.appointment!]);
          this.applyFilters();
          this.generateCalendar();
          this.closeCreateModal();
        }
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Failed to create appointment:', err);
        alert('Failed to create appointment');
        this.loading.set(false);
      }
    });
  }

  validateCreateForm(): boolean {
    const form = this.createForm();
    if (!form.customerId || !form.carId || !form.scheduledDate || 
        !form.scheduledTime || form.type === null) {
      alert('Please fill in all required fields');
      return false;
    }
    return true;
  }

  viewAppointment(appointment: Appointment) {
    this.selectedAppointment.set(appointment);
    this.showDetailsModal.set(true);
  }

  closeDetailsModal() {
    this.showDetailsModal.set(false);
    this.selectedAppointment.set(null);
  }

  confirmAppointment(appointment: Appointment) {
    if (!confirm('Confirm this appointment?')) return;

    this.appointmentService.confirmAppointment(appointment.id).subscribe({
      next: (result) => {
        if (result.error) {
          alert(`Failed to confirm: ${result.error}`);
          return;
        }
        if (result.appointment) {
          this.updateAppointmentInList(result.appointment);
          if (this.selectedAppointment()?.id === result.appointment.id) {
            this.selectedAppointment.set(result.appointment);
          }
        }
      },
      error: (err) => {
        console.error('Failed to confirm:', err);
        alert('Failed to confirm appointment');
      }
    });
  }

  checkInAppointment(appointment: Appointment) {
    this.appointmentService.checkInAppointment(appointment.id).subscribe({
      next: (result) => {
        if (result.error) {
          alert(`Failed to check in: ${result.error}`);
          return;
        }
        if (result.appointment) {
          this.updateAppointmentInList(result.appointment);
          if (this.selectedAppointment()?.id === result.appointment.id) {
            this.selectedAppointment.set(result.appointment);
          }
        }
      },
      error: (err) => {
        console.error('Failed to check in:', err);
        alert('Failed to check in appointment');
      }
    });
  }

  startAppointment(appointment: Appointment) {
    this.appointmentService.startAppointment(appointment.id).subscribe({
      next: (result) => {
        if (result.error) {
          alert(`Failed to start: ${result.error}`);
          return;
        }
        if (result.appointment) {
          this.updateAppointmentInList(result.appointment);
          if (this.selectedAppointment()?.id === result.appointment.id) {
            this.selectedAppointment.set(result.appointment);
          }
        }
      },
      error: (err) => {
        console.error('Failed to start:', err);
        alert('Failed to start appointment');
      }
    });
  }

  convertToSession(appointment: Appointment) {
    if (!confirm('Convert this appointment to a session?')) return;

    this.appointmentService.convertToSession(appointment.id).subscribe({
      next: (result) => {
        if (result.error) {
          alert(`Failed to convert: ${result.error}`);
          return;
        }
        if (result.appointment?.session) {
          // Navigate to the session
          this.router.navigate(['/dashboard/sessions', result.appointment.session.id]);
        }
      },
      error: (err) => {
        console.error('Failed to convert:', err);
        alert('Failed to convert appointment to session');
      }
    });
  }

  completeAppointment(appointment: Appointment) {
    if (!confirm('Mark this appointment as completed?')) return;

    this.appointmentService.completeAppointment(appointment.id).subscribe({
      next: (result) => {
        if (result.error) {
          alert(`Failed to complete: ${result.error}`);
          return;
        }
        if (result.appointment) {
          this.updateAppointmentInList(result.appointment);
          if (this.selectedAppointment()?.id === result.appointment.id) {
            this.selectedAppointment.set(result.appointment);
          }
        }
      },
      error: (err) => {
        console.error('Failed to complete:', err);
        alert('Failed to complete appointment');
      }
    });
  }

  cancelAppointment(appointment: Appointment) {
    const reason = prompt('Reason for cancellation (optional):');
    if (reason === null) return; // User clicked cancel

    this.appointmentService.cancelAppointment(appointment.id, reason || undefined).subscribe({
      next: (result) => {
        if (result.error) {
          alert(`Failed to cancel: ${result.error}`);
          return;
        }
        if (result.appointment) {
          this.updateAppointmentInList(result.appointment);
          if (this.selectedAppointment()?.id === result.appointment.id) {
            this.selectedAppointment.set(result.appointment);
          }
          this.closeDetailsModal();
        }
      },
      error: (err) => {
        console.error('Failed to cancel:', err);
        alert('Failed to cancel appointment');
      }
    });
  }

  markNoShow(appointment: Appointment) {
    if (!confirm('Mark this appointment as no-show?')) return;

    this.appointmentService.markNoShow(appointment.id).subscribe({
      next: (result) => {
        if (result.error) {
          alert(`Failed to mark no-show: ${result.error}`);
          return;
        }
        if (result.appointment) {
          this.updateAppointmentInList(result.appointment);
          if (this.selectedAppointment()?.id === result.appointment.id) {
            this.selectedAppointment.set(result.appointment);
          }
        }
      },
      error: (err) => {
        console.error('Failed to mark no-show:', err);
        alert('Failed to mark as no-show');
      }
    });
  }

  openRescheduleModal(appointment: Appointment) {
    this.selectedAppointment.set(appointment);
    const startDate = new Date(appointment.scheduledStartTime);
    this.rescheduleDate.set(startDate.toISOString().split('T')[0]);
    this.rescheduleTime.set(startDate.toTimeString().slice(0, 5));
    this.showRescheduleModal.set(true);
  }

  closeRescheduleModal() {
    this.showRescheduleModal.set(false);
    this.rescheduleDate.set('');
    this.rescheduleTime.set('');
  }

  rescheduleAppointment() {
    const appointment = this.selectedAppointment();
    if (!appointment) return;

    const newDate = this.rescheduleDate();
    const newTime = this.rescheduleTime();

    if (!newDate || !newTime) {
      alert('Please select both date and time');
      return;
    }

    // Calculate new start and end times
    const newStartTime = new Date(`${newDate}T${newTime}:00`).toISOString();
    const duration = new Date(appointment.scheduledEndTime).getTime() - new Date(appointment.scheduledStartTime).getTime();
    const newEndTime = new Date(new Date(newStartTime).getTime() + duration).toISOString();

    this.appointmentService.updateAppointment(appointment.id, {
      scheduledStartTime: newStartTime,
      scheduledEndTime: newEndTime
    }).subscribe({
      next: (result) => {
        if (result.error) {
          alert(`Failed to reschedule: ${result.error}`);
          return;
        }
        if (result.appointment) {
          this.updateAppointmentInList(result.appointment);
          this.closeRescheduleModal();
          this.closeDetailsModal();
          this.generateCalendar();
        }
      },
      error: (err) => {
        console.error('Failed to reschedule:', err);
        alert('Failed to reschedule appointment');
      }
    });
  }

  updateAppointmentInList(updated: Appointment) {
    this.appointments.update(apps => 
      apps.map(a => a.id === updated.id ? updated : a)
    );
    this.applyFilters();
    this.generateCalendar();
  }

  // Helper methods
  getStatusColor(status: AppointmentStatus): string {
    return this.appointmentService.getStatusColor(status);
  }

  formatStatus(status: AppointmentStatus | number): string {
    return this.appointmentService.formatStatus(status);
  }

  formatType(type: AppointmentType | number): string {
    return this.appointmentService.formatType(type);
  }

  formatTime(dateTime: string): string {
    return this.appointmentService.formatTime(dateTime);
  }

  formatDate(dateTime: string): string {
    return this.appointmentService.formatDate(dateTime);
  }

  formatDateTime(dateTime: string): string {
    return this.appointmentService.formatDateTime(dateTime);
  }

  formatDuration(startTime: string, endTime: string): string {
    return this.appointmentService.formatDuration(startTime, endTime);
  }

  formatDateForComparison(date: Date): string {
    return date.toISOString().split('T')[0];
  }

  isSameDay(date1: Date, date2: Date): boolean {
    return this.formatDateForComparison(date1) === this.formatDateForComparison(date2);
  }

  getMonthName(): string {
    return this.currentMonth().toLocaleDateString('en-US', { month: 'long', year: 'numeric' });
  }

  canConfirm(appointment: Appointment): boolean {
    return this.appointmentService.canConfirm(appointment);
  }

  canCheckIn(appointment: Appointment): boolean {
    return this.appointmentService.canCheckIn(appointment);
  }

  canStart(appointment: Appointment): boolean {
    return this.appointmentService.canStart(appointment);
  }

  canComplete(appointment: Appointment): boolean {
    return this.appointmentService.canComplete(appointment);
  }

  canCancel(appointment: Appointment): boolean {
    return this.appointmentService.canCancel(appointment);
  }

  canConvertToSession(appointment: Appointment): boolean {
    return this.appointmentService.canConvertToSession(appointment);
  }

  canReschedule(appointment: Appointment): boolean {
    return appointment.status === AppointmentStatus.SCHEDULED || 
           appointment.status === AppointmentStatus.CONFIRMED;
  }

  isSlotSelected(slotDateTime: string): boolean {
    const form = this.createForm();
    if (!form.scheduledTime) return false;
    const slotTime = new Date(slotDateTime).toTimeString().slice(0, 5);
    return slotTime === form.scheduledTime;
  }

  getTodayAppointments(): Appointment[] {
    const today = new Date().toISOString().split('T')[0];
    return this.filteredAppointments().filter(a => {
      const appointmentDate = new Date(a.scheduledStartTime);
      return this.formatDateForComparison(appointmentDate) === today;
    });
  }

  getUpcomingAppointments(): Appointment[] {
    const now = new Date();
    return this.filteredAppointments()
      .filter(a => new Date(a.scheduledStartTime) >= now)
      .sort((a, b) => {
        return new Date(a.scheduledStartTime).getTime() - new Date(b.scheduledStartTime).getTime();
      });
  }
}

interface CalendarDay {
  date: Date;
  isCurrentMonth: boolean;
  isToday: boolean;
  appointments: Appointment[];
}
