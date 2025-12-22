import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { ProfileService, UserProfile, UpdateProfileInput, Organization, UpdateOrganizationInput, AddressInput, OrganizationUser, CreateUserInput } from '../../services/profile.service';
import { catchError, of, firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.css'
})
export class ProfileComponent implements OnInit {
  private profileService = inject(ProfileService);
  private router = inject(Router);

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

  // Organization data
  organization = signal<Organization | null>(null);
  isSavingOrganization = signal<boolean>(false);
  isUploadingLogo = signal<boolean>(false);
  logoPreview = signal<string | null>(null);
  selectedLogoFile: File | null = null;

  // Organization form data
  organizationFormData = signal<UpdateOrganizationInput>({
    name: '',
    address: {
      country: '',
      city: '',
      street: '',
      phoneCountryCode: ''
    }
  });

  // Users management
  organizationUsers = signal<OrganizationUser[]>([]);
  isLoadingUsers = signal<boolean>(false);
  showCreateUserModal = signal<boolean>(false);
  isCreatingUser = signal<boolean>(false);
  
  // Create user form
  createUserForm = signal<CreateUserInput>({
    fullName: '',
    email: '',
    password: '',
    phoneNumber: '',
    userType: 'ORGANIZATIONAL',
    roles: ['User']
  });
  createUserErrors = signal<{ [key: string]: string }>({});

  // Validation
  errors = signal<{ [key: string]: string }>({});
  organizationErrors = signal<{ [key: string]: string }>({});

  ngOnInit() {
    this.loadProfile();
    this.loadOrganization();
    this.loadOrganizationUsers();
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
        
        // Avatar URLs are now permanent and served via API
        // Format: https://api.gixat.com/api/media/avatars/{userId}/{fileName}
        // No expiration, no special handling needed
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
      // Step 1: Get presigned upload URL from backend
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

      // Step 3: Reload profile to get new presigned GET URL (valid for 24 hours)
      // Backend automatically generates presigned URL when fetching profile
      this.loadProfile();

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

  onAvatarImageError() {
    // Avatar URLs are permanent, so if it fails to load, it might be:
    // 1. File was deleted
    // 2. Network issue
    // 3. Invalid URL
    console.warn('Avatar image failed to load. Showing fallback.');
    
    // Show initials as fallback
    this.avatarPreview.set(null);
  }

  // Organization methods
  loadOrganization() {
    this.profileService.getMyOrganization().pipe(
      catchError((err: Error) => {
        // Organization might not exist, that's okay
        console.warn('Failed to load organization:', err.message);
        return of(null);
      })
    ).subscribe((org) => {
      if (org) {
        this.organization.set(org);
        this.organizationFormData.set({
          name: org.name || '',
          address: {
            country: org.address?.country || '',
            city: org.address?.city || '',
            street: org.address?.street || '',
            phoneCountryCode: org.address?.phoneCountryCode || ''
          }
        });
        this.logoPreview.set(org.logo?.url || null);
      }
    });
  }

  validateOrganizationForm(): boolean {
    const errors: { [key: string]: string } = {};
    const data = this.organizationFormData();

    if (!data.name || data.name.trim().length === 0) {
      errors['name'] = 'Organization name is required';
    } else if (data.name.trim().length < 2) {
      errors['name'] = 'Organization name must be at least 2 characters';
    }

    if (data.address) {
      if (!data.address.country || data.address.country.trim().length === 0) {
        errors['country'] = 'Country is required';
      }
      if (!data.address.city || data.address.city.trim().length === 0) {
        errors['city'] = 'City is required';
      }
      if (!data.address.street || data.address.street.trim().length === 0) {
        errors['street'] = 'Street address is required';
      }
      if (!data.address.phoneCountryCode || data.address.phoneCountryCode.trim().length === 0) {
        errors['phoneCountryCode'] = 'Phone country code is required';
      }
    }

    this.organizationErrors.set(errors);
    return Object.keys(errors).length === 0;
  }

  onOrganizationFieldChange(field: 'name' | 'address', value: string | AddressInput) {
    if (field === 'name') {
      this.organizationFormData.update(data => ({
        ...data,
        name: value as string
      }));
    } else if (field === 'address') {
      this.organizationFormData.update(data => ({
        ...data,
        address: value as AddressInput
      }));
    }

    // Clear error for this field
    if (this.organizationErrors()[field]) {
      this.organizationErrors.update(errors => {
        const newErrors = { ...errors };
        delete newErrors[field];
        return newErrors;
      });
    }
  }

  onOrganizationAddressFieldChange(field: keyof AddressInput, value: string) {
    this.organizationFormData.update(data => ({
      ...data,
      address: {
        ...(data.address || { country: '', city: '', street: '', phoneCountryCode: '' }),
        [field]: value
      }
    }));

    // Clear error for this field
    if (this.organizationErrors()[field]) {
      this.organizationErrors.update(errors => {
        const newErrors = { ...errors };
        delete newErrors[field];
        return newErrors;
      });
    }
  }

  async saveOrganization() {
    if (!this.validateOrganizationForm()) {
      return;
    }

    this.isSavingOrganization.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    const data = this.organizationFormData();
    const updateData: UpdateOrganizationInput = {};

    // Only include fields that have changed
    const currentOrg = this.organization();
    if (currentOrg) {
      if (data.name?.trim() !== currentOrg.name) {
        updateData.name = data.name?.trim();
      }

      if (data.address) {
        const addressChanged = 
          data.address.country?.trim() !== (currentOrg.address?.country || '') ||
          data.address.city?.trim() !== (currentOrg.address?.city || '') ||
          data.address.street?.trim() !== (currentOrg.address?.street || '') ||
          data.address.phoneCountryCode?.trim() !== (currentOrg.address?.phoneCountryCode || '');

        if (addressChanged) {
          updateData.address = {
            country: data.address.country?.trim() || '',
            city: data.address.city?.trim() || '',
            street: data.address.street?.trim() || '',
            phoneCountryCode: data.address.phoneCountryCode?.trim() || ''
          };
        }
      }
    } else {
      // New organization - include all fields
      updateData.name = data.name?.trim();
      if (data.address) {
        updateData.address = {
          country: data.address.country?.trim() || '',
          city: data.address.city?.trim() || '',
          street: data.address.street?.trim() || '',
          phoneCountryCode: data.address.phoneCountryCode?.trim() || ''
        };
      }
    }

    // If nothing changed, show message and return
    if (Object.keys(updateData).length === 0) {
      this.isSavingOrganization.set(false);
      this.successMessage.set('No changes to save');
      setTimeout(() => this.successMessage.set(null), 3000);
      return;
    }

    this.profileService.updateOrganization(updateData).pipe(
      catchError((err: Error) => {
        this.errorMessage.set(err instanceof Error ? err.message : 'Failed to update organization');
        this.isSavingOrganization.set(false);
        return of(null);
      })
    ).subscribe((updatedOrg) => {
      if (updatedOrg) {
        this.organization.set(updatedOrg);
        this.logoPreview.set(updatedOrg.logo?.url || null);
        this.successMessage.set('Organization updated successfully');
        setTimeout(() => this.successMessage.set(null), 3000);
      }
      this.isSavingOrganization.set(false);
    });
  }

  onLogoSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];

