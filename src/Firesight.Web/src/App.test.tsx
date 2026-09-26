import { beforeEach, describe, expect, it, vi } from 'vitest'
import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import App from './App'
import { askFiresight } from './services/askFiresightService'
import { getWildfires, type Wildfire } from './services/wildfireService'
import { buildWildfire, hoursAgoUtc } from './test/fixtures'
import type { MapFocusArea } from './utils/mapView'

// MapLibre needs WebGL, which jsdom doesn't have. The fake map stands in for
// it and just prints the props App gives it, which is exactly what these
// tests care about: which fires the map is told to show, and where to focus.
vi.mock('./components/WildfireMap', () => ({
  WildfireMap: ({
    wildfires,
    selectedWildfire,
    focusArea,
    onReset,
  }: {
    wildfires: Wildfire[]
    selectedWildfire?: Wildfire | null
    focusArea?: MapFocusArea
    onReset?: () => void
  }) => (
    <div data-testid="map">
      <button onClick={onReset}>Show all fires</button>
      <span data-testid="map-fire-ids">
        {wildfires.map((wildfire) => wildfire.id).join(',')}
      </span>
      <span data-testid="map-selected-id">{selectedWildfire?.id ?? ''}</span>
      <span data-testid="map-focus">
        {focusArea ? `${focusArea.latitude},${focusArea.longitude}` : ''}
      </span>
    </div>
  ),
}))

vi.mock('./services/wildfireService', () => ({
  getWildfires: vi.fn(),
  getNearbyWildfires: vi.fn(),
}))

vi.mock('./services/askFiresightService', () => ({
  askFiresight: vi.fn(),
}))

vi.mock('./services/locationService', () => ({
  geocodeLocation: vi.fn(),
}))

const mockedGetWildfires = vi.mocked(getWildfires)
const mockedAskFiresight = vi.mocked(askFiresight)

// Updated 1, 2 and 3 hours ago, so the Recent tab lists them in this order.
// Each sits somewhere different so the focus assertions can tell them apart.
const fires = [
  buildWildfire({
    id: 'fire-a',
    externalId: '2026_NT_A',
    latitude: 61,
    longitude: -118,
    statusDateUtc: hoursAgoUtc(1),
  }),
  buildWildfire({
    id: 'fire-b',
    externalId: '2026_NT_B',
    latitude: 62,
    longitude: -119,
    statusDateUtc: hoursAgoUtc(2),
  }),
  buildWildfire({
    id: 'fire-c',
    externalId: '2026_NT_C',
    latitude: 63,
    longitude: -120,
    statusDateUtc: hoursAgoUtc(3),
  }),
]

describe('App', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mockedGetWildfires.mockResolvedValue(fires)
  })

  it('narrows the map to one fire when it is picked from the Recent tab, and restores all fires after', async () => {
    const user = userEvent.setup()
    render(<App />)

    const mapFireIds = await screen.findByTestId('map-fire-ids')
    expect(mapFireIds).toHaveTextContent('fire-a,fire-b,fire-c')

    await user.click(screen.getByRole('button', { name: 'Recent' }))
    const row = screen.getByRole('button', { name: /2026_NT_B/ })
    await user.click(row)

    // Only the picked fire is on the map, it's selected, and the camera is
    // told to focus on it.
    expect(mapFireIds).toHaveTextContent(/^fire-b$/)
    expect(screen.getByTestId('map-selected-id')).toHaveTextContent('fire-b')
    expect(screen.getByTestId('map-focus')).toHaveTextContent(
      `${fires[1].latitude},${fires[1].longitude}`,
    )
    expect(row).toHaveAttribute('aria-current', 'true')

    const banner = screen.getByRole('status', { name: 'Map view' })
    expect(within(banner).getByText('Single fire view')).toBeInTheDocument()
    expect(within(banner).getByText(/2026_NT_B/)).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Show all fires' }))

    // A full reset: every fire, nothing focused, nothing selected.
    expect(mapFireIds).toHaveTextContent('fire-a,fire-b,fire-c')
    expect(screen.getByTestId('map-focus')).toBeEmptyDOMElement()
    expect(screen.getByTestId('map-selected-id')).toBeEmptyDOMElement()
    expect(row).not.toHaveAttribute('aria-current')
    expect(
      screen.queryByRole('status', { name: 'Map view' }),
    ).not.toBeInTheDocument()
  })

  it('shows the fires an Ask AI answer names on the map', async () => {
    mockedAskFiresight.mockResolvedValue({
      answer: 'The largest are 2026_NT_C and 2026_NT_A.',
      toolsUsed: ['get_active_wildfires'],
      mapContext: null,
      wildfireExternalIds: ['2026_NT_C', '2026_NT_A'],
    })
    const user = userEvent.setup()
    render(<App />)

    const mapFireIds = await screen.findByTestId('map-fire-ids')
    await user.type(
      screen.getByRole('textbox', { name: 'Ask Firesight question' }),
      'largest fires',
    )
    await user.click(screen.getByRole('button', { name: 'Ask Firesight' }))
    await user.click(
      await screen.findByRole('button', { name: 'Show 2 fires on map' }),
    )

    // Only the named fires, with details from the loaded fire data.
    expect(mapFireIds).toHaveTextContent(/^fire-a,fire-c$/)
    expect(screen.getByTestId('map-selected-id')).toBeEmptyDOMElement()
    const banner = screen.getByRole('status', { name: 'Map view' })
    expect(within(banner).getByText('AI map view')).toBeInTheDocument()
  })
})
