import { Component, OnInit, inject, signal, computed, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule, NavigationEnd } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { SearchService, SearchResult } from '../../services/search.service';
import { filter, debounceTime, distinctUntilChanged, switchMap } from 'rxjs';
import { Subject } from 'rxjs';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './header.component.html',
})
export class HeaderComponent implements OnInit {
  private router = inject(Router);
  private authService = inject(AuthService);
  private searchService = inject(SearchService);
  
  pageTitle = signal<string>('Dashboard');
  searchQuery = signal<string>('');
  searchResults = signal<SearchResult[]>([]);
  showSearchResults = signal<boolean>(false);
  isSearching = signal<boolean>(false);
  showUserMenu = signal<boolean>(false);
  userInitials = signal<string>('');
  userName = signal<string>('');
  userEmail = signal<string>('');
  avatarUrl = signal<string | null>(null);
  userId = signal<string | null>(null);

  private searchSubject = new Subject<string>();

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent) {
    const target = event.target as HTMLElement;
    
    // Check if click is inside search area
    const isInsideSearch = target.closest('.max-w-xl.relative');
    
    // Close search results if clicking outside search area
    if (!isInsideSearch && this.showSearchResults()) {
      this.closeSearchResults();
    }
    
    // Close user menu if clicking outside
    const isInsideUserMenu = target.closest('.relative');
    if (!isInsideUserMenu && this.showUserMenu()) {
      this.closeUserMenu();
    }
  }

  ngOnInit() {
    // Load user data
    this.authService.me().subscribe({
      next: (data) => {
        const user = data.me;
        if (user) {
          this.userName.set(user.fullName || '');
          this.userEmail.set(user.email || '');
          this.avatarUrl.set(user.avatarUrl || null);
          this.userId.set(user.id || null);
          
          // Generate initials from full name
          const names = user.fullName?.split(' ') || [];
          const initials = names
            .slice(0, 2)
            .map((n: string) => n.charAt(0).toUpperCase())
            .join('');
          this.userInitials.set(initials || user.email?.charAt(0).toUpperCase() || '?');
        }
      },
      error: (err) => {
        console.error('Error loading user data:', err);
      }
    });

    // Setup search with debounce
    this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap(query => {
        if (!query || query.trim().length < 2) {
          this.searchResults.set([]);
          this.showSearchResults.set(false);
          this.isSearching.set(false);
          return [];
        }
        this.isSearching.set(true);
        return this.searchService.globalSearch(query);
      })
    ).subscribe({
      next: (results) => {
        this.searchResults.set(results);
        this.showSearchResults.set(results.length > 0);
        this.isSearching.set(false);
      },
      error: (err) => {
        console.error('Search error:', err);
        this.isSearching.set(false);
      }
    });

    // Update page title based on route
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe(() => {
      this.updatePageTitle();
      this.showSearchResults.set(false);
      this.searchQuery.set('');
      this.searchResults.set([]);
    });
    
    this.updatePageTitle();
  }

  private updatePageTitle() {
    const url = this.router.url;
    
    if (url.includes('/customers') && !url.match(/\/customers\/[^/]+/)) {
      this.pageTitle.set('Customers');
    } else if (url.match(/\/customers\/[^/]+\/vehicles/)) {
      this.pageTitle.set('Vehicle Details');
    } else if (url.match(/\/customers\/[^/]+/)) {
      this.pageTitle.set('Customer Details');
    } else if (url.includes('/vehicles') && !url.match(/\/vehicles\/[^/]+/)) {
      this.pageTitle.set('Vehicles');
    } else if (url.match(/\/vehicles\/[^/]+/)) {
      this.pageTitle.set('Vehicle Details');
    } else if (url.includes('/sessions') && !url.match(/\/sessions\/[^/]+/)) {
      this.pageTitle.set('Sessions');
    } else if (url.match(/\/sessions\/[^/]+/)) {
      this.pageTitle.set('Session Details');
    } else if (url.includes('/appointments') && !url.match(/\/appointments\/[^/]+/)) {
      this.pageTitle.set('Appointments');
    } else if (url.match(/\/appointments\/[^/]+/)) {
      this.pageTitle.set('Appointment Details');
    } else if (url.includes('/job-cards') && !url.match(/\/job-cards\/[^/]+/)) {
      this.pageTitle.set('Job Cards');
    } else if (url.match(/\/job-cards\/[^/]+/)) {
      this.pageTitle.set('Job Card Details');
    } else if (url.includes('/organization')) {
      if (url.match(/\/organization\/[^/]+/)) {
        this.pageTitle.set('Edit User');
      } else {
        this.pageTitle.set('Organization');
      }
    } else if (url.includes('/profile')) {
      this.pageTitle.set('Profile');
    } else if (url === '/dashboard' || url === '/dashboard/') {
      this.pageTitle.set('Dashboard');
    }
  }

  toggleUserMenu() {
    this.showUserMenu.set(!this.showUserMenu());
  }

  closeUserMenu() {
    this.showUserMenu.set(false);
  }

  onSearchInput(value: string) {
    this.searchQuery.set(value);
    this.searchSubject.next(value);
  }

  onSearch() {
    const query = this.searchQuery();
    if (query && query.trim().length >= 2) {
      this.searchSubject.next(query);
    }
  }

  onResultClick(result: SearchResult) {
    this.router.navigateByUrl(result.url);
  }

  clearSearch() {
    this.searchQuery.set('');
    this.searchResults.set([]);
    this.showSearchResults.set(false);
  }

  closeSearchResults() {
    this.showSearchResults.set(false);
  }

  getSearchResultIcon(type: string): string {
    switch (type) {
      case 'customer': return 'ri-user-line';
      case 'vehicle': return 'ri-car-line';
      case 'session': return 'ri-calendar-check-line';
      default: return 'ri-file-line';
    }
  }

  getSearchResultColor(type: string): string {
    switch (type) {
      case 'customer': return 'text-blue-600 bg-blue-50';
      case 'vehicle': return 'text-green-600 bg-green-50';
      case 'session': return 'text-purple-600 bg-purple-50';
      default: return 'text-slate-600 bg-slate-50';
    }
  }

  navigateToDashboard() {
    this.router.navigate(['/dashboard']);
    this.closeUserMenu();
  }

  navigateToProfile() {
    // Navigate to organization page with profile tab query parameter
    this.router.navigate(['/dashboard/organization'], {
      queryParams: { tab: 'profile' }
    });
    this.closeUserMenu();
  }

  logout() {
    this.authService.logout().subscribe({
      next: () => {
        this.router.navigate(['/auth/login']);
        this.closeUserMenu();
      },
      error: () => {
        // Navigate to login even if logout fails
        this.router.navigate(['/auth/login']);
        this.closeUserMenu();
      }
    });
  }
}
