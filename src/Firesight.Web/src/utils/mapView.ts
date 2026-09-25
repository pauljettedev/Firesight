import type { Wildfire } from '../services/wildfireService'

// A circle on the map to frame: a centre point plus how far around it to show.
export interface MapFocusArea {
  latitude: number
  longitude: number
  radiusKm: number
}

// The map normally shows every fire. A MapView narrows it: either to the
// fires around a place (from an Ask AI answer) or to one fire (from the
// Recent tab). null means "show everything".
//
// Both kinds carry a focusArea, so the map has one way to move its camera
// to "whatever is being shown" instead of a separate path per kind.
export type MapView =
  | {
      kind: 'area'
      focusArea: MapFocusArea
      wildfires: Wildfire[]
      label: string | null
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

export function wildfiresInView(
  mapView: MapView | null,
  allWildfires: Wildfire[],
): Wildfire[] {
  if (!mapView) {
    return allWildfires
  }

  switch (mapView.kind) {
    case 'area':
      return mapView.wildfires
    case 'wildfire':
      return [mapView.wildfire]
  }
}
