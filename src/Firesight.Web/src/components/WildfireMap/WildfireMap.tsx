import { useEffect, useRef } from 'react'
import { flushSync } from 'react-dom'
import { createRoot } from 'react-dom/client'
import * as maplibregl from 'maplibre-gl'
import type {
  ExpressionSpecification,
  GeoJSONSource,
  Map as MapLibreMap,
} from 'maplibre-gl'
import 'maplibre-gl/dist/maplibre-gl.css'
import maplibreWorkerUrl from 'maplibre-gl/dist/maplibre-gl-worker.mjs?url'
import './WildfireMap.css'
import type { Wildfire } from '../../services/wildfireService'
import type { MapFocusArea } from '../../utils/mapView'
import {
  defaultMapCenter,
  defaultMapZoom,
  useWildfireMapCamera,
} from './useWildfireMapCamera'
import {
  hasWildfireLayers,
  noSelectionId,
  pointsLayerId,
  selectionLayerId,
  sourceId,
} from './wildfireMapLayers'
import { ResetViewControl } from './ResetViewControl'
import { WildfirePopup } from './WildfirePopup'

// MapLibre works out its worker script's URL at runtime by guessing it sits
// next to whichever bundle chunk is currently executing (see maplibre-gl's
// internal getWorkerUrl()). Vite doesn't know to emit that file there on its
// own, so the guess 404s in production. Importing it with `?url` makes Vite
// bundle the real worker file and hand back its actual built URL, which we
// hand to MapLibre directly instead of letting it guess.
maplibregl.setWorkerUrl(maplibreWorkerUrl)

interface WildfireMapProps {
  wildfires: Wildfire[]
  selectedWildfire?: Wildfire | null
  onWildfireSelect?: (wildfire: Wildfire | null) => void
  // Called by the "Show all fires" button, after the map has started
  // moving its camera back to the full view.
  onReset?: () => void
  focusArea?: MapFocusArea
}

// MapLibre draws from GeoJSON, not our Wildfire objects. The popup is built
// from these properties, so everything it shows has to be copied in here.
function toGeoJson(wildfires: Wildfire[]) {
  return {
    type: 'FeatureCollection' as const,
    features: wildfires.map((fire) => ({
      type: 'Feature' as const,
      geometry: {
        type: 'Point' as const,
        coordinates: [fire.longitude, fire.latitude] as [number, number],
      },
      properties: {
        id: fire.id,
        name: fire.name ?? fire.externalId,
        agency: fire.agency,
        status: fire.status,
        areaHectares: fire.areaHectares,
        statusDateUtc: fire.statusDateUtc,
        lastSeenInFeedUtc: fire.lastSeenInFeedUtc,
        isStale: fire.isStale,
      },
    })),
  }
}

// Bigger fires get bigger dots, but the growth flattens out so a handful of
// huge fires don't cover everything around them.
const circleRadius: ExpressionSpecification = [
  'interpolate',
  ['linear'],
  ['coalesce', ['get', 'areaHectares'], 0],
  0, 5,
  1000, 7,
  10000, 10,
  100000, 14,
]