    if (!file) {
      return;
    }

    // Validate file type
    const allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/webp'];
    if (!allowedTypes.includes(file.type)) {
      this.errorMessage.set('Please select a valid image file (JPEG, PNG, or WebP)');
      return;
    }

    // Validate file size (max 5MB)
    const maxSize = 5 * 1024 * 1024; // 5MB
    if (file.size > maxSize) {
      this.errorMessage.set('Image size must be less than 5MB');
      return;
    }

    this.selectedLogoFile = file;
    this.errorMessage.set(null);

    // Create preview
    const reader = new FileReader();
    reader.onload = (e) => {
      this.logoPreview.set(e.target?.result as string);
    };
    reader.readAsDataURL(file);
  }

  async uploadLogo() {
    if (!this.selectedLogoFile) {
      return;
    }

    this.isUploadingLogo.set(true);
    this.errorMessage.set(null);

    this.profileService.uploadOrganizationLogo(this.selectedLogoFile).pipe(
      catchError((err: Error) => {
        this.errorMessage.set(err instanceof Error ? err.message : 'Failed to upload logo');
        this.isUploadingLogo.set(false);
        return of(null);
      })
    ).subscribe((result) => {
      if (result) {
        this.logoPreview.set(result.logoUrl);
        this.selectedLogoFile = null;
        this.loadOrganization(); // Reload to get updated organization data
        this.successMessage.set('Logo uploaded successfully');
        setTimeout(() => this.successMessage.set(null), 3000);

        // Reset file input
        const fileInput = document.getElementById('logo-input') as HTMLInputElement;
        if (fileInput) {
          fileInput.value = '';
        }
      }
      this.isUploadingLogo.set(false);
    });
  }

  async deleteLogo() {
    if (!confirm('Are you sure you want to delete the organization logo?')) {
      return;
    }

    this.isUploadingLogo.set(true);
    this.errorMessage.set(null);

    this.profileService.deleteOrganizationLogo().pipe(
      catchError((err: Error) => {
        this.errorMessage.set(err instanceof Error ? err.message : 'Failed to delete logo');
        this.isUploadingLogo.set(false);
        return of(false);
      })
    ).subscribe((success) => {
      if (success) {
        this.logoPreview.set(null);
        this.selectedLogoFile = null;
        this.loadOrganization();
        this.successMessage.set('Logo deleted successfully');
        setTimeout(() => this.successMessage.set(null), 3000);
      }
      this.isUploadingLogo.set(false);
    });
  }

  onLogoImageError() {
    console.warn('Logo image failed to load.');
    this.logoPreview.set(null);
  }

  // User management methods
  loadOrganizationUsers() {
    this.isLoadingUsers.set(true);
    this.profileService.getOrganizationUsers().pipe(
      catchError((err: Error) => {
        console.warn('Failed to load organization users:', err.message);
        this.isLoadingUsers.set(false);
        return of([]);
      })
    ).subscribe((users) => {
      this.organizationUsers.set(users);
      this.isLoadingUsers.set(false);
    });
  }

  openCreateUserModal() {
    this.showCreateUserModal.set(true);
    this.createUserForm.set({
      fullName: '',
      email: '',
      password: '',
      phoneNumber: '',
      userType: 'ORGANIZATIONAL',
      roles: ['User']
    });
    this.createUserErrors.set({});
  }

  closeCreateUserModal() {
    this.showCreateUserModal.set(false);
    this.createUserForm.set({
      fullName: '',
      email: '',
      password: '',
      phoneNumber: '',
      userType: 'ORGANIZATIONAL',
      roles: ['User']
    });
    this.createUserErrors.set({});
  }

  validateCreateUserForm(): boolean {
    const errors: { [key: string]: string } = {};
    const data = this.createUserForm();

    if (!data.fullName || data.fullName.trim().length === 0) {
      errors['fullName'] = 'Full name is required';
    }

    if (!data.email || data.email.trim().length === 0) {
      errors['email'] = 'Email is required';
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(data.email)) {
      errors['email'] = 'Please enter a valid email address';
    }

    if (!data.password || data.password.length < 8) {
      errors['password'] = 'Password must be at least 8 characters';
    }

    this.createUserErrors.set(errors);
    return Object.keys(errors).length === 0;
  }

  onCreateUserFieldChange(field: keyof CreateUserInput, value: string | string[]) {
    this.createUserForm.update(data => ({
      ...data,
      [field]: value
    }));

    if (this.createUserErrors()[field]) {
      this.createUserErrors.update(errors => {
        const newErrors = { ...errors };
        delete newErrors[field];
        return newErrors;
      });
    }
  }

  async createUser() {
    if (!this.validateCreateUserForm()) {
      return;
    }

    this.isCreatingUser.set(true);
    this.errorMessage.set(null);

    const data = this.createUserForm();
    const input: CreateUserInput = {
      fullName: data.fullName.trim(),
      email: data.email.trim(),
      password: data.password,
      phoneNumber: data.phoneNumber?.trim() || undefined,
      userType: data.userType || 'ORGANIZATIONAL',
      roles: data.roles || ['User']
    };

    this.profileService.createUser(input).pipe(
      catchError((err: Error) => {
        this.errorMessage.set(err instanceof Error ? err.message : 'Failed to create user');
        this.isCreatingUser.set(false);
        return of(null);
      })
    ).subscribe((user) => {
      if (user) {
        this.successMessage.set('User created successfully');
        setTimeout(() => this.successMessage.set(null), 3000);
        this.closeCreateUserModal();
        this.loadOrganizationUsers(); // Reload users list
      }
      this.isCreatingUser.set(false);
    });
  }

  editUser(userId: string) {
    this.router.navigate(['/dashboard/organization', userId]);
  }

  getUserInitials(user: OrganizationUser): string {
    if (!user.fullName) {
      return '?';
    }
    const names = user.fullName.trim().split(' ');
    if (names.length >= 2) {
      return (names[0][0] + names[names.length - 1][0]).toUpperCase();
    }
    return names[0][0].toUpperCase();
  }
}

