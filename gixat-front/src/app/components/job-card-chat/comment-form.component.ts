import { Component, Input, Output, EventEmitter, inject, signal, ElementRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { JobCardService } from '../../services/job-card.service';
import { AuthService } from '../../services/auth.service';

interface User {
  id: string;
  fullName: string;
  email: string;
  userName?: string | null;
  avatar?: string | null;
}

@Component({
  selector: 'app-comment-form',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="p-4 relative">
      <!-- Mention Suggestions Dropdown -->
      @if (showMentionSuggestions() && filteredUsers().length > 0) {
        <div class="absolute bottom-full left-4 right-4 mb-2 bg-white border border-slate-200 rounded-lg shadow-lg max-h-48 overflow-y-auto z-10">
          @for (user of filteredUsers(); track user.id) {
            <button
              type="button"
              (click)="selectUser(user)"
              class="w-full px-4 py-2 text-left hover:bg-slate-50 flex items-center gap-3 transition-colors"
            >
              @if (user.avatar) {
                <img [src]="user.avatar" [alt]="user.fullName" class="w-8 h-8 rounded-full object-cover" />
              } @else {
                <div class="w-8 h-8 rounded-full bg-brand text-white flex items-center justify-center text-sm font-semibold">
                  {{ getInitials(user.fullName) }}
                </div>
              }
              <div class="flex-1 min-w-0">
                <div class="text-sm font-medium text-slate-900">{{ user.fullName }}</div>
                @if (user.userName) {
                  <div class="text-xs text-slate-500">@{{ user.userName }}</div>
                }
              </div>
            </button>
          }
        </div>
      }

      <!-- Input Area -->
      <div class="flex gap-3">
        <!-- Current User Avatar -->
        <div class="flex-shrink-0">
          @if (currentUserAvatar()) {
            <img
              [src]="currentUserAvatar()"
              alt="You"
              class="w-10 h-10 rounded-full object-cover"
            />
          } @else {
            <div class="w-10 h-10 rounded-full bg-brand text-white flex items-center justify-center font-semibold text-sm">
              {{ getCurrentUserInitials() }}
            </div>
          }
        </div>

        <!-- Textarea -->
        <div class="flex-1">
          <textarea
            #messageInput
            [(ngModel)]="message"
            (keydown)="onKeyDown($event)"
            (input)="onInput()"
            [placeholder]="parentCommentId ? 'Write a reply... Use @ to mention someone' : 'Write a message... Use @ to mention someone'"
            class="w-full px-4 py-3 border border-slate-200 rounded-lg focus:ring-2 focus:ring-brand/20 focus:border-brand resize-none text-sm"
            rows="3"
            [disabled]="isSending()"
          ></textarea>

          <!-- Actions -->
          <div class="flex items-center justify-between mt-2">
            <div class="text-xs text-slate-500">
              <i class="ri-information-line"></i>
              Press <kbd class="px-1.5 py-0.5 bg-slate-100 rounded text-xs">Ctrl+Enter</kbd> or <kbd class="px-1.5 py-0.5 bg-slate-100 rounded text-xs">Cmd+Enter</kbd> to send
            </div>

            <button
              type="button"
              (click)="sendMessage()"
              [disabled]="!message.trim() || isSending()"
              class="px-4 py-2 bg-brand text-white rounded-lg text-sm font-medium hover:bg-brand-hover disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
            >
              @if (isSending()) {
                <i class="ri-loader-4-line animate-spin"></i>
                Sending...
              } @else {
                <i class="ri-send-plane-fill"></i>
                Send
              }
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    :host {
      display: block;
    }

    kbd {
      font-family: ui-monospace, monospace;
      font-size: 0.75rem;
    }
  `]
})
export class CommentFormComponent {
  private jobCardService = inject(JobCardService);
  private authService = inject(AuthService);

  @ViewChild('messageInput') messageInput!: ElementRef<HTMLTextAreaElement>;

  @Input({ required: true }) jobCardId!: string;
  @Input() parentCommentId: string | null = null;
  @Input() jobItemId?: string | null;

  @Output() commentAdded = new EventEmitter<void>();

  message = '';
  isSending = signal<boolean>(false);
  showMentionSuggestions = signal<boolean>(false);
  mentionSearch = signal<string>('');
  users = signal<User[]>([]);
  filteredUsers = signal<User[]>([]);
  currentUserAvatar = signal<string | null>(null);

  ngOnInit() {
    this.loadUsers();
    this.loadCurrentUser();
  }

  loadUsers() {
    this.authService.getOrganizationUsers().subscribe({
      next: (users) => {
        this.users.set(users.map(u => ({
          id: u.id,
          fullName: u.fullName || u.email,
          email: u.email,
          userName: u.userName || u.email.split('@')[0],
          avatar: null
        })));
      },
      error: (err) => {
        console.error('Error loading users:', err);
      }
    });
  }

  loadCurrentUser() {
    const currentUser = this.authService.currentUser();
    if (currentUser) {
      this.currentUserAvatar.set(null); // Set avatar if available from backend
    }
  }

  onKeyDown(event: KeyboardEvent) {
    // Send on Ctrl+Enter or Cmd+Enter
    if ((event.ctrlKey || event.metaKey) && event.key === 'Enter') {
      event.preventDefault();
      this.sendMessage();
      return;
    }

    // Navigate mention suggestions with arrow keys
    if (this.showMentionSuggestions()) {
      if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
        event.preventDefault();
        // TODO: Implement keyboard navigation for suggestions
      }
      if (event.key === 'Enter' && this.filteredUsers().length > 0) {
        event.preventDefault();
        this.selectUser(this.filteredUsers()[0]);
      }
      if (event.key === 'Escape') {
        this.showMentionSuggestions.set(false);
      }
    }
  }

  onInput() {
    // Check for @ mention
    const cursorPosition = this.messageInput.nativeElement.selectionStart;
    const textBeforeCursor = this.message.substring(0, cursorPosition);
    const lastAtIndex = textBeforeCursor.lastIndexOf('@');

    if (lastAtIndex !== -1) {
      const textAfterAt = textBeforeCursor.substring(lastAtIndex + 1);
      // Check if there's no space after @
      if (!textAfterAt.includes(' ') && textAfterAt.length >= 0) {
        this.mentionSearch.set(textAfterAt.toLowerCase());
        this.filterUsers();
        this.showMentionSuggestions.set(true);
        return;
      }
    }

    this.showMentionSuggestions.set(false);
  }

  filterUsers() {
    const search = this.mentionSearch().toLowerCase();
    const filtered = this.users().filter(user =>
      user.fullName.toLowerCase().includes(search) ||
      user.userName?.toLowerCase().includes(search) ||
      user.email.toLowerCase().includes(search)
    );
    this.filteredUsers.set(filtered.slice(0, 5)); // Limit to 5 suggestions
  }

  selectUser(user: User) {
    const cursorPosition = this.messageInput.nativeElement.selectionStart;
    const textBeforeCursor = this.message.substring(0, cursorPosition);
    const lastAtIndex = textBeforeCursor.lastIndexOf('@');

    if (lastAtIndex !== -1) {
      const beforeAt = this.message.substring(0, lastAtIndex);
      const afterCursor = this.message.substring(cursorPosition);
      const username = user.userName || user.email.split('@')[0];

      this.message = `${beforeAt}@${username} ${afterCursor}`;

      // Reset suggestions
      this.showMentionSuggestions.set(false);
      this.mentionSearch.set('');

      // Focus back on textarea
      setTimeout(() => {
        this.messageInput.nativeElement.focus();
        const newPosition = beforeAt.length + username.length + 2;
        this.messageInput.nativeElement.setSelectionRange(newPosition, newPosition);
      });
    }
  }

  sendMessage() {
    if (!this.message.trim() || this.isSending()) return;

    this.isSending.set(true);

    this.jobCardService.addJobCardComment(
      this.jobCardId,
      this.message.trim(),
      this.jobItemId || null,
      this.parentCommentId
    ).subscribe({
      next: () => {
        this.message = '';
        this.isSending.set(false);
        this.commentAdded.emit();
        this.messageInput.nativeElement.focus();
      },
      error: (err) => {
        console.error('Error sending message:', err);
        alert('Failed to send message');
        this.isSending.set(false);
      }
    });
  }

  getInitials(fullName: string): string {
    return fullName
      .split(' ')
      .map(n => n[0])
      .join('')
      .toUpperCase()
      .substring(0, 2);
  }

  getCurrentUserInitials(): string {
    const currentUser = this.authService.currentUser();
    if (currentUser?.fullName) {
      return this.getInitials(currentUser.fullName);
    }
    return 'U';
  }
}
