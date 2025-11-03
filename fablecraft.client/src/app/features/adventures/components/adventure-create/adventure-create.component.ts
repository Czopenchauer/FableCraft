import { Component, OnInit, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, Validators, FormArray } from '@angular/forms';
import { Router } from '@angular/router';
import { AdventureService } from '../../services/adventure.service';
import { AvailableLorebookDto, AdventureDto, LorebookGenerationState, GenerateLorebookDto } from '../../models/adventure.model';
import { Subject, takeUntil } from 'rxjs';

@Component({
  selector: 'app-adventure-create',
  standalone: false,
  templateUrl: './adventure-create.component.html',
  styleUrl: './adventure-create.component.css'
})
export class AdventureCreateComponent implements OnInit, OnDestroy {
  adventureForm!: FormGroup;
  availableLorebooks: AvailableLorebookDto[] = [];
  isLoading = false;
  isSubmitting = false;
  errorMessage = '';

  currentStep = 0;
  totalSteps = 3;

  // AI Generation state
  customInstruction = '';
  lorebookGenerationStates: Map<number, LorebookGenerationState> = new Map();
  isGenerating = false;
  private stopGeneration$ = new Subject<void>();
  private destroy$ = new Subject<void>();

  constructor(
    private fb: FormBuilder,
    private adventureService: AdventureService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.initializeForm();
    this.loadAvailableLorebooks();
  }

  ngOnDestroy(): void {
    // Complete all subjects to prevent memory leaks
    this.destroy$.next();
    this.destroy$.complete();

    // Stop any ongoing generation
    if (this.isGenerating) {
      this.stopGeneration$.next();
      this.stopGeneration$.complete();
    }
  }

