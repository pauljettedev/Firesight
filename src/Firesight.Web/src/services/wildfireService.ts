export interface Wildfire {
  id: string
  externalId: string
  agency: string
  name: string | null
  latitude: number
  longitude: number
  startDate: string | null
  areaHectares: number | null
  status: string
  statusDateUtc: string | null
  lastSeenInFeedUtc: string
  isStale: boolean
}

export async function getWildfires(): Promise<Wildfire[]> {
  const response = await fetch('/api/wildfires')

  if (!response.ok) {
    throw new Error(`Wildfire request failed: ${response.status}`)
  }

  return response.json()
}
