import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, effect, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { catchError, debounceTime, distinctUntilChanged, map, of, Subject, switchMap } from 'rxjs';
import { CustomerService } from '../../services/customer.service';
import { FormsModule } from '@angular/forms';
import { AddCustomerModalComponent } from '../../components/add-customer-modal/add-customer-modal.component';
import { AddCarModalComponent } from '../../components/add-car-modal/add-car-modal.component';

type CustomerRowVM = {
  id: string;
  name: string;
  phone: string;
  carsLabel: string;
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
  showAddModal = signal(false);
  showAddCarModal = signal(false);
  newCustomerId = signal<string | null>(null);
  newCustomerName = signal<string | null>(null);

  customers = toSignal(
    this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap(query => {
        if (!query || query.trim().length === 0) {
          return this.customerService.getCustomers(100, [{ createdAt: 'DESC' }]).pipe(
            map(list => list.map(c => ({
              id: c.id,
              name: `${c.firstName} ${c.lastName}`.trim(),
              phone: c.phoneNumber,
              carsLabel: c.cars?.length ? `${c.cars.length} car${c.cars.length > 1 ? 's' : ''}` : 'No cars',
            } satisfies CustomerRowVM))),
            catchError(err => {
              this.errorMessage.set(err instanceof Error ? err.message : 'Failed to load customers');
              return of([] as CustomerRowVM[]);
            })
          );
        }
        
        return this.customerService.searchCustomers(query, 50).pipe(
          map(list => list.map(c => ({
            id: c.id,
            name: `${c.firstName} ${c.lastName}`.trim(),
            phone: c.phoneNumber,
            carsLabel: c.cars?.length 
              ? c.cars.map(car => `${car.make} ${car.model}`).join(', ')
              : 'No cars',
          } satisfies CustomerRowVM))),
          catchError(err => {
            this.errorMessage.set(err instanceof Error ? err.message : 'Failed to search customers');
            return of([] as CustomerRowVM[]);
          })
        );
      })
    ),
    { initialValue: [] as CustomerRowVM[] }
  );

  ngOnInit() {
    this.searchSubject.next('');
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
}