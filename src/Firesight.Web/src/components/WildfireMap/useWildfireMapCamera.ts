import { useEffect, useRef, type RefObject } from 'react'
import type { Map as MapLibreMap } from 'maplibre-gl'
import type { Wildfire } from '../../services/wildfireService'
import type { MapFocusArea } from '../../utils/mapView'
import { hasWildfireLayers } from './wildfireMapLayers'

export const defaultMapCenter: [number, number] = [-96, 57]
export const defaultMapZoom = 2.7

interface WildfireMapCameraOptions {
  mapRef: RefObject<MapLibreMap | null>
  focusArea?: MapFocusArea
  selectedWildfire?: Wildfire | null
  // Set by the map's click handler when a fire was picked by clicking its
  // dot. The map is already looking at that fire, so there's nothing to
  // fly to.
  mapSelectedWildfireIdRef: RefObject<string | null>
}

// Everything that moves the map's camera lives here, because the two ways
// it moves (framing a focus area, flying to a selected fire) have to agree
// on who wins when both change in the same render.
export function useWildfireMapCamera({
  mapRef,
  focusArea,
  selectedWildfire,
  mapSelectedWildfireIdRef,
}: WildfireMapCameraOptions) {
  const focusAreaRef = useRef(focusArea)
  const hadFocusAreaRef = useRef(false)

  // Lets the fly-to effect read the current focus area without re-running
  // every time it changes. Declared first so it's up to date before the
  // effects below read it (React runs effects in declaration order).
  useEffect(() => {
    focusAreaRef.current = focusArea
  }, [focusArea])

  // The two camera effects below are declared in this order on purpose.
  // A newer MapLibre camera animation replaces one already running, so when
  // both change in one render, the more specific request wins: leaving a
  // focused view starts a zoom back out to Canada here, and a fire selected
  // at the same time (e.g. from Nearby) replaces that with a flight to the
  // fire in the next effect.
  useEffect(() => {
    const map = mapRef.current
    if (!map || !hasWildfireLayers(map)) {
      return
    }

    if (!focusArea) {
      if (!hadFocusAreaRef.current) {
        return
      }

      hadFocusAreaRef.current = false
      map.easeTo({
        center: defaultMapCenter,
        zoom: defaultMapZoom,
        duration: 800,
      })
      return
    }

    hadFocusAreaRef.current = true

    // 1 degree of latitude is about 111 km, so dividing by 111 turns our
    // radius into degrees. A degree of longitude covers less distance as
    // you move away from the equator, so we shrink it using cos(latitude).
    const latitudeDelta = focusArea.radiusKm / 111
    const longitudeScale = Math.max(
      Math.cos((focusArea.latitude * Math.PI) / 180),
      0.15,
    )
    const longitudeDelta = focusArea.radiusKm / (111 * longitudeScale)

    map.fitBounds(
      [
        [
          focusArea.longitude - longitudeDelta,
          focusArea.latitude - latitudeDelta,
        ],
        [
          focusArea.longitude + longitudeDelta,
          focusArea.latitude + latitudeDelta,
        ],
      ],
      {
        padding: 48,
        duration: 800,
        maxZoom: 10,
      },
    )
  }, [mapRef, focusArea])

  useEffect(() => {
    const map = mapRef.current
    if (!map || !hasWildfireLayers(map)) {
      return
    }

    // The marker is used up on every run, whether or not it matches. If a
    // click didn't actually change the selection (the fire was already
    // selected), this effect doesn't run for it, and a marker that was only
    // cleared on a match would sit there until some later selection of
    // that same fire, which would then wrongly skip its flight.
    const pickedOnMap =
      selectedWildfire != null &&
      mapSelectedWildfireIdRef.current === selectedWildfire.id
    mapSelectedWildfireIdRef.current = null

    if (!selectedWildfire || pickedOnMap) {
      return
    }

    // While a focus area is set, the effect above owns the camera. Flying
    // here too would pull the map off the area it framed.
    if (focusAreaRef.current) {
      return
    }

    map.flyTo({
      center: [selectedWildfire.longitude, selectedWildfire.latitude],
      zoom: Math.max(map.getZoom(), 6),
      essential: true,
    })
  }, [mapRef, mapSelectedWildfireIdRef, selectedWildfire])
}
