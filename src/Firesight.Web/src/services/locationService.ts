export interface GeocodedLocation {
  displayName: string
  latitude: number
  longitude: number
}

export async function geocodeLocation(
  query: string,
): Promise<GeocodedLocation | null> {
  const parameters = new URLSearchParams({ query })
  const response = await fetch(`/api/locations/geocode?${parameters}`)

  if (response.status === 404) {
    return null
  }

  if (!response.ok) {
    throw new Error(`Location search failed: ${response.status}`)
  }

  return response.json()
}
