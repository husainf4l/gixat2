import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { SessionService, Session, SessionMedia } from '../../../services/session.service';
import { catchError, of } from 'rxjs';
import imageCompression from 'browser-image-compression';

interface WorkflowStep {
  id: string;
  title: string;
  description: string;
  completed: boolean;
  notes: string | null | undefined;
  requests?: string | null | undefined;
  icon: string;
}

interface PendingUpload {
  id: string;
  file: File;
  previewUrl: string;
  stage: string;
  status: 'pending' | 'uploading' | 'uploaded' | 'error';
  errorMessage?: string;
}

interface JobCardData {
  customerFirstName: string;
  customerLastName: string;
  customerPhone: string;
  customerEmail: string;
  carMake: string;
  carModel: string;
  carYear: string;
  licensePlate: string;
  vin: string;
  mileage: number;
  requests: string[];
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
  stepRequests = signal<string[]>([]);
  isSavingStep = signal<boolean>(false);
  
  // Job Card Modal
  showJobCardModal = signal<boolean>(false);
  isCreatingJobCard = signal<boolean>(false);
  jobCardData = signal<JobCardData>({
    customerFirstName: '',
    customerLastName: '',
    customerPhone: '',
    customerEmail: '',
    carMake: '',
    carModel: '',
    carYear: '',
    licensePlate: '',
    vin: '',
    mileage: 0,
    requests: []
  });
  
