export interface Adventure {
  id: string;
  title: string;
  description: string;
  status: AdventureStatus;
  createdAt: Date;
  updatedAt: Date;
}

export interface AdventureDto {
  title: string;
  description: string;
  lorebooks?: Record<string, any>;
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
  lorebooks: Record<string, any>;
  category: string;
  additionalInstruction?: string;
}
