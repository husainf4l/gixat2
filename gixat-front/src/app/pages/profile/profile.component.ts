import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProfileService, UserProfile, UpdateProfileInput } from '../../services/profile.service';
import { catchError, of, firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.css'
})
export class ProfileComponent implements OnInit {
  private profileService = inject(ProfileService);

  // Profile data
  profile = signal<UserProfile | null>(null);
  isLoading = signal<boolean>(true);
  isSaving = signal<boolean>(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);

  // Form data
  formData = signal<UpdateProfileInput>({
    fullName: '',
    bio: '',
    phoneNumber: ''
  });

  // Avatar upload
  isUploadingAvatar = signal<boolean>(false);
  avatarPreview = signal<string | null>(null);
  selectedFile: File | null = null;

  // Validation
  errors = signal<{ [key: string]: string }>({});

  ngOnInit() {
    this.loadProfile();
  }

  loadProfile() {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.profileService.getMyProfile().pipe(
      catchError((err: Error) => {
        this.errorMessage.set(err instanceof Error ? err.message : 'Failed to load profile');
        this.isLoading.set(false);
        return of(null);
      })
    ).subscribe((profile) => {
      if (profile) {
        this.profile.set(profile);
        this.formData.set({
          fullName: profile.fullName || '',
          bio: profile.bio || '',
          phoneNumber: profile.phoneNumber || ''
        });
        this.avatarPreview.set(profile.avatarUrl || null);
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
    } else if (data.fullName.trim().length > 100) {
      errors['fullName'] = 'Full name must be less than 100 characters';
    }

    if (data.bio && data.bio.length > 500) {
      errors['bio'] = 'Bio must be less than 500 characters';
    }

    if (data.phoneNumber) {
      // Basic phone validation (allows international formats)
      const phoneRegex = /^[\+]?[(]?[0-9]{1,4}[)]?[-\s\.]?[(]?[0-9]{1,4}[)]?[-\s\.]?[0-9]{1,9}$/;
      if (!phoneRegex.test(data.phoneNumber.replace(/\s/g, ''))) {
        errors['phoneNumber'] = 'Please enter a valid phone number';
      }
    }

    this.errors.set(errors);
    return Object.keys(errors).length === 0;
  }

  onFieldChange(field: keyof UpdateProfileInput, value: string) {
    this.formData.update(data => ({
      ...data,
      [field]: value
    }));
    // Clear error for this field when user starts typing
    if (this.errors()[field]) {
      this.errors.update(errors => {
        const newErrors = { ...errors };
        delete newErrors[field];
        return newErrors;
      });
    }
    // Clear success message when form changes
    this.successMessage.set(null);
  }

  async saveProfile() {
    if (!this.validateForm()) {
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    const data = this.formData();
    const updateData: UpdateProfileInput = {};

    // Only include fields that have changed
    const currentProfile = this.profile();
    if (currentProfile) {
      if (data.fullName?.trim() !== currentProfile.fullName) {
        updateData.fullName = data.fullName?.trim();
      }
      if (data.bio?.trim() !== (currentProfile.bio || '')) {
        updateData.bio = data.bio?.trim() || undefined;
      }
      if (data.phoneNumber?.trim() !== (currentProfile.phoneNumber || '')) {
        updateData.phoneNumber = data.phoneNumber?.trim() || undefined;
      }
    }

    // If nothing changed, show message and return
    if (Object.keys(updateData).length === 0) {
      this.isSaving.set(false);
      this.successMessage.set('No changes to save');
      setTimeout(() => this.successMessage.set(null), 3000);
      return;
    }

    this.profileService.updateProfile(updateData).pipe(
      catchError((err: Error) => {
        this.errorMessage.set(err instanceof Error ? err.message : 'Failed to update profile');
        this.isSaving.set(false);
        return of(null);
      })
    ).subscribe((updatedProfile) => {
      if (updatedProfile) {
        this.profile.set({ ...this.profile()!, ...updatedProfile });
        this.successMessage.set('Profile updated successfully');
        setTimeout(() => this.successMessage.set(null), 3000);
      }
      this.isSaving.set(false);
    });
  }

  onAvatarSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];

    if (!file) {
      return;
    }

    // Validate file type
    const allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif', 'image/webp'];
    if (!allowedTypes.includes(file.type)) {
      this.errorMessage.set('Please select a valid image file (JPEG, PNG, GIF, or WebP)');
      return;
    }

    // Validate file size (max 5MB)
    const maxSize = 5 * 1024 * 1024; // 5MB
    if (file.size > maxSize) {
      this.errorMessage.set('Image size must be less than 5MB');
      return;
    }

    this.selectedFile = file;
    this.errorMessage.set(null);

    // Create preview
    const reader = new FileReader();
    reader.onload = (e) => {
      this.avatarPreview.set(e.target?.result as string);
    };
    reader.readAsDataURL(file);
  }

  async uploadAvatar() {
    if (!this.selectedFile) {
      return;
    }

    this.isUploadingAvatar.set(true);
    this.errorMessage.set(null);

    try {
      // Step 1: Get presigned URL from backend
      const presignedUrl = await firstValueFrom(
        this.profileService.generateAvatarUploadUrl(
          this.selectedFile.name,
          this.selectedFile.type
        )
      );

      if (!presignedUrl) {
        throw new Error('Failed to get upload URL');
      }

      // Step 2: Upload file directly to S3 using presigned URL
      await firstValueFrom(
        this.profileService.uploadToS3(presignedUrl, this.selectedFile)
      );

      // Step 3: Extract base URL from presigned URL (remove query parameters)
      const avatarUrl = this.profileService.extractBaseUrlFromPresignedUrl(presignedUrl);

      // Step 4: Update profile with the new avatar URL
      const updatedProfile = await firstValueFrom(
        this.profileService.updateProfile({ avatarUrl })
      );

      // Step 5: Update local state with new avatar URL
      if (updatedProfile.avatarUrl) {
        this.profile.update(profile => profile ? { ...profile, avatarUrl: updatedProfile.avatarUrl } : null);
        this.avatarPreview.set(updatedProfile.avatarUrl);
      }

      this.successMessage.set('Avatar uploaded successfully');
      setTimeout(() => this.successMessage.set(null), 3000);
      this.selectedFile = null;

      // Reset file input
      const fileInput = document.getElementById('avatar-input') as HTMLInputElement;
      if (fileInput) {
        fileInput.value = '';
      }
    } catch (error) {
      this.errorMessage.set(
        error instanceof Error ? error.message : 'Failed to upload avatar'
      );
    } finally {
      this.isUploadingAvatar.set(false);
    }
  }

  async deleteAvatar() {
    if (!confirm('Are you sure you want to delete your avatar?')) {
      return;
    }

    this.isUploadingAvatar.set(true);
    this.errorMessage.set(null);

    this.profileService.deleteAvatar().pipe(
      catchError((err: Error) => {
        this.errorMessage.set(err instanceof Error ? err.message : 'Failed to delete avatar');
        this.isUploadingAvatar.set(false);
        return of(false);
      })
    ).subscribe((success) => {
      if (success) {
        this.avatarPreview.set(null);
        this.selectedFile = null;
        this.loadProfile();
        this.successMessage.set('Avatar deleted successfully');
        setTimeout(() => this.successMessage.set(null), 3000);
      }
      this.isUploadingAvatar.set(false);
    });
  }

  getInitials(): string {
    const profile = this.profile();
    if (!profile?.fullName) {
      return '?';
    }
    const names = profile.fullName.trim().split(' ');
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

