import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, Router } from '@angular/router';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { LogoComponent } from '../../../components/logo/logo.component';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-signup',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, ReactiveFormsModule, LogoComponent],
  templateUrl: './signup.component.html',
  styleUrl: './signup.component.css'
})
export class SignupComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);

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
          } else if (response.register.token) {
            localStorage.setItem('token', response.register.token);
            this.router.navigate(['/']);
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
