import type { Map as MapLibreMap } from 'maplibre-gl'

// The IDs of the GeoJSON source and layers WildfireMap adds, shared with the
// hooks that update them.
export const sourceId = 'wildfires'
export const pointsLayerId = 'wildfire-points'
export const selectionLayerId = 'wildfire-selection'
export const noSelectionId = '__no_selection__'

// True once the map's 'load' handler has added our wildfire source and
// layers. The selection layer is checked because it's the last thing that
// handler adds, so if it exists, everything before it does too.
//
// This deliberately isn't map.isStyleLoaded(): that also reports false
// while MapLibre is still processing new GeoJSON from setData(), so an
// effect running right after new data arrives (like selecting a fire at
// the same moment the map is narrowed to it) would wrongly bail out.
export function hasWildfireLayers(map: MapLibreMap): boolean {
  return map.getLayer(selectionLayerId) !== undefined
}
