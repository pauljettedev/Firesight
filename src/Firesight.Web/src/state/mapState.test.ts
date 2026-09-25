import { describe, expect, it } from 'vitest'
import { buildWildfire } from '../test/fixtures'
import { initialMapState, mapStateReducer, type MapState } from './mapState'

const fireA = buildWildfire({ id: 'a' })
const fireB = buildWildfire({ id: 'b' })
const areaState: MapState = mapStateReducer(initialMapState, {
  type: 'showArea',
  focusArea: { latitude: 50, longitude: -120, radiusKm: 100 },
  wildfires: [fireA, fireB],
  label: 'Kamloops',
})

describe('mapStateReducer', () => {
  it('showArea narrows the map to the area and clears any selection', () => {
    const selected = { ...initialMapState, selectedWildfire: fireA }

    const state = mapStateReducer(selected, {
      type: 'showArea',
      focusArea: { latitude: 50, longitude: -120, radiusKm: 100 },
      wildfires: [fireA, fireB],
      label: 'Kamloops',
    })

    expect(state.mapView).toMatchObject({ kind: 'area', label: 'Kamloops' })
    expect(state.selectedWildfire).toBeNull()
  })

  it('focusWildfire narrows the map to that fire and selects it', () => {
    const state = mapStateReducer(areaState, {
      type: 'focusWildfire',
      wildfire: fireB,
    })

    expect(state.mapView).toMatchObject({ kind: 'wildfire', wildfire: fireB })
    expect(state.selectedWildfire).toBe(fireB)
  })

  it('focusWildfire makes a new focus request each time, even for the same fire', () => {
    // A new focusArea object is what makes the map move back to the fire
    // after the user has panned away and clicks it again.
    const first = mapStateReducer(initialMapState, {
      type: 'focusWildfire',
      wildfire: fireA,
    })
    const second = mapStateReducer(first, {
      type: 'focusWildfire',
      wildfire: fireA,
    })

    expect(second.mapView?.focusArea).not.toBe(first.mapView?.focusArea)
  })

  it('selectWildfire shows every fire again with that one selected', () => {
    const state = mapStateReducer(areaState, {
      type: 'selectWildfire',
      wildfire: fireA,
    })

    expect(state.mapView).toBeNull()
    expect(state.selectedWildfire).toBe(fireA)
  })

  it('mapSelectionChanged changes the selection but keeps the current view', () => {
    const state = mapStateReducer(areaState, {
      type: 'mapSelectionChanged',
      wildfire: fireB,
    })

    expect(state.mapView).toBe(areaState.mapView)
    expect(state.selectedWildfire).toBe(fireB)
  })

  it('showAll clears the view but keeps the selected fire highlighted', () => {
    const focused = mapStateReducer(initialMapState, {
      type: 'focusWildfire',
      wildfire: fireA,
    })

    const state = mapStateReducer(focused, { type: 'showAll' })

    expect(state.mapView).toBeNull()
    expect(state.selectedWildfire).toBe(fireA)
  })
})
