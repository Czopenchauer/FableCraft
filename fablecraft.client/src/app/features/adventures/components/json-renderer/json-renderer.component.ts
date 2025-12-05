import { Component, Input, OnInit } from '@angular/core';
import { ValueType, RenderNode, ConstantFieldConfig, JsonValue } from './json-renderer.types';

@Component({
  selector: 'app-json-renderer',
  standalone: false,
  templateUrl: './json-renderer.component.html',
  styleUrl: './json-renderer.component.css'
})
export class JsonRendererComponent implements OnInit {
  @Input() data: any;
  @Input() level: number = 0;
  @Input() parentKey: string = '';
  @Input() constantFields?: ConstantFieldConfig;

  // Internal state
  renderedNodes: RenderNode[] = [];
  expandedKeys: Set<string> = new Set<string>();

  // Expose ValueType enum to template
  ValueType = ValueType;

  // Default constant fields configuration
  private defaultConstantFields: ConstantFieldConfig = {
    paths: [
      'story.time',
      'story.location',
      'story.weather',
      'charactersPresent',
      'mainCharacter.name',
      'characters.*.name'
    ],
    patterns: [/^name$/i]
  };

  ngOnInit(): void {
    this.processData();
    this.autoExpandConstantFields();
  }

  /**
   * Process input data into renderable nodes
   */
  processData(): void {
    if (this.isObject(this.data)) {
      this.renderedNodes = Object.entries(this.data).map(([key, value]) =>
        this.createRenderNode(key, value)
      );
    } else if (this.isArray(this.data)) {
      this.renderedNodes = (this.data as any[]).map((value, index) =>
        this.createRenderNode(`[${index}]`, value)
      );
    }
  }

  /**
   * Create a render node with metadata
   */
  createRenderNode(key: string, value: any): RenderNode {
    const fullPath = this.buildPath(key);
    return {
      key,
      value,
      type: this.detectValueType(value),
      isConstant: this.isConstantField(fullPath),
      level: this.level
    };
  }

  /**
   * Detect the type of a value
   */
  detectValueType(value: any): ValueType {
    if (value === null) return ValueType.NULL;
    if (value === undefined) return ValueType.UNDEFINED;

    const type = typeof value;
    if (type === 'string') return ValueType.STRING;
    if (type === 'number') return ValueType.NUMBER;
    if (type === 'boolean') return ValueType.BOOLEAN;

    if (Array.isArray(value)) {
      return this.detectArrayType(value);
    }

    if (type === 'object') return ValueType.OBJECT;

    return ValueType.STRING; // Fallback
  }

  /**
   * Detect array type (primitives vs objects)
   */
  detectArrayType(array: any[]): ValueType {
    if (array.length === 0) return ValueType.ARRAY;

    const firstElement = array[0];
    const isPrimitive = typeof firstElement !== 'object' || firstElement === null;

    return isPrimitive ? ValueType.ARRAY_OF_PRIMITIVES : ValueType.ARRAY_OF_OBJECTS;
  }

  /**
   * Check if a field is constant
   */
  isConstantField(path: string): boolean {
    const config = this.constantFields || this.defaultConstantFields;

    // Check exact paths
    if (config.paths.some(p => this.matchPath(path, p))) {
      return true;
    }

    // Check patterns
    const key = path.split('.').pop() || '';
    return config.patterns.some(pattern => pattern.test(key));
  }

  /**
   * Match path with wildcard support
   */
  matchPath(actualPath: string, configPath: string): boolean {
    const actualParts = actualPath.split('.');
    const configParts = configPath.split('.');

    if (actualParts.length !== configParts.length) {
      return false;
    }

    return configParts.every((part, i) =>
      part === '*' || part === actualParts[i]
    );
  }

  /**
   * Build full dot-notation path
   */
  buildPath(key: string): string {
    // Remove array brackets for path building
    const cleanKey = key.replace(/^\[|\]$/g, '');
    return this.parentKey ? `${this.parentKey}.${cleanKey}` : cleanKey;
  }

  /**
   * Toggle panel expansion
   */
  toggleExpanded(key: string): void {
    if (this.expandedKeys.has(key)) {
      this.expandedKeys.delete(key);
    } else {
      this.expandedKeys.add(key);
    }
  }

  /**
   * Check if panel is expanded
   */
  isExpanded(key: string): boolean {
    return this.expandedKeys.has(key);
  }

  /**
   * Auto-expand panels containing constant fields
   */
  autoExpandConstantFields(): void {
    this.renderedNodes.forEach(node => {
      if (this.shouldAutoExpand(node)) {
        this.expandedKeys.add(node.key);
      }
    });
  }

  /**
   * Determine if a node should be auto-expanded
   */
  shouldAutoExpand(node: RenderNode): boolean {
    // Always expand if this node is constant
    if (node.isConstant) return true;

    // Expand objects/arrays that contain constant children
    if (node.type === ValueType.OBJECT ||
        node.type === ValueType.ARRAY_OF_OBJECTS) {
      return this.hasConstantChildren(node.value, this.buildPath(node.key));
    }

    return false;
  }

  /**
   * Check if a value has constant children
   */
  hasConstantChildren(value: any, basePath: string): boolean {
    if (typeof value !== 'object' || value === null) return false;

    const entries = Array.isArray(value)
      ? value.map((v, i) => [`[${i}]`, v])
      : Object.entries(value);

    return entries.some(([key, val]) => {
      const childPath = `${basePath}.${key.replace(/^\[|\]$/g, '')}`;
      return this.isConstantField(childPath) || this.hasConstantChildren(val, childPath);
    });
  }

  /**
   * Helper: Check if value is an object
   */
  isObject(value: any): boolean {
    return typeof value === 'object' && value !== null && !Array.isArray(value);
  }

  /**
   * Helper: Check if value is an array
   */
  isArray(value: any): boolean {
    return Array.isArray(value);
  }

  /**
   * TrackBy function for ngFor performance
   */
  trackByKey(index: number, node: RenderNode): string {
    return node.key;
  }

  /**
   * Format key for display
   */
  formatLabel(key: string): string {
    // Remove array brackets
    const cleanKey = key.replace(/^\[|\]$/g, '');

    // Custom label mappings
    const labelMap: { [key: string]: string } = {
      'story': 'Story',
      'charactersPresent': 'Characters on Scene',
      'mainCharacter': 'Protagonist',
      'characters': 'Characters'
    };

    return labelMap[cleanKey] || cleanKey;
  }

  /**
   * Handle keyboard events for accessibility
   */
  onKeyDown(event: KeyboardEvent, key: string): void {
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      this.toggleExpanded(key);
    }
  }
}
