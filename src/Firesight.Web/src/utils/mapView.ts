import type { Wildfire } from '../services/wildfireService'

// A circle on the map to frame: a centre point plus how far around it to show.
export interface MapFocusArea {
  latitude: number
  longitude: number
  radiusKm: number
}

// The map normally shows every fire. A MapView narrows it: to the fires
// around a place or the fires an answer names (both from Ask AI), or to one
// fire (from the Recent tab). null means "show everything".
//
// Every kind carries a focusArea, so the map has one way to move its camera
// to "whatever is being shown" instead of a separate path per kind.
export type MapView =
  | {
      kind: 'area'
      focusArea: MapFocusArea
      wildfires: Wildfire[]
      label: string | null
    }
  | {
      kind: 'wildfires'
      focusArea: MapFocusArea
      wildfires: Wildfire[]
    }
  | {
      kind: 'wildfire'
      focusArea: MapFocusArea
      wildfire: Wildfire
    }

// How much of the map around a single fire stays in view. Large enough to
// show the surrounding towns and roads for context.
const singleWildfireFocusRadiusKm = 40

export function createSingleWildfireView(wildfire: Wildfire): MapView {
  return {
    kind: 'wildfire',
    wildfire,
    // A new focusArea object on every call is deliberate: clicking the same
    // fire again after panning away still moves the map back to it, because
    // the map sees a new focus request.
    focusArea: {
      latitude: wildfire.latitude,
      longitude: wildfire.longitude,
      radiusKm: singleWildfireFocusRadiusKm,
    },
  }
}

// Frames a group of fires. The map's camera turns a focusArea back into a
// box using the same 111 km per degree maths, so a radius covering the
// fires' bounding box keeps them all in view.
export function createWildfiresView(wildfires: Wildfire[]): MapView {
  const latitudes = wildfires.map((wildfire) => wildfire.latitude)
  const longitudes = wildfires.map((wildfire) => wildfire.longitude)
  const latitude = (Math.min(...latitudes) + Math.max(...latitudes)) / 2
  const longitude = (Math.min(...longitudes) + Math.max(...longitudes)) / 2

  const latitudeSpanKm =
    ((Math.max(...latitudes) - Math.min(...latitudes)) / 2) * 111
  const longitudeSpanKm =
    ((Math.max(...longitudes) - Math.min(...longitudes)) / 2) *
    111 *
    Math.max(Math.cos((latitude * Math.PI) / 180), 0.15)

  return {
    kind: 'wildfires',
    wildfires,
    focusArea: {
      latitude,
      longitude,
      radiusKm: Math.max(
        latitudeSpanKm,
        longitudeSpanKm,
        singleWildfireFocusRadiusKm,
      ),
    },
  }
}

// The loaded fires with these CWFIS IDs, in the same order as the IDs. IDs
// with no loaded fire are skipped. Fire details always come from the
// Firesight API, never from the AI's answer text. (The API has already
// replaced Claude's copy of each ID with the real one, so an exact match is
// enough.)
export function wildfiresWithExternalIds(
  allWildfires: Wildfire[],
  externalIds: string[],
): Wildfire[] {
  const found: Wildfire[] = []

  for (const id of externalIds) {
    const wildfire = allWildfires.find(
      (candidate) => candidate.externalId === id,
    )

    if (wildfire) {
      found.push(wildfire)
    }
  }

  return found
}

export function wildfiresInView(
  mapView: MapView | null,
  allWildfires: Wildfire[],
): Wildfire[] {
  if (!mapView) {
    return allWildfires
  }

  switch (mapView.kind) {
    case 'area':
    case 'wildfires':
      return mapView.wildfires
    case 'wildfire':
      return [mapView.wildfire]
  }
}
