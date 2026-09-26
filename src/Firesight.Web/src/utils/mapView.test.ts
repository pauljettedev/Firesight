import { describe, expect, it } from 'vitest'
import { buildWildfire } from '../test/fixtures'
import { createWildfiresView, wildfiresWithExternalIds } from './mapView'

describe('createWildfiresView', () => {
  it('centres on the fires and uses a radius that reaches the furthest one', () => {
    const view = createWildfiresView([
      buildWildfire({ latitude: 50, longitude: -120 }),
      buildWildfire({ latitude: 60, longitude: -120 }),
    ])

    expect(view.focusArea.latitude).toBe(55)
    expect(view.focusArea.longitude).toBe(-120)
    // Half of 10 degrees of latitude, at about 111 km per degree.
    expect(view.focusArea.radiusKm).toBeCloseTo(555)
  })

  it('never frames fires closer together than the single-fire view does', () => {
    const view = createWildfiresView([
      buildWildfire({ latitude: 50, longitude: -120 }),
      buildWildfire({ latitude: 50.01, longitude: -120 }),
    ])

    expect(view.focusArea.radiusKm).toBe(40)
  })
})

describe('wildfiresWithExternalIds', () => {
  it('finds loaded fires by CWFIS ID in the order given, skipping unknown IDs', () => {
    const fireA = buildWildfire({ id: 'a', externalId: '2026_BC_A' })
    const fireB = buildWildfire({ id: 'b', externalId: '2026_BC_B' })

    expect(
      wildfiresWithExternalIds(
        [fireA, fireB],
        ['2026_BC_B', '2026_XX_GONE', '2026_BC_A'],
      ),
    ).toEqual([fireB, fireA])
  })
})
