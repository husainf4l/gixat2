import { Component, signal, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, Router } from '@angular/router';
import { LogoComponent } from '../logo/logo.component';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, LogoComponent],
  template: `
    @if (!isAuthPage()) {
      <nav 
        class="fixed top-0 left-0 right-0 z-50 transition-all duration-300 border-b"
        [class.bg-white/80]="!isMenuOpen()"
        [class.bg-white]="isMenuOpen()"
        [class.backdrop-blur-md]="!isMenuOpen()"
        [class.border-slate-200/60]="!isMenuOpen()"
        [class.border-transparent]="isMenuOpen()"
      >
        <div class="max-w-[1200px] mx-auto px-6 h-12 flex items-center justify-between">
          <!-- Logo -->
          <div class="flex-1 flex items-center">
            <app-logo [height]="24" />
          </div>

          <!-- Desktop Navigation -->
          <div class="hidden md:flex items-center gap-8">
            @for (link of navLinks; track link.path) {
              <a 
                [routerLink]="link.path" 
                routerLinkActive="text-sky-600"
                [routerLinkActiveOptions]="{ exact: true }"
                class="text-[12px] font-normal text-slate-600 hover:text-slate-900 transition-colors tracking-tight"
              >
                {{ link.label }}
              </a>
            }
          </div>

          <!-- Desktop Actions -->
          <div class="flex-1 hidden md:flex items-center justify-end gap-6">
            <a routerLink="/auth/login" class="text-[12px] font-normal text-slate-600 hover:text-slate-900 transition-colors">
              Log in
            </a>
            <a 
              routerLink="/auth/register" 
              class="px-3 py-1 bg-[#1b75bc] text-white text-[12px] font-medium rounded-sm hover:bg-[#155a92] transition-all duration-200"
            >
              Get Started
            </a>
          </div>

          <!-- Mobile Menu Button -->
          <button 
            (click)="toggleMenu()"
            class="md:hidden p-2 -mr-2 text-slate-600 hover:text-slate-900 transition-colors"
            aria-label="Toggle menu"
          >
            <div class="w-5 h-5 relative flex flex-col justify-center gap-1.5">
              <span 
                class="w-full h-0.5 bg-current transition-all duration-300"
                [class.rotate-45]="isMenuOpen()"
                [class.translate-y-2]="isMenuOpen()"
              ></span>
              <span 
                class="w-full h-0.5 bg-current transition-all duration-300"
                [class.opacity-0]="isMenuOpen()"
              ></span>
              <span 
                class="w-full h-0.5 bg-current transition-all duration-300"
                [class.-rotate-45]="isMenuOpen()"
                [class.-translate-y-1]="isMenuOpen()"
              ></span>
            </div>
          </button>
        </div>

        <!-- Mobile Navigation Overlay -->
        @if (isMenuOpen()) {
          <div 
            class="md:hidden fixed inset-0 top-12 bg-white z-40 px-10 py-8 animate-in fade-in slide-in-from-top-4 duration-300"
          >
            <div class="flex flex-col gap-6">
              @for (link of navLinks; track link.path) {
                <a 
                  [routerLink]="link.path" 
                  (click)="closeMenu()"
                  class="text-2xl font-semibold text-slate-900 hover:text-sky-600 transition-colors"
                >
                  {{ link.label }}
                </a>
              }
              <hr class="border-slate-100 my-2" />
              <a 
                routerLink="/auth/login" 
                (click)="closeMenu()"
                class="text-xl font-medium text-slate-600"
              >
                Log in
              </a>
              <a 
                routerLink="/auth/register" 
                (click)="closeMenu()"
                class="text-xl font-medium text-[#1b75bc]"
              >
                Get Started
              </a>
            </div>
          </div>
        }
      </nav>

      <!-- Spacer to prevent content from going under the fixed navbar -->
      <div class="h-12"></div>
    }
  `
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
