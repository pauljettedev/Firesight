import type { Wildfire } from '../services/wildfireService'
import {
  createSingleWildfireView,
  type MapFocusArea,
  type MapView,
} from '../utils/mapView'

// What the map is showing (mapView) and which fire is highlighted
// (selectedWildfire). They live together because most user actions change
// both at once, and keeping every change in one reducer means each action's
// effect on the map is spelled out in exactly one place.
export interface MapState {
  mapView: MapView | null
  selectedWildfire: Wildfire | null
}

export type MapAction =
  // Ask AI "Show on map": narrow to the fires around a place.
  | {
      type: 'showArea'
      focusArea: MapFocusArea
      wildfires: Wildfire[]
      label: string | null
    }
  // Recent tab: narrow the map to just this fire.
  | { type: 'focusWildfire'; wildfire: Wildfire }
  // Nearby tab: show every fire again, with this one highlighted.
  | { type: 'selectWildfire'; wildfire: Wildfire }
  // A dot was clicked on the map, or its popup was closed (null). Whatever
  // the map is showing stays as it is.
  | { type: 'mapSelectionChanged'; wildfire: Wildfire | null }
  // The map's "Show all fires" button: a full reset, back to every fire
  // with nothing selected.
  | { type: 'resetMap' }

export const initialMapState: MapState = {
  mapView: null,
  selectedWildfire: null,
}

export function mapStateReducer(state: MapState, action: MapAction): MapState {
  switch (action.type) {
    case 'showArea':
      return {
        mapView: {
          kind: 'area',
          focusArea: action.focusArea,
          wildfires: action.wildfires,
          label: action.label,
        },
        selectedWildfire: null,
      }
    case 'focusWildfire':
      return {
        mapView: createSingleWildfireView(action.wildfire),
        selectedWildfire: action.wildfire,
      }
    case 'selectWildfire':
      return { mapView: null, selectedWildfire: action.wildfire }
    case 'mapSelectionChanged':
      // Clicking the fire that's already selected changes nothing, so hand
      // back the same state object and React skips the re-render.
      if (action.wildfire?.id === state.selectedWildfire?.id) {
        return state
      }

      return { ...state, selectedWildfire: action.wildfire }
    case 'resetMap':
      return initialMapState
  }
}
