import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { SidebarComponent } from '../../components/sidebar/sidebar';
import { LogoComponent } from '../../components/logo/logo.component';
import { HeaderComponent } from '../../components/header/header.component';
import { LayoutService } from '../../services/layout.service';

@Component({
  selector: 'app-dashboard-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, SidebarComponent, LogoComponent, HeaderComponent],
  templateUrl: './dashboard-layout.html',
})
export class DashboardLayoutComponent {
  layoutService = inject(LayoutService);
}
