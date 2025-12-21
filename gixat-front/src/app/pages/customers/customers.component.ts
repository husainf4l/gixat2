import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, effect, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { catchError, debounceTime, distinctUntilChanged, map, of, Subject, switchMap } from 'rxjs';
import { Customer, CustomerService, CustomerStatistics } from '../../services/customer.service';
import { FormsModule } from '@angular/forms';
import { AddCustomerModalComponent } from '../../components/add-customer-modal/add-customer-modal.component';
import { AddCarModalComponent } from '../../components/add-car-modal/add-car-modal.component';

type CustomerRowVM = {
  id: string;
  name: string;
  email: string;
  phone: string;
  city: string;
  carsLabel: string;
  lastVisit: string;
  totalVisits: number;
  totalSpent: number;
  activeJobCards: number;
  status: string;
};

@Component({
  selector: 'app-customers',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, AddCustomerModalComponent, AddCarModalComponent],
  templateUrl: './customers.component.html',
})
export class CustomersComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private customerService = inject(CustomerService);

  searchQuery = signal<string>('');
  private searchSubject = new Subject<string>();
  errorMessage = signal<string | null>(null);
  isLoading = signal<boolean>(true);
  showAddModal = signal(false);
  showAddCarModal = signal(false);
  newCustomerId = signal<string | null>(null);
  newCustomerName = signal<string | null>(null);

  // Statistics
  statistics = signal<CustomerStatistics | null>(null);

  // Pagination
  totalCount = signal<number>(0);
  hasNextPage = signal<boolean>(false);
  endCursor = signal<string | null>(null);
  currentPage = signal<number>(1);

  customers = toSignal(
    this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap(query => {
        this.isLoading.set(true);
        this.errorMessage.set(null);

        if (!query || query.trim().length === 0) {
          return this.customerService.getCustomers(100, [{ createdAt: 'DESC' }]).pipe(
            map(list => {
              this.isLoading.set(false);
              return list.map(c => this.mapToCustomerRowVM(c));
            }),
            catchError(err => {
              this.isLoading.set(false);
              this.errorMessage.set(err instanceof Error ? err.message : 'Failed to load customers');
              return of([] as CustomerRowVM[]);
            })
          );
        }

        return this.customerService.searchCustomers(query, 50).pipe(
          map(list => {
            this.isLoading.set(false);
            return list.map(c => this.mapToCustomerRowVM(c));
          }),
          catchError(err => {
            this.isLoading.set(false);
            this.errorMessage.set(err instanceof Error ? err.message : 'Failed to search customers');
            return of([] as CustomerRowVM[]);
          })
        );
      })
    ),
    { initialValue: [] as CustomerRowVM[] }
  );

  ngOnInit() {
    this.loadStatistics();
    this.searchSubject.next('');
  }

  private mapToCustomerRowVM(c: Customer): CustomerRowVM {
    const status = this.getCustomerStatus(c);
    return {
      id: c.id,
      name: `${c.firstName} ${c.lastName}`.trim(),
      email: c.email || '-',
      phone: c.phoneNumber,
      city: c.address?.city || '-',
      carsLabel: c.totalCars ? `${c.totalCars}` : '0',
      lastVisit: c.lastSessionDate || 'Never',
      totalVisits: c.totalVisits || 0,
      totalSpent: c.totalSpent || 0,
      activeJobCards: c.activeJobCards || 0,
      status,
    };
  }

  private getCustomerStatus(customer: Customer): string {
    if (customer.activeJobCards && customer.activeJobCards > 0) return 'Active Job';
    if (!customer.lastSessionDate) return 'New';

    const lastVisit = new Date(customer.lastSessionDate);
    const now = new Date();
    const daysSince = Math.floor((now.getTime() - lastVisit.getTime()) / (1000 * 60 * 60 * 24));

    if (daysSince < 30) return 'Recent';
    if (daysSince < 90) return 'Active';
    return 'Inactive';
  }

  private loadStatistics() {
    this.customerService.getCustomerStatistics().subscribe({
      next: (stats) => {
        this.statistics.set(stats);
      },
      error: (err) => {
        console.error('Failed to load statistics:', err);
      }
    });
  }

  onSearchChange(query: string) {
    this.searchQuery.set(query);
    this.searchSubject.next(query);
  }

  openCustomer(id: string) {
    this.router.navigate(['/dashboard/customers', id]);
  }

  addCustomer() {
    this.showAddModal.set(true);
  }

  onModalClose() {
    this.showAddModal.set(false);
  }

  onCustomerCreated(customerData: { id: string; name: string }) {
    this.showAddModal.set(false);
    // Refresh the list first
    this.searchSubject.next(this.searchQuery());
    // Then open add car modal
    this.newCustomerId.set(customerData.id);
    this.newCustomerName.set(customerData.name);
    this.showAddCarModal.set(true);
  }

  onCarModalClose() {
    this.showAddCarModal.set(false);
    this.newCustomerId.set(null);
    this.newCustomerName.set(null);
  }

  onCarCreated() {
    this.showAddCarModal.set(false);
    this.newCustomerId.set(null);
    this.newCustomerName.set(null);
    // Refresh the list
    this.searchSubject.next(this.searchQuery());
  }

  onCreateSession(data: { carId: string; customerId: string }) {
    this.showAddCarModal.set(false);
    this.newCustomerId.set(null);
    this.newCustomerName.set(null);
    // Navigate to create session page with car and customer IDs
    // TODO: Implement session creation page
    console.log('Create session for car:', data.carId, 'customer:', data.customerId);
    alert('Session creation page will be implemented. CarID: ' + data.carId);
    // Refresh the list
    this.searchSubject.next(this.searchQuery());
  }

  exportToCsv() {
    this.customerService.exportCustomersToCsv().subscribe({
      next: (base64Data) => {
        // Decode base64 to CSV
        const csvContent = atob(base64Data);

        // Create blob and download
        const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        const today = new Date().toISOString().split('T')[0];
        link.download = `customers-${today}.csv`;
        link.click();
        URL.revokeObjectURL(url);
      },
      error: (err) => {
        this.errorMessage.set(err instanceof Error ? err.message : 'Failed to export customers');
      }
    });
  }
}