  // Media upload
  selectedStage = signal<string>('intake');
  pendingUploads = signal<PendingUpload[]>([]);

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
        requests: session.intakeRequests,
        icon: 'ri-login-box-line'
      },
      {
        id: 'customerRequests',
        title: 'Customer Requests',
        description: 'Customer concerns documented',
        completed: !!session.customerRequests,
        notes: '',
        requests: session.customerRequests,
        icon: 'ri-user-voice-line'
      },
      {
        id: 'inspection',
        title: 'Inspection',
        description: 'Vehicle inspection performed',
        completed: !!session.inspectionNotes,
        notes: session.inspectionNotes,
        requests: session.inspectionRequests,
        icon: 'ri-search-eye-line'
      },
      {
        id: 'testDrive',
        title: 'Test Drive',
        description: 'Test drive completed',
        completed: !!session.testDriveNotes,
        notes: session.testDriveNotes,
        requests: session.testDriveRequests,
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

  getProgress(): number {
    const steps = this.getWorkflowSteps();
    if (steps.length === 0) return 0;
    const completedSteps = steps.filter(s => s.completed).length;
    return (completedSteps / steps.length) * 100;
  }

  copyToClipboard(text: string) {
    navigator.clipboard.writeText(text).then(() => {
      // Optional: Show a toast or feedback
    });
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
    const session = this.sessionDetail();
    if (!session) return;

    // For request-enabled steps, navigate to full-page widget
    if (['intake', 'customerRequests', 'inspection', 'testDrive'].includes(step.id)) {
      this.router.navigate(['/dashboard/sessions', session.id, 'request-widget'], {
        queryParams: { stepId: step.id }
      });
      return;
    }

    // For other steps, use the modal
    this.selectedStep.set(step);
    this.stepNotes.set(step.notes || '');

    const requestsStr = step.requests || '';
    const requestsArray = requestsStr ? requestsStr.split('\n').filter(r => r.trim()) : [];
    this.stepRequests.set(requestsArray.length > 0 ? requestsArray : ['']);

    this.showStepModal.set(true);

    setTimeout(() => {
      const textareas = document.querySelectorAll('.request-input');
      textareas.forEach((textarea: any) => {
        textarea.style.height = 'auto';
        textarea.style.height = textarea.scrollHeight + 'px';
      });
    }, 100);
  }

  closeStepModal() {
    this.showStepModal.set(false);
    this.selectedStep.set(null);
    this.stepNotes.set('');
    this.stepRequests.set([]);
  }

  addRequest(index?: number) {
    this.stepRequests.update(reqs => {
      const newReqs = [...reqs];
      if (index !== undefined) {
        newReqs.splice(index + 1, 0, '');
      } else {
        newReqs.push('');
      }
      return newReqs;
    });

    // Focus the new request in the next tick
    setTimeout(() => {
      const inputs = document.querySelectorAll('.request-input');
      const targetIndex = index !== undefined ? index + 1 : this.stepRequests().length - 1;
      (inputs[targetIndex] as HTMLElement)?.focus();
    }, 0);
  }

  handleRequestKeydown(event: KeyboardEvent, index: number) {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.addRequest(index);
    } else if (event.key === 'Backspace' && !this.stepRequests()[index] && this.stepRequests().length > 1) {
      event.preventDefault();
      this.removeRequest(index);
      
      // Focus previous input
      setTimeout(() => {
        const inputs = document.querySelectorAll('.request-input');
        (inputs[Math.max(0, index - 1)] as HTMLElement)?.focus();
      }, 0);
    }
  }

  removeRequest(index: number) {
    this.stepRequests.update(reqs => {
      const newReqs = [...reqs];
      newReqs.splice(index, 1);
      return newReqs.length > 0 ? newReqs : [''];
    });
  }

  updateRequest(index: number, value: string) {
    this.stepRequests.update(reqs => {
      const newReqs = [...reqs];
      newReqs[index] = value;
      return newReqs;
    });
    
    // Auto-resize the textarea
    setTimeout(() => {
      const textareas = document.querySelectorAll('.request-input');
      const textarea = textareas[index] as HTMLElement;
      if (textarea) {
        textarea.style.height = 'auto';
        textarea.style.height = textarea.scrollHeight + 'px';
      }
    }, 0);
  }

  hasValidRequests(): boolean {
    return this.stepRequests().some(r => r.trim().length > 0);
  }

  async saveStepNotes() {
    const session = this.sessionDetail();
    const step = this.selectedStep();
    if (!session || !step) return;

    this.isSavingStep.set(true);
    
    try {
      // Join requests with newlines, filtering out empty ones
      const requestsStr = this.stepRequests()
        .filter(r => r.trim())
        .join('\n');

      // Call the update mutation based on step id
      await this.sessionService.updateSessionStep(
        session.id, 
        step.id, 
        this.stepNotes(),
        requestsStr
      ).toPromise();

      // Reload session data
      this.loadSessionDetail(session.id);
      this.closeStepModal();
    } catch (error: any) {
      console.error('Failed to save step:', error);
      
      // Check for specific error types
      const errorMessage = error?.message || '';
      if (errorMessage.includes('concurrency') || errorMessage.includes('modified or deleted')) {
        alert('The session has been modified by another user. Please refresh and try again.');
        this.loadSessionDetail(session.id);
      } else if (errorMessage.includes('not found') || errorMessage.includes('does not exist')) {
        alert('Session not found. It may have been deleted.');
        this.router.navigate(['/dashboard/sessions']);
      } else {
        alert('Failed to save step notes. Please try again.');
      }
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

  openJobCardModal() {
    const session = this.sessionDetail();
    if (!session) return;

    // Populate job card data from session
    const requests: string[] = [];
    
    if (session.intakeRequests) {
      requests.push(...session.intakeRequests.split('\n').filter(r => r.trim()));
    }
    if (session.customerRequests) {
      requests.push(...session.customerRequests.split('\n').filter(r => r.trim()));
    }
    if (session.inspectionRequests) {
      requests.push(...session.inspectionRequests.split('\n').filter(r => r.trim()));
    }
    if (session.testDriveRequests) {
      requests.push(...session.testDriveRequests.split('\n').filter(r => r.trim()));
    }

    this.jobCardData.set({
      customerFirstName: session.customer?.firstName || '',
      customerLastName: session.customer?.lastName || '',
      customerPhone: session.customer?.phoneNumber || '',
      customerEmail: session.customer?.email || '',
      carMake: session.car?.make || '',
      carModel: session.car?.model || '',
      carYear: session.car?.year?.toString() || '',
      licensePlate: session.car?.licensePlate || '',
      vin: session.car?.vin || '',
      mileage: session.car?.mileage || 0,
      requests: requests
    });

    this.showJobCardModal.set(true);
  }

  closeJobCardModal() {
    this.showJobCardModal.set(false);
  }

  addJobCardRequest() {
    const current = this.jobCardData();
    this.jobCardData.set({
      ...current,
      requests: [...current.requests, '']
    });
  }

  removeJobCardRequest(index: number) {
    const current = this.jobCardData();
    this.jobCardData.set({
      ...current,
      requests: current.requests.filter((_, i) => i !== index)
    });
  }

  async submitJobCard() {
    const session = this.sessionDetail();
    if (!session) return;

    this.isCreatingJobCard.set(true);

    try {
      // TODO: Call backend mutation to create job card with the edited data
      // For now, just call the existing generateJobCard method
      await this.sessionService.generateJobCard(session.id).toPromise();
      
      this.closeJobCardModal();
      alert('Job card created successfully! Redirecting to job cards page...');
      
      // TODO: Navigate to job cards page when it's ready
      // this.router.navigate(['/dashboard/job-cards']);
      
      this.loadSessionDetail(session.id);
    } catch (error) {
      console.error('Failed to create job card:', error);
      alert('Failed to create job card. Please try again.');
    } finally {
      this.isCreatingJobCard.set(false);
    }
  }

  async onMediaUpload(event: Event, stage: string) {
    const input = event.target as HTMLInputElement;
    const files = input.files;
    if (!files || files.length === 0) return;

    const session = this.sessionDetail();
    if (!session) return;

    // Convert FileList to Array
    const fileArray = Array.from(files);

    // Validate all files first
    for (const file of fileArray) {
      const validationError = this.validateFile(file);
      if (validationError) {
        alert(`${file.name}: ${validationError}`);
        input.value = '';
        return;
      }
    }

    this.selectedStage.set(stage);

    // Create local previews and add to pending immediately for best UX
    const newPending: PendingUpload[] = fileArray.map(file => ({
      id: Math.random().toString(36).substring(7),
      file,
      previewUrl: URL.createObjectURL(file),
      stage,
      status: 'uploading'
    }));

    this.pendingUploads.update(prev => [...prev, ...newPending]);
    input.value = ''; // Reset input immediately

    // Start background compression and upload
    this.processAndUpload(session.id, stage, newPending);
  }

  private async processAndUpload(sessionId: string, stage: string, pendingItems: PendingUpload[]) {
    try {
      // Step 1: Compress files (especially images)
      const processedItems = await Promise.all(pendingItems.map(async (item) => {
        if (this.isImage(item.file.name)) {
          try {
            const options = {
              maxSizeMB: 1,
              maxWidthOrHeight: 1920,
              useWebWorker: true
            };
            const compressedFile = await imageCompression(item.file, options);
            // Keep the original name but use the compressed blob
            const finalFile = new File([compressedFile], item.file.name, { type: compressedFile.type });
            return { ...item, file: finalFile };
          } catch (e) {
            console.warn('Image compression failed, using original:', e);
            return item;
          }
        }
        // For videos, we currently don't compress browser-side as it's too heavy
        // but we could add logic here if needed in the future
        return item;
      }));

      const fileRequests = processedItems.map(item => ({
        fileName: item.file.name,
        contentType: item.file.type
      }));

      const presignedUrls = await this.sessionService.getPresignedUrls(
        sessionId,
        this.getBackendStage(stage),
        fileRequests
      ).toPromise();

      if (!presignedUrls || presignedUrls.length !== processedItems.length) {
        throw new Error('Failed to get presigned URLs');
      }

      // Upload all files to S3 in parallel
      const uploadPromises = processedItems.map((item, index) => {
        const presignedData = presignedUrls[index];
        return this.sessionService.uploadToS3(presignedData.uploadUrl, item.file);
      });

      await Promise.all(uploadPromises);

      // Process all uploaded files with session context
      const processRequests = presignedUrls.map((presignedData, index) => ({
        fileKey: presignedData.fileKey,
        stage: this.getBackendStage(stage),
        alt: `${stage} - ${processedItems[index].file.name}`
      }));

      const result = await this.sessionService.processBulkSessionUploads(
        sessionId,
        processRequests
      ).toPromise();

      if (result) {
        const failures = result.filter(r => !r.success);
        if (failures.length > 0) {
          console.error('Some files failed to process:', failures);
        }
      }

      // Update status to uploaded and then remove after a delay
      this.pendingUploads.update(prev =>
        prev.map(p => pendingItems.find(item => item.id === p.id) ? { ...p, status: 'uploaded' as const } : p)
      );

      // Reload session to show real media
      this.loadSessionDetail(sessionId);

      // Clean up pending items after a short delay
      setTimeout(() => {
        this.pendingUploads.update(prev => 
          prev.filter(p => !pendingItems.find(item => item.id === p.id))
        );
        // Revoke object URLs
        pendingItems.forEach(item => URL.revokeObjectURL(item.previewUrl));
      }, 2000);

    } catch (error) {
      console.error('Background upload failed:', error);
      this.pendingUploads.update(prev => 
        prev.map(p => pendingItems.find(item => item.id === p.id) 
          ? { ...p, status: 'error', errorMessage: error instanceof Error ? error.message : 'Upload failed' } 
          : p
        )
      );
    }
  }

  private getBackendStage(stage: string): string {
    const stageMap: { [key: string]: string } = {
      'intake': 'INTAKE',
      'customerRequests': 'GENERAL',
      'inspection': 'INSPECTION',
      'testDrive': 'TEST_DRIVE',
      'initialReport': 'GENERAL'
    };
    return stageMap[stage] || stage.toUpperCase();
  }

  getMediaByStage(stage: string): SessionMedia[] {
    const session = this.sessionDetail();
    const backendStage = this.getBackendStage(stage);
    
    const uploadedMedia = session?.media?.filter(m => m.stage?.toUpperCase() === backendStage) || [];
    const pendingMedia = this.pendingUploads().filter(p => p.stage === stage);
    
    return [
      ...pendingMedia.map(p => ({
        id: p.id,
        stage: p.stage,
        isPending: true,
        status: p.status,
        media: { id: p.id, url: p.previewUrl, alt: p.file.name }
      })),
      ...uploadedMedia
    ];
  }

  getAllMedia(): SessionMedia[] {
    const session = this.sessionDetail();
    const uploadedMedia = session?.media || [];
    const pendingMedia = this.pendingUploads();
    
    return [
      ...pendingMedia.map(p => ({
        id: p.id,
        stage: p.stage,
        isPending: true,
        status: p.status,
        media: { id: p.id, url: p.previewUrl, alt: p.file.name }
      })),
      ...uploadedMedia
    ];
  }

  isImage(url: string): boolean {
    if (!url) return false;
    // Handle URLs with query parameters (like S3 presigned URLs)
    const cleanUrl = url.split('?')[0];
    return /\.(jpg|jpeg|png|gif|webp|heic)$/i.test(cleanUrl);
  }

  isVideo(url: string): boolean {
    if (!url) return false;
    // Handle URLs with query parameters
    const cleanUrl = url.split('?')[0];
    return /\.(mp4|mov|avi|webm)$/i.test(cleanUrl);
  }

  openMedia(url: string) {
    window.open(url, '_blank');
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
