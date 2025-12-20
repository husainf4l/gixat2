import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, Router } from '@angular/router';
import { LogoComponent } from '../logo/logo.component';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, LogoComponent],
  template: `
    <aside class="w-64 h-screen bg-white border-r border-slate-200 flex flex-col fixed left-0 top-0 z-40">
      <!-- Logo Section -->
      <div class="p-6 border-b border-slate-100">
        <app-logo [height]="28" />
      </div>

      <!-- Navigation Links -->
      <nav class="flex-1 p-4 space-y-1 overflow-y-auto">
        @for (item of menuItems; track item.path) {
          <a 
            [routerLink]="item.path"
            routerLinkActive="bg-slate-50 text-[#1b75bc]"
            [routerLinkActiveOptions]="{ exact: true }"
            class="flex items-center gap-3 px-4 py-3 text-sm font-medium text-slate-600 rounded-xl hover:bg-slate-50 hover:text-slate-900 transition-all duration-200"
          >
            <i [class]="item.icon" class="text-lg"></i>
            {{ item.label }}
          </a>
        }
      </nav>

      <!-- User Section / Logout -->
      <div class="p-4 border-t border-slate-100">
        <button 
          (click)="logout()"
          class="w-full flex items-center gap-3 px-4 py-3 text-sm font-medium text-red-600 rounded-xl hover:bg-red-50 transition-all duration-200"
        >
          <i class="ri-logout-box-line text-lg"></i>
          Sign Out
        </button>
      </div>
    </aside>
  `,
  styles: [`
    :host {
      display: block;
    }
  `]
})
export class SidebarComponent {
  private router = inject(Router);

  menuItems = [
    { path: '/dashboard', label: 'Dashboard', icon: 'ri-dashboard-line' },
    { path: '/dashboard/projects', label: 'Projects', icon: 'ri-folder-line' },
    { path: '/dashboard/tasks', label: 'Tasks', icon: 'ri-checkbox-line' },
    { path: '/dashboard/team', label: 'Team', icon: 'ri-group-line' },
    { path: '/dashboard/settings', label: 'Settings', icon: 'ri-settings-3-line' }
  ];

  logout() {
    // Since we use HTTP-only cookies, we should ideally call a logout mutation on the backend
    // For now, we'll just navigate to login. The cookie will be cleared by the backend if implemented.
    this.router.navigate(['/auth/login']);
  }
}
