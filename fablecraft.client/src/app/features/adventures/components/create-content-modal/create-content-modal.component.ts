import {Component, EventEmitter, Input, OnChanges, OnDestroy, Output, SimpleChanges} from '@angular/core';
import {AdventureService} from '../../services/adventure.service';
import {ToastService} from '../../../../core/services/toast.service';
import {
  CHARACTER_IMPORTANCE_OPTIONS,
  ITEM_POWER_LEVEL_OPTIONS,
  LOCATION_IMPORTANCE_OPTIONS,
  LORE_CATEGORY_OPTIONS,
  ManualContentType,
  ManualCreateContentRequest,
  ManualCreateContentResult
} from '../../models/manual-content.model';

@Component({
  selector: 'app-create-content-modal',
  standalone: false,
  templateUrl: './create-content-modal.component.html',
  styleUrl: './create-content-modal.component.css'
})
export class CreateContentModalComponent implements OnChanges, OnDestroy {
  @Input() isOpen = false;
  @Input() adventureId: string | null = null;
  @Input() sceneId: string | null = null;
  @Output() close = new EventEmitter<void>();
  @Output() created = new EventEmitter<ManualCreateContentResult>();

  readonly types: { value: ManualContentType; label: string }[] = [
    {value: 'character', label: 'Character'},
    {value: 'location', label: 'Location'},
    {value: 'item', label: 'Item'},
    {value: 'lore', label: 'Lore'}
  ];

  readonly characterImportanceOptions = CHARACTER_IMPORTANCE_OPTIONS;
  readonly locationImportanceOptions = LOCATION_IMPORTANCE_OPTIONS;
  readonly itemPowerLevelOptions = ITEM_POWER_LEVEL_OPTIONS;
  readonly loreCategoryOptions = LORE_CATEGORY_OPTIONS;

  type: ManualContentType = 'character';
  name = '';
  details = '';
  characterImportance = 'significant';
  locationImportance = 'standard';
  powerLevel = 'mundane';
  category = 'historical';

  isSaving = false;

  get isValid(): boolean {
    return this.name.trim() !== '' && this.details.trim() !== '';
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isOpen']) {
      document.body.style.overflow = this.isOpen ? 'hidden' : '';
      if (this.isOpen) {
        this.reset();
      }
    }
  }

  ngOnDestroy(): void {
    document.body.style.overflow = '';
  }

  onClose(): void {
    this.close.emit();
  }

  onBackdropClick(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.onClose();
    }
  }

  submit(): void {
    if (!this.adventureId || !this.sceneId || !this.isValid || this.isSaving) {
      return;
    }

    const body: ManualCreateContentRequest = {
      type: this.type,
      name: this.name.trim(),
      details: this.details.trim(),
      importance: this.type === 'character'
        ? this.characterImportance
        : this.type === 'location'
          ? this.locationImportance
          : null,
      powerLevel: this.type === 'item' ? this.powerLevel : null,
      category: this.type === 'lore' ? this.category : null
    };

    this.isSaving = true;
    this.adventureService.createContent(this.adventureId, this.sceneId, body).subscribe({
      next: (result) => {
        this.isSaving = false;
        this.toastService.success(`Created ${result.kind.toLowerCase()} "${result.name}"`);
        this.created.emit(result);
        this.onClose();
      },
      error: (err) => {
        console.error('Error creating content:', err);
        this.isSaving = false;
        this.toastService.error(err?.error?.error || 'Failed to create content');
      }
    });
  }

  constructor(
    private adventureService: AdventureService,
    private toastService: ToastService
  ) {
  }

  private reset(): void {
    this.type = 'character';
    this.name = '';
    this.details = '';
    this.characterImportance = 'significant';
    this.locationImportance = 'standard';
    this.powerLevel = 'mundane';
    this.category = 'historical';
    this.isSaving = false;
  }
}
