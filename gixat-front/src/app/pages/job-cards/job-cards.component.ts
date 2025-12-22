import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { JobCardService, JobCardStatus, JobCard } from '../../services/job-card.service';

type SortField = 'id' | 'customerName' | 'status' | 'createdAt';
type SortDirection = 'asc' | 'desc';

@Component({
  selector: 'app-job-cards',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './job-cards.component.html',
})
export class JobCardsComponent implements OnInit {
  private router = inject(Router);
  private jobCardService = inject(JobCardService);

  isLoading = signal<boolean>(false);
  errorMessage = signal<string | null>(null);

  // Search & Filter
  searchQuery = signal<string>('');
  statusFilter = signal<JobCardStatus | 'ALL'>('ALL');
  showFilters = signal<boolean>(false);

  // Sort
  sortField = signal<SortField>('createdAt');
  sortDirection = signal<SortDirection>('desc');

  allJobCards = signal<JobCard[]>([]);
  
  jobCardStatuses = Object.values(JobCardStatus);

  jobCards = computed(() => {
    return this.allJobCards();
  });

  ngOnInit() {
    this.loadJobCards();
  }

  loadJobCards() {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    const query = this.searchQuery();
    const status = this.statusFilter() === 'ALL' ? undefined : this.statusFilter() as JobCardStatus;

    this.jobCardService.searchJobCards(query, status).subscribe({
      next: (connection) => {
        this.allJobCards.set(connection.edges.map(e => e.node));
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Error loading job cards:', err);
        this.errorMessage.set('Failed to load job cards. Please try again.');
        this.isLoading.set(false);
      }
    });
  }

  onSearch() {
    this.loadJobCards();
  }

  setStatusFilter(filter: JobCardStatus | 'ALL') {
    this.statusFilter.set(filter);
    this.loadJobCards();
  }

  toggleSort(field: SortField) {
    if (this.sortField() === field) {
      this.sortDirection.set(this.sortDirection() === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortField.set(field);
      this.sortDirection.set('asc');
    }
    // In a real app with server-side sorting, we'd reload here
    // For now we just sort locally or let the service handle it if we add those params
    this.loadJobCards();
  }

  toggleFilters() {
    this.showFilters.update(v => !v);
  }

  viewJobCard(id: string) {
    this.router.navigate(['/dashboard/job-cards', id]);
  }

  getStatusColor(status: string): string {
    switch (status) {
      case 'PENDING':
        return 'bg-yellow-100 text-yellow-700';
      case 'IN_PROGRESS':
        return 'bg-blue-100 text-blue-700';
      case 'COMPLETED':
        return 'bg-emerald-100 text-emerald-700';
      case 'CANCELLED':
        return 'bg-slate-100 text-slate-700';
      default:
        return 'bg-slate-100 text-slate-600';
    }
  }

  formatStatus(status: string): string {
    return status.replace(/_/g, ' ').replace(/\b\w/g, l => l.toUpperCase());
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
