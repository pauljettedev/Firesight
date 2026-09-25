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
  it('passes the clicked recent fire to onRecentWildfireSelect', async () => {
    const user = userEvent.setup()
    const onRecentWildfireSelect = vi.fn()
    const first = buildWildfire({ externalId: '2026_NT_FS016-26' })
    const second = buildWildfire({
      id: 'wildfire-2',
      externalId: '2026_NT_FS014-26',
    })

    render(
      <WildfireContextRail
        recentWildfires={[first, second]}
        selectedWildfireId={null}
        onRecentWildfireSelect={onRecentWildfireSelect}
        onNearbyWildfireSelect={vi.fn()}
        onShowAskResultOnMap={vi.fn()}
      />,
    )

    await user.click(screen.getByRole('button', { name: 'Recent' }))
    await user.click(screen.getByRole('button', { name: /2026_NT_FS014-26/ }))

    expect(onRecentWildfireSelect).toHaveBeenCalledOnce()
    expect(onRecentWildfireSelect).toHaveBeenCalledWith(second)
  })
})
