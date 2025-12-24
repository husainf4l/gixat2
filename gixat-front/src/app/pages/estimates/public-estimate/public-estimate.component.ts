import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { JobCardService, JobCard } from '../../../services/job-card.service';
import { EstimateViewComponent } from '../estimate-view/estimate-view.component';

@Component({
  selector: 'app-public-estimate',
  standalone: true,
  imports: [CommonModule, EstimateViewComponent],
  template: `
    <div class="min-h-screen bg-slate-50">
      @if (isLoading()) {
        <div class="min-h-screen flex items-center justify-center">
          <div class="text-center space-y-4">
            <div class="w-12 h-12 border-4 border-slate-200 border-t-[#1b75bc] rounded-full animate-spin mx-auto"></div>
            <p class="text-sm text-slate-600">Loading estimate...</p>
          </div>
        </div>
      } @else if (errorMessage()) {
        <div class="min-h-screen flex items-center justify-center p-6">
          <div class="text-center space-y-4 max-w-md">
            <i class="ri-alert-triangle-line text-6xl text-amber-500 mx-auto"></i>
            <h1 class="text-2xl font-semibold text-slate-900">Link Expired or Invalid</h1>
            <p class="text-sm text-slate-600">
              {{ errorMessage() }}
            </p>
            <p class="text-xs text-slate-500">
              Please contact the garage for a new link.
            </p>
          </div>
        </div>
      } @else if (jobCardId()) {
        <app-estimate-view [jobCardIdInput]="jobCardId()!" [readOnlyInput]="false"></app-estimate-view>
      }
    </div>
  `
})
export class PublicEstimateComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private jobCardService = inject(JobCardService);

  shareToken = signal<string | null>(null);
  jobCardId = signal<string | null>(null);
  isLoading = signal<boolean>(true);
  errorMessage = signal<string | null>(null);

  ngOnInit() {
    const token = this.route.snapshot.paramMap.get('token');
    if (token) {
      this.shareToken.set(token);
      this.loadEstimateByToken(token);
    } else {
      this.errorMessage.set('Share token not found');
      this.isLoading.set(false);
    }
  }

  loadEstimateByToken(token: string) {
    this.isLoading.set(true);
    this.jobCardService.getEstimateByShareToken(token).subscribe({
      next: (data) => {
        if (!data.isActive) {
          this.errorMessage.set('This estimate link has been revoked.');
          this.isLoading.set(false);
          return;
        }

        const expiresAt = new Date(data.expiresAt);
        if (expiresAt < new Date()) {
          this.errorMessage.set('This estimate link has expired.');
          this.isLoading.set(false);
          return;
        }

        this.jobCardId.set(data.jobCardId);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Error loading estimate by token:', err);
        this.errorMessage.set('Invalid or expired estimate link.');
        this.isLoading.set(false);
      }
    });
  }
}

