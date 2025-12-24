import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { VehicleService, VehicleWithCustomer } from '../../services/vehicle.service';

type SortField = 'make' | 'model' | 'year' | 'customerName';
type SortDirection = 'asc' | 'desc';

@Component({
  selector: 'app-vehicles',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './vehicles.component.html',
})
export class VehiclesComponent implements OnInit {
  private vehicleService = inject(VehicleService);
  private router = inject(Router);

  vehicles = signal<VehicleWithCustomer[]>([]);
  isLoading = signal<boolean>(true);
  searchQuery = signal<string>('');
  sortField = signal<SortField>('make');
  sortDirection = signal<SortDirection>('asc');

  filteredVehicles = computed(() => {
    let result = [...this.vehicles()];
    
    // Apply search filter
    const query = this.searchQuery().toLowerCase();
    if (query) {
      result = result.filter(vehicle => 
        vehicle.make.toLowerCase().includes(query) ||
        vehicle.model.toLowerCase().includes(query) ||
        vehicle.licensePlate.toLowerCase().includes(query) ||
        vehicle.customer.firstName.toLowerCase().includes(query) ||
        vehicle.customer.lastName.toLowerCase().includes(query)
      );
    }

    // Apply sorting
    const field = this.sortField();
    const direction = this.sortDirection();
    
    result.sort((a, b) => {
      let aVal: string | number;
      let bVal: string | number;

      if (field === 'customerName') {
        aVal = `${a.customer.firstName} ${a.customer.lastName}`.toLowerCase();
        bVal = `${b.customer.firstName} ${b.customer.lastName}`.toLowerCase();
      } else {
        aVal = field === 'year' ? a[field] : a[field].toLowerCase();
        bVal = field === 'year' ? b[field] : b[field].toLowerCase();
      }

      if (aVal < bVal) return direction === 'asc' ? -1 : 1;
      if (aVal > bVal) return direction === 'asc' ? 1 : -1;
      return 0;
    });

    return result;
  });

  ngOnInit() {
    this.loadVehicles();
  }

  loadVehicles() {
    this.isLoading.set(true);
    this.vehicleService.getAllVehicles().subscribe({
      next: (vehicles) => {
        this.vehicles.set(vehicles);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Error loading vehicles:', err);
        this.isLoading.set(false);
      }
    });
  }

  onSearch() {
    // Trigger filtering via computed signal
  }

  toggleSort(field: SortField) {
    if (this.sortField() === field) {
      this.sortDirection.set(this.sortDirection() === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortField.set(field);
      this.sortDirection.set('asc');
    }
  }

  viewVehicle(customerId: string, vehicleId: string) {
    this.router.navigate(['/dashboard/vehicles', vehicleId]);
  }

  viewCustomer(customerId: string) {
    this.router.navigate(['/dashboard/customers', customerId]);
  }
}
