import { Component, signal, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, Router } from '@angular/router';
import { LogoComponent } from '../logo/logo.component';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, LogoComponent],
  templateUrl: './navbar.component.html'
})
export class NavbarComponent {
  private router = inject(Router);
  isMenuOpen = signal(false);

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
}
