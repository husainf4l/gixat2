import { Component, signal, output, inject, OnInit, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CustomerService, UpdateCustomerInput, Customer } from '../../services/customer.service';
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
  selector: 'app-edit-customer-modal',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './edit-customer-modal.component.html',
})
export class EditCustomerModalComponent implements OnInit {
  private fb = inject(FormBuilder);
  private customerService = inject(CustomerService);
  private apollo = inject(Apollo);

  customer = input.required<Customer>();
  closeModal = output<void>();
  customerUpdated = output<Customer>();

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
    this.populateForm();

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
            
            if (metadata.phoneCode && !this.customerForm.get('phoneCountryCode')?.value) {
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
      } else {
        this.availableCities.set([]);
        this.phoneNumberLength.set(null);
      }
    });
  }

  populateForm() {
    const customer = this.customer();
    this.customerForm.patchValue({
      firstName: customer.firstName || '',
      lastName: customer.lastName || '',
      email: customer.email || '',
      phoneNumber: customer.phoneNumber || '',
      country: customer.address?.country || '',
      city: customer.address?.city || '',
      street: customer.address?.street || '',
      phoneCountryCode: customer.address?.phoneCountryCode || '',
    });

    // Load cities for the selected country
    if (customer.address?.country) {
      const selectedCountry = this.countries().find(c => c.country === customer.address?.country);
      if (selectedCountry) {
        this.availableCities.set(selectedCountry.cities || []);
      }
    }
  }

  loadCountries() {
    this.apollo.query<{ lookupItems: any[] }>({
      query: GET_COUNTRIES_WITH_CITIES,
      fetchPolicy: 'cache-first',
    }).subscribe({
      next: (result) => {
        const data = result.data;
        if (data) {
          this.countries.set(data.lookupItems || []);
          // Populate form after countries are loaded
          this.populateForm();
        }
      },
      error: (err) => {
        console.error('Error loading countries:', err);
        this.errorMessage.set('Failed to load countries');
      }
    });
  }

  onSubmit() {
    if (this.customerForm.invalid) {
      this.customerForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    const formValue = this.customerForm.value;
    const updateInput: UpdateCustomerInput = {
      firstName: formValue.firstName.trim(),
      lastName: formValue.lastName.trim(),
      email: formValue.email?.trim() || undefined,
      phoneNumber: formValue.phoneNumber.trim(),
    };

    // Add address if provided
    if (formValue.country || formValue.city || formValue.street) {
      updateInput.address = {
        country: formValue.country?.trim() || undefined,
        city: formValue.city?.trim() || undefined,
        street: formValue.street?.trim() || undefined,
        phoneCountryCode: formValue.phoneCountryCode?.trim() || undefined,
      };
    }

    this.customerService.updateCustomer(this.customer().id, updateInput).subscribe({
      next: (updatedCustomer) => {
        this.customerUpdated.emit(updatedCustomer);
        this.closeModal.emit();
      },
      error: (err) => {
        console.error('Error updating customer:', err);
        this.errorMessage.set(err instanceof Error ? err.message : 'Failed to update customer');
        this.isSubmitting.set(false);
      }
    });
  }

  onClose() {
    this.closeModal.emit();
  }
}

