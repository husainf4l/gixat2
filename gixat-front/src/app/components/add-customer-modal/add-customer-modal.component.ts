import { Component, signal, output, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CustomerService, CreateCustomerInput } from '../../services/customer.service';
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
  selector: 'app-add-customer-modal',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './add-customer-modal.component.html',
})
export class AddCustomerModalComponent implements OnInit {
  private fb = inject(FormBuilder);
  private customerService = inject(CustomerService);
  private apollo = inject(Apollo);

  closeModal = output<void>();
  customerCreated = output<{ id: string; name: string }>();

  customerForm: FormGroup;
  isSubmitting = signal(false);
  errorMessage = signal<string | null>(null);

  countries = signal<any[]>([]);
  availableCities = signal<any[]>([]);
  selectedCountryMetadata = signal<any>(null);
  phoneNumberLength = signal<number | null>(null);

  constructor() {
    this.customerForm = this.fb.group({
      firstName: ['', [Validators.required, Validators.minLength(2)]],
      lastName: ['', [Validators.required, Validators.minLength(2)]],
      phoneNumber: ['', [Validators.required]],
      phoneCountryCode: ['', [Validators.required]],
      email: ['', [Validators.email]],
      country: [''],
      city: [''],
      street: [''],
    });
  }

  ngOnInit() {
    this.loadCountries();

    // Watch for country changes to update cities and phone code
    this.customerForm.get('country')?.valueChanges.subscribe(countryValue => {
      const selectedCountry = this.countries().find(c => c.country === countryValue);
      if (selectedCountry) {
        this.availableCities.set(selectedCountry.cities || []);
        
        // Parse metadata to get phone code and length
        if (selectedCountry.metadata) {
          try {
            const metadata = JSON.parse(selectedCountry.metadata);
            this.selectedCountryMetadata.set(metadata);
            
            if (metadata.phoneCode) {
              this.customerForm.patchValue({ phoneCountryCode: metadata.phoneCode }, { emitEvent: false });
            }
            
            // Set phone number length validation
            if (metadata.phoneLength) {
              this.phoneNumberLength.set(metadata.phoneLength);
              const phoneControl = this.customerForm.get('phoneNumber');
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
        this.customerForm.patchValue({ city: '', phoneNumber: '' }, { emitEvent: false });
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
        this.errorMessage.set('Failed to load countries. Please try again.');
      }
    });
  }

  onSubmit() {
    if (this.customerForm.invalid || this.isSubmitting()) {
      this.customerForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    // Combine phone country code and phone number
    const rawCountryCode = String(this.customerForm.value.phoneCountryCode ?? '').trim();
    const normalizedCountryCode = rawCountryCode
      ? (rawCountryCode.startsWith('+') ? rawCountryCode : `+${rawCountryCode.replace(/^\+/, '')}`)
      : '';
    const localNumber = String(this.customerForm.value.phoneNumber ?? '').replace(/\D/g, '');
    const fullPhone = `${normalizedCountryCode}${localNumber}`;

    const input: CreateCustomerInput = {
      firstName: this.customerForm.value.firstName,
      lastName: this.customerForm.value.lastName,
      phoneNumber: fullPhone,
      email: this.customerForm.value.email || null,
      country: this.customerForm.value.country || null,
      city: this.customerForm.value.city || null,
      street: this.customerForm.value.street || null,
    };

    this.customerService.createCustomer(input).subscribe({
      next: (customer) => {
        this.isSubmitting.set(false);
        this.customerCreated.emit({
          id: customer.id,
          name: `${customer.firstName} ${customer.lastName}`.trim()
        });
        this.close();
      },
      error: (err) => {
        this.isSubmitting.set(false);
        this.errorMessage.set(err instanceof Error ? err.message : 'Failed to create customer');
      },
    });
  }

  close() {
    this.closeModal.emit();
  }

  getFieldError(fieldName: string): string | null {
    const control = this.customerForm.get(fieldName);
    if (!control || !control.touched || !control.errors) {
      return null;
    }

    if (control.errors['required']) return 'This field is required';
    if (control.errors['minlength']) return `Minimum ${control.errors['minlength'].requiredLength} characters`;
    if (control.errors['email']) return 'Invalid email format';
    if (control.errors['pattern']) return 'Invalid phone number format';
    
    return 'Invalid value';
  }
}
