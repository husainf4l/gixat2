import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-callback',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="min-h-screen flex items-center justify-center bg-gray-50">
      <div class="text-center">
        <div class="inline-block animate-spin rounded-full h-12 w-12 border-b-2 border-[#1b75bc] mb-4"></div>
        <p class="text-gray-600">{{ message }}</p>
      </div>
    </div>
  `
})
export class CallbackComponent implements OnInit {
  private router = inject(Router);
  private authService = inject(AuthService);
  message = 'Processing authentication...';

  ngOnInit() {
    // Handle OAuth callback - the backend will set the auth cookie
    // Check if user is authenticated and redirect accordingly
    this.authService.me().subscribe({
      next: (userData) => {
        if (userData.me.organizationId) {
          this.router.navigate(['/dashboard']);
        } else {
          this.router.navigate(['/organization-setup']);
        }
      },
      error: (error) => {
        console.error('Authentication failed:', error);
        this.message = 'Authentication failed. Redirecting to login...';
        setTimeout(() => {
          this.router.navigate(['/auth/login']);
        }, 2000);
      }
    });
  }
}
