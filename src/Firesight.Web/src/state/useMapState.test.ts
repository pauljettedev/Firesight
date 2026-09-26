import { beforeEach, describe, expect, it, vi } from 'vitest'
import { act, renderHook } from '@testing-library/react'
import {
  getNearbyWildfires,
  type NearbyWildfire,
} from '../services/wildfireService'
import { buildWildfire } from '../test/fixtures'
import { useMapState } from './useMapState'

vi.mock('../services/wildfireService', () => ({
  getNearbyWildfires: vi.fn(),
}))

const mockedGetNearbyWildfires = vi.mocked(getNearbyWildfires)

const askContext = {
  latitude: 50,
  longitude: -120,
  radiusKm: 100,
  label: 'Kamloops',
}

describe('useMapState', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('shows the Ask AI area once its lookup finishes', async () => {
    const nearbyFire = buildWildfire({ id: 'nearby' })
    mockedGetNearbyWildfires.mockResolvedValue([
      { wildfire: nearbyFire, distanceKm: 5 },
    ])
    const { result } = renderHook(() => useMapState())

    await act(() => result.current.showAskAreaOnMap(askContext))

    expect(result.current.mapView).toMatchObject({
      kind: 'area',
      wildfires: [nearbyFire],
    })
  })

  it('ignores a slow Ask AI lookup if the user picked another view while it was loading', async () => {
    // Hold the lookup open so we control exactly when it finishes.
    let finishLookup!: (results: NearbyWildfire[]) => void
    mockedGetNearbyWildfires.mockReturnValue(
      new Promise((resolve) => {
        finishLookup = resolve
      }),
    )
    const recentFire = buildWildfire({ id: 'recent' })
    const { result } = renderHook(() => useMapState())

    let lookup!: Promise<void>
    act(() => {
      lookup = result.current.showAskAreaOnMap(askContext)
    })
    act(() => {
      result.current.focusWildfire(recentFire)
    })
    await act(async () => {
      finishLookup([{ wildfire: buildWildfire(), distanceKm: 5 }])
      await lookup
    })

    // The user's later Recent pick is still what's showing.
    expect(result.current.mapView).toMatchObject({
      kind: 'wildfire',
      wildfire: recentFire,
    })
  })
})
