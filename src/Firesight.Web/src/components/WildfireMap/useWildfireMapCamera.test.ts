import { describe, expect, it } from 'vitest'
import { renderHook } from '@testing-library/react'
import type { Map as MapLibreMap } from 'maplibre-gl'
import type { Wildfire } from '../../services/wildfireService'
import { buildWildfire } from '../../test/fixtures'
import type { MapFocusArea } from '../../utils/mapView'
import { useWildfireMapCamera } from './useWildfireMapCamera'

// A stand-in for MapLibre's Map (which needs WebGL) that just records which
// camera methods were called, in order. That's all the hook does to the map.
function createStubMap() {
  const calls: string[] = []
  const map = {
    getLayer: () => ({}),
    getZoom: () => 4,
    easeTo: () => calls.push('easeTo'),
    fitBounds: () => calls.push('fitBounds'),
    flyTo: () => calls.push('flyTo'),
  }

  return { map: map as unknown as MapLibreMap, calls }
}

interface CameraProps {
  focusArea?: MapFocusArea
  selectedWildfire?: Wildfire | null
}

function renderCamera(initialProps: CameraProps) {
  const { map, calls } = createStubMap()
  const mapRef = { current: map }
  const mapSelectedWildfireIdRef = { current: null as string | null }

  const hook = renderHook(
    (props: CameraProps) =>
      useWildfireMapCamera({ mapRef, mapSelectedWildfireIdRef, ...props }),
    { initialProps },
  )

  // Only count what happens after the first render, i.e. in response to
  // the prop changes each test makes.
  calls.length = 0

  return { ...hook, calls, mapSelectedWildfireIdRef }
}

const focusArea: MapFocusArea = { latitude: 50, longitude: -120, radiusKm: 40 }
const fireA = buildWildfire({ id: 'a' })
const fireB = buildWildfire({ id: 'b' })

describe('useWildfireMapCamera', () => {
  it('frames a focus area without also flying to the fire selected with it', () => {
    const { rerender, calls } = renderCamera({})

    rerender({ focusArea, selectedWildfire: fireA })

    expect(calls).toEqual(['fitBounds'])
  })

  it('flies to a fire picked while leaving a focused view, after starting the zoom-out', () => {
    // This is the ordering rule the hook's comments describe. MapLibre lets
    // the newest camera animation win, so flyTo has to come last for the
    // map to end up on the fire rather than zoomed out over Canada.
    const { rerender, calls } = renderCamera({ focusArea, selectedWildfire: fireA })

    rerender({ focusArea: undefined, selectedWildfire: fireB })

    expect(calls).toEqual(['easeTo', 'flyTo'])
  })

  it('zooms back out when a focused view is cleared and the selection stays the same', () => {
    const { rerender, calls } = renderCamera({ focusArea, selectedWildfire: fireA })

    rerender({ focusArea: undefined, selectedWildfire: fireA })

    expect(calls).toEqual(['easeTo'])
  })

  it("doesn't fly to a fire that was picked by clicking it on the map", () => {
    const { rerender, calls, mapSelectedWildfireIdRef } = renderCamera({})

    mapSelectedWildfireIdRef.current = fireA.id
    rerender({ selectedWildfire: fireA })

    expect(calls).toEqual([])
  })

  it('clears a leftover map-click marker so a later pick of that fire still flies', () => {
    // A click on the already-selected fire leaves the marker set without
    // the selection changing. The next selection change has to clear it,
    // or picking that fire again later would wrongly skip its flight.
    const { rerender, calls, mapSelectedWildfireIdRef } = renderCamera({
      selectedWildfire: fireA,
    })

    mapSelectedWildfireIdRef.current = fireA.id
    rerender({ selectedWildfire: fireB })
    rerender({ selectedWildfire: fireA })

    expect(calls).toEqual(['flyTo', 'flyTo'])
  })
})