  startAdventureCreation(): void {
    // Validate adventure name before proceeding
    const nameControl = this.adventureForm.get('name');
    nameControl?.markAsTouched();

    if (nameControl?.valid) {
      this.currentStep = 1;
      this.errorMessage = '';
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  }

  private initializeForm(): void {
    this.adventureForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3)]],
      firstSceneDescription: ['', Validators.required],
      authorNotes: ['', Validators.required],
      character: this.fb.group({
        name: ['', [Validators.required, Validators.minLength(2)]],
        description: ['', Validators.required],
        background: ['', Validators.required]
      }),
      lorebook: this.fb.array([])
    });
  }

  private loadAvailableLorebooks(): void {
    this.isLoading = true;
    this.adventureService.getSupportedLorebooks()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (lorebooks) => {
          this.availableLorebooks = lorebooks.sort((a, b) => a.priority - b.priority);
          this.initializeLorebookEntries();
          this.isLoading = false;
        },
        error: (error) => {
          console.error('Error loading lorebooks:', error);
          this.errorMessage = 'Failed to load lorebook categories. Please try again.';
          this.isLoading = false;
        }
      });
  }

  private initializeLorebookEntries(): void {
    const lorebookArray = this.adventureForm.get('lorebook') as FormArray;
    this.availableLorebooks.forEach(lorebook => {
      lorebookArray.push(this.fb.group({
        category: [lorebook.category],
        description: [lorebook.description],
        content: ['']
      }));
    });
  }

  get lorebookEntries(): FormArray {
    return this.adventureForm.get('lorebook') as FormArray;
  }

  getLorebookAt(index: number): FormGroup {
    return this.lorebookEntries.at(index) as FormGroup;
  }

  nextStep(): void {
    if (!this.validateCurrentStep()) {
      return;
    }

    if (this.currentStep < this.totalSteps) {
      this.currentStep++;
      this.errorMessage = '';
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  }

  previousStep(): void {
    if (this.currentStep > 1) {
      this.currentStep--;
      this.errorMessage = '';
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  }

  validateCurrentStep(): boolean {
    switch (this.currentStep) {
      case 1: // World Building (lorebook - all optional)
        return true;

      case 2: // Character Creation
        const characterGroup = this.adventureForm.get('character') as FormGroup;
        this.markFormGroupTouched(characterGroup);
        return characterGroup?.valid || false;

      case 3: // Starting Scene
        const authorNotes = this.adventureForm.get('authorNotes');
        const sceneDesc = this.adventureForm.get('firstSceneDescription');
        authorNotes?.markAsTouched();
        sceneDesc?.markAsTouched();
        return (authorNotes?.valid && sceneDesc?.valid) || false;

      default:
        return true;
    }
  }

  onSubmit(): void {
    if (!this.validateCurrentStep()) {
      return;
    }

    if (this.adventureForm.invalid) {
      this.markFormGroupTouched(this.adventureForm);
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';

    const formValue = this.adventureForm.value;
    const adventureDto: AdventureDto = {
      adventureId: crypto.randomUUID(),
      name: formValue.name,
      firstSceneDescription: formValue.firstSceneDescription,
      authorNotes: formValue.authorNotes,
      character: formValue.character,
      lorebook: formValue.lorebook.filter((entry: any) => entry.content.trim() !== ''),
    };

    this.adventureService.createAdventure(adventureDto)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (status) => {
          // Navigate to the status screen to monitor progress
          this.router.navigate(['/adventures/status', status.adventureId]);
        },
        error: (error) => {
          console.error('Error creating adventure:', error);
          this.errorMessage = error.error?.message || 'Failed to create adventure. Please try again.';
          this.isSubmitting = false;
        }
      });
  }

  getStepTitle(): string {
    switch (this.currentStep) {
      case 1: return 'World Building';
      case 2: return 'Character Creation';
      case 3: return 'Starting Scene';
      default: return '';
    }
  }

  getStepDescription(): string {
    switch (this.currentStep) {
      case 1: return 'Add details about your world (optional)';
      case 2: return 'Create your protagonist';
      case 3: return 'Set the stage for your adventure';
      default: return '';
    }
  }

  private markFormGroupTouched(formGroup: FormGroup): void {
    Object.keys(formGroup.controls).forEach(key => {
      const control = formGroup.get(key);
      control?.markAsTouched();

      if (control instanceof FormGroup) {
        this.markFormGroupTouched(control);
      }
    });
  }

  isFieldInvalid(fieldName: string, parentGroup?: FormGroup): boolean {
    const group = parentGroup || this.adventureForm;
    const field = group.get(fieldName);
    return !!(field && field.invalid && field.touched);
  }

  getFieldError(fieldName: string, parentGroup?: FormGroup): string {
    const group = parentGroup || this.adventureForm;
    const field = group.get(fieldName);

    if (field?.errors) {
      if (field.errors['required']) return 'This field is required';
      if (field.errors['minlength']) {
        return `Minimum length is ${field.errors['minlength'].requiredLength} characters`;
      }
    }
    return '';
  }

  get characterGroup(): FormGroup {
    return this.adventureForm.get('character') as FormGroup;
  }

  generateSingleLorebook(index: number): void {
    const lorebookGroup = this.getLorebookAt(index);
    const category = lorebookGroup.get('category')?.value;
    const additionalInstruction = lorebookGroup.get('content')?.value || undefined;

    // Mark as generating
    this.lorebookGenerationStates.set(index, { status: 'generating' });
    this.errorMessage = '';

    // Collect other lorebooks that have content (excluding the current one)
    const existingLorebooks = this.lorebookEntries.controls
      .map((control, idx) => {
        const group = control as FormGroup;
        return {
          category: group.get('category')?.value || '',
          description: group.get('description')?.value || '',
          content: group.get('content')?.value || ''
        };
      })
      .filter((entry, idx) => idx !== index && entry.content.trim() !== '');

    const dto: GenerateLorebookDto = {
      lorebooks: existingLorebooks,
      category: category,
      additionalInstruction: additionalInstruction || undefined
    };

    this.adventureService.generateLorebook(dto)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          // Update the content
          lorebookGroup.get('content')?.setValue(response.content);
          this.lorebookGenerationStates.set(index, {
            status: 'completed',
            content: response.content
          });
        },
        error: (error) => {
          console.error(`Error generating lorebook for ${category}:`, error);
          this.lorebookGenerationStates.set(index, {
            status: 'error',
            error: error.error?.message || 'Generation failed'
          });
        }
      });
  }

  startLorebookGeneration(): void {
    if (this.isGenerating) {
      return;
    }

    this.isGenerating = true;
    this.errorMessage = '';

    // Complete previous stopGeneration$ if it exists and create a new one
    if (this.stopGeneration$) {
      this.stopGeneration$.next();
      this.stopGeneration$.complete();
    }
    this.stopGeneration$ = new Subject<void>();

    // Initialize all lorebooks as pending
    this.lorebookEntries.controls.forEach((_, index) => {
      this.lorebookGenerationStates.set(index, { status: 'pending' });
    });

    // Start sequential generation
    this.generateNextLorebook(0);
  }

  private generateNextLorebook(index: number): void {
    if (index >= this.lorebookEntries.length) {
      // All done
      this.isGenerating = false;
      return;
    }

    const lorebookGroup = this.getLorebookAt(index);
    const category = lorebookGroup.get('category')?.value;

    // Skip if already has content
    if (lorebookGroup.get('content')?.value?.trim()) {
      this.generateNextLorebook(index + 1);
      return;
    }

    // Mark as generating
    this.lorebookGenerationStates.set(index, { status: 'generating' });

    // Collect other lorebooks that have content
    const existingLorebooks = this.lorebookEntries.controls
      .map((control, idx) => {
        const group = control as FormGroup;
        return {
          category: group.get('category')?.value || '',
          description: group.get('description')?.value || '',
          content: group.get('content')?.value || ''
        };
      })
      .filter(entry => entry.content.trim() !== '');

    const dto: GenerateLorebookDto = {
      lorebooks: existingLorebooks,
      category: category,
      additionalInstruction: this.customInstruction || undefined
    };

    this.adventureService.generateLorebook(dto)
      .pipe(takeUntil(this.stopGeneration$), takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          // Update the content
          lorebookGroup.get('content')?.setValue(response.content);
          this.lorebookGenerationStates.set(index, {
            status: 'completed',
            content: response.content
          });

          // Generate next
          this.generateNextLorebook(index + 1);
        },
        error: (error) => {
          console.error(`Error generating lorebook for ${category}:`, error);
          this.lorebookGenerationStates.set(index, {
            status: 'error',
            error: error.error?.message || 'Generation failed'
          });

          // Stop generation on first failure
          this.isGenerating = false;
          this.errorMessage = `Failed to generate ${category}: ${error.error?.message || 'Generation failed'}`;

          // Clean up remaining lorebook states
          this.lorebookGenerationStates.forEach((state, idx) => {
            if (idx !== index && (state.status === 'pending' || state.status === 'generating')) {
              this.lorebookGenerationStates.delete(idx);
            }
          });
        }
      });
  }

  stopLorebookGeneration(): void {
    this.stopGeneration$.next();
    this.stopGeneration$.complete();
    this.isGenerating = false;

    // Mark all pending as completed (cancelled)
    this.lorebookGenerationStates.forEach((state, index) => {
      if (state.status === 'pending' || state.status === 'generating') {
        this.lorebookGenerationStates.set(index, { status: 'completed' });
      }
    });
  }

  getLorebookState(index: number): LorebookGenerationState | undefined {
    return this.lorebookGenerationStates.get(index);
  }

  isLorebookGenerating(index: number): boolean {
    const state = this.lorebookGenerationStates.get(index);
    return state?.status === 'generating' || state?.status === 'pending';
  }

  isGenerateAllDisabled(): boolean {
    return this.isGenerating || !this.customInstruction.trim();
  }
}
