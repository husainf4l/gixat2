import { Injectable, inject } from '@angular/core';
import { Apollo, gql } from 'apollo-angular';
import { Observable, map, combineLatest } from 'rxjs';
import { CustomerService } from './customer.service';
import { VehicleService } from './vehicle.service';
import { SessionService } from './session.service';

export interface SearchResult {
  type: 'customer' | 'vehicle' | 'session';
  id: string;
  title: string;
  subtitle: string;
  description?: string;
  url: string;
}

@Injectable({
  providedIn: 'root'
})
export class SearchService {
  private customerService = inject(CustomerService);
  private vehicleService = inject(VehicleService);
  private sessionService = inject(SessionService);

  globalSearch(query: string): Observable<SearchResult[]> {
    if (!query || query.trim().length < 2) {
      return new Observable(observer => {
        observer.next([]);
        observer.complete();
      });
    }

    const searchQuery = query.trim();

    // Search customers
    const customers$ = this.customerService.searchCustomers(searchQuery, 5).pipe(
      map(customers => customers.map(customer => ({
        type: 'customer' as const,
        id: customer.id,
        title: `${customer.firstName} ${customer.lastName}`,
        subtitle: customer.phoneNumber || customer.email || '',
        description: customer.cars?.length ? `${customer.cars.length} vehicle(s)` : undefined,
        url: `/dashboard/customers/${customer.id}`
      })))
    );

    // Search vehicles
    const vehicles$ = this.vehicleService.getAllVehicles().pipe(
      map(vehicles => {
        const filtered = vehicles.filter(v => 
          v.make.toLowerCase().includes(searchQuery.toLowerCase()) ||
          v.model.toLowerCase().includes(searchQuery.toLowerCase()) ||
          v.licensePlate.toLowerCase().includes(searchQuery.toLowerCase()) ||
          v.customer.firstName.toLowerCase().includes(searchQuery.toLowerCase()) ||
          v.customer.lastName.toLowerCase().includes(searchQuery.toLowerCase())
        ).slice(0, 5);

        return filtered.map(vehicle => ({
          type: 'vehicle' as const,
          id: vehicle.id,
          title: `${vehicle.make} ${vehicle.model}`,
          subtitle: vehicle.licensePlate,
          description: `Owner: ${vehicle.customer.firstName} ${vehicle.customer.lastName}`,
          url: `/dashboard/vehicles/${vehicle.id}`
        }));
      })
    );

    // Search sessions
    const sessions$ = this.sessionService.getSessions(100).pipe(
      map(result => {
        const sessions = result.edges.map(edge => edge.node);
        const filtered = sessions.filter(s => 
          s.id.toLowerCase().includes(searchQuery.toLowerCase()) ||
          s.customer?.firstName?.toLowerCase().includes(searchQuery.toLowerCase()) ||
          s.customer?.lastName?.toLowerCase().includes(searchQuery.toLowerCase()) ||
          s.car?.make?.toLowerCase().includes(searchQuery.toLowerCase()) ||
          s.car?.model?.toLowerCase().includes(searchQuery.toLowerCase())
        ).slice(0, 5);

        return filtered.map(session => ({
          type: 'session' as const,
          id: session.id,
          title: `Session #${session.id.split('-')[0]}`,
          subtitle: session.customer ? `${session.customer.firstName} ${session.customer.lastName}` : 'Unknown',
          description: session.car ? `${session.car.make} ${session.car.model}` : undefined,
          url: `/dashboard/sessions/${session.id}`
        }));
      })
    );

    return combineLatest([customers$, vehicles$, sessions$]).pipe(
      map(([customers, vehicles, sessions]) => {
        return [...customers, ...vehicles, ...sessions];
      })
    );
  }
}
