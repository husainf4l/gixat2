import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { CustomerService, CustomerDetail as CustomerDetailData } from '../../../services/customer.service';
import { catchError, of } from 'rxjs';
import { AddCarModalComponent } from '../../../components/add-car-modal/add-car-modal.component';

@Component({
  selector: 'app-customer-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, AddCarModalComponent],
  templateUrl: './customer-detail.html',
  styleUrl: './customer-detail.css',
})
export class CustomerDetail implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private customerService = inject(CustomerService);

  customerDetail = signal<CustomerDetailData | null>(null);
  isLoading = signal<boolean>(true);
  errorMessage = signal<string | null>(null);
  showAddCarModal = signal<boolean>(false);

  ngOnInit() {
    const customerId = this.route.snapshot.paramMap.get('id');
    if (!customerId) {
      this.router.navigate(['/dashboard/customers']);
      return;
    }

    this.loadCustomerDetail(customerId);
  }

  loadCustomerDetail(id: string) {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.customerService.getCustomerDetail(id).pipe(
      catchError((err: Error) => {
        this.errorMessage.set(err instanceof Error ? err.message : 'Failed to load customer details');
        this.isLoading.set(false);
        return of(null);
      })
    ).subscribe((data: CustomerDetailData | null) => {
      this.customerDetail.set(data);
      this.isLoading.set(false);
    });
  }

  goBack() {
    this.router.navigate(['/dashboard/customers']);
  }

  getStatusColor(status: string): string {
    const statusLower = status.toLowerCase();
    if (statusLower.includes('completed') || statusLower.includes('done')) return 'bg-green-100 text-green-800';
    if (statusLower.includes('progress') || statusLower.includes('active')) return 'bg-blue-100 text-blue-800';
    if (statusLower.includes('pending') || statusLower.includes('waiting')) return 'bg-yellow-100 text-yellow-800';
    if (statusLower.includes('cancelled') || statusLower.includes('failed')) return 'bg-red-100 text-red-800';
    return 'bg-slate-100 text-slate-800';
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
  }

  formatCurrency(value: string | number): string {
    const num = typeof value === 'string' ? parseFloat(value) : value;
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(num);
  }

  getInitial(): string {
    const firstName = this.customerDetail()?.customer?.firstName;
    return (firstName && firstName.length > 0) ? firstName[0].toUpperCase() : '?';
  }

  hasCars(): boolean {
    const cars = this.customerDetail()?.customer?.cars;
    return !!(cars && cars.length > 0);
  }

  getCars() {
    return this.customerDetail()?.customer?.cars || [];
  }

  openAddCarModal() {
    this.showAddCarModal.set(true);
  }

  closeAddCarModal() {
    this.showAddCarModal.set(false);
  }

  onCarCreated() {
    this.showAddCarModal.set(false);
    // Reload customer details to show the new car
    const customerId = this.route.snapshot.paramMap.get('id');
    if (customerId) {
      this.loadCustomerDetail(customerId);
    }
  }

  onCreateSession(event: { carId: string; customerId: string }) {
    this.showAddCarModal.set(false);
    
    // Create the session via API
    this.customerService.createSession(event.carId, event.customerId).subscribe({
      next: (session) => {
        // Reload customer details to show the new session
        const customerId = this.route.snapshot.paramMap.get('id');
        if (customerId) {
          this.loadCustomerDetail(customerId);
        }
        
        // Show success message
        alert(`Session created successfully!\n\nSession ID: ${session.id}\nStatus: ${session.status}`);
        
        // TODO: Navigate to session detail page once implemented
        // this.router.navigate(['/dashboard/sessions', session.id]);
      },
      error: (err) => {
        // Show error message
        const errorMsg = err instanceof Error ? err.message : 'Failed to create session';
        alert(`Error creating session: ${errorMsg}`);
        
        // Still reload to show the new car
        const customerId = this.route.snapshot.paramMap.get('id');
        if (customerId) {
          this.loadCustomerDetail(customerId);
        }
      }
    });
  }

  createSessionForCar(carId: string) {
    const customerId = this.route.snapshot.paramMap.get('id');
    if (!customerId) return;
    
    // Create the session via API
    this.customerService.createSession(carId, customerId).subscribe({
      next: (session) => {
        // Reload customer details to show the new session
        this.loadCustomerDetail(customerId);
        
        // Show success message
        alert(`Session created successfully!\n\nSession ID: ${session.id}\nStatus: ${session.status}`);
        
        // TODO: Navigate to session detail page once implemented
        // this.router.navigate(['/dashboard/sessions', session.id]);
      },
      error: (err) => {
        // Show error message
        const errorMsg = err instanceof Error ? err.message : 'Failed to create session';
        alert(`Error creating session: ${errorMsg}`);
      }
    });
  }

  getCustomerId(): string {
    return this.customerDetail()?.customer?.id || '';
  }

  getCustomerName(): string {
    const customer = this.customerDetail()?.customer;
    return customer ? `${customer.firstName} ${customer.lastName}` : '';
  }

  getActiveSessionForCar(carId: string) {
    const sessions = this.customerDetail()?.sessions || [];
    // Find active session (INTAKE or IN_PROGRESS status) for this car
    return sessions.find(s => 
      s.carId === carId && 
      (s.status === 'INTAKE' || s.status === 'IN_PROGRESS' || s.status === 'Intake' || s.status === 'InProgress')
    );
  }

  hasActiveSession(carId: string): boolean {
    return !!this.getActiveSessionForCar(carId);
  }

  navigateToSession(carId: string) {
    const activeSession = this.getActiveSessionForCar(carId);
    if (activeSession) {
      // TODO: Navigate to session detail page once implemented
      alert(`Navigate to Session: ${activeSession.id}\nStatus: ${activeSession.status}`);
      // this.router.navigate(['/dashboard/sessions', activeSession.id]);
    }
  }
}
