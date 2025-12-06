import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TrackerDefinitionService } from '../../services/tracker-definition.service';
import {
  TrackerDefinitionDto,
  TrackerDefinitionResponseDto,
  TrackerStructure,
  FieldDefinition,
  FieldType
} from '../../models/tracker-definition.model';
import { JsonRendererComponent } from '../json-renderer/json-renderer.component';
import { FieldEditorComponent } from '../field-editor/field-editor.component';

@Component({
  selector: 'app-tracker-definition-builder',
  standalone: true,
  imports: [CommonModule, FormsModule, JsonRendererComponent, FieldEditorComponent],
  templateUrl: './tracker-definition-builder.component.html',
  styleUrls: ['./tracker-definition-builder.component.css']
})
export class TrackerDefinitionBuilderComponent implements OnInit {
  definitionId: string | null = null;
  definitionName = '';
  structure: TrackerStructure | null = null;
  isEditMode = false;
  isLoading = false;
  isSaving = false;
  error: string | null = null;

  // Live preview
  visualizationData: any = null;
  isLoadingVisualization = false;

  // Framework field names that are locked
  private readonly frameworkFields = {
    story: ['Time', 'Weather', 'Location'],
    mainCharacter: ['Name'],
    characters: ['Name'],
    charactersPresent: 'CharactersPresent'
  };

