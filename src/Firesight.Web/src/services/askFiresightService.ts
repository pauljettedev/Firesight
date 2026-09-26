import { requestJson } from './http'

export interface AskFiresightMapContext {
  latitude: number
  longitude: number
  radiusKm: number
  label: string | null
}

export interface AskFiresightResult {
  answer: string
  toolsUsed: string[]
  mapContext: AskFiresightMapContext | null
  // CWFIS IDs (Wildfire.externalId) of the fires the answer mentions.
  wildfireExternalIds: string[]
}

export function askFiresight(
  question: string,
): Promise<AskFiresightResult> {
  return requestJson('/api/ask/', 'Ask Firesight request failed', {
    init: {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ question }),
    },
  })
}
