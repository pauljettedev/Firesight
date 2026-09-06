import { useEffect, useRef } from 'react'
import * as maplibregl from 'maplibre-gl'
import 'maplibre-gl/dist/maplibre-gl.css'
import type { Wildfire } from '../services/wildfireService'

interface WildfireMapProps {
  wildfires: Wildfire[]
}

const sourceId = 'wildfires'

const stageOfControlLabel = (code: unknown): string => {
  switch (String(code ?? '').toUpperCase()) {
    case 'OC': return 'Out of Control'
    case 'BH': return 'Being Held'
    case 'UC': return 'Under Control'
    case 'EX': return 'Extinguished'
    default: return String(code || 'Unknown status')
  }
}

function toGeoJson(wildfires: Wildfire[]): GeoJSON.FeatureCollection<GeoJSON.Point> {
  return {
    type: 'FeatureCollection',
    features: wildfires.map((fire) => ({
      type: 'Feature',
      geometry: {
        type: 'Point',
        coordinates: [fire.longitude, fire.latitude],
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

export function WildfireMap({ wildfires }: WildfireMapProps) {
  const containerRef = useRef<HTMLDivElement | null>(null)
  const mapRef = useRef<MapLibreMap | null>(null)

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
          },
        ],
      },
    })

    map.addControl(new maplibregl.NavigationControl(), 'top-right')

    map.on('load', () => {
      map.addSource(sourceId, {
        type: 'geojson',
        data: toGeoJson(wildfires),
      })

      map.addLayer({
        id: 'wildfire-points',
        type: 'circle',
        source: sourceId,
        paint: {
          'circle-radius': [
            'interpolate',
            ['linear'],
            ['coalesce', ['get', 'areaHectares'], 0],
            0, 5,
            1000, 7,
            10000, 10,
            100000, 14,
          ],
          'circle-color': [
            'match',
            ['get', 'status'],
            'OC', '#d32f2f',
            'BH', '#ed6c02',
            'UC', '#2e7d32',
            'EX', '#616161',
            '#7b1fa2',
          ],
          'circle-stroke-color': '#ffffff',
          'circle-stroke-width': [
            'case',
            ['==', ['get', 'isStale'], true],
            3,
            1,
          ],
          'circle-opacity': [
            'case',
            ['==', ['get', 'isStale'], true],
            0.45,
            0.85,
          ],
        },
      })

      map.on('click', 'wildfire-points', (event) => {
        const feature = event.features?.[0]
        if (!feature || feature.geometry.type !== 'Point') {
          return
        }

        const coordinates = [...feature.geometry.coordinates] as [number, number]
        const properties = feature.properties ?? {}
        const area = properties.areaHectares
          ? `${Number(properties.areaHectares).toLocaleString()} ha`
          : 'Area unavailable'
        const statusDate = formatTimestamp(properties.statusDateUtc)
        const lastSeen = formatTimestamp(properties.lastSeenInFeedUtc)
        const freshness = properties.isStale ? 'Stale observation' : 'Recent observation'

        new maplibregl.Popup()
          .setLngLat(coordinates)
          .setHTML(
            `<strong>${escapeHtml(String(properties.name ?? 'Wildfire'))}</strong><br />` +
            `${escapeHtml(String(properties.agency ?? 'Unknown agency'))}<br />` +
            `${escapeHtml(stageOfControlLabel(properties.status))}<br />` +
            `${escapeHtml(area)}<br />` +
            `Last seen in feed: ${escapeHtml(lastSeen)}<br />` +
            `Freshness: ${escapeHtml(freshness)}<br />` +
            `Status date: ${escapeHtml(statusDate)}`,
          )
          .addTo(map)
      })

      map.on('mouseenter', 'wildfire-points', () => {
        map.getCanvas().style.cursor = 'pointer'
      })

      map.on('mouseleave', 'wildfire-points', () => {
        map.getCanvas().style.cursor = ''
      })
    })

    mapRef.current = map

    return () => {
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

  return <div ref={containerRef} style={{ height: '65vh', minHeight: 480, width: '100%' }} />
}

function escapeHtml(value: string): string {
  return value.replace(/[&<>'"]/g, (character) => {
    const entities: Record<string, string> = {
      '&': '&amp;',
      '<': '&lt;',
      '>': '&gt;',
      "'": '&#39;',
      '"': '&quot;',
    }

    return entities[character]
  })
}

function formatTimestamp(value: unknown): string {
  if (!value) {
    return 'Not provided'
  }

  const date = new Date(String(value))
  if (Number.isNaN(date.getTime())) {
    return 'Invalid timestamp'
  }

  return date.toLocaleString()
}
