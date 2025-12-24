import { Component, Input, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { interval, Subscription } from 'rxjs';
import { JobCardService, JobCardComment } from '../../services/job-card.service';
import { CommentItemComponent } from './comment-item.component';
import { CommentFormComponent } from './comment-form.component';

@Component({
  selector: 'app-chat-panel',
  standalone: true,
  imports: [CommonModule, FormsModule, CommentItemComponent, CommentFormComponent],
  template: `
    <div class="bg-white rounded-xl border border-slate-200 shadow-sm">
      <!-- Header -->
      <div class="px-6 py-4 border-b border-slate-100 flex items-center justify-between">
        <div class="flex items-center gap-2">
          <i class="ri-chat-3-line text-xl text-brand"></i>
          <h3 class="font-semibold text-slate-900">Team Chat</h3>
          @if (comments().length > 0) {
            <span class="text-sm text-slate-500">({{ comments().length }})</span>
          }
        </div>
        <button
          (click)="refreshComments()"
          [disabled]="isRefreshing()"
          class="p-2 hover:bg-slate-50 rounded-lg transition-colors"
          title="Refresh comments"
        >
          <i class="ri-refresh-line text-lg"
             [class.animate-spin]="isRefreshing()"
             [class.text-slate-400]="!isRefreshing()"
             [class.text-brand]="isRefreshing()"></i>
        </button>
      </div>

      <!-- Messages Area -->
      <div
        class="p-6 space-y-4 max-h-[600px] overflow-y-auto custom-scrollbar"
        #messagesContainer
      >
        @if (isLoading()) {
          <div class="flex justify-center py-8">
            <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-brand"></div>
          </div>
        } @else if (comments().length === 0) {
          <div class="text-center py-12">
            <i class="ri-chat-3-line text-5xl text-slate-300 mb-3"></i>
            <p class="text-slate-500">No messages yet</p>
            <p class="text-sm text-slate-400 mt-1">Start the conversation!</p>
          </div>
        } @else {
          @for (comment of topLevelComments(); track comment.id) {
            <app-comment-item
              [comment]="comment"
              [jobCardId]="jobCardId"
              (reply)="handleReply($event)"
              (edited)="loadComments()"
              (deleted)="loadComments()"
            />
          }
        }
      </div>

      <!-- Reply Indicator -->
      @if (replyingTo()) {
        <div class="px-6 py-2 bg-blue-50 border-t border-blue-100 flex items-center justify-between">
          <div class="flex items-center gap-2 text-sm">
            <i class="ri-reply-line text-blue-600"></i>
            <span class="text-blue-900">
              Replying to <strong>{{ replyingTo()?.author?.fullName }}</strong>
            </span>
          </div>
          <button
            (click)="cancelReply()"
            class="text-blue-600 hover:text-blue-700 text-sm font-medium"
          >
            Cancel
          </button>
        </div>
      }

      <!-- Input Area -->
      <div class="border-t border-slate-100">
        <app-comment-form
          [jobCardId]="jobCardId"
          [parentCommentId]="replyingTo()?.id || null"
          [jobItemId]="jobItemId"
          (commentAdded)="onCommentAdded()"
        />
      </div>
    </div>
  `,
  styles: [`
    :host {
      display: block;
    }
  `]
})
export class ChatPanelComponent implements OnInit, OnDestroy {
  private jobCardService = inject(JobCardService);

  @Input({ required: true }) jobCardId!: string;
  @Input() jobItemId?: string | null;
  @Input() autoRefresh = true;
  @Input() refreshInterval = 10000; // 10 seconds

  comments = signal<JobCardComment[]>([]);
  isLoading = signal<boolean>(false);
  isRefreshing = signal<boolean>(false);
  replyingTo = signal<JobCardComment | null>(null);

  private refreshSubscription?: Subscription;

  ngOnInit() {
    this.loadComments();

    if (this.autoRefresh) {
      this.startAutoRefresh();
    }
  }

  ngOnDestroy() {
    this.stopAutoRefresh();
  }

  loadComments() {
    this.isLoading.set(true);

    this.jobCardService.getJobCardComments(this.jobCardId).subscribe({
      next: (result) => {
        const comments = result.edges
          .map(edge => edge.node)
          .filter(comment => !comment.isDeleted)
          .sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime());

        this.comments.set(comments);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Error loading comments:', err);
        this.isLoading.set(false);
      }
    });
  }

  refreshComments() {
    this.isRefreshing.set(true);

    this.jobCardService.getJobCardComments(this.jobCardId).subscribe({
      next: (result) => {
        const comments = result.edges
          .map(edge => edge.node)
          .filter(comment => !comment.isDeleted)
          .sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime());

        this.comments.set(comments);
        this.isRefreshing.set(false);
      },
      error: (err) => {
        console.error('Error refreshing comments:', err);
        this.isRefreshing.set(false);
      }
    });
  }

  topLevelComments(): JobCardComment[] {
    return this.comments().filter(c => !c.parentCommentId);
  }

  handleReply(comment: JobCardComment) {
    this.replyingTo.set(comment);
  }

  cancelReply() {
    this.replyingTo.set(null);
  }

  onCommentAdded() {
    this.loadComments();
    this.cancelReply();
  }

  private startAutoRefresh() {
    this.refreshSubscription = interval(this.refreshInterval).subscribe(() => {
      this.refreshComments();
    });
  }

  private stopAutoRefresh() {
    this.refreshSubscription?.unsubscribe();
  }
}
