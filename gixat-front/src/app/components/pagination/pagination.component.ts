import { Component, Input, Output, EventEmitter, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-pagination',
  standalone: true,
  imports: [CommonModule],
  template: `
    @if (totalPages > 1) {
      <div class="flex items-center justify-between">
        <!-- Previous Button -->
        <button
          (click)="onPrevious()"
          [disabled]="currentPage === 1"
          class="inline-flex items-center gap-2 px-3.5 py-2 bg-white border border-slate-200 text-slate-700 text-sm font-medium rounded-lg hover:bg-slate-50 transition-all disabled:opacity-50 disabled:cursor-not-allowed disabled:hover:bg-white"
        >
          <i class="ri-arrow-left-s-line"></i>
          <span>Previous</span>
        </button>

        <!-- Page Numbers -->
        <div class="flex items-center gap-1">
          @for (page of pageNumbers(); track page) {
            @if (page === -1) {
              <span class="px-3 py-2 text-sm text-slate-400">...</span>
            } @else {
              <button
                (click)="onPageClick(page)"
                class="inline-flex items-center justify-center w-10 h-10 text-sm font-medium rounded-lg transition-all"
                [class.bg-[#1b75bc]]="currentPage === page"
                [class.text-white]="currentPage === page"
                [class.hover:bg-[#155a92]]="currentPage === page"
                [class.bg-white]="currentPage !== page"
                [class.border]="currentPage !== page"
                [class.border-slate-200]="currentPage !== page"
                [class.text-slate-700]="currentPage !== page"
                [class.hover:bg-slate-50]="currentPage !== page"
              >
                {{ page }}
              </button>
            }
          }
        </div>

        <!-- Next Button -->
        <button
          (click)="onNext()"
          [disabled]="currentPage === totalPages"
          class="inline-flex items-center gap-2 px-3.5 py-2 bg-white border border-slate-200 text-slate-700 text-sm font-medium rounded-lg hover:bg-slate-50 transition-all disabled:opacity-50 disabled:cursor-not-allowed disabled:hover:bg-white"
        >
          <span>Next</span>
          <i class="ri-arrow-right-s-line"></i>
        </button>
      </div>
    }
  `
})
export class PaginationComponent {
  @Input() currentPage: number = 1;
  @Input() totalPages: number = 1;
  @Output() pageChange = new EventEmitter<number>();

  pageNumbers = computed(() => {
    const total = this.totalPages;
    const current = this.currentPage;
    const pages: number[] = [];

    if (total <= 7) {
      // Show all pages if 7 or less
      for (let i = 1; i <= total; i++) {
        pages.push(i);
      }
    } else {
      // Always show first page
      pages.push(1);

      if (current > 3) {
        pages.push(-1); // Ellipsis
      }

      // Show pages around current
      const start = Math.max(2, current - 1);
      const end = Math.min(total - 1, current + 1);
      for (let i = start; i <= end; i++) {
        pages.push(i);
      }

      if (current < total - 2) {
        pages.push(-1); // Ellipsis
      }

      // Always show last page
      pages.push(total);
    }

    return pages;
  });

  onPageClick(page: number) {
    if (page >= 1 && page <= this.totalPages) {
      this.pageChange.emit(page);
    }
  }

  onPrevious() {
    if (this.currentPage > 1) {
      this.pageChange.emit(this.currentPage - 1);
    }
  }

  onNext() {
    if (this.currentPage < this.totalPages) {
      this.pageChange.emit(this.currentPage + 1);
    }
  }
}