export function WildfireMap({
  wildfires,
  selectedWildfire,
  onWildfireSelect,
  onReset,
  focusArea,
}: WildfireMapProps) {
  const containerRef = useRef<HTMLDivElement | null>(null)
  const mapRef = useRef<MapLibreMap | null>(null)
  const wildfiresRef = useRef(wildfires)
  const selectedWildfireRef = useRef(selectedWildfire)
  const onWildfireSelectRef = useRef(onWildfireSelect)
  // What the "Show all fires" control does when clicked. Kept in a ref for
  // the same reason as the others: the control is created once, with the
  // map, but has to call the latest version.
  const resetRef = useRef<(map: MapLibreMap) => void>(() => {})
  const activePopupRef = useRef<maplibregl.Popup | null>(null)
  const activePopupWildfireIdRef = useRef<string | null>(null)
  // Set when a fire is picked by clicking its dot, so the camera knows the
  // map is already looking at it and doesn't need to fly there.
  const mapSelectedWildfireIdRef = useRef<string | null>(null)

  // The map below is created once and its click handler is set up only
  // that one time. These effects keep the refs updated so that
  // handler can always read the latest wildfires, selection, and
  // onWildfireSelect, without us having to rebuild the whole map every
  // time those props change.
  useEffect(() => {
    wildfiresRef.current = wildfires
  }, [wildfires])

  useEffect(() => {
    onWildfireSelectRef.current = onWildfireSelect
  }, [onWildfireSelect])

  useEffect(() => {
    selectedWildfireRef.current = selectedWildfire
  }, [selectedWildfire])

  useEffect(() => {
    if (!containerRef.current || mapRef.current) {
      return
    }

    // Created once and kept for the life of the component. Rebuilding a
    // MapLibre map is slow and resets the view, so prop changes are applied
    // to this one map by the effects further down instead.
    const map = new maplibregl.Map({
      container: containerRef.current,
      center: defaultMapCenter,
      zoom: defaultMapZoom,
      style: {
        version: 8,
        sources: {
          osm: {
            type: 'raster',
            tiles: ['https://tile.openstreetmap.org/{z}/{x}/{y}.png'],
            tileSize: 256,
            attribution: '© OpenStreetMap contributors',
          },
        },
        layers: [
          {
            id: 'osm',
            type: 'raster',
            source: 'osm',
            // Muted and darkened so the base map sits back and the
            // coloured fire dots stand out against the dark theme.
            paint: {
              'raster-saturation': -0.35,
              'raster-contrast': 0.28,
              'raster-brightness-min': 0.05,
              'raster-brightness-max': 0.82,
            },
          },
        ],
      },
    })

    map.addControl(new maplibregl.NavigationControl(), 'top-right')
    map.addControl(new ResetViewControl(() => resetRef.current(map)), 'top-left')

    map.on('load', () => {
      map.addSource(sourceId, {
        type: 'geojson',
        data: toGeoJson(wildfiresRef.current),
      })

      map.addLayer({
        id: pointsLayerId,
        type: 'circle',
        source: sourceId,
        paint: {
          'circle-radius': circleRadius,
          'circle-color': [
            'match',
            ['get', 'status'],
            'OC', '#ef6a6a',
            'BH', '#f0b45a',
            'UC', '#69c58f',
            'EX', '#8796a1',
            '#8e78b5',
          ],
          // Fires not seen in the feed recently are faded, so it's clear
          // their details may be out of date.
          'circle-stroke-color': '#eef4f7',
          'circle-stroke-width': [
            'case',
            ['==', ['get', 'isStale'], true],
            2,
            1,
          ],
          'circle-opacity': [
            'case',
            ['==', ['get', 'isStale'], true],
            0.48,
            0.88,
          ],
        },
      })

      // The ring around the selected fire is a second layer over the same
      // data, filtered down to one fire. Selecting a different fire is then
      // just a filter change, with no need to restyle every dot.
      map.addLayer({
        id: selectionLayerId,
        type: 'circle',
        source: sourceId,
        filter: ['==', ['get', 'id'], noSelectionId],
        paint: {
          'circle-radius': ['+', circleRadius, 4],
          'circle-color': 'rgba(0, 0, 0, 0)',
          'circle-stroke-color': '#41d9e8',
          'circle-stroke-width': 3,
          'circle-opacity': 1,
        },
      })

      map.on('click', pointsLayerId, (event) => {
        const feature = event.features?.[0]
        if (!feature || feature.geometry.type !== 'Point') {
          return
        }

        const coordinates = [...feature.geometry.coordinates] as [number, number]
        const properties = feature.properties ?? {}
        const selectedId = String(properties.id ?? noSelectionId)
        const selected =
          wildfiresRef.current.find((wildfire) => wildfire.id === selectedId) ??
          null

        // Only one popup open at a time.
        activePopupRef.current?.remove()
        map.setFilter(selectionLayerId, ['==', ['get', 'id'], selectedId])
        mapSelectedWildfireIdRef.current = selectedId
        onWildfireSelectRef.current?.(selected)

        // MapLibre popups take a plain DOM element, so the React popup is
        // rendered into one. flushSync finishes that render before MapLibre
        // measures the element to position the popup.
        const popupContent = document.createElement('div')
        const popupRoot = createRoot(popupContent)

        flushSync(() => {
          popupRoot.render(
            <WildfirePopup
              name={String(properties.name ?? 'Wildfire')}
              agency={String(properties.agency ?? 'Unknown agency')}
              status={String(properties.status ?? '')}
              areaHectares={toNullableNumber(properties.areaHectares)}
              statusDateUtc={toNullableString(properties.statusDateUtc)}
              lastSeenInFeedUtc={toNullableString(properties.lastSeenInFeedUtc)}
              isStale={toBoolean(properties.isStale)}
            />,
          )
        })

        const popup = new maplibregl.Popup({
          className: 'firesight-popup',
          offset: 14,
          maxWidth: '320px',
        })
          .setLngLat(coordinates)
          .setDOMContent(popupContent)
          .addTo(map)

        activePopupRef.current = popup
        activePopupWildfireIdRef.current = selectedId

        popup.on('close', () => {
          popupRoot.unmount()

          activePopupRef.current = null
          activePopupWildfireIdRef.current = null
          map.setFilter(selectionLayerId, [
            '==',
            ['get', 'id'],
            noSelectionId,
          ])

          // Closing the popup (its X, or a click elsewhere on the map)
          // deselects its fire. But picking a different fire, e.g. from the
          // Recent list, also closes it, and by then the selection has moved
          // on to the new fire, which mustn't be cleared.
          if (selectedWildfireRef.current?.id === selectedId) {
            onWildfireSelectRef.current?.(null)
          }
        })
      })

      map.on('mouseenter', pointsLayerId, () => {
        map.getCanvas().style.cursor = 'pointer'
      })

      map.on('mouseleave', pointsLayerId, () => {
        map.getCanvas().style.cursor = ''
      })
    })

    mapRef.current = map

    return () => {
      activePopupRef.current?.remove()
      activePopupRef.current = null
      activePopupWildfireIdRef.current = null
      mapSelectedWildfireIdRef.current = null
      map.remove()
      mapRef.current = null
    }
  }, [])

  useEffect(() => {
    const map = mapRef.current
    if (!map || !hasWildfireLayers(map)) {
      return
    }

    // Swaps the data on the existing map rather than rebuilding it, so the
    // view, controls and any open popup stay put.
    const source = map.getSource(sourceId) as GeoJSONSource | undefined
    source?.setData(toGeoJson(wildfires))
  }, [wildfires])

  // Keeps the selection ring (and any open popup) in step with the selected
  // fire. Moving the camera to it is the camera hook's job.
  useEffect(() => {
    const map = mapRef.current
    if (!map || !hasWildfireLayers(map)) {
      return
    }

    if (
      activePopupWildfireIdRef.current &&
      activePopupWildfireIdRef.current !== selectedWildfire?.id
    ) {
      activePopupRef.current?.remove()
    }

    const selectedId = selectedWildfire?.id ?? noSelectionId
    map.setFilter(selectionLayerId, ['==', ['get', 'id'], selectedId])
  }, [selectedWildfire])

  const { resetCamera } = useWildfireMapCamera({
    mapRef,
    focusArea,
    selectedWildfire,
    mapSelectedWildfireIdRef,
  })

  // No dependency list: onReset is a new function on every render, so this
  // would re-run every time anyway.
  useEffect(() => {
    resetRef.current = (map) => {
      resetCamera(map)
      onReset?.()
    }
  })

  return (
    <div
      ref={containerRef}
      className="firesight-map"
      style={{ height: '65vh', minHeight: 480, width: '100%' }}
    />
  )
}

function toNullableNumber(value: unknown): number | null {
  if (value == null) return null
  const parsed = Number(value)
  return Number.isFinite(parsed) ? parsed : null
}

function toNullableString(value: unknown): string | null {
  if (value == null || value === '') return null
  return String(value)
}

function toBoolean(value: unknown): boolean {
  return value === true || value === 'true'
}
