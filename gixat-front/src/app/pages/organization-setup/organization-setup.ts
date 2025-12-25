import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { Apollo, gql } from 'apollo-angular';

const GET_COUNTRIES_WITH_CITIES = gql`
  query GetCountriesWithCities {
    lookupItems(category: "Country") {
      id
      country: value
      metadata
      cities: children {
        id
        city: value
      }
    }
  }
`;

@Component({
  selector: 'app-organization-setup',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './organization-setup.html'
})
export class OrganizationSetupComponent implements OnInit {
  private router = inject(Router);
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private apollo = inject(Apollo);

  activeTab = signal<'create' | 'join'>('create');
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);
  
  countries = signal<any[]>([]);
  availableCities = signal<any[]>([]);
  selectedCountryMetadata = signal<any>(null);
  phoneNumberLength = signal<number | null>(null);

  createForm: FormGroup = this.fb.group({
    name: ['', [Validators.required, Validators.minLength(3)]],
    country: ['', [Validators.required]],
    city: ['', [Validators.required]],
    street: ['', [Validators.required]],
    phoneCountryCode: ['', [Validators.required]],
    phoneNumber: ['', [Validators.required]]
  });

  ngOnInit() {
    this.loadCountries();
    
    // Watch for country changes to update cities and phone code
    this.createForm.get('country')?.valueChanges.subscribe(countryValue => {
      const selectedCountry = this.countries().find(c => c.country === countryValue);
      if (selectedCountry) {
        this.availableCities.set(selectedCountry.cities || []);
        
        // Parse metadata to get phone code and length
        if (selectedCountry.metadata) {
          try {
            const metadata = JSON.parse(selectedCountry.metadata);
            this.selectedCountryMetadata.set(metadata);
            
            if (metadata.phoneCode) {
              this.createForm.patchValue({ phoneCountryCode: metadata.phoneCode }, { emitEvent: false });
            }
            
            // Set phone number length validation
            if (metadata.phoneLength) {
              this.phoneNumberLength.set(metadata.phoneLength);
              const phoneControl = this.createForm.get('phoneNumber');
              phoneControl?.setValidators([
                Validators.required,
                Validators.minLength(metadata.phoneLength),
                Validators.maxLength(metadata.phoneLength),
                Validators.pattern(/^[0-9]+$/)
              ]);
              phoneControl?.updateValueAndValidity();
            }
          } catch (e) {
            console.error('Error parsing metadata:', e);
          }
        }
        
        // Reset city and phone when country changes
        this.createForm.patchValue({ city: '', phoneNumber: '' }, { emitEvent: false });
      } else {
        this.availableCities.set([]);
        this.phoneNumberLength.set(null);
      }
    });
  }

  loadCountries() {
    this.apollo.query<any>({
      query: GET_COUNTRIES_WITH_CITIES,
      fetchPolicy: 'network-only'
    }).subscribe({
      next: (result) => {
        this.countries.set(result.data.lookupItems || []);
      },
      error: (err) => {
        console.error('Error loading countries:', err);
        this.errorMessage.set('Failed to load countries. Please refresh the page.');
      }
    });
  }

  joinForm: FormGroup = this.fb.group({
    inviteCode: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(12)]]
  });

  setTab(tab: 'create' | 'join') {
    this.activeTab.set(tab);
  }

  onCreateSubmit() {
    if (this.createForm.valid) {
      this.isLoading.set(true);
      this.errorMessage.set(null);

      const rawCountryCode = String(this.createForm.value.phoneCountryCode ?? '').trim();
      const normalizedCountryCode = rawCountryCode
        ? (rawCountryCode.startsWith('+') ? rawCountryCode : `+${rawCountryCode.replace(/^\+/, '')}`)
        : '';
      const localNumber = String(this.createForm.value.phoneNumber ?? '').replace(/\D/g, '');
      const fullPhone = `${normalizedCountryCode}${localNumber}`;
      
      const input = {
        name: this.createForm.value.name,
        address: {
          country: this.createForm.value.country,
          city: this.createForm.value.city,
          street: this.createForm.value.street,
          phoneCountryCode: fullPhone
        },
        logoUrl: null,  // Optional: Can be added later via file upload
        logoAlt: null   // Optional: Can be added later
      };
      
      this.authService.createOrganization(input).subscribe({
        next: (res) => {
          this.isLoading.set(false);
          if (res.createOrganization) {
            this.router.navigate(['/dashboard']);
          }
        },
        error: (err) => {
          this.isLoading.set(false);
          this.errorMessage.set(err.message || 'Failed to create organization. Please try again.');
          console.error('Error creating organization:', err);
        }
      });
    }
  }

  onJoinSubmit() {
    if (this.joinForm.valid) {
      this.isLoading.set(true);
      this.errorMessage.set(null);
      
      const inviteCode = this.joinForm.value.inviteCode.toUpperCase().trim();
      
      this.authService.joinOrganization(inviteCode).subscribe({
        next: (invite) => {
          this.isLoading.set(false);
          if (invite) {
            // Successfully validated invite - redirect to dashboard
            this.router.navigate(['/dashboard']);
          }
        },
        error: (err) => {
          this.isLoading.set(false);
          this.errorMessage.set(err.message || 'Invalid or expired invitation code.');
          console.error('Error joining organization:', err);
        }
      });
    }
  }
}
