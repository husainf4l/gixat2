import { Component, signal, output, inject, input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CustomerService, CreateCarInput } from '../../services/customer.service';
import { Apollo, gql } from 'apollo-angular';

const GET_CAR_MAKES_WITH_MODELS = gql`
  query GetCarMakesWithModels {
    lookupItems(where: { category: { eq: "CarMake" } }) {
      id
      make: value
      children {
        id
        model: value
      }
    }
  }
`;

@Component({
  selector: 'app-add-car-modal',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './add-car-modal.component.html',
})
export class AddCarModalComponent implements OnInit {
  private fb = inject(FormBuilder);
  private customerService = inject(CustomerService);
  private apollo = inject(Apollo);

  customerId = input.required<string>();
  customerName = input<string>('');
  
  closeModal = output<void>();
  carCreated = output<void>();
  createSession = output<{ carId: string; customerId: string }>();

  carForm: FormGroup;
  isSubmitting = signal(false);
  errorMessage = signal<string | null>(null);
  createdCarId = signal<string | null>(null);

  carMakes = signal<any[]>([]);
  availableModels = signal<any[]>([]);
  filteredMakes = signal<any[]>([]);
  filteredModels = signal<any[]>([]);
  showMakeSuggestions = signal(false);
  showModelSuggestions = signal(false);

  constructor() {
    const currentYear = new Date().getFullYear();
    
    this.carForm = this.fb.group({
      make: ['', [Validators.required, Validators.minLength(2)]],
      model: ['', [Validators.required, Validators.minLength(1)]],
      year: [currentYear, [Validators.required, Validators.min(1900), Validators.max(currentYear + 1)]],
      licensePlate: ['', [Validators.required, Validators.minLength(2)]],
      vin: ['', [Validators.minLength(17), Validators.maxLength(17)]],
      color: [''],
    });
  }

  ngOnInit() {
    this.loadCarMakes();

    // Watch for make input changes to filter suggestions
    this.carForm.get('make')?.valueChanges.subscribe(value => {
      if (typeof value === 'string' && value.length > 0) {
        const filtered = this.carMakes().filter(m => 
          m.make.toLowerCase().includes(value.toLowerCase())
        ).slice(0, 5);
        this.filteredMakes.set(filtered);
        this.showMakeSuggestions.set(true);
        
        // Update available models when make matches
        const exactMatch = this.carMakes().find(m => 
          m.make.toLowerCase() === value.toLowerCase()
        );
        if (exactMatch) {
          this.availableModels.set(exactMatch.children || []);
        }
      } else {
        this.filteredMakes.set([]);
        this.showMakeSuggestions.set(false);
        this.availableModels.set([]);
      }
    });

    // Watch for model input changes to filter suggestions
    this.carForm.get('model')?.valueChanges.subscribe(value => {
      if (typeof value === 'string' && value.length > 0 && this.availableModels().length > 0) {
        const filtered = this.availableModels().filter(m => 
          m.model.toLowerCase().includes(value.toLowerCase())
        ).slice(0, 5);
        this.filteredModels.set(filtered);
        this.showModelSuggestions.set(true);
      } else {
        this.filteredModels.set([]);
        this.showModelSuggestions.set(false);
      }
    });
  }

  loadCarMakes() {
    this.apollo.query<any>({
      query: GET_CAR_MAKES_WITH_MODELS,
      fetchPolicy: 'network-only'
    }).subscribe({
      next: (result) => {
        this.carMakes.set(result.data.lookupItems || []);
      },
      error: (err) => {
        console.error('Error loading car makes:', err);
        this.errorMessage.set('Failed to load car makes. You can still enter manually.');
      }
    });
  }

  selectMake(make: string) {
    this.carForm.patchValue({ make, model: '' });
    this.showMakeSuggestions.set(false);
    const selectedMake = this.carMakes().find(m => m.make === make);
    if (selectedMake) {
      this.availableModels.set(selectedMake.children || []);
    }
  }

  selectModel(model: string) {
    this.carForm.patchValue({ model });
    this.showModelSuggestions.set(false);
  }

  hideMakeSuggestions() {
    setTimeout(() => this.showMakeSuggestions.set(false), 200);
  }

  hideModelSuggestions() {
    setTimeout(() => this.showModelSuggestions.set(false), 200);
  }

  onSubmit() {
    if (this.carForm.invalid || this.isSubmitting()) {
      this.carForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    const input: CreateCarInput = {
      customerId: this.customerId(),
      make: this.carForm.value.make,
      model: this.carForm.value.model,
      year: parseInt(this.carForm.value.year, 10),
      licensePlate: this.carForm.value.licensePlate,
      vin: this.carForm.value.vin || null,
      color: this.carForm.value.color || null,
    };

    this.customerService.createCar(input).subscribe({
      next: (car) => {
        this.isSubmitting.set(false);
        this.createdCarId.set(car.id);
        // Don't close immediately, show the action buttons
      },
      error: (err) => {
        this.isSubmitting.set(false);
        this.errorMessage.set(err instanceof Error ? err.message : 'Failed to create car');
      },
    });
  }

  onSaveOnly() {
    this.carCreated.emit();
    this.close();
  }

  onCreateSession() {
    if (this.createdCarId()) {
      this.createSession.emit({
        carId: this.createdCarId()!,
        customerId: this.customerId()
      });
      this.close();
    }
  }

  close() {
    this.closeModal.emit();
  }

  getFieldError(fieldName: string): string | null {
    const control = this.carForm.get(fieldName);
    if (!control || !control.touched || !control.errors) {
      return null;
    }

    if (control.errors['required']) return 'This field is required';
    if (control.errors['minlength']) return `Minimum ${control.errors['minlength'].requiredLength} characters`;
    if (control.errors['maxlength']) return `Maximum ${control.errors['maxlength'].requiredLength} characters`;
    if (control.errors['min']) return `Minimum year is ${control.errors['min'].min}`;
    if (control.errors['max']) return `Maximum year is ${control.errors['max'].max}`;
    
    return 'Invalid value';
  }
}
