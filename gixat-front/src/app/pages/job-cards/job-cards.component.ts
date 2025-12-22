import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';

interface JobCard {
  id: string;
  jobNumber: string;
  customerName: string;
  vehicle: string;
  status: string;
  createdAt: string;
  estimatedCompletion?: string;
  totalCost?: number;
}

type SortField = 'jobNumber' | 'customerName' | 'status' | 'createdAt';
type SortDirection = 'asc' | 'desc';
type StatusFilter = 'all' | 'pending' | 'in-progress' | 'completed' | 'cancelled';

@Component({
  selector: 'app-job-cards',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './job-cards.component.html',
})
export class JobCardsComponent implements OnInit {
  private router = inject(Router);

  isLoading = signal<boolean>(false);
  errorMessage = signal<string | null>(null);

  // Search & Filter
  searchQuery = signal<string>('');
  statusFilter = signal<StatusFilter>('all');
  showFilters = signal<boolean>(false);

  // Sort
  sortField = signal<SortField>('createdAt');
  sortDirection = signal<SortDirection>('desc');

  // Mock data - will be replaced with real API call
  allJobCards = signal<JobCard[]>([]);

  jobCards = computed(() => {
    let result = this.allJobCards();

    // Apply search filter
    const query = this.searchQuery().toLowerCase();
    if (query) {
      result = result.filter(jc => 
        jc.jobNumber.toLowerCase().includes(query) ||
        jc.customerName.toLowerCase().includes(query) ||
        jc.vehicle.toLowerCase().includes(query)
      );
    }

    // Apply status filter
    const filter = this.statusFilter();
    if (filter !== 'all') {
      result = result.filter(jc => jc.status.toLowerCase() === filter);
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

  ngOnInit() {
    this.loadJobCards();
  }

  loadJobCards() {
    // TODO: Replace with actual API call
    // For now, using empty array
    this.allJobCards.set([]);
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

  viewJobCard(id: string) {
    this.router.navigate(['/dashboard/job-cards', id]);
  }

  getStatusColor(status: string): string {
    switch (status.toLowerCase()) {
      case 'pending':
        return 'bg-yellow-100 text-yellow-700';
      case 'in-progress':
        return 'bg-blue-100 text-blue-700';
      case 'completed':
        return 'bg-emerald-100 text-emerald-700';
      case 'cancelled':
        return 'bg-slate-100 text-slate-700';
      default:
        return 'bg-slate-100 text-slate-600';
    }
  }

  formatStatus(status: string): string {
    return status.replace(/-/g, ' ').replace(/\b\w/g, l => l.toUpperCase());
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', { 
      year: 'numeric', 
      month: 'short', 
      day: 'numeric'
    });
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD'
    }).format(amount);
  }
}
