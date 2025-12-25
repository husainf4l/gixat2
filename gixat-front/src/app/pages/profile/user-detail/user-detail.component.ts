import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ProfileService, UserProfile, UpdateProfileInput, UpdateUserInput } from '../../../services/profile.service';
import { catchError, of } from 'rxjs';

@Component({
  selector: 'app-user-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './user-detail.component.html'
})
export class UserDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private profileService = inject(ProfileService);

  // User data
  user = signal<UserProfile | null>(null);
  isLoading = signal<boolean>(true);
  isSaving = signal<boolean>(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);

  // Form data
  formData = signal<UpdateUserInput>({
    fullName: '',
    email: '',
    phoneNumber: '',
    bio: ''
  });

  // Validation
  errors = signal<{ [key: string]: string }>({});

  ngOnInit() {
    const userId = this.route.snapshot.paramMap.get('userId');
    if (userId) {
      this.loadUser(userId);
    } else {
      this.router.navigate(['/dashboard/profile']);
    }
  }

  loadUser(userId: string) {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.profileService.getUserById(userId).pipe(
      catchError((err: Error) => {
        this.errorMessage.set(err instanceof Error ? err.message : 'Failed to load user');
        this.isLoading.set(false);
        return of(null);
      })
    ).subscribe((user) => {
      if (user) {
        this.user.set(user);
        this.formData.set({
          fullName: user.fullName || '',
          email: user.email || '',
          phoneNumber: user.phoneNumber || '',
          bio: user.bio || ''
        });
      }
      this.isLoading.set(false);
    });
  }

  validateForm(): boolean {
    const errors: { [key: string]: string } = {};
    const data = this.formData();

    if (!data.fullName || data.fullName.trim().length === 0) {
      errors['fullName'] = 'Full name is required';
    } else if (data.fullName.trim().length < 2) {
      errors['fullName'] = 'Full name must be at least 2 characters';
    }

    if (data.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(data.email)) {
      errors['email'] = 'Please enter a valid email address';
    }

    if (data.bio && data.bio.length > 500) {
      errors['bio'] = 'Bio must be less than 500 characters';
    }

    this.errors.set(errors);
    return Object.keys(errors).length === 0;
  }

  onFieldChange(field: keyof UpdateUserInput, value: string) {
    this.formData.update(data => ({
      ...data,
      [field]: value
    }));
    
    if (this.errors()[field]) {
      this.errors.update(errors => {
        const newErrors = { ...errors };
        delete newErrors[field];
        return newErrors;
      });
    }
    this.successMessage.set(null);
  }

  async saveUser() {
    if (!this.validateForm()) {
      return;
    }

    const userId = this.route.snapshot.paramMap.get('userId');
    if (!userId) {
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    const data = this.formData();
    const updateData: UpdateUserInput = {};

    const currentUser = this.user();
    if (currentUser) {
      if (data.fullName?.trim() !== currentUser.fullName) {
        updateData.fullName = data.fullName?.trim();
      }
      if (data.email?.trim() !== currentUser.email) {
        updateData.email = data.email?.trim();
      }
      if (data.phoneNumber?.trim() !== (currentUser.phoneNumber || '')) {
        updateData.phoneNumber = data.phoneNumber?.trim() || undefined;
      }
      if (data.bio?.trim() !== (currentUser.bio || '')) {
        updateData.bio = data.bio?.trim() || undefined;
      }
    }

    if (Object.keys(updateData).length === 0) {
      this.isSaving.set(false);
      this.successMessage.set('No changes to save');
      setTimeout(() => this.successMessage.set(null), 3000);
      return;
    }

    this.profileService.updateUser(userId, updateData).pipe(
      catchError((err: Error) => {
        this.errorMessage.set(err instanceof Error ? err.message : 'Failed to update user');
        this.isSaving.set(false);
        return of(null);
      })
    ).subscribe((updatedUser) => {
      if (updatedUser) {
        this.user.set(updatedUser);
        this.successMessage.set('User updated successfully');
        setTimeout(() => this.successMessage.set(null), 3000);
      }
      this.isSaving.set(false);
    });
  }

  goBack() {
    this.router.navigate(['/dashboard/profile']);
  }

  getInitials(): string {
    const user = this.user();
    if (!user?.fullName) {
      return '?';
    }
    const names = user.fullName.trim().split(' ');
    if (names.length >= 2) {
      return (names[0][0] + names[names.length - 1][0]).toUpperCase();
    }
    return names[0][0].toUpperCase();
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    });
  }
}





