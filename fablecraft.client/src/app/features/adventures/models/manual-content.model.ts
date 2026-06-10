export type ManualContentType = 'character' | 'location' | 'item' | 'lore';

export interface ManualCreateContentRequest {
  type: ManualContentType;
  name: string;
  details: string;
  importance?: string | null;
  powerLevel?: string | null;
  category?: string | null;
  description?: string | null;
}

export interface ManualCreateContentResult {
  kind: string;
  id: string | null;
  name: string;
  summary: string;
}

export interface ManualContentDraftResult {
  kind: string;
  name: string;
  summary: string;
  rawJson: any;
}

export interface ManualContentConfirmRequest {
  type: ManualContentType;
  rawJson: any;
}

export const CHARACTER_IMPORTANCE_OPTIONS = ['arc_important', 'significant', 'background'];
export const LOCATION_IMPORTANCE_OPTIONS = ['landmark', 'significant', 'standard', 'minor'];
export const ITEM_POWER_LEVEL_OPTIONS = ['mundane', 'uncommon', 'rare', 'legendary'];
export const LORE_CATEGORY_OPTIONS = [
  'economic',
  'legal',
  'historical',
  'cultural',
  'metaphysical',
  'geographic',
  'factional',
  'biological'
];
