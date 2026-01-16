export interface LlmLogResponseDto {
  id: string;
  adventureId: string | null;
  sceneId: string | null;
  callerName: string | null;
  requestContent: string;
  responseContent: string;
  receivedAt: string;
  inputToken: number | null;
  outputToken: number | null;
  totalToken: number | null;
  duration: number;
}

export interface LlmLogListResponseDto {
  items: LlmLogResponseDto[];
  totalCount: number;
}
