export interface WorldbookDto {
  name: string;
}

export interface WorldbookResponseDto {
  id: string;
  name: string;
  lorebooks: LorebookResponseDto[];
}

export interface LorebookDto {
  worldbookId: string;
  title: string;
  content: string;
  category: string;
}

export interface LorebookResponseDto {
  id: string;
  worldbookId: string;
  title: string;
  content: string;
  category: string;
}
