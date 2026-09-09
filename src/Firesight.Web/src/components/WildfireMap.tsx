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
import './WildfireMap.css'
import type { Wildfire } from '../services/wildfireService'
import { WildfirePopup } from './WildfirePopup'

interface WildfireMapProps {
  wildfires: Wildfire[]
  selectedWildfire?: Wildfire | null
  onWildfireSelect?: (wildfire: Wildfire | null) => void
}

const sourceId = 'wildfires'
const pointsLayerId = 'wildfire-points'
const selectionLayerId = 'wildfire-selection'
const noSelectionId = '__no_selection__'

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
}: WildfireMapProps) {
  const containerRef = useRef<HTMLDivElement | null>(null)
  const mapRef = useRef<MapLibreMap | null>(null)
  const wildfiresRef = useRef(wildfires)
  const selectedWildfireRef = useRef(selectedWildfire)
  const onWildfireSelectRef = useRef(onWildfireSelect)
  const activePopupRef = useRef<maplibregl.Popup | null>(null)
  const activePopupWildfireIdRef = useRef<string | null>(null)
  const mapSelectedWildfireIdRef = useRef<string | null>(null)

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

    const map = new maplibregl.Map({
      container: containerRef.current,
      center: [-96, 57],
      zoom: 2.7,
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

        activePopupRef.current?.remove()
        map.setFilter(selectionLayerId, ['==', ['get', 'id'], selectedId])
        mapSelectedWildfireIdRef.current = selectedId
        onWildfireSelectRef.current?.(selected)

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

          if (activePopupRef.current !== popup) {
            return
          }

          activePopupRef.current = null
          activePopupWildfireIdRef.current = null
          map.setFilter(selectionLayerId, [
            '==',
            ['get', 'id'],
            noSelectionId,
          ])

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
    if (!map?.isStyleLoaded()) {
      return
    }

    const source = map.getSource(sourceId) as GeoJSONSource | undefined
    source?.setData(toGeoJson(wildfires))
  }, [wildfires])

  useEffect(() => {
    const map = mapRef.current
    if (!map?.isStyleLoaded()) {
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

    if (!selectedWildfire) {
      mapSelectedWildfireIdRef.current = null
      return
    }

    if (mapSelectedWildfireIdRef.current === selectedWildfire.id) {
      mapSelectedWildfireIdRef.current = null
      return
    }

    map.flyTo({
      center: [selectedWildfire.longitude, selectedWildfire.latitude],
      zoom: Math.max(map.getZoom(), 6),
      essential: true,
    })
  }, [selectedWildfire])

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
