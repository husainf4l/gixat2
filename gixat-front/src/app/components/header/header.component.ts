import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule, NavigationEnd } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { filter } from 'rxjs';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './header.component.html',
  styleUrl: './header.component.css'
})
export class HeaderComponent implements OnInit {
  private router = inject(Router);
  
  pageTitle = signal<string>('Dashboard');
  searchQuery = signal<string>('');
  showUserMenu = signal<boolean>(false);
  userInitials = signal<string>('JD');
  userName = signal<string>('John Doe');
  userEmail = signal<string>('john@example.com');

  ngOnInit() {
    // Update page title based on route
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe(() => {
      this.updatePageTitle();
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
    } else if (url.includes('/job-cards') && !url.match(/\/job-cards\/[^/]+/)) {
      this.pageTitle.set('Job Cards');
    } else if (url.match(/\/job-cards\/[^/]+/)) {
      this.pageTitle.set('Job Card Details');
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

  onSearch() {
    // Implement global search functionality
    console.log('Searching for:', this.searchQuery());
  }

  navigateToProfile() {
    this.router.navigate(['/dashboard/profile']);
    this.closeUserMenu();
  }

  logout() {
    this.router.navigate(['/auth/login']);
    this.closeUserMenu();
  }
}
