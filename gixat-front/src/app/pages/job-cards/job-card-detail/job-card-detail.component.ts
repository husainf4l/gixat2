import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { JobCardService, JobCard, JobCardStatus, JobItemStatus, JobItem, JobCardMediaType, Technician } from '../../../services/job-card.service';
import { AuthService } from '../../../services/auth.service';
import { ChatPanelComponent } from '../../../components/job-card-chat/chat-panel.component';
import { LiveWorkshopAssistantComponent } from '../../../components/live-workshop-assistant/live-workshop-assistant.component';

@Component({
  selector: 'app-job-card-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, ChatPanelComponent, LiveWorkshopAssistantComponent],
  templateUrl: './job-card-detail.component.html',
})
export class JobCardDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private jobCardService = inject(JobCardService);
  private authService = inject(AuthService);

  jobCard = signal<JobCard | null>(null);
  isLoading = signal<boolean>(true);
  errorMessage = signal<string | null>(null);
  
  // Live Workshop Assistant
  showLiveAssistant = signal<boolean>(false);
  
  technicians = signal<Technician[]>([]);

  // Add Item Modal/Form
  showAddItemModal = signal<boolean>(false);
  newItem = {
    description: '',
    estimatedLaborCost: 0,
    estimatedPartsCost: 0,
    assignedTechnicianId: ''
  };

  // Update Item Modal/Form
  showUpdateItemModal = signal<boolean>(false);
  selectedItem = signal<JobItem | null>(null);
  updateData = {
    status: JobItemStatus.PENDING,
    actualLaborCost: 0,
    actualPartsCost: 0,
    technicianNotes: ''
  };

  // Technician Assignment Modal
  showAssignTechModal = signal<boolean>(false);
  assignTarget = signal<{ type: 'job' | 'item', id: string } | null>(null);
  selectedTechId = signal<string>('');

  // Media Upload Modal
  showMediaUploadModal = signal<boolean>(false);
  mediaUploadTarget = signal<{ type: 'job' | 'item', id: string } | null>(null);
  selectedMediaType = signal<JobCardMediaType>(JobCardMediaType.DURING_WORK);
  selectedFile = signal<File | null>(null);
  isUploadingMedia = signal<boolean>(false);

  // Share Link Modal
  showShareLinkModal = signal<boolean>(false);
  shareLink = signal<string | null>(null);
  shareLinkExpiresAt = signal<string | null>(null);
  isGeneratingLink = signal<boolean>(false);

  jobCardStatuses = Object.values(JobCardStatus);
  jobItemStatuses = Object.values(JobItemStatus);
  mediaTypes = Object.values(JobCardMediaType);

  // Make enums available to template
  JobCardStatus = JobCardStatus;
  JobItemStatus = JobItemStatus;
  JobCardMediaType = JobCardMediaType;

  // Active Tab
  activeTab = signal<'items' | 'media' | 'costs' | 'chat'>('items');

  getMediaByType(media: any[], type: JobCardMediaType) {
    return media.filter(m => m.type === type);
  }

  openShareLinkModal() {
    this.showShareLinkModal.set(true);
    this.shareLink.set(null);
    this.shareLinkExpiresAt.set(null);
  }

  async generateShareLink() {
    const current = this.jobCard();
    if (!current) return;

    this.isGeneratingLink.set(true);
    try {
      // Generate link that expires in 72 hours (3 days)
      const result = await firstValueFrom(
        this.jobCardService.generateEstimateShareLink(current.id, 72)
      );
      
      // Construct the full URL
      const baseUrl = window.location.origin;
      const fullUrl = `${baseUrl}/e/${result.shareToken}`;
      
      this.shareLink.set(fullUrl);
      this.shareLinkExpiresAt.set(result.expiresAt);
    } catch (err) {
      console.error('Error generating share link:', err);
      alert('Failed to generate share link. Please try again.');
    } finally {
      this.isGeneratingLink.set(false);
    }
  }

  copyShareLink() {
    const link = this.shareLink();
    if (!link) return;

    navigator.clipboard.writeText(link).then(() => {
      alert('Share link copied to clipboard!');
    }).catch(() => {
      alert('Failed to copy link. Please copy manually.');
    });
  }

  canCompleteItemWithUpdateData(item: JobItem | null): boolean {
    if (!item) return false;
    return this.jobCardService.canCompleteJobItem({ 
      ...item, 
      ...this.updateData 
    } as JobItem);
  }

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadJobCard(id);
      this.loadTechnicians();
    } else {
      this.errorMessage.set('Job card ID not found');
      this.isLoading.set(false);
    }
  }

  loadJobCard(id: string) {
    this.isLoading.set(true);
    this.jobCardService.getJobCardById(id).subscribe({
      next: (jobCard) => {
        this.jobCard.set(jobCard);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Error loading job card:', err);
        this.errorMessage.set('Failed to load job card details.');
        this.isLoading.set(false);
      }
    });
  }

  loadTechnicians() {
    this.authService.getOrganizationUsers().subscribe({
      next: (users) => {
        this.technicians.set(users.map(u => ({
          id: u.id,
          fullName: u.fullName || u.email,
          email: u.email
        })));
      }
    });
  }

  updateJobCardStatus(status: JobCardStatus) {
    const current = this.jobCard();
    if (!current) return;

    // Business rule: Cannot complete if items are not all completed/cancelled
    if (status === JobCardStatus.COMPLETED && !this.jobCardService.canCompleteJobCard(current)) {
      alert('Cannot complete job card. All items must be completed or cancelled first.');
      return;
    }

    this.jobCardService.updateJobCardStatus(current.id, status).subscribe({
      next: (updated) => {
        this.loadJobCard(current.id);
      },
      error: (err) => {
        console.error('Error updating status:', err);
        alert(err.message || 'Failed to update status');
      }
    });
  }

  approveJobCard() {
    const current = this.jobCard();
    if (!current) return;

    if (!confirm('Are you sure you want to mark this job card as approved by the customer?')) return;

    this.jobCardService.approveJobCard(current.id).subscribe({
      next: () => {
        this.loadJobCard(current.id);
      },
      error: (err) => {
        console.error('Error approving job card:', err);
        alert('Failed to approve job card');
      }
    });
  }

  openAddItemModal() {
    this.newItem = { 
      description: '', 
      estimatedLaborCost: 0, 
      estimatedPartsCost: 0,
      assignedTechnicianId: this.jobCard()?.assignedTechnicianId || ''
    };
    this.showAddItemModal.set(true);
  }

  addItem() {
    const current = this.jobCard();
    if (!current || !this.newItem.description) return;

    this.jobCardService.addJobItem(
      current.id, 
      this.newItem.description, 
      this.newItem.estimatedLaborCost,
      this.newItem.estimatedPartsCost,
      this.newItem.assignedTechnicianId || undefined
    ).subscribe({
      next: () => {
        this.loadJobCard(current.id);
        this.showAddItemModal.set(false);
      },
      error: (err) => {
        console.error('Error adding item:', err);
        alert('Failed to add job item');
      }
    });
  }

  openUpdateItemModal(item: JobItem) {
    this.selectedItem.set(item);
    this.updateData = {
      status: item.status,
      actualLaborCost: item.actualLaborCost || item.estimatedLaborCost,
      actualPartsCost: item.actualPartsCost || item.estimatedPartsCost,
      technicianNotes: item.technicianNotes || ''
    };
    this.showUpdateItemModal.set(true);
  }

  updateItem() {
    const item = this.selectedItem();
    const current = this.jobCard();
    if (!item || !current) return;

    // Business rule: Cannot start if not approved
    if (this.updateData.status === JobItemStatus.IN_PROGRESS && !item.isApprovedByCustomer) {
      alert('Cannot start work on this item. It must be approved by the customer first.');
      return;
    }

    // Business rule: Cannot complete without actual costs
    if (this.updateData.status === JobItemStatus.COMPLETED) {
      if (!this.jobCardService.canCompleteJobItem({ ...item, ...this.updateData } as JobItem)) {
        alert('Cannot complete item. Please enter actual labor or parts cost.');
        return;
      }
    }

    this.jobCardService.updateJobItemStatus(
      item.id,
      this.updateData.status,
      this.updateData.actualLaborCost,
      this.updateData.actualPartsCost,
      this.updateData.technicianNotes
    ).subscribe({
      next: () => {
        this.loadJobCard(current.id);
        this.showUpdateItemModal.set(false);
      },
      error: (err) => {
        console.error('Error updating item:', err);
        alert(err.message || 'Failed to update item');
      }
    });
  }

  approveJobItem(itemId: string) {
    if (!confirm('Approve this job item for the customer?')) return;

    this.jobCardService.approveJobItem(itemId).subscribe({
      next: () => {
        const current = this.jobCard();
        if (current) this.loadJobCard(current.id);
      },
      error: (err) => {
        console.error('Error approving job item:', err);
        alert('Failed to approve job item');
      }
    });
  }

  openAssignTechModal(type: 'job' | 'item', id: string, currentTechId?: string | null) {
    this.assignTarget.set({ type, id });
    this.selectedTechId.set(currentTechId || '');
    this.showAssignTechModal.set(true);
  }

  assignTechnician() {
    const target = this.assignTarget();
    if (!target) return;

    if (target.type === 'job') {
      this.jobCardService.assignTechnicianToJobCard(target.id, this.selectedTechId()).subscribe({
        next: () => {
          const current = this.jobCard();
          if (current) this.loadJobCard(current.id);
          this.showAssignTechModal.set(false);
        },
        error: (err: Error) => {
          console.error('Error assigning technician:', err);
          alert('Failed to assign technician');
        }
      });
    } else {
      this.jobCardService.assignTechnicianToJobItem(target.id, this.selectedTechId()).subscribe({
        next: () => {
          const current = this.jobCard();
          if (current) this.loadJobCard(current.id);
          this.showAssignTechModal.set(false);
        },
        error: (err: Error) => {
          console.error('Error assigning technician:', err);
          alert('Failed to assign technician');
        }
      });
    }
  }

  openMediaUploadModal(type: 'job' | 'item', id: string) {
    this.mediaUploadTarget.set({ type, id });
    this.selectedMediaType.set(JobCardMediaType.DURING_WORK);
    this.selectedFile.set(null);
    this.showMediaUploadModal.set(true);
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    // Validate file
    const maxSize = 50 * 1024 * 1024; // 50MB
    if (file.size > maxSize) {
      alert('File size exceeds 50MB limit. Please choose a smaller file.');
      return;
    }

    const allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif', 'image/webp', 'image/heic', 'video/mp4', 'video/mpeg', 'video/quicktime', 'video/x-msvideo', 'video/webm'];
    if (!allowedTypes.includes(file.type)) {
      alert('File type not allowed. Please upload images or videos only.');
      return;
    }

    this.selectedFile.set(file);
  }

  async uploadMedia() {
    const target = this.mediaUploadTarget();
    const file = this.selectedFile();
    if (!target || !file) return;

    this.isUploadingMedia.set(true);

    try {
      if (target.type === 'item') {
        await firstValueFrom(this.jobCardService.uploadMediaToJobItem(
          target.id,
          file,
          this.selectedMediaType(),
          file.name
        ));
      } else {
        await firstValueFrom(this.jobCardService.uploadMediaToJobCard(
          target.id,
          file,
          this.selectedMediaType(),
          file.name
        ));
      }

      const current = this.jobCard();
      if (current) this.loadJobCard(current.id);
      this.showMediaUploadModal.set(false);
      this.selectedFile.set(null);
    } catch (err: any) {
      console.error('Error uploading media:', err);
      alert(err.message || 'Failed to upload media');
    } finally {
      this.isUploadingMedia.set(false);
    }
  }

  openMedia(url: string) {
    window.open(url, '_blank');
  }

  getCostVariance(jobCard: JobCard): { amount: number; percentage: number } {
    const variance = jobCard.totalActualCost - jobCard.totalEstimatedCost;
    const percentage = jobCard.totalEstimatedCost > 0 
      ? (variance / jobCard.totalEstimatedCost) * 100 
      : 0;
    return { amount: variance, percentage };
  }

  canStartItem(item: JobItem): boolean {
    return this.jobCardService.canStartJobItem(item);
  }

  canCompleteItem(item: JobItem): boolean {
    return this.jobCardService.canCompleteJobItem(item);
  }

  getStatusColor(status: string): string {
    switch (status) {
      case 'PENDING': return 'bg-yellow-100 text-yellow-700';
      case 'IN_PROGRESS': return 'bg-blue-100 text-blue-700';
      case 'COMPLETED': return 'bg-emerald-100 text-emerald-700';
      case 'CANCELLED': return 'bg-slate-100 text-slate-700';
      default: return 'bg-slate-100 text-slate-600';
    }
  }

  formatStatus(status: string): string {
    return status.replace(/_/g, ' ').replace(/\b\w/g, l => l.toUpperCase());
  }

  formatDate(dateString: string): string {
    if (!dateString) return 'N/A';
    return new Date(dateString).toLocaleString('en-US', {
      year: 'numeric', month: 'short', day: 'numeric',
      hour: '2-digit', minute: '2-digit'
    });
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency', currency: 'USD'
    }).format(amount);
  }
}

