import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { JobCardService, JobCard, JobCardStatus, JobItemStatus, JobItem, JobCardMediaType, Technician } from '../../../services/job-card.service';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-job-card-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
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

  jobCardStatuses = Object.values(JobCardStatus);

  openMedia(url: string) {
    window.open(url, '_blank');
  }
  jobItemStatuses = Object.values(JobItemStatus);
  mediaTypes = Object.values(JobCardMediaType);

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

  async onMediaUpload(event: Event, itemId: string) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    const type = prompt('Select media type:\n0: BEFORE_WORK\n1: DURING_WORK\n2: AFTER_WORK\n3: DOCUMENTATION', '1');
    if (type === null) return;

    const mediaType = Object.values(JobCardMediaType)[parseInt(type)] || JobCardMediaType.DURING_WORK;

    this.jobCardService.uploadMediaToJobItem(itemId, file, mediaType).subscribe({
      next: () => {
        const current = this.jobCard();
        if (current) this.loadJobCard(current.id);
      },
      error: (err) => {
        console.error('Error uploading media:', err);
        alert('Failed to upload media');
      }
    });
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

