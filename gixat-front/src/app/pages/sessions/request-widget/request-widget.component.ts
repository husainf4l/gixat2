import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { SessionService, Session, SessionMedia } from '../../../services/session.service';
import imageCompression from 'browser-image-compression';

interface PendingUpload {
  id: string;
  file: File;
  previewUrl: string;
  stage: string;
  status: 'pending' | 'uploading' | 'uploaded' | 'error';
  errorMessage?: string;
  progress?: number; // Upload progress percentage
}

// File upload constants
const MAX_FILE_SIZE = 50 * 1024 * 1024; // 50MB
const ALLOWED_IMAGE_TYPES = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif', 'image/webp', 'image/heic', 'image/heif'];
const ALLOWED_VIDEO_TYPES = ['video/mp4', 'video/mpeg', 'video/quicktime', 'video/x-msvideo', 'video/webm'];
const ALLOWED_MIME_TYPES = [...ALLOWED_IMAGE_TYPES, ...ALLOWED_VIDEO_TYPES];
const ALLOWED_EXTENSIONS = ['.jpg', '.jpeg', '.png', '.gif', '.webp', '.heic', '.heif', '.mp4', '.mov', '.avi', '.mpeg', '.webm'];

@Component({
  selector: 'app-request-widget',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './request-widget.component.html',
})
export class RequestWidgetComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private sessionService = inject(SessionService);

  sessionId = signal<string>('');
  stepId = signal<string>('');
  stepTitle = signal<string>('');
  stepIcon = signal<string>('ri-user-voice-line');

  requests = signal<string[]>(['']);
  notes = signal<string>('');
  mileage = signal<number>(0);

  sessionDetail = signal<Session | null>(null);
  pendingUploads = signal<PendingUpload[]>([]);

  isSaving = signal<boolean>(false);
  isLoading = signal<boolean>(true);

  // File upload helpers
  readonly maxFileSizeMB = MAX_FILE_SIZE / (1024 * 1024);
  readonly allowedFormats = ALLOWED_EXTENSIONS.join(', ');

  ngOnInit() {
    const sessionId = this.route.snapshot.paramMap.get('sessionId');
    const stepId = this.route.snapshot.queryParamMap.get('stepId');

    if (!sessionId || !stepId) {
      this.goBack();
      return;
    }

    this.sessionId.set(sessionId);
    this.stepId.set(stepId);

    // Set step title and icon based on stepId
    this.updateStepMetadata(stepId);

    // Load session data
    this.loadSessionDetail(sessionId);
  }

  updateStepMetadata(stepId: string) {
    const stepMeta: Record<string, { title: string; icon: string }> = {
      customerRequests: { title: 'Customer Requests', icon: 'ri-user-voice-line' },
      inspection: { title: 'Inspection Requests', icon: 'ri-search-eye-line' },
      testDrive: { title: 'Test Drive Requests', icon: 'ri-steering-2-line' },
      initialReport: { title: 'Initial Report', icon: 'ri-file-list-3-line' }
    };

    const meta = stepMeta[stepId] || { title: 'Session Step', icon: 'ri-file-list-line' };
    this.stepTitle.set(meta.title);
    this.stepIcon.set(meta.icon);
  }

  loadSessionDetail(id: string) {
    this.isLoading.set(true);
    this.sessionService.getSessionById(id).subscribe({
      next: (session) => {
        this.sessionDetail.set(session);

        // Load existing requests based on stepId (simplified workflow)
        const stepId = this.stepId();
        if (stepId === 'customerRequests' && session.customerRequests) {
          this.requests.set(session.customerRequests.split('\n').filter((r: string) => r.trim()));
        } else if (stepId === 'inspection' && session.inspectionRequests) {
          this.requests.set(session.inspectionRequests.split('\n').filter((r: string) => r.trim()));
          this.notes.set(session.inspectionNotes || '');
          if (session.mileage) {
            this.mileage.set(session.mileage);
          }
        } else if (stepId === 'testDrive' && session.testDriveRequests) {
          this.requests.set(session.testDriveRequests.split('\n').filter((r: string) => r.trim()));
          this.notes.set(session.testDriveNotes || '');
        } else if (stepId === 'initialReport' && session.initialReport) {
          this.notes.set(session.initialReport || '');
          this.requests.set([]); // No requests for initial report
        }

        // Ensure at least one empty request if none exist (except for initialReport)
        if (this.requests().length === 0 && stepId !== 'initialReport') {
          this.requests.set(['']);
        }

        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.goBack();
      }
    });
  }

  goBack() {
    const sessionId = this.sessionId();
    if (sessionId) {
      this.router.navigate(['/dashboard/sessions', sessionId]);
    } else {
      this.router.navigate(['/dashboard/sessions']);
    }
  }

  addRequest(index?: number) {
    this.requests.update(reqs => {
      const newReqs = [...reqs];
      if (index !== undefined) {
        newReqs.splice(index + 1, 0, '');
      } else {
        newReqs.push('');
      }
      return newReqs;
    });

    setTimeout(() => {
      const inputs = document.querySelectorAll('.request-input');
      const targetIndex = index !== undefined ? index + 1 : this.requests().length - 1;
      (inputs[targetIndex] as HTMLElement)?.focus();
    }, 0);
  }

  removeRequest(index: number) {
    this.requests.update(reqs => {
      const newReqs = [...reqs];
      newReqs.splice(index, 1);
      return newReqs.length > 0 ? newReqs : [''];
    });
  }

  updateRequest(index: number, value: string) {
    this.requests.update(reqs => {
      const newReqs = [...reqs];
      newReqs[index] = value;
      return newReqs;
    });

    setTimeout(() => {
      const textareas = document.querySelectorAll('.request-input');
      const textarea = textareas[index] as HTMLElement;
      if (textarea) {
        textarea.style.height = 'auto';
        textarea.style.height = textarea.scrollHeight + 'px';
      }
    }, 0);
  }

  handleRequestKeydown(event: KeyboardEvent, index: number) {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.addRequest(index);
    } else if (event.key === 'Backspace' && !this.requests()[index] && this.requests().length > 1) {
      event.preventDefault();
      this.removeRequest(index);

      setTimeout(() => {
        const inputs = document.querySelectorAll('.request-input');
        (inputs[Math.max(0, index - 1)] as HTMLElement)?.focus();
      }, 0);
    }
  }

  hasValidRequests(): boolean {
    return this.requests().some(r => r.trim().length > 0);
  }

  hasUploadsInProgress(): boolean {
    const pending = this.pendingUploads();
    return pending.some(p => p.status === 'pending' || p.status === 'uploading');
  }

  hasFailedUploads(): boolean {
    const pending = this.pendingUploads();
    return pending.some(p => p.status === 'error');
  }

  private validateStep(): { valid: boolean; errors: string[] } {
    const errors: string[] = [];
    const stepId = this.stepId();
    
    if (stepId === 'inspection') {
      if (!this.mileage() || this.mileage() <= 0) {
        errors.push('Mileage is required and must be greater than 0');
      }
      if (!this.hasValidRequests() && !this.notes()) {
        errors.push('Please add either inspection notes or requests');
      }
    }
    
    if (stepId === 'customerRequests' && !this.hasValidRequests()) {
      errors.push('Please add at least one customer request');
    }

    if (stepId === 'testDrive' && !this.hasValidRequests() && !this.notes()) {
      errors.push('Please add either test drive notes or requests');
    }

    if (stepId === 'initialReport' && !this.notes()) {
      errors.push('Please add initial report notes');
    }
    
    return { valid: errors.length === 0, errors };
  }

  getUploadStatusMessage(): string {
    const pending = this.pendingUploads();
    const uploading = pending.filter(p => p.status === 'uploading').length;
    const pendingCount = pending.filter(p => p.status === 'pending').length;
    const failed = pending.filter(p => p.status === 'error').length;

    if (failed > 0) {
      return `${failed} upload${failed > 1 ? 's' : ''} failed. Please retry before saving.`;
    }
    if (uploading > 0) {
      return `${uploading} file${uploading > 1 ? 's are' : ' is'} still uploading...`;
    }
    if (pendingCount > 0) {
      return `${pendingCount} file${pendingCount > 1 ? 's are' : ' is'} pending upload...`;
    }
    return '';
  }

  async generateReport() {
    const sessionId = this.sessionId();
    if (!sessionId) return;

    this.isSaving.set(true);
    try {
      await this.sessionService.updateSessionStep(sessionId, 'initialReport', '').toPromise();
      this.goBack();
    } catch (error) {
      console.error('Failed to generate report:', error);
      alert('Failed to generate report. Please try again.');
    } finally {
      this.isSaving.set(false);
    }
  }

  async save() {
    const sessionId = this.sessionId();
    const stepId = this.stepId();

    if (!sessionId || !stepId) return;

    if (stepId === 'initialReport') {
      this.generateReport();
      return;
    }

    // Enhanced validation
    const validation = this.validateStep();
    if (!validation.valid) {
      alert(validation.errors.join('\n'));
      return;
    }

    // Check for uploads in progress or failed
    if (this.hasFailedUploads()) {
      const confirmed = confirm(
        'Some uploads have failed. Do you want to continue without these files?'
      );
      if (!confirmed) return;
    }

    if (this.hasUploadsInProgress()) {
      const confirmed = confirm(
        'Some files are still uploading. Do you want to wait for them to finish?'
      );
      if (!confirmed) return;
    }

    this.isSaving.set(true);

    try {
      const requestsStr = this.requests()
        .filter(r => r.trim())
        .join('\n');

      await this.sessionService.updateSessionStep(
        sessionId,
        stepId,
        this.notes(),
        requestsStr,
        stepId === 'inspection' ? this.mileage() : undefined
      ).toPromise();

      this.goBack();
    } catch (error) {
      console.error('Failed to save requests:', error);
      alert('Failed to save. Please try again.');
    } finally {
      this.isSaving.set(false);
    }
  }

  async onMediaUpload(event: Event) {
    const input = event.target as HTMLInputElement;
    const files = input.files;
    if (!files || files.length === 0) return;

    const sessionId = this.sessionId();
    const stepId = this.stepId();
    if (!sessionId || !stepId) return;

    const fileArray = Array.from(files);

    for (const file of fileArray) {
      const validationError = this.validateFile(file);
      if (validationError) {
        alert(`${file.name}: ${validationError}`);
        input.value = '';
        return;
      }
    }

    // Add images immediately to the gallery with preview URLs
    const newPending: PendingUpload[] = fileArray.map(file => {
      const previewUrl = URL.createObjectURL(file);
      return {
        id: Math.random().toString(36).substring(7),
        file,
        previewUrl,
        stage: stepId,
        status: 'pending' as const
      };
    });

    // Add to pending uploads - images appear instantly
    this.pendingUploads.update(prev => [...prev, ...newPending]);
    input.value = '';

    // Start upload process in the background
    this.processAndUpload(sessionId, stepId, newPending);
  }

  private async processAndUpload(sessionId: string, stage: string, pendingItems: PendingUpload[]) {
    try {
      // Mark as uploading
      this.pendingUploads.update(prev =>
        prev.map(p => {
          if (pendingItems.find(item => item.id === p.id)) {
            return { ...p, status: 'uploading' as const };
          }
          return p;
        })
      );

      // Compress images in the background
      const processedItems = await Promise.all(pendingItems.map(async (item) => {
        if (this.isImage(item.file.name)) {
          try {
            const options = {
              maxSizeMB: 1,
              maxWidthOrHeight: 1920,
              useWebWorker: true
            };
            const compressedFile = await imageCompression(item.file, options);
            const finalFile = new File([compressedFile], item.file.name, { type: compressedFile.type });
            return { ...item, file: finalFile };
          } catch (e) {
            return item;
          }
        }
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

      const uploadPromises = processedItems.map((item, index) => {
        const presignedData = presignedUrls[index];
        return this.sessionService.uploadToS3(
          presignedData.uploadUrl, 
          item.file,
          (progress) => {
            // Update progress for this specific file
            this.pendingUploads.update(prev =>
              prev.map(p => p.id === item.id ? { ...p, progress } : p)
            );
          }
        );
      });

      await Promise.all(uploadPromises);

      const processRequests = presignedUrls.map((presignedData, index) => ({
        fileKey: presignedData.fileKey,
        stage: this.getBackendStage(stage),
        alt: `${stage} - ${processedItems[index].file.name}`
      }));

      await this.sessionService.processBulkSessionUploads(
        sessionId,
        processRequests
      ).toPromise();

      // Mark as uploaded - success!
      this.pendingUploads.update(prev =>
        prev.map(p => {
          if (pendingItems.find(item => item.id === p.id)) {
            return { ...p, status: 'uploaded' as const };
          }
          return p;
        })
      );

    } catch (error) {
      console.error('Upload failed:', error);

      // Mark as error
      this.pendingUploads.update(prev =>
        prev.map(p => {
          if (pendingItems.find(item => item.id === p.id)) {
            return { ...p, status: 'error' as const, errorMessage: 'Upload failed' };
          }
          return p;
        })
      );
    }
  }

  private getBackendStage(stage: string): string {
    const stageMap: { [key: string]: string } = {
      'customerRequests': 'GENERAL',
      'inspection': 'INSPECTION',
      'testDrive': 'TEST_DRIVE',
      'initialReport': 'GENERAL'
    };
    return stageMap[stage] || stage.toUpperCase();
  }

  getMediaByStage(): SessionMedia[] {
    const session = this.sessionDetail();
    const stage = this.stepId();
    const backendStage = this.getBackendStage(stage);

    const uploadedMedia = session?.media?.filter(m => m.stage?.toUpperCase() === backendStage) || [];
    const pendingMedia = this.pendingUploads().filter(p => p.stage === stage);

    // Combine pending (using blob URLs) and uploaded media (using S3 URLs)
    // Pending items stay in their original positions - no repositioning
    return [
      ...pendingMedia.map(p => ({
        id: p.id,
        stage: p.stage,
        isPending: p.status === 'error', // Only mark as pending if there's an error
        status: p.status,
        progress: p.progress, // Include progress for UI
        media: { id: p.id, url: p.previewUrl, alt: p.file.name }
      })),
      ...uploadedMedia
    ];
  }

  isImage(url: string): boolean {
    if (!url) return false;

    // Handle blob URLs - check by file extension from the pending uploads
    if (url.startsWith('blob:')) {
      const pendingItem = this.pendingUploads().find(p => p.previewUrl === url);
      if (pendingItem) {
        const fileName = pendingItem.file.name.toLowerCase();
        return /\.(jpg|jpeg|png|gif|webp|heic)$/i.test(fileName);
      }
    }

    const cleanUrl = url.split('?')[0];
    return /\.(jpg|jpeg|png|gif|webp|heic)$/i.test(cleanUrl);
  }

  isVideo(url: string): boolean {
    if (!url) return false;

    // Handle blob URLs - check by file extension from the pending uploads
    if (url.startsWith('blob:')) {
      const pendingItem = this.pendingUploads().find(p => p.previewUrl === url);
      if (pendingItem) {
        const fileName = pendingItem.file.name.toLowerCase();
        return /\.(mp4|mov|avi|webm)$/i.test(fileName);
      }
    }

    const cleanUrl = url.split('?')[0];
    return /\.(mp4|mov|avi|webm)$/i.test(cleanUrl);
  }

  openMedia(url: string) {
    window.open(url, '_blank');
  }

  retryUpload(sessionMedia: SessionMedia) {
    // Find the pending upload item
    const pendingItem = this.pendingUploads().find(p => p.id === sessionMedia.id);
    if (!pendingItem) return;

    const sessionId = this.sessionId();
    const stepId = this.stepId();
    if (!sessionId || !stepId) return;

    // Reset status to 'pending' to hide error overlay
    this.pendingUploads.update(prev =>
      prev.map(p => p.id === pendingItem.id ? { ...p, status: 'pending' as const } : p)
    );

    // Retry the upload
    this.processAndUpload(sessionId, stepId, [pendingItem]);
  }

  async deleteMedia(sessionMedia: SessionMedia) {
    // Check if it's a pending upload (not yet saved to backend)
    const pendingItem = this.pendingUploads().find(p => p.id === sessionMedia.id);

    if (pendingItem) {
      // Remove from pending uploads
      this.pendingUploads.update(prev => prev.filter(p => p.id !== sessionMedia.id));
      // Revoke the blob URL to free memory
      URL.revokeObjectURL(pendingItem.previewUrl);
      return;
    }

    // It's a saved media, delete from backend
    const confirmed = confirm('Are you sure you want to delete this media?');
    if (!confirmed) return;

    try {
      await this.sessionService.deleteSessionMedia(sessionMedia.media.id).toPromise();

      // Reload session to update the media list
      const sessionId = this.sessionId();
      if (sessionId) {
        this.loadSessionDetail(sessionId);
      }
    } catch (error) {
      console.error('Failed to delete media:', error);
      alert('Failed to delete media. Please try again.');
    }
  }

  private validateFile(file: File): string | null {
    if (file.size > MAX_FILE_SIZE) {
      return `File size exceeds ${this.maxFileSizeMB}MB limit.`;
    }

    if (file.size === 0) {
      return 'File is empty.';
    }

    if (!ALLOWED_MIME_TYPES.includes(file.type)) {
      return `File type "${file.type}" is not allowed. Allowed types: images and videos.`;
    }

    const fileName = file.name.toLowerCase();
    const hasValidExtension = ALLOWED_EXTENSIONS.some(ext => fileName.endsWith(ext));

    if (!hasValidExtension) {
      return `File extension is not allowed. Supported: ${this.allowedFormats}`;
    }

    if (file.name.includes('..') || file.name.includes('/') || file.name.includes('\\')) {
      return 'Invalid filename.';
    }

    const parts = file.name.split('.');
    if (parts.length > 2) {
      return 'Files with multiple extensions are not allowed.';
    }

    return null;
  }
}
