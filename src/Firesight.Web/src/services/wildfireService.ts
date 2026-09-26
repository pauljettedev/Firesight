import { requestJson } from './http'

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

export function getWildfires(): Promise<Wildfire[]> {
  return requestJson('/api/wildfires', 'Wildfire request failed')
}

export function getNearbyWildfires(
  latitude: number,
  longitude: number,
  radiusKm: number,
): Promise<NearbyWildfire[]> {
  const query = new URLSearchParams({
    latitude: latitude.toString(),
    longitude: longitude.toString(),
    radiusKm: radiusKm.toString(),
  })

  return requestJson(
    `/api/wildfires/near?${query}`,
    'Nearby wildfire request failed',
  )
}

export function getWildfireSyncState(): Promise<WildfireFeedSyncState | null> {
  // 204: no sync has run yet.
  return requestJson(
    '/api/wildfires/sync-state',
    'Wildfire sync-state request failed',
    { nullWhen: [204] },
  )
}
