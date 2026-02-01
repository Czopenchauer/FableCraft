export interface AdventureSettingsResponseDto {
  adventureId: string;
  name: string;
  promptPath: string;
  agentLlmPresets: AgentLlmPresetDto[];
  graphRagSettingsId?: string | null;
  graphRagSettingsName?: string | null;
}

export interface AgentLlmPresetDto {
  id?: string | null;
  agentName: string;
  llmPresetId?: string | null;
  llmPresetName?: string | null;
}

export interface UpdateAdventureSettingsDto {
  promptPath: string;
  agentLlmPresets: UpdateAgentLlmPresetDto[];
  graphRagSettingsId?: string | null;
}

export interface UpdateAgentLlmPresetDto {
  agentName: string;
  llmPresetId?: string | null;
}
