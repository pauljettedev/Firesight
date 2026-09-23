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
    throw new Error(
      await describeError(response, 'Nearby wildfire request failed'),
    )
  }

  return response.json()
}

// A failed request like a bad radius comes back with a JSON body explaining
// what was wrong, for example "Radius must be at most 1000 km." We read that
// instead of just showing a status code, so the user knows what to fix.
async function describeError(
  response: Response,
  fallback: string,
): Promise<string> {
  try {
    const body = await response.json()
    const messages = Object.values(body.errors ?? {}).flat()

    if (messages.length > 0) {
      return messages.join(' ')
    }

    if (typeof body.title === 'string') {
      return body.title
    }
  } catch {
    // The response body wasn't JSON, or didn't have the shape we expected.
  }

  return `${fallback}: ${response.status}`
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
