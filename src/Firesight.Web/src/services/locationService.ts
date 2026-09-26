import { requestJson } from './http'

export interface GeocodedLocation {
  displayName: string
  latitude: number
  longitude: number
}

export function geocodeLocation(
  query: string,
): Promise<GeocodedLocation | null> {
  const parameters = new URLSearchParams({ query })

  // 404: no place matched.
  return requestJson(
    `/api/locations/geocode?${parameters}`,
    'Location search failed',
    { nullWhen: [404] },
  )
}
