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

export interface WildfireFeedSyncState {
  lastAttemptUtc: string
  lastSuccessfulFetchUtc: string | null
  lastAttemptSucceeded: boolean
  received: number
  accepted: number
  rejected: number
}

export interface NearbyWildfire {
  wildfire: Wildfire
  distanceKm: number
}

export async function getWildfires(): Promise<Wildfire[]> {
  const response = await fetch('/api/wildfires')

  if (!response.ok) {
    throw new Error(`Wildfire request failed: ${response.status}`)
  }

  return response.json()
}

export async function getNearbyWildfires(
  latitude: number,
  longitude: number,
  radiusKm: number,
): Promise<NearbyWildfire[]> {
  const query = new URLSearchParams({
    latitude: latitude.toString(),
    longitude: longitude.toString(),
    radiusKm: radiusKm.toString(),
  })

  const response = await fetch(`/api/wildfires/near?${query}`)

  if (!response.ok) {
    throw new Error(`Nearby wildfire request failed: ${response.status}`)
  }

  return response.json()
}

export async function getWildfireSyncState(): Promise<WildfireFeedSyncState | null> {
  const response = await fetch('/api/wildfires/sync-state')

  if (response.status === 204) {
    return null
  }

  if (!response.ok) {
    throw new Error(`Wildfire sync-state request failed: ${response.status}`)
  }

  return response.json()
}