  FieldType = FieldType;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private trackerDefinitionService: TrackerDefinitionService
  ) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      this.definitionId = params.get('id');
      this.isEditMode = !!this.definitionId;

      if (this.isEditMode) {
        this.loadDefinition(this.definitionId!);
      } else {
        this.loadDefaultStructure();
      }
    });
  }

  loadDefaultStructure(): void {
    this.isLoading = true;
    this.trackerDefinitionService.getDefaultStructure().subscribe({
      next: (structure) => {
        this.structure = structure;
        this.isLoading = false;
      },
      error: (err) => {
        this.error = 'Failed to load default structure';
        console.error('Error loading default structure:', err);
        this.isLoading = false;
      }
    });
  }

  loadDefinition(id: string): void {
    this.isLoading = true;
    this.trackerDefinitionService.getDefinitionById(id).subscribe({
      next: (definition) => {
        this.definitionName = definition.name;
        this.structure = definition.structure;
        this.isLoading = false;
      },
      error: (err) => {
        this.error = 'Failed to load tracker definition';
        console.error('Error loading tracker definition:', err);
        this.isLoading = false;
      }
    });
  }

  onRefreshPreview(): void {
    if (!this.structure) return;

    this.isLoadingVisualization = true;
    this.trackerDefinitionService.visualizeTracker(this.structure).subscribe({
      next: (data) => {
        this.visualizationData = data;
        this.isLoadingVisualization = false;
      },
      error: (err) => {
        console.error('Error visualizing tracker:', err);
        this.isLoadingVisualization = false;
      }
    });
  }

  isFrameworkField(section: string, fieldName: string): boolean {
    switch (section) {
      case 'story':
        return this.frameworkFields.story.includes(fieldName);
      case 'mainCharacter':
        return this.frameworkFields.mainCharacter.includes(fieldName);
      case 'characters':
        return this.frameworkFields.characters.includes(fieldName);
      case 'charactersPresent':
        return fieldName === this.frameworkFields.charactersPresent;
      default:
        return false;
    }
  }

  addCustomField(section: keyof TrackerStructure): void {
    if (!this.structure) return;

    const newField: FieldDefinition = {
      name: 'NewField',
      type: FieldType.String,
      prompt: 'Enter prompt for this field',
      defaultValue: '',
      exampleValues: []
    };

    const sectionData = this.structure[section];
    if (Array.isArray(sectionData)) {
      sectionData.push(newField);
    }
  }

  removeField(section: keyof TrackerStructure, index: number): void {
    if (!this.structure) return;

    const sectionData = this.structure[section];
    if (Array.isArray(sectionData)) {
      const field = sectionData[index];

      // Prevent deletion of framework fields
      if (this.isFrameworkField(section, field.name)) {
        alert('Cannot delete framework fields');
        return;
      }

      sectionData.splice(index, 1);
    }
  }

  addExampleValue(field: FieldDefinition): void {
    if (!field.exampleValues) {
      field.exampleValues = [];
    }
    field.exampleValues.push('');
  }

  removeExampleValue(field: FieldDefinition, index: number): void {
    if (field.exampleValues) {
      field.exampleValues.splice(index, 1);
    }
  }

  addNestedField(field: FieldDefinition): void {
    if (!field.nestedFields) {
      field.nestedFields = [];
    }

    const newNestedField: FieldDefinition = {
      name: 'NestedField',
      type: FieldType.String,
      prompt: 'Enter prompt for nested field',
      defaultValue: '',
      exampleValues: []
    };

    field.nestedFields.push(newNestedField);
  }

  removeNestedField(field: FieldDefinition, index: number): void {
    if (field.nestedFields) {
      field.nestedFields.splice(index, 1);
    }
  }

  enableCharacterDevelopment(): void {
    if (this.structure && !this.structure.characterDevelopment) {
      this.structure.characterDevelopment = [];
    }
  }

  disableCharacterDevelopment(): void {
    if (this.structure) {
      this.structure.characterDevelopment = undefined;
    }
  }

  enableMainCharacterDevelopment(): void {
    if (this.structure && !this.structure.mainCharacterDevelopment) {
      this.structure.mainCharacterDevelopment = [];
    }
  }

  disableMainCharacterDevelopment(): void {
    if (this.structure) {
      this.structure.mainCharacterDevelopment = undefined;
    }
  }

  onSave(): void {
    if (!this.structure || !this.definitionName.trim()) {
      alert('Please provide a name for the tracker definition');
      return;
    }

    const dto: TrackerDefinitionDto = {
      name: this.definitionName,
      structure: this.structure
    };

    this.isSaving = true;
    this.error = null;

    if (this.isEditMode && this.definitionId) {
      // Update existing
      this.trackerDefinitionService.updateDefinition(this.definitionId, dto).subscribe({
        next: () => {
          this.isSaving = false;
          this.router.navigate(['/adventures/tracker-definitions']);
        },
        error: (err) => {
          this.isSaving = false;
          this.handleSaveError(err);
        }
      });
    } else {
      // Create new
      this.trackerDefinitionService.createDefinition(dto).subscribe({
        next: () => {
          this.isSaving = false;
          this.router.navigate(['/adventures/tracker-definitions']);
        },
        error: (err) => {
          this.isSaving = false;
          this.handleSaveError(err);
        }
      });
    }
  }

  private handleSaveError(err: any): void {
    if (err.status === 409) {
      this.error = 'A tracker definition with this name already exists';
    } else if (err.status === 400) {
      this.error = 'Validation failed. Please check that all required framework fields are present';
    } else {
      this.error = 'Failed to save tracker definition';
    }
    console.error('Error saving tracker definition:', err);
  }

  onCancel(): void {
    this.router.navigate(['/adventures/tracker-definitions']);
  }

  onDelete(): void {
    if (!this.definitionId) return;

    if (!confirm(`Are you sure you want to delete this tracker definition?`)) {
      return;
    }

    this.trackerDefinitionService.deleteDefinition(this.definitionId).subscribe({
      next: () => {
        this.router.navigate(['/adventures/tracker-definitions']);
      },
      error: (err) => {
        if (err.status === 409) {
          alert('Cannot delete this tracker definition because it is currently in use by one or more adventures.');
        } else {
          alert('Failed to delete tracker definition');
        }
        console.error('Error deleting tracker definition:', err);
      }
    });
  }

  trackByIndex(index: number): number {
    return index;
  }
}
