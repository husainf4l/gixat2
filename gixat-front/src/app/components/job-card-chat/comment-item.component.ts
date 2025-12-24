import { Component, Input, Output, EventEmitter, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { JobCardService, JobCardComment } from '../../services/job-card.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-comment-item',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="group" [class.ml-12]="comment.parentCommentId">
      <div class="flex gap-3">
        <!-- Avatar -->
        <div class="flex-shrink-0">
          @if (comment.author.avatar) {
            <img
              [src]="comment.author.avatar"
              [alt]="comment.author.fullName"
              class="w-10 h-10 rounded-full object-cover"
            />
          } @else {
            <div class="w-10 h-10 rounded-full bg-brand text-white flex items-center justify-center font-semibold text-sm">
              {{ getInitials(comment.author.fullName) }}
            </div>
          }
        </div>

        <!-- Content -->
        <div class="flex-1 min-w-0">
          <!-- Header -->
          <div class="flex items-center gap-2 mb-1">
            <span class="font-semibold text-slate-900 text-sm">
              {{ comment.author.fullName }}
            </span>
            <span class="text-xs text-slate-500">
              {{ formatDate(comment.createdAt) }}
            </span>
            @if (comment.isEdited) {
              <span class="text-xs text-slate-400 italic">(edited)</span>
            }
          </div>

          <!-- Message Body -->
          @if (isEditing()) {
            <div class="space-y-2">
              <textarea
                [(ngModel)]="editContent"
                class="w-full px-3 py-2 border border-slate-200 rounded-lg focus:ring-2 focus:ring-brand/20 focus:border-brand text-sm resize-none"
                rows="3"
                placeholder="Edit your message..."
              ></textarea>
              <div class="flex gap-2">
                <button
                  (click)="saveEdit()"
                  [disabled]="isSaving() || !editContent.trim()"
                  class="px-3 py-1.5 bg-brand text-white rounded-lg text-sm font-medium hover:bg-brand-hover disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  @if (isSaving()) {
                    <i class="ri-loader-4-line animate-spin"></i>
                  } @else {
                    Save
                  }
                </button>
                <button
                  (click)="cancelEdit()"
                  class="px-3 py-1.5 bg-slate-100 text-slate-700 rounded-lg text-sm font-medium hover:bg-slate-200"
                >
                  Cancel
                </button>
              </div>
            </div>
          } @else {
            <div class="text-sm text-slate-700 whitespace-pre-wrap break-words" [innerHTML]="renderContent(comment.content)"></div>
          }

          <!-- Actions -->
          @if (!isEditing()) {
            <div class="flex items-center gap-3 mt-2">
              <button
                (click)="reply.emit(comment)"
                class="text-xs text-slate-500 hover:text-brand font-medium flex items-center gap-1"
              >
                <i class="ri-reply-line"></i>
                Reply
              </button>

              @if (canEdit()) {
                <button
                  (click)="startEdit()"
                  class="text-xs text-slate-500 hover:text-brand font-medium flex items-center gap-1"
                >
                  <i class="ri-edit-line"></i>
                  Edit
                </button>
              }

              @if (canDelete()) {
                <button
                  (click)="deleteComment()"
                  class="text-xs text-slate-500 hover:text-red-600 font-medium flex items-center gap-1"
                >
                  <i class="ri-delete-bin-line"></i>
                  Delete
                </button>
              }

              @if (comment.mentions.length > 0) {
                <span class="text-xs text-slate-400 flex items-center gap-1">
                  <i class="ri-at-line"></i>
                  {{ comment.mentions.length }} mentioned
                </span>
              }
            </div>
          }

          <!-- Replies -->
          @if (comment.replies && comment.replies.length > 0) {
            <div class="mt-4 space-y-4">
              @for (replyComment of comment.replies; track replyComment.id) {
                <app-comment-item
                  [comment]="replyComment"
                  [jobCardId]="jobCardId"
                  (reply)="reply.emit($event)"
                  (edited)="edited.emit()"
                  (deleted)="deleted.emit()"
                />
              }
            </div>
          }
        </div>
      </div>
    </div>
  `,
  styles: [`
    :host {
      display: block;
    }
  `]
})
export class CommentItemComponent {
  private jobCardService = inject(JobCardService);
  private authService = inject(AuthService);

  @Input({ required: true }) comment!: JobCardComment;
  @Input({ required: true }) jobCardId!: string;

  @Output() reply = new EventEmitter<JobCardComment>();
  @Output() edited = new EventEmitter<void>();
  @Output() deleted = new EventEmitter<void>();

  isEditing = signal<boolean>(false);
  isSaving = signal<boolean>(false);
  editContent = '';

  getInitials(fullName: string): string {
    return fullName
      .split(' ')
      .map(n => n[0])
      .join('')
      .toUpperCase()
      .substring(0, 2);
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return 'just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays < 7) return `${diffDays}d ago`;

    return date.toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: date.getFullYear() !== now.getFullYear() ? 'numeric' : undefined
    });
  }

  renderContent(content: string): string {
    // Highlight @mentions
    return content.replace(/@(\w+)/g, '<span class="text-brand font-semibold">@$1</span>');
  }

  canEdit(): boolean {
    const currentUser = this.authService.currentUser();
    return currentUser?.id === this.comment.authorId;
  }

  canDelete(): boolean {
    const currentUser = this.authService.currentUser();
    return currentUser?.id === this.comment.authorId;
  }

  startEdit() {
    this.editContent = this.comment.content;
    this.isEditing.set(true);
  }

  cancelEdit() {
    this.isEditing.set(false);
    this.editContent = '';
  }

  saveEdit() {
    if (!this.editContent.trim()) return;

    this.isSaving.set(true);

    this.jobCardService.editJobCardComment(this.comment.id, this.editContent.trim()).subscribe({
      next: () => {
        this.isEditing.set(false);
        this.isSaving.set(false);
        this.edited.emit();
      },
      error: (err) => {
        console.error('Error editing comment:', err);
        alert('Failed to edit comment');
        this.isSaving.set(false);
      }
    });
  }

  deleteComment() {
    if (!confirm('Are you sure you want to delete this comment?')) return;

    this.jobCardService.deleteJobCardComment(this.comment.id).subscribe({
      next: () => {
        this.deleted.emit();
      },
      error: (err) => {
        console.error('Error deleting comment:', err);
        alert('Failed to delete comment');
      }
    });
  }
}
