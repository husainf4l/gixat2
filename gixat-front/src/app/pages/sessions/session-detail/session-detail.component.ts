import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { SessionService, Session } from '../../../services/session.service';
import { catchError, of } from 'rxjs';

interface WorkflowStep {
  id: string;
  title: string;
  description: string;
  completed: boolean;
  notes: string | null | undefined;
  icon: string;
}

@Component({
  selector: 'app-session-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './session-detail.component.html',
})
export class SessionDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private sessionService = inject(SessionService);

  sessionDetail = signal<Session | null>(null);
  isLoading = signal<boolean>(true);
  errorMessage = signal<string | null>(null);
  
  // Modal states
  showStepModal = signal<boolean>(false);
  selectedStep = signal<WorkflowStep | null>(null);
  stepNotes = signal<string>('');
  isSavingStep = signal<boolean>(false);
  
  // Media upload
  isUploadingMedia = signal<boolean>(false);
  selectedStage = signal<string>('intake');

  ngOnInit() {
    const sessionId = this.route.snapshot.paramMap.get('id');
    if (!sessionId) {
      this.router.navigate(['/dashboard/sessions']);
      return;
    }

    this.loadSessionDetail(sessionId);
  }

  loadSessionDetail(id: string) {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.sessionService.getSessionById(id).pipe(
      catchError((err: Error) => {
        this.errorMessage.set(err instanceof Error ? err.message : 'Failed to load session details');
        this.isLoading.set(false);
        return of(null);
      })
    ).subscribe((data: Session | null) => {
      this.sessionDetail.set(data);
      this.isLoading.set(false);
    });
  }

  goBack() {
    this.router.navigate(['/dashboard/sessions']);
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

  getWorkflowSteps() {
    const session = this.sessionDetail();
    if (!session) return [];

    return [
      {
        id: 'intake',
        title: 'Intake',
        description: 'Vehicle received',
        completed: !!session.intakeNotes,
        notes: session.intakeNotes,
        icon: 'ri-login-box-line'
      },
      {
        id: 'customerRequests',
        title: 'Customer Requests',
        description: 'Customer concerns documented',
        completed: !!session.customerRequests,
        notes: session.customerRequests,
        icon: 'ri-user-voice-line'
      },
      {
        id: 'inspection',
        title: 'Inspection',
        description: 'Vehicle inspection performed',
        completed: !!session.inspectionNotes,
        notes: session.inspectionNotes,
        icon: 'ri-search-eye-line'
      },
      {
        id: 'testDrive',
        title: 'Test Drive',
        description: 'Test drive completed',
        completed: !!session.testDriveNotes,
        notes: session.testDriveNotes,
        icon: 'ri-steering-2-line'
      },
      {
        id: 'initialReport',
        title: 'Initial Report',
        description: 'Diagnostic report ready',
        completed: !!session.initialReport,
        notes: session.initialReport,
        icon: 'ri-file-list-3-line'
      }
    ];
  }

  canGenerateJobCard(): boolean {
    const steps = this.getWorkflowSteps();
    return steps.every(step => step.completed);
  }

  formatStatus(status: string): string {
    return status
      .split('_')
      .map(word => word.charAt(0).toUpperCase() + word.slice(1).toLowerCase())
      .join(' ');
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMins / 60);
    const diffDays = Math.floor(diffHours / 24);

    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays < 7) return `${diffDays}d ago`;
    
    return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  }

  openStepModal(step: WorkflowStep) {
    this.selectedStep.set(step);
    this.stepNotes.set(step.notes || '');
    this.showStepModal.set(true);
  }

  closeStepModal() {
    this.showStepModal.set(false);
    this.selectedStep.set(null);
    this.stepNotes.set('');
  }

  async saveStepNotes() {
    const session = this.sessionDetail();
    const step = this.selectedStep();
    if (!session || !step) return;

    this.isSavingStep.set(true);
    
    try {
      // Call the update mutation based on step id
      await this.sessionService.updateSessionStep(
        session.id, 
        step.id, 
        this.stepNotes()
      ).toPromise();

      // Reload session data
      this.loadSessionDetail(session.id);
      this.closeStepModal();
    } catch (error) {
      console.error('Failed to save step:', error);
      alert('Failed to save step notes. Please try again.');
    } finally {
      this.isSavingStep.set(false);
    }
  }

  async generateJobCard() {
    const session = this.sessionDetail();
    if (!session || !this.canGenerateJobCard()) return;

    if (!confirm('Generate job card for this session?')) return;

    try {
      await this.sessionService.generateJobCard(session.id).toPromise();
      alert('Job card generated successfully!');
      this.loadSessionDetail(session.id);
    } catch (error) {
      console.error('Failed to generate job card:', error);
      alert('Failed to generate job card. Please try again.');
    }
  }

  async onMediaUpload(event: Event, stage: string) {
    const input = event.target as HTMLInputElement;
    const files = input.files;
    if (!files || files.length === 0) return;

    const session = this.sessionDetail();
    if (!session) return;

    const file = files[0];

    // Security validations
    const validationError = this.validateFile(file);
    if (validationError) {
      alert(validationError);
      input.value = '';
      return;
    }

    this.isUploadingMedia.set(true);
    this.selectedStage.set(stage);

    try {
      // Step 1: Get presigned URL from backend (no sessionId needed)
      const presignedData = await this.sessionService.getPresignedUrl(
        file.name,
        file.type
      ).toPromise();

      if (!presignedData) {
        throw new Error('Failed to get presigned URL');
      }

      const { uploadUrl, fileKey } = presignedData;

      // Step 2: Upload directly to S3
      await this.sessionService.uploadToS3(uploadUrl, file);

      // Step 3: Process the uploaded file (backend downloads, scans, compresses)
      await this.sessionService.processUploadedFile(
        fileKey,
        `${stage} - ${file.name}`
      ).toPromise();

      // Reload session to show new media
      this.loadSessionDetail(session.id);
      alert('Media uploaded successfully! Processing in background...');
    } catch (error) {
      console.error('Failed to upload media:', error);
      const errorMessage = error instanceof Error ? error.message : 'Failed to upload media';
      alert(`Upload failed: ${errorMessage}`);
    } finally {
      this.isUploadingMedia.set(false);
      input.value = ''; // Reset input
    }
  }

  private validateFile(file: File): string | null {
    // 1. File size validation (50MB max)
    const MAX_SIZE = 50 * 1024 * 1024; // 50MB in bytes
    if (file.size > MAX_SIZE) {
      return 'File size exceeds 50MB limit. Please choose a smaller file.';
    }

    if (file.size === 0) {
      return 'File is empty. Please choose a valid file.';
    }

    // 2. MIME type validation (whitelist approach)
    const allowedMimeTypes = [
      // Images
      'image/jpeg',
      'image/jpg',
      'image/png',
      'image/gif',
      'image/webp',
      'image/heic',
      'image/heif',
      // Videos
      'video/mp4',
      'video/mpeg',
      'video/quicktime', // .mov
      'video/x-msvideo', // .avi
      'video/webm'
    ];

    if (!allowedMimeTypes.includes(file.type)) {
      return `File type "${file.type}" is not allowed. Please upload images (JPEG, PNG, GIF, WebP, HEIC) or videos (MP4, MOV, AVI, WebM).`;
    }

    // 3. File extension validation (additional layer)
    const allowedExtensions = [
      '.jpg', '.jpeg', '.png', '.gif', '.webp', '.heic', '.heif',
      '.mp4', '.mov', '.avi', '.mpeg', '.webm'
    ];
    
    const fileName = file.name.toLowerCase();
    const hasValidExtension = allowedExtensions.some(ext => fileName.endsWith(ext));
    
    if (!hasValidExtension) {
      return 'File extension is not allowed. Please upload images or videos only.';
    }

    // 4. Filename validation (prevent path traversal)
    if (file.name.includes('..') || file.name.includes('/') || file.name.includes('\\')) {
      return 'Invalid filename. Please rename the file and try again.';
    }

    // 5. Check for double extensions (e.g., file.php.jpg)
    const parts = file.name.split('.');
    if (parts.length > 2) {
      return 'Files with multiple extensions are not allowed.';
    }

    return null; // File is valid
  }
}
