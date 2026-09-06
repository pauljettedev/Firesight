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

export async function syncWildfires(): Promise<void> {
  const response = await fetch('/api/wildfires/sync', {
    method: 'POST',
  })

  if (!response.ok) {
    throw new Error(`Wildfire sync failed: ${response.status}`)
  }
}

export async function getWildfiresWithInitialSync(): Promise<Wildfire[]> {
  const wildfires = await getWildfires()

  if (wildfires.length > 0) {
    return wildfires
  }

  await syncWildfires()
  return getWildfires()
}
