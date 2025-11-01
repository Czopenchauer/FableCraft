import { Component, OnInit } from '@angular/core';
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
export class AdventureCreateComponent implements OnInit {
  adventureForm!: FormGroup;
  availableLorebooks: AvailableLorebookDto[] = [];
  isLoading = false;
  isSubmitting = false;
  errorMessage = '';

  currentStep = 0;
  totalSteps = 4;

  // AI Generation state
  customInstruction = '';
  lorebookGenerationStates: Map<number, LorebookGenerationState> = new Map();
  isGenerating = false;
  private stopGeneration$ = new Subject<void>();

  constructor(
    private fb: FormBuilder,
    private adventureService: AdventureService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.initializeForm();
    this.loadAvailableLorebooks();
  }

  startAdventureCreation(): void {
    this.currentStep = 1;
  }

  private initializeForm(): void {
    this.adventureForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3)]],
      worldDescription: ['', Validators.required],
      firstSceneDescription: ['', Validators.required],
      authorNotes: [''],
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
    this.adventureService.getSupportedLorebooks().subscribe({
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
      case 1: // Adventure Settings
        const nameControl = this.adventureForm.get('name');
        nameControl?.markAsTouched();
        return nameControl?.valid || false;

      case 2: // World Building (lorebook - all optional)
        return true;

      case 3: // Character Creation
        const characterGroup = this.adventureForm.get('character') as FormGroup;
        this.markFormGroupTouched(characterGroup);
        return characterGroup?.valid || false;

      case 4: // Starting Scene
        const worldDesc = this.adventureForm.get('worldDescription');
        const sceneDesc = this.adventureForm.get('firstSceneDescription');
        worldDesc?.markAsTouched();
        sceneDesc?.markAsTouched();
        return (worldDesc?.valid && sceneDesc?.valid) || false;

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
      worldDescription: formValue.worldDescription,
      firstSceneDescription: formValue.firstSceneDescription,
      authorNotes: formValue.authorNotes || '',
      character: formValue.character,
      lorebook: formValue.lorebook.filter((entry: any) => entry.content.trim() !== '')
    };

    this.adventureService.createAdventure(adventureDto).subscribe({
      next: (status) => {
        this.router.navigate(['/']);
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
      case 1: return 'Adventure Settings';
      case 2: return 'World Building';
      case 3: return 'Character Creation';
      case 4: return 'Starting Scene';
      default: return '';
    }
  }

  getStepDescription(): string {
    switch (this.currentStep) {
      case 1: return 'Define the name and style of your adventure';
      case 2: return 'Add details about your world (optional)';
      case 3: return 'Create your protagonist';
      case 4: return 'Set the stage for your adventure';
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
      .subscribe({
        next: (response) => {
          // Update the content
          lorebookGroup.get('content')?.setValue(response.content || response);
          this.lorebookGenerationStates.set(index, {
            status: 'completed',
            content: response.content || response
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
      .pipe(takeUntil(this.stopGeneration$))
      .subscribe({
        next: (response) => {
          // Update the content
          lorebookGroup.get('content')?.setValue(response.content || response);
          this.lorebookGenerationStates.set(index, {
            status: 'completed',
            content: response.content || response
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

          // Continue with next lorebook even if this one failed
          this.generateNextLorebook(index + 1);
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
}
