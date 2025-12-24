import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { AppointmentService, Appointment, AppointmentStatus, CreateAppointmentInput, TimeSlot, RecurrenceType } from '../../services/appointment.service';
import { CustomerService, Customer } from '../../services/customer.service';
import { ProfileService } from '../../services/profile.service';

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
  private router = inject(Router);

  // State
  appointments = signal<Appointment[]>([]);
  filteredAppointments = signal<Appointment[]>([]);
  loading = signal(false);
  viewMode = signal<'calendar' | 'list'>('calendar');
  
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

  // Create appointment form
  createForm = signal<CreateAppointmentInput>({
    customerId: '',
    carId: '',
    scheduledDate: '',
    scheduledTime: '',
    duration: 60,
    serviceType: '',
    serviceDescription: '',
    technicianId: '',
    notes: ''
  });

  customers = signal<Customer[]>([]);
  technicians = signal<any[]>([]);
  availableSlots = signal<TimeSlot[]>([]);
  customerCars = signal<any[]>([]);

  // Reschedule form
  rescheduleDate = signal('');
  rescheduleTime = signal('');

  // Enum access for template
  AppointmentStatus = AppointmentStatus;
  statuses = Object.values(AppointmentStatus);

  ngOnInit() {
    this.loadAppointments();
    this.loadTechnicians();
    this.loadCustomers();
    this.generateCalendar();
  }

  loadAppointments() {
    this.loading.set(true);
    this.appointmentService.getAppointments(100).subscribe({
      next: (result) => {
        const apps = result.edges.map(e => e.node);
        this.appointments.set(apps);
        this.applyFilters();
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
    this.createForm.update(f => ({ ...f, duration: value }));
    this.loadAvailableSlots();
  }

  updateScheduledTime(startTime: string) {
    this.createForm.update(f => ({ ...f, scheduledTime: startTime }));
  }

  updateServiceType(event: Event) {
    const value = (event.target as HTMLSelectElement).value;
    this.createForm.update(f => ({ ...f, serviceType: value }));
  }

  updateServiceDescription(event: Event) {
    const value = (event.target as HTMLTextAreaElement).value;
    this.createForm.update(f => ({ ...f, serviceDescription: value }));
  }

  updateTechnicianId(event: Event) {
    const value = (event.target as HTMLSelectElement).value || undefined;
    this.createForm.update(f => ({ ...f, technicianId: value }));
    this.loadAvailableSlots();
  }

  updateNotes(event: Event) {
    const value = (event.target as HTMLTextAreaElement).value;
    this.createForm.update(f => ({ ...f, notes: value }));
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
      filtered = filtered.filter(a => a.technicianId === this.selectedTechnician());
    }

    // Search filter
    const query = this.searchQuery().toLowerCase();
    if (query) {
      filtered = filtered.filter(a => 
        a.customer.firstName.toLowerCase().includes(query) ||
        a.customer.lastName.toLowerCase().includes(query) ||
        a.car.make.toLowerCase().includes(query) ||
        a.car.model.toLowerCase().includes(query) ||
        a.serviceType.toLowerCase().includes(query)
      );
    }

    this.filteredAppointments.set(filtered);
  }

  onStatusFilterChange(status: string) {
    this.selectedStatus.set(status as any);
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
        appointments: this.appointments().filter(a => 
          this.formatDateForComparison(new Date(a.scheduledDate)) === dateStr
        )
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
      duration: 60,
      serviceType: '',
      serviceDescription: '',
      technicianId: '',
      notes: ''
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
    if (form.scheduledDate && form.duration) {
      this.appointmentService.getAvailableTimeSlots(
        form.scheduledDate,
        form.duration,
        form.technicianId || undefined
      ).subscribe({
        next: (slots) => this.availableSlots.set(slots),
        error: (err) => console.error('Failed to load slots:', err)
      });
    }
  }

  createAppointment() {
    const form = this.createForm();
    if (!this.validateCreateForm()) return;

    this.loading.set(true);
    this.appointmentService.createAppointment(form).subscribe({
      next: (appointment) => {
        this.appointments.update(apps => [...apps, appointment]);
        this.applyFilters();
        this.generateCalendar();
        this.closeCreateModal();
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
        !form.scheduledTime || !form.serviceType) {
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
      next: (updated) => {
        this.updateAppointmentInList(updated);
        if (this.selectedAppointment()?.id === updated.id) {
          this.selectedAppointment.set(updated);
        }
      },
      error: (err) => {
        console.error('Failed to confirm:', err);
        alert('Failed to confirm appointment');
      }
    });
  }

  startAppointment(appointment: Appointment) {
    this.appointmentService.startAppointment(appointment.id).subscribe({
      next: (updated) => {
        this.updateAppointmentInList(updated);
        if (this.selectedAppointment()?.id === updated.id) {
          this.selectedAppointment.set(updated);
        }
        // Navigate to create session
        this.router.navigate(['/dashboard/sessions/create'], {
          queryParams: { appointmentId: appointment.id }
        });
      },
      error: (err) => {
        console.error('Failed to start:', err);
        alert('Failed to start appointment');
      }
    });
  }

  completeAppointment(appointment: Appointment) {
    if (!confirm('Mark this appointment as completed?')) return;

    this.appointmentService.completeAppointment(appointment.id).subscribe({
      next: (updated) => {
        this.updateAppointmentInList(updated);
        if (this.selectedAppointment()?.id === updated.id) {
          this.selectedAppointment.set(updated);
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

    this.appointmentService.cancelAppointment(appointment.id, reason).subscribe({
      next: (updated) => {
        this.updateAppointmentInList(updated);
        if (this.selectedAppointment()?.id === updated.id) {
          this.selectedAppointment.set(updated);
        }
        this.closeDetailsModal();
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
      next: (updated) => {
        this.updateAppointmentInList(updated);
        if (this.selectedAppointment()?.id === updated.id) {
          this.selectedAppointment.set(updated);
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
    this.rescheduleDate.set(appointment.scheduledDate.split('T')[0]);
    this.rescheduleTime.set(appointment.scheduledTime);
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

    this.appointmentService.rescheduleAppointment(appointment.id, newDate, newTime).subscribe({
      next: (updated) => {
        this.updateAppointmentInList(updated);
        this.closeRescheduleModal();
        this.closeDetailsModal();
        this.generateCalendar();
      },
      error: (err) => {
        console.error('Failed to reschedule:', err);
        alert('Failed to reschedule appointment');
      }
    });
  }

  sendReminder(appointment: Appointment, type: 'SMS' | 'EMAIL') {
    this.appointmentService.sendReminder(appointment.id, type as any).subscribe({
      next: (result) => {
        alert(result.message);
      },
      error: (err) => {
        console.error('Failed to send reminder:', err);
        alert('Failed to send reminder');
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

  formatStatus(status: string): string {
    return this.appointmentService.formatStatus(status);
  }

  formatTime(time: string): string {
    return this.appointmentService.formatTime(time);
  }

  formatDuration(minutes: number): string {
    return this.appointmentService.formatDuration(minutes);
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
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

  canStart(appointment: Appointment): boolean {
    return this.appointmentService.canStart(appointment);
  }

  canComplete(appointment: Appointment): boolean {
    return this.appointmentService.canComplete(appointment);
  }

  canCancel(appointment: Appointment): boolean {
    return this.appointmentService.canCancel(appointment);
  }

  canReschedule(appointment: Appointment): boolean {
    return this.appointmentService.canReschedule(appointment);
  }

  getTodayAppointments(): Appointment[] {
    const today = new Date().toISOString().split('T')[0];
    return this.filteredAppointments().filter(a => 
      a.scheduledDate.split('T')[0] === today
    );
  }

  getUpcomingAppointments(): Appointment[] {
    const today = new Date().toISOString().split('T')[0];
    return this.filteredAppointments()
      .filter(a => a.scheduledDate.split('T')[0] >= today)
      .sort((a, b) => {
        const dateCompare = a.scheduledDate.localeCompare(b.scheduledDate);
        if (dateCompare !== 0) return dateCompare;
        return a.scheduledTime.localeCompare(b.scheduledTime);
      });
  }
}

interface CalendarDay {
  date: Date;
  isCurrentMonth: boolean;
  isToday: boolean;
  appointments: Appointment[];
}
