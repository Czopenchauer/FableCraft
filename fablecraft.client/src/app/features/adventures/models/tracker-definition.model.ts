export enum FieldType {
  String = 'String',
  Array = 'Array',
  Object = 'Object',
  ForEachObject = 'ForEachObject'
}

export interface FieldDefinition {
  name: string;
  type: FieldType;
  prompt: string;
  defaultValue?: any;
  exampleValues?: any[];
  nestedFields?: FieldDefinition[];
}

export interface TrackerStructure {
  story: FieldDefinition[];
  charactersPresent: FieldDefinition;
  mainCharacter: FieldDefinition[];
  characters: FieldDefinition[];
  characterDevelopment?: FieldDefinition[];
  mainCharacterDevelopment?: FieldDefinition[];
}

export interface TrackerDefinitionDto {
  name: string;
  structure: TrackerStructure;
}

export interface TrackerDefinitionResponseDto {
  id: string;
  name: string;
  structure: TrackerStructure;
}
