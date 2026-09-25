import type { Wildfire } from '../services/wildfireService'

// One place to build a test Wildfire, so a new field on the type means one
// fixture to update rather than one per test file. Tests override only the
// fields they actually care about.
export function buildWildfire(overrides: Partial<Wildfire> = {}): Wildfire {
  return {
    id: 'wildfire-1',
    externalId: 'cwfis:test-1',
    agency: 'ON',
    name: null,
    latitude: 45.4215,
    longitude: -75.6972,
    startDate: null,
    areaHectares: 1250,
    status: 'OC',
    statusDateUtc: null,
    lastSeenInFeedUtc: new Date().toISOString(),
    isStale: false,
    ...overrides,
  }
}

export function hoursAgoUtc(hours: number): string {
  return new Date(Date.now() - hours * 3_600_000).toISOString()
}
