import { Component, OnInit, inject, signal, computed, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { JobCardService, JobCard, JobItem } from '../../../services/job-card.service';
import { EstimateTimelineComponent } from '../../../components/estimate-timeline/estimate-timeline.component';

@Component({
  selector: 'app-estimate-view',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, EstimateTimelineComponent],
  templateUrl: './estimate-view.component.html'
})
export class EstimateViewComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private jobCardService = inject(JobCardService);

  // Inputs for when used as a child component
  jobCardIdInput = input<string | null>(null);
  readOnlyInput = input<boolean>(false);

  jobCardId = signal<string | null>(null);
  estimate = signal<JobCard | null>(null);
  isLoading = signal<boolean>(true);
  errorMessage = signal<string | null>(null);
  readOnly = signal<boolean>(false);

  selectedItems = signal<Set<string>>(new Set());

  // Computed values
  selectedCosts = computed(() => {
    const estimate = this.estimate();
    const selected = this.selectedItems();
    
    if (!estimate || selected.size === 0) {
      return {
        total: estimate?.totalEstimatedCost || 0,
        labor: estimate?.totalEstimatedLabor || 0,
        parts: estimate?.totalEstimatedParts || 0
      };
    }

    const selectedItems = estimate.items.filter(item => selected.has(item.id));
    return {
      total: selectedItems.reduce((sum, item) => 
        sum + item.estimatedLaborCost + item.estimatedPartsCost, 0),
      labor: selectedItems.reduce((sum, item) => sum + item.estimatedLaborCost, 0),
      parts: selectedItems.reduce((sum, item) => sum + item.estimatedPartsCost, 0)
    };
  });

  isFullApproval = computed(() => {
    const estimate = this.estimate();
    const selected = this.selectedItems();
    return estimate && selected.size === estimate.items.length;
  });

  ngOnInit() {
    // Check if jobCardId is provided as input (when used as child component)
    const inputId = this.jobCardIdInput();
    if (inputId) {
      this.jobCardId.set(inputId);
      this.readOnly.set(this.readOnlyInput());
      this.loadEstimate(inputId);
    } else {
      // Otherwise, get from route params
      const id = this.route.snapshot.paramMap.get('id');
      if (id) {
        this.jobCardId.set(id);
        this.loadEstimate(id);
      } else {
        this.errorMessage.set('Job card ID not found');
        this.isLoading.set(false);
      }
    }
  }

  loadEstimate(id: string) {
    this.isLoading.set(true);
    this.jobCardService.getJobCardById(id).subscribe({
      next: (jobCard) => {
        this.estimate.set(jobCard);
        // Pre-select approved items
        const approvedIds = new Set(
          jobCard.items
            .filter(item => item.isApprovedByCustomer)
            .map(item => item.id)
        );
        this.selectedItems.set(approvedIds);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Error loading estimate:', err);
        this.errorMessage.set('Failed to load estimate. Please try again.');
        this.isLoading.set(false);
      }
    });
  }

  toggleItem(itemId: string) {
    const estimate = this.estimate();
    if (!estimate || estimate.isApprovedByCustomer || this.readOnly()) return;

    const item = estimate.items.find(i => i.id === itemId);
    if (!item || item.isApprovedByCustomer) return;

    const selected = new Set(this.selectedItems());
    if (selected.has(itemId)) {
      selected.delete(itemId);
    } else {
      selected.add(itemId);
    }
    this.selectedItems.set(selected);
  }

  selectAll() {
    const estimate = this.estimate();
    if (!estimate) return;
    
    const allIds = new Set(
      estimate.items
        .filter(item => !item.isApprovedByCustomer)
        .map(item => item.id)
    );
    this.selectedItems.set(allIds);
  }

  deselectAll() {
    this.selectedItems.set(new Set());
  }

  async approveEstimate() {
    const estimate = this.estimate();
    const selected = this.selectedItems();
    
    if (!estimate || selected.size === 0) {
      alert('Please select at least one service to approve');
      return;
    }

    if (!confirm(`Are you sure you want to approve ${selected.size} service${selected.size > 1 ? 's' : ''}?`)) {
      return;
    }

    try {
      if (selected.size === estimate.items.length) {
        // Approve entire job card
        await firstValueFrom(this.jobCardService.approveJobCard(estimate.id));
      } else {
        // Approve individual items
        await firstValueFrom(this.jobCardService.approveJobItems(estimate.id, Array.from(selected)));
      }

      alert('Estimate approved successfully! We will begin work shortly.');
      this.loadEstimate(estimate.id);
    } catch (err) {
      console.error('Error approving estimate:', err);
      alert('Failed to approve estimate. Please try again.');
    }
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD'
    }).format(amount);
  }

  getEstimatedCost(item: JobItem): number {
    return item.estimatedLaborCost + item.estimatedPartsCost;
  }

  requestChanges() {
    alert('Contact us to discuss modifications');
  }
}

