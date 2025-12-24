import { Component, inject, signal, OnInit, PLATFORM_ID } from '@angular/core';
import { CommonModule, NgOptimizedImage, isPlatformBrowser } from '@angular/common';
import { RouterLink, Router } from '@angular/router';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { LogoComponent } from '../../../components/logo/logo.component';
import { AuthService } from '../../../services/auth.service';
import { environment } from '../../../../environments/environment';

declare const google: any;

@Component({
  selector: 'app-signup',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, ReactiveFormsModule, LogoComponent, NgOptimizedImage],
  templateUrl: './signup.component.html',
})
export class SignupComponent implements OnInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  private platformId = inject(PLATFORM_ID);

  errorMessage = signal<string | null>(null);

  signupForm: FormGroup = this.fb.group({
    fullName: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    confirmPassword: ['', [Validators.required]],
    agreeTerms: [false, [Validators.requiredTrue]]
  }, {
    validators: this.passwordMatchValidator
  });

  passwordMatchValidator(g: FormGroup) {
    return g.get('password')?.value === g.get('confirmPassword')?.value
      ? null : { 'mismatch': true };
  }

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
        document.getElementById('google-signup-button'),
        { 
          theme: 'outline', 
          size: 'large',
          width: '100%',
          text: 'signup_with'
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
          this.errorMessage.set(result.loginWithGoogle.message || 'Google sign-up failed');
        }
      },
      error: (error) => {
        this.errorMessage.set('Google sign-up failed. Please try again.');
        console.error('Google sign-up failed', error);
      }
    });
  }

  onSubmit() {
    if (this.signupForm.valid) {
      this.errorMessage.set(null);
      const { fullName, email, password } = this.signupForm.value;
      const input = {
        fullName,
        email,
        password,
        role: 'Admin',
        userType: 'ORGANIZATIONAL',
        organizationId: 'ee32dcea-4539-4517-9b09-ee93bad302cc' // Default for now
      };
      this.authService.register(input).subscribe({
        next: (response) => {
          if (response.register.error) {
            this.errorMessage.set(response.register.error);
          } else {
            // Token is now handled via HTTP-only cookie
            this.router.navigate(['/dashboard']);
          }
        },
        error: (error) => {
          this.errorMessage.set('An unexpected error occurred. Please try again.');
          console.error('Signup failed', error);
        }
      });
    }
  }
}
