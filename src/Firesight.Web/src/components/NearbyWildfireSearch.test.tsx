import { beforeEach, describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { NearbyWildfireSearch } from './NearbyWildfireSearch'
import { geocodeLocation } from '../services/locationService'
import {
  getNearbyWildfires,
  type Wildfire,
} from '../services/wildfireService'

vi.mock('../services/locationService', () => ({
  geocodeLocation: vi.fn(),
}))

vi.mock('../services/wildfireService', () => ({
  getNearbyWildfires: vi.fn(),
}))

const mockedGeocodeLocation = vi.mocked(geocodeLocation)
const mockedGetNearbyWildfires = vi.mocked(getNearbyWildfires)

function buildWildfire(overrides: Partial<Wildfire> = {}): Wildfire {
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

describe('NearbyWildfireSearch', () => {
  const onSelect = vi.fn()

  beforeEach(() => {
    // The service mocks and the onSelect spy are shared across every test
    // in this file. vi.mock only sets them up once per file, not once per test.
    // Without this line, one test's mockResolvedValue or recorded calls would
    // carry over into the next test's checks.
    vi.clearAllMocks()
  })

  it('shows a validation error and never calls the geocoder for an empty query', async () => {
    const user = userEvent.setup()
    render(<NearbyWildfireSearch onSelect={onSelect} />)

    await user.click(screen.getByRole('button', { name: 'Search nearby fires' }))

    expect(await screen.findByText('Enter a town or city.')).toBeInTheDocument()
    expect(mockedGeocodeLocation).not.toHaveBeenCalled()
  })

  it('shows a validation error and never calls the geocoder for a non-positive radius', async () => {
    const user = userEvent.setup()
    render(<NearbyWildfireSearch onSelect={onSelect} />)

    await user.type(screen.getByLabelText('Town or city'), 'Ottawa')
    await user.clear(screen.getByLabelText('Radius (km)'))
    await user.type(screen.getByLabelText('Radius (km)'), '0')
    await user.click(screen.getByRole('button', { name: 'Search nearby fires' }))

    expect(
      await screen.findByText('Radius must be greater than 0 km.'),
    ).toBeInTheDocument()
    expect(mockedGeocodeLocation).not.toHaveBeenCalled()
  })

  it('shows a not-found message when the geocoder finds no match, without searching wildfires', async () => {
    mockedGeocodeLocation.mockResolvedValue(null)
    const user = userEvent.setup()
    render(<NearbyWildfireSearch onSelect={onSelect} />)

    await user.type(screen.getByLabelText('Town or city'), 'Nowhereville')
    await user.click(screen.getByRole('button', { name: 'Search nearby fires' }))

    expect(
      await screen.findByText('Location not found in Canada.'),
    ).toBeInTheDocument()
    expect(mockedGetNearbyWildfires).not.toHaveBeenCalled()
  })

  it('renders results after a successful search, using the resolved coordinates and entered radius', async () => {
    mockedGeocodeLocation.mockResolvedValue({
      displayName: 'Ottawa, Ontario, Canada',
      latitude: 45.4215,
      longitude: -75.6972,
    })
    mockedGetNearbyWildfires.mockResolvedValue([
      { wildfire: buildWildfire(), distanceKm: 4.2 },
    ])

    const user = userEvent.setup()
    render(<NearbyWildfireSearch onSelect={onSelect} />)

    await user.type(screen.getByLabelText('Town or city'), 'Ottawa')
    await user.click(screen.getByRole('button', { name: 'Search nearby fires' }))

    expect(await screen.findByText('Near Ottawa, Ontario, Canada')).toBeInTheDocument()
    // The default radius field value (25) flows through to the service call
    // as a number, not the string the TextField stores it as.
    expect(mockedGetNearbyWildfires).toHaveBeenCalledWith(45.4215, -75.6972, 25)

    // The location caption alone doesn't prove a fire card actually rendered.
    // So we also check the formatted details a user is there to see.
    // Area uses the same locale-sensitive toLocaleString() as formatArea's
    // own test (see wildfirePresentation.test.ts), so we check it the same
    // way, by stripping out everything except digits instead of matching an
    // exact string.
    // Distance is kept under 10 km on purpose. formatDistance() only switches
    // to toLocaleString() at 10 km and above. Below that it just uses
    // toFixed(1), which has no locale dependence to worry about.
    expect(screen.getByText('Out of Control')).toBeInTheDocument()
    expect(screen.getByText('4.2 km')).toBeInTheDocument()
    const areaText = screen.getByText(/ha$/)
    expect(areaText.textContent?.replace(/\D/g, '')).toBe('1250')
    expect(screen.getByText('cwfis:test-1')).toBeInTheDocument()
  })

  it('shows a specific message when no wildfires are found nearby', async () => {
    mockedGeocodeLocation.mockResolvedValue({
      displayName: 'Ottawa, Ontario, Canada',
      latitude: 45.4215,
      longitude: -75.6972,
    })
    mockedGetNearbyWildfires.mockResolvedValue([])

    const user = userEvent.setup()
    render(<NearbyWildfireSearch onSelect={onSelect} />)

    await user.type(screen.getByLabelText('Town or city'), 'Ottawa')
    await user.click(screen.getByRole('button', { name: 'Search nearby fires' }))

    expect(
      await screen.findByText('No wildfires were found within this radius.'),
    ).toBeInTheDocument()
  })
})
