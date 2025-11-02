export interface Adventure {
  id: string;
  title: string;
  description: string;
  status: AdventureStatus;
  createdAt: Date;
  updatedAt: Date;
}

export interface AdventureListItemDto {
  adventureId: string;
  name: string;
  lastScenePreview?: string;
  created: string;
  lastPlayed?: string;
}

export interface CharacterDto {
  name: string;
  description: string;
  background: string;
}

export interface LorebookEntryDto {
  description: string;
  content: string;
  category: string;
}

export interface AdventureDto {
  adventureId: string;
  name: string;
  firstSceneDescription: string;
  authorNotes: string;
  character: CharacterDto;
  lorebook: LorebookEntryDto[];
}

export interface AvailableLorebookDto {
  category: string;
  description: string;
  priority: number;
}

export interface AdventureCreationStatus {
  adventureId: string;
  status: AdventureStatus;
  progress: number;
  message?: string;
  completedSteps: string[];
  currentStep?: string;
}

export enum AdventureStatus {
  Creating = 'Creating',
  Processing = 'Processing',
  Ready = 'Ready',
  Failed = 'Failed'
}

export interface GenerateLorebookDto {
  lorebooks: LorebookEntryDto[];
  category: string;
  additionalInstruction?: string;
}

export interface GeneratedLorebookDto {
  content: string;
}

export interface LorebookGenerationState {
  status: 'pending' | 'generating' | 'completed' | 'error';
  content?: string;
  error?: string;
}
