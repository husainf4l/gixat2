import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.html',
})
export class DashboardComponent implements OnInit {
  private authService = inject(AuthService);
  user = this.authService.currentUser;

  ngOnInit() {
    // The guard already called me(), but we can call it again to ensure fresh data
    // or just rely on the signal if we're sure it's populated.
    if (!this.user()) {
      this.authService.me().subscribe();
    }
  }
}
