import { Component, inject, signal } from '@angular/core';
import { CommonModule, NgOptimizedImage } from '@angular/common';
import { RouterLink, Router } from '@angular/router';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { LogoComponent } from '../../../components/logo/logo.component';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, ReactiveFormsModule, LogoComponent, NgOptimizedImage],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);

  errorMessage = signal<string | null>(null);

  loginForm: FormGroup = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
    rememberMe: [false]
  });

  onSubmit() {
    if (this.loginForm.valid) {
      this.errorMessage.set(null);
      const { email, password } = this.loginForm.value;
      const input = { email, password };
      
      this.authService.login(input).subscribe({
        next: (response) => {
          if (response.login.error) {
            this.errorMessage.set(response.login.error);
          } else {
            // Token is handled via HTTP-only cookie
            // Check if user has organization
            this.authService.me().subscribe({
              next: (userData) => {
                if (userData.me.organizationId) {
                  this.router.navigate(['/dashboard']);
                } else {
                  this.router.navigate(['/organization-setup']);
                }
              },
              error: () => {
                this.router.navigate(['/dashboard']);
              }
            });
          }
        },
        error: (error) => {
          this.errorMessage.set('An unexpected error occurred. Please try again.');
          console.error('Login failed', error);
        }
      });
    }
  }
}
