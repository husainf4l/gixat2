import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, Router } from '@angular/router';
import { LogoComponent } from '../logo/logo.component';
import { LayoutService } from '../../services/layout.service';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, LogoComponent],
  templateUrl: './sidebar.html',
  styleUrl: './sidebar.css'
})
export class SidebarComponent {
  private router = inject(Router);
  layoutService = inject(LayoutService);

  menuItems = [
    { path: '/dashboard', label: 'Dashboard', icon: 'ri-dashboard-line' },
    { path: '/dashboard/customers', label: 'Customers', icon: 'ri-user-3-line' },
    { path: '/dashboard/vehicles', label: 'Vehicles', icon: 'ri-car-line' },
    { path: '/dashboard/sessions', label: 'Sessions', icon: 'ri-calendar-check-line' },
    { path: '/dashboard/job-cards', label: 'Job Cards', icon: 'ri-file-list-3-line' },
    { path: '/dashboard/projects', label: 'Projects', icon: 'ri-folder-line' },
    { path: '/dashboard/tasks', label: 'Tasks', icon: 'ri-checkbox-line' },
    { path: '/dashboard/team', label: 'Team', icon: 'ri-group-line' },
    { path: '/dashboard/settings', label: 'Settings', icon: 'ri-settings-3-line' }
  ];

  organizationPath = '/dashboard/organization';

  onNavigate() {
    // Close sidebar on mobile when navigating
    if (window.innerWidth < 1024) {
      this.layoutService.closeSidebar();
    }
  }

  logout() {
    // Since we use HTTP-only cookies, we should ideally call a logout mutation on the backend
    // For now, we'll just navigate to login. The cookie will be cleared by the backend if implemented.
    this.router.navigate(['/auth/login']);
  }
}
