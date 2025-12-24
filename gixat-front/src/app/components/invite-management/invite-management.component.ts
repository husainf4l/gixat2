import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { InviteService, UserInvite, InviteStatus } from '../../services/invite.service';

@Component({
  selector: 'app-invite-management',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './invite-management.component.html',
})
export class InviteManagementComponent implements OnInit {
  private inviteService = inject(InviteService);
  private fb = inject(FormBuilder);

  // State
  invites = signal<UserInvite[]>([]);
  isLoading = signal<boolean>(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  showCreateModal = signal<boolean>(false);
  showLinkModal = signal<boolean>(false);
  generatedLink = signal<string | null>(null);

  // Filter state
  statusFilter = signal<InviteStatus | 'ALL'>('ALL');
  searchQuery = signal<string>('');

  // Form
  inviteForm!: FormGroup;

  // Computed
  filteredInvites = computed(() => {
    let filtered = this.invites();

    // Status filter
    if (this.statusFilter() !== 'ALL') {
      filtered = filtered.filter(inv => inv.status === this.statusFilter());
    }

    // Search filter
    const query = this.searchQuery().toLowerCase();
    if (query) {
      filtered = filtered.filter(inv =>
        inv.email.toLowerCase().includes(query) ||
        inv.role.toLowerCase().includes(query)
      );
    }

    return filtered;
  });

  // Expose enum to template
  InviteStatus = InviteStatus;

  // Role options
  roles = [
    { value: 'OrgUser', label: 'User', description: 'Standard user access' },
    { value: 'OrgManager', label: 'Manager', description: 'Can manage users and invites' },
    { value: 'Mechanic', label: 'Mechanic', description: 'Workshop technician' },
    { value: 'Accountant', label: 'Accountant', description: 'Financial access' }
  ];

  ngOnInit() {
    this.initForm();
    this.loadInvites();
  }

  initForm() {
    this.inviteForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      role: ['OrgUser', Validators.required]
    });
  }

  loadInvites() {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.inviteService.getInvites(
      undefined,
      [{ createdAt: 'DESC' }]
    ).subscribe({
      next: (invites) => {
        this.invites.set(invites);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load invites:', err);
        this.errorMessage.set('Failed to load invites. Please try again.');
        this.isLoading.set(false);
      }
    });
  }

  openCreateModal() {
    this.showCreateModal.set(true);
    this.inviteForm.reset({ role: 'OrgUser' });
    this.errorMessage.set(null);
  }

  closeCreateModal() {
    this.showCreateModal.set(false);
    this.inviteForm.reset();
  }

  closeLinkModal() {
    this.showLinkModal.set(false);
    this.generatedLink.set(null);
  }

  submitInvite() {
    if (this.inviteForm.invalid) return;

    this.isLoading.set(true);
    this.errorMessage.set(null);

    const formValue = this.inviteForm.value;

    this.inviteService.inviteUser(formValue).subscribe({
      next: (response) => {
        this.isLoading.set(false);
        
        if (response.error) {
          this.errorMessage.set(response.error);
        } else if (response.link) {
          this.generatedLink.set(response.link);
          this.showCreateModal.set(false);
          this.showLinkModal.set(true);
          this.loadInvites(); // Refresh list
        }
      },
      error: (err) => {
        console.error('Failed to create invite:', err);
        this.errorMessage.set('Failed to send invite. Please try again.');
        this.isLoading.set(false);
      }
    });
  }

  copyToClipboard(text: string) {
    navigator.clipboard.writeText(text).then(() => {
      this.successMessage.set('Link copied to clipboard!');
      setTimeout(() => this.successMessage.set(null), 3000);
    });
  }

  cancelInvite(invite: UserInvite) {
    if (!confirm(`Cancel invitation for ${invite.email}?`)) return;

    this.inviteService.cancelInvite(invite.id).subscribe({
      next: (success) => {
        if (success) {
          this.successMessage.set('Invitation cancelled successfully');
          setTimeout(() => this.successMessage.set(null), 3000);
          this.loadInvites();
        }
      },
      error: (err) => {
        console.error('Failed to cancel invite:', err);
        this.errorMessage.set('Failed to cancel invitation. Please try again.');
      }
    });
  }

  resendInvite(invite: UserInvite) {
    // Re-create the invite with same email and role
    this.inviteService.inviteUser({
      email: invite.email,
      role: invite.role
    }).subscribe({
      next: (response) => {
        if (response.error) {
          this.errorMessage.set(response.error);
        } else if (response.link) {
          this.generatedLink.set(response.link);
          this.showLinkModal.set(true);
          this.successMessage.set('New invitation created!');
          setTimeout(() => this.successMessage.set(null), 3000);
          this.loadInvites();
        }
      },
      error: (err) => {
        console.error('Failed to resend invite:', err);
        this.errorMessage.set('Failed to resend invitation. Please try again.');
      }
    });
  }

  setStatusFilter(status: InviteStatus | 'ALL') {
    this.statusFilter.set(status);
  }

  onSearchChange(value: string) {
    this.searchQuery.set(value);
  }

  getStatusColor(status: InviteStatus): string {
    return this.inviteService.getStatusColor(status);
  }

  getStatusIcon(status: InviteStatus): string {
    return this.inviteService.getStatusIcon(status);
  }

  getExpiryText(invite: UserInvite): string {
    return this.inviteService.getExpiryText(invite);
  }

  isExpired(invite: UserInvite): boolean {
    return this.inviteService.isExpired(invite);
  }

  formatDate(dateStr: string): string {
    const date = new Date(dateStr);
    return date.toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  getInviteStats() {
    const all = this.invites();
    return {
      total: all.length,
      pending: all.filter(i => i.status === InviteStatus.PENDING).length,
      accepted: all.filter(i => i.status === InviteStatus.ACCEPTED).length,
      expired: all.filter(i => i.status === InviteStatus.EXPIRED).length
    };
  }
}
