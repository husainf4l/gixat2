import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { catchError, debounceTime, distinctUntilChanged, Subject, switchMap, map, of } from 'rxjs';
import { toSignal } from '@angular/core/rxjs-interop';
import { SessionService, Session } from '../../services/session.service';
import { FormsModule } from '@angular/forms';
import { PaginationComponent } from '../../components/pagination/pagination.component';

interface SessionRowVM {
  id: string;
  sessionNumber: string;
  customerName: string;
  carInfo: string;
  licensePlate: string;
  status: string;
  createdAt: string;
  formattedDate: string;
}

type SortField = 'sessionNumber' | 'customerName' | 'createdAt' | 'status';
type SortDirection = 'asc' | 'desc';
type StatusFilter = 'all' | 'in-progress' | 'quality-check' | 'ready-for-pickup' | 'completed';

@Component({
  selector: 'app-sessions',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, PaginationComponent],
  templateUrl: './sessions.component.html',
})
export class SessionsComponent implements OnInit {
  private router = inject(Router);
  private sessionService = inject(SessionService);

  isLoading = signal<boolean>(true);
  errorMessage = signal<string | null>(null);
  totalCount = signal<number>(0);
  hasNextPage = signal<boolean>(false);
  endCursor = signal<string | null>(null);

  // Search & Filter
  searchQuery = signal<string>('');
  private searchSubject = new Subject<string>();
  statusFilter = signal<StatusFilter>('all');
  showFilters = signal<boolean>(false);

  // Sort
  sortField = signal<SortField>('createdAt');
  sortDirection = signal<SortDirection>('desc');

  // Pagination
  Math = Math;
  currentPage = signal<number>(1);
  pageSize = signal<number>(20);

  readonly statusFilters: StatusFilter[] = ['in-progress', 'quality-check', 'ready-for-pickup', 'completed'];

  private allSessions = toSignal(
    this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap(() => {
        this.isLoading.set(true);
        this.errorMessage.set(null);
        return this.sessionService.getSessions(50).pipe(
          map(data => {
            this.isLoading.set(false);
            if (data) {
              this.totalCount.set(data.totalCount);
              this.hasNextPage.set(data.pageInfo.hasNextPage);
              this.endCursor.set(data.pageInfo.endCursor);
              return data.edges.map(edge => this.mapToSessionRowVM(edge.node));
            }
            return [];
          }),
          catchError((err: Error) => {
            this.errorMessage.set(err instanceof Error ? err.message : 'Failed to load sessions');
            this.isLoading.set(false);
            return of([] as SessionRowVM[]);
          })
        );
      })
    ),
    { initialValue: [] as SessionRowVM[] }
  );

  sessions = computed(() => {
    let result = this.allSessions() || [];

    // Apply search filter
    const query = this.searchQuery().toLowerCase();
    if (query) {
      result = result.filter(s => 
        s.customerName.toLowerCase().includes(query) ||
        s.carInfo.toLowerCase().includes(query) ||
        s.licensePlate.toLowerCase().includes(query) ||
        s.sessionNumber.toLowerCase().includes(query)
      );
    }

    // Apply status filter
    const filter = this.statusFilter();
    if (filter !== 'all') {
      result = result.filter(s => s.status.toLowerCase().replace(/_/g, '-') === filter);
    }

    // Apply sorting
    const field = this.sortField();
    const direction = this.sortDirection();
    result = [...result].sort((a, b) => {
      let aVal: any = a[field];
      let bVal: any = b[field];

      if (field === 'createdAt') {
        aVal = new Date(a.createdAt).getTime();
        bVal = new Date(b.createdAt).getTime();
      } else if (typeof aVal === 'string') {
        aVal = aVal.toLowerCase();
        bVal = bVal.toLowerCase();
      }

      if (aVal < bVal) return direction === 'asc' ? -1 : 1;
      if (aVal > bVal) return direction === 'asc' ? 1 : -1;
      return 0;
    });

    return result;
  });

  totalPages = computed(() => Math.ceil(this.sessions().length / this.pageSize()));

  paginatedSessions = computed(() => {
    const start = (this.currentPage() - 1) * this.pageSize();
    const end = start + this.pageSize();
    return this.sessions().slice(start, end);
  });

  ngOnInit() {
    this.searchSubject.next('');
  }

  onSearchChange(query: string) {
    this.searchQuery.set(query);
  }

  toggleSort(field: SortField) {
    if (this.sortField() === field) {
      this.sortDirection.set(this.sortDirection() === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortField.set(field);
      this.sortDirection.set('asc');
    }
  }

  setStatusFilter(filter: StatusFilter) {
    this.statusFilter.set(filter);
  }

  toggleFilters() {
    this.showFilters.update(v => !v);
  }

  private mapToSessionRowVM(session: Session): SessionRowVM {
    return {
      id: session.id,
      sessionNumber: `#${session.id.slice(0, 8)}`,
      customerName: session.customer 
        ? `${session.customer.firstName} ${session.customer.lastName}`
        : 'Unknown Customer',
      carInfo: session.car 
        ? `${session.car.make} ${session.car.model}`
        : 'Unknown Vehicle',
      licensePlate: session.car?.licensePlate || 'N/A',
      status: session.status,
      createdAt: session.createdAt,
      formattedDate: this.formatDate(session.createdAt)
    };
  }

  private formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', { 
      year: 'numeric', 
      month: 'short', 
      day: 'numeric'
    });
  }

  viewSession(sessionId: string) {
    this.router.navigate(['/dashboard/sessions', sessionId]);
  }

  createNewSession() {
    // Navigate to customers page to select customer and car
    this.router.navigate(['/dashboard/customers']);
  }

  goToPage(page: number) {
    if (page >= 1 && page <= this.totalPages()) {
      this.currentPage.set(page);
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  }

  getStatusColor(status: string): string {
    switch (status.toUpperCase()) {
      case 'CUSTOMERREQUEST':
        return 'bg-blue-100 text-blue-800';
      case 'INSPECTION':
        return 'bg-purple-100 text-purple-800';
      case 'TESTDRIVE':
        return 'bg-yellow-100 text-yellow-800';
      case 'REPORTGENERATED':
        return 'bg-indigo-100 text-indigo-800';
      case 'JOBCARDCREATED':
        return 'bg-green-100 text-green-800';
      case 'COMPLETED':
        return 'bg-slate-100 text-slate-800';
      case 'CANCELLED':
        return 'bg-red-100 text-red-800';
      default:
        return 'bg-slate-100 text-slate-600';
    }
  }

  formatStatus(status: string): string {
    return status.replace(/_/g, ' ').replace(/\b\w/g, l => l.toUpperCase());
  }
}