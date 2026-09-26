import { describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { WildfireContextRail } from './WildfireContextRail'
import { buildWildfire } from '../test/fixtures'

// The rail also renders the Ask AI and Nearby panels, which import these
// services. Mocking them keeps this test from making real network calls.
vi.mock('../services/askFiresightService', () => ({
  askFiresight: vi.fn(),
}))

vi.mock('../services/locationService', () => ({
  geocodeLocation: vi.fn(),
}))

vi.mock('../services/wildfireService', () => ({
  getNearbyWildfires: vi.fn(),
}))

describe('WildfireContextRail', () => {
  it('passes the clicked recent fire to onFocusWildfire', async () => {
    const user = userEvent.setup()
    const onFocusWildfire = vi.fn()
    const first = buildWildfire({ externalId: '2026_NT_FS016-26' })
    const second = buildWildfire({
      id: 'wildfire-2',
      externalId: '2026_NT_FS014-26',
    })

    render(
      <WildfireContextRail
        wildfires={[first, second]}
        recentWildfires={[first, second]}
        selectedWildfireId={null}
        onFocusWildfire={onFocusWildfire}
        onNearbyWildfireSelect={vi.fn()}
        onShowAskAreaOnMap={vi.fn()}
        onShowAskWildfiresOnMap={vi.fn()}
      />,
    )

    await user.click(screen.getByRole('button', { name: 'Recent' }))
    await user.click(screen.getByRole('button', { name: /2026_NT_FS014-26/ }))

    expect(onFocusWildfire).toHaveBeenCalledOnce()
    expect(onFocusWildfire).toHaveBeenCalledWith(second)
  })
})
