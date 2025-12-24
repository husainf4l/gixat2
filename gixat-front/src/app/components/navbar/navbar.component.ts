import { Component, signal, inject, OnInit, OnDestroy } from '@angular/core';
import { RouterLink, RouterLinkActive, Router } from '@angular/router';
import { LogoComponent } from '../logo/logo.component';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { Subscription, catchError, of } from 'rxjs';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, LogoComponent],
  templateUrl: './navbar.component.html'
})
export class NavbarComponent implements OnInit, OnDestroy {
  private router = inject(Router);
  private authService = inject(AuthService);
  private authSubscription?: Subscription;
  
  isMenuOpen = signal(false);
  isAuthenticated = signal<boolean>(false);
  showUserMenu = signal<boolean>(false);
  userInitials = signal<string>('');
  userName = signal<string>('');
  userEmail = signal<string>('');
  avatarUrl = signal<string | null>(null);
  userId = signal<string | null>(null);

  navLinks = [
    { path: '/', label: 'Home' },
    { path: '/features', label: 'Features' },
    { path: '/pricing', label: 'Pricing' },
    { path: '/support', label: 'Support' }
  ];

  isAuthPage(): boolean {
    return this.router.url.includes('/auth/') || 
           this.router.url.includes('/dashboard') || 
           this.router.url.includes('/organization-setup');
  }

  toggleMenu() {
    this.isMenuOpen.update(v => !v);
    if (this.isMenuOpen()) {
      document.body.style.overflow = 'hidden';
    } else {
      document.body.style.overflow = '';
    }
  }

  closeMenu() {
    this.isMenuOpen.set(false);
    document.body.style.overflow = '';
  }

  ngOnInit() {
    // Check authentication status
    this.checkAuth();
  }

  ngOnDestroy() {
    if (this.authSubscription) {
      this.authSubscription.unsubscribe();
    }
  }

  checkAuth() {
    this.authSubscription = this.authService.me().pipe(
      catchError(() => {
        this.isAuthenticated.set(false);
        return of(null);
      })
    ).subscribe((data) => {
      const isAuth = !!(data && data.me);
      this.isAuthenticated.set(isAuth);
      
      if (isAuth && data.me) {
        const user = data.me;
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
    });
  }

  toggleUserMenu() {
    this.showUserMenu.set(!this.showUserMenu());
  }

  closeUserMenu() {
    this.showUserMenu.set(false);
  }

  navigateToDashboard() {
    this.router.navigate(['/dashboard']);
    this.closeUserMenu();
  }

  navigateToProfile() {
    const userId = this.userId();
    if (userId) {
      this.router.navigate(['/dashboard/organization', userId]);
    } else {
      this.router.navigate(['/dashboard/organization']);
    }
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
