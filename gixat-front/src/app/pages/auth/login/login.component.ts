import { Component, inject, signal, OnInit, PLATFORM_ID } from '@angular/core';
import { CommonModule, NgOptimizedImage, isPlatformBrowser } from '@angular/common';
import { RouterLink, Router } from '@angular/router';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { LogoComponent } from '../../../components/logo/logo.component';
import { AuthService } from '../../../services/auth.service';
import { environment } from '../../../../environments/environment';

declare const google: any;

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, ReactiveFormsModule, LogoComponent, NgOptimizedImage],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent implements OnInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  private platformId = inject(PLATFORM_ID);

  errorMessage = signal<string | null>(null);

  loginForm: FormGroup = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
    rememberMe: [false]
  });

  ngOnInit() {
    if (isPlatformBrowser(this.platformId)) {
      this.initializeGoogleSignIn();
    }
  }

  private initializeGoogleSignIn() {
    if (typeof google !== 'undefined') {
      google.accounts.id.initialize({
        client_id: environment.googleClientId,
        callback: (response: any) => this.handleGoogleSignIn(response)
      });
      google.accounts.id.renderButton(
        document.getElementById('google-signin-button'),
        { 
          theme: 'outline', 
          size: 'large',
          width: '100%',
          text: 'signin_with'
        }
      );
    }
  }

  handleGoogleSignIn(response: any) {
    this.errorMessage.set(null);
    const idToken = response.credential;
    
    this.authService.loginWithGoogle(idToken).subscribe({
      next: (result) => {
        if (result.loginWithGoogle.success) {
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
        } else {
          this.errorMessage.set(result.loginWithGoogle.message || 'Google sign-in failed');
        }
      },
      error: (error) => {
        this.errorMessage.set('Google sign-in failed. Please try again.');
        console.error('Google sign-in failed', error);
      }
    });
  }

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
