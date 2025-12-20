import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { SidebarComponent } from '../../components/sidebar/sidebar';

@Component({
  selector: 'app-dashboard-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, SidebarComponent],
  template: `
    <div class="flex min-h-screen bg-slate-50">
      <!-- Sidebar -->
      <app-sidebar />

      <!-- Main Content -->
      <main class="flex-1 ml-64">
        <router-outlet />
      </main>
    </div>
  `,
  styles: [`
    :host {
      display: block;
    }
  `]
})
export class DashboardLayoutComponent {}
