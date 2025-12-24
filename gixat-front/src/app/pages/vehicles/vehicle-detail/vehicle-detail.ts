import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { VehicleService } from '../../../services/vehicle.service';
import { catchError, of } from 'rxjs';

interface VehicleDetail {
  id: string;
  make: string;
  model: string;
  year: number;
  licensePlate: string;
  vin?: string | null;
  color?: string | null;
  mileage?: number;
  customer: {
    id: string;
    firstName: string;
    lastName: string;
    email: string;
    phoneNumber?: string;
  };
  sessions: Array<{
    id: string;
    status: string;
    createdAt: string;
  }>;
}

@Component({
  selector: 'app-vehicle-detail',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './vehicle-detail.html',
})
export class VehicleDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private vehicleService = inject(VehicleService);

  vehicleDetail = signal<VehicleDetail | null>(null);
  isLoading = signal<boolean>(true);
  errorMessage = signal<string | null>(null);

  ngOnInit() {
    const vehicleId = this.route.snapshot.paramMap.get('vehicleId');
    
    if (!vehicleId) {
      this.router.navigate(['/dashboard/vehicles']);
      return;
    }

    this.loadVehicleDetail(vehicleId);
  }

  loadVehicleDetail(vehicleId: string) {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.vehicleService.getVehicleById(vehicleId).pipe(
      catchError((err: Error) => {
        this.errorMessage.set(err instanceof Error ? err.message : 'Failed to load vehicle details');
        this.isLoading.set(false);
        return of(null);
      })
    ).subscribe((data) => {
      if (data) {
        this.vehicleDetail.set(data);
      } else {
        this.errorMessage.set('Vehicle not found');
      }
      this.isLoading.set(false);
    });
  }

  goBack() {
    this.router.navigate(['/dashboard/vehicles']);
  }

  navigateToCustomer() {
    const customerId = this.vehicleDetail()?.customer.id;
    if (customerId) {
      this.router.navigate(['/dashboard/customers', customerId]);
    }
  }

  navigateToSession(sessionId: string) {
    this.router.navigate(['/dashboard/sessions', sessionId]);
  }

  createSession() {
    const vehicleId = this.vehicleDetail()?.id;
    const customerId = this.vehicleDetail()?.customer.id;
    if (vehicleId && customerId) {
      // Navigate to session creation with pre-filled data
      this.router.navigate(['/dashboard/sessions/new'], {
        queryParams: { customerId, vehicleId }
      });
    }
  }

  getStatusColor(status: string): string {
    const statusLower = status.toLowerCase();
    if (statusLower.includes('completed')) return 'bg-emerald-50 text-emerald-700 border-emerald-200';
    if (statusLower.includes('progress') || statusLower.includes('active')) return 'bg-blue-50 text-blue-700 border-blue-200';
    if (statusLower.includes('pending')) return 'bg-amber-50 text-amber-700 border-amber-200';
    if (statusLower.includes('cancelled')) return 'bg-red-50 text-red-700 border-red-200';
    return 'bg-slate-50 text-slate-700 border-slate-200';
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', { 
      year: 'numeric', 
      month: 'short', 
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  formatCurrency(value: number): string {
    return new Intl.NumberFormat('en-US', { 
      style: 'currency', 
      currency: 'USD' 
    }).format(value);
  }

  formatStatus(status: string): string {
    return status.replace(/_/g, ' ').toLowerCase()
      .split(' ')
      .map(word => word.charAt(0).toUpperCase() + word.slice(1))
      .join(' ');
  }
}
