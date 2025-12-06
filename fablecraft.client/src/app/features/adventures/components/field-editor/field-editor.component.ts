import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { FieldDefinition, FieldType } from '../../models/tracker-definition.model';

@Component({
  selector: 'app-field-editor',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './field-editor.component.html',
  styleUrls: ['./field-editor.component.css']
})
export class FieldEditorComponent {
  @Input() field!: FieldDefinition;
  @Input() isFrameworkField: boolean = false;
  @Input() nestingLevel: number = 0;

  FieldType = FieldType;

  addExampleValue(): void {
    if (!this.field.exampleValues) {
      this.field.exampleValues = [];
    }
    this.field.exampleValues.push('');
  }

  removeExampleValue(index: number): void {
    if (this.field.exampleValues) {
      this.field.exampleValues.splice(index, 1);
    }
  }

  addNestedField(): void {
    if (!this.field.nestedFields) {
      this.field.nestedFields = [];
    }

    const newNestedField: FieldDefinition = {
      name: 'NestedField',
      type: FieldType.String,
      prompt: 'Enter prompt for nested field',
      defaultValue: '',
      exampleValues: []
    };

    this.field.nestedFields.push(newNestedField);
  }

  removeNestedField(index: number): void {
    if (this.field.nestedFields) {
      this.field.nestedFields.splice(index, 1);
    }
  }

  trackByIndex(index: number): number {
    return index;
  }
}
