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
}

export async function askFiresight(
  question: string,
): Promise<AskFiresightResult> {
  const response = await fetch('/api/ask/', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ question }),
  })

  if (!response.ok) {
    throw new Error(`Ask Firesight request failed: ${response.status}`)
  }

  return response.json()
}
