import { Component, input, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { JobItem } from '../../services/job-card.service';

@Component({
  selector: 'app-estimate-timeline',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './estimate-timeline.component.html'
})
export class EstimateTimelineComponent {
  approvedAt = input<string | null | undefined>(null);
  items = input<JobItem[]>([]);

  approvedItems = computed(() => {
    return this.items().filter(i => i.isApprovedByCustomer && i.approvedAt);
  });

  formatDate(dateString: string | null | undefined): string {
    if (!dateString) return 'N/A';
    return new Date(dateString).toLocaleString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }
}

