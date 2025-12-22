import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { catchError, of } from 'rxjs';
import { SessionService, Session } from '../../services/session.service';

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

@Component({
  selector: 'app-sessions',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './sessions.component.html',
})
export class SessionsComponent implements OnInit {
  private router = inject(Router);
  private sessionService = inject(SessionService);

  sessions = signal<SessionRowVM[]>([]);
  isLoading = signal<boolean>(true);
  errorMessage = signal<string | null>(null);
  totalCount = signal<number>(0);
  hasNextPage = signal<boolean>(false);
  endCursor = signal<string | null>(null);

  ngOnInit() {
    this.loadSessions();
  }

  loadSessions() {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.sessionService.getSessions(50).pipe(
      catchError((err: Error) => {
        this.errorMessage.set(err instanceof Error ? err.message : 'Failed to load sessions');
        this.isLoading.set(false);
        return of(null);
      })
    ).subscribe((data) => {
      if (data) {
        const sessions = data.edges.map(edge => this.mapToSessionRowVM(edge.node));
        this.sessions.set(sessions);
        this.totalCount.set(data.totalCount);
        this.hasNextPage.set(data.pageInfo.hasNextPage);
        this.endCursor.set(data.pageInfo.endCursor);
      }
      this.isLoading.set(false);
    });
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

  getStatusColor(status: string): string {
    switch (status.toUpperCase()) {
      case 'INTAKE':
        return 'bg-blue-100 text-blue-800';
      case 'IN_PROGRESS':
        return 'bg-yellow-100 text-yellow-800';
      case 'QUALITY_CHECK':
        return 'bg-purple-100 text-purple-800';
      case 'READY_FOR_PICKUP':
        return 'bg-green-100 text-green-800';
      case 'COMPLETED':
        return 'bg-slate-100 text-slate-800';
      default:
        return 'bg-slate-100 text-slate-600';
    }
  }

  formatStatus(status: string): string {
    return status.replace(/_/g, ' ').replace(/\b\w/g, l => l.toUpperCase());
  }
}
