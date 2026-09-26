import { beforeEach, describe, expect, it, vi } from 'vitest'
import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { AskFiresightPanel } from './AskFiresightPanel'
import {
  askFiresight,
  type AskFiresightResult,
} from '../services/askFiresightService'
import { buildWildfire } from '../test/fixtures'

vi.mock('../services/askFiresightService', () => ({
  askFiresight: vi.fn(),
}))

const mockedAskFiresight = vi.mocked(askFiresight)

const kamloops = {
  latitude: 50.67,
  longitude: -120.33,
  radiusKm: 200,
  label: 'Kamloops, BC',
}

const fireK1 = buildWildfire({ id: 'k1', externalId: '2026_BC_K1' })
const fireK12 = buildWildfire({ id: 'k12', externalId: '2026_BC_K12' })

function answer(overrides: Partial<AskFiresightResult>): AskFiresightResult {
  return {
    answer: 'An answer.',
    toolsUsed: [],
    mapContext: null,
    wildfireExternalIds: [],
    ...overrides,
  }
}

describe('AskFiresightPanel', () => {
  const onShowArea = vi.fn()
  const onShowWildfires = vi.fn()
  const onFocusWildfire = vi.fn()

  beforeEach(() => {
    vi.clearAllMocks()
  })

  async function ask() {
    const user = userEvent.setup()
    render(
      <AskFiresightPanel
        wildfires={[fireK1, fireK12]}
        selectedWildfireId={null}
        onShowArea={onShowArea}
        onShowWildfires={onShowWildfires}
        onFocusWildfire={onFocusWildfire}
      />,
    )
    await user.type(
      screen.getByRole('textbox', { name: 'Ask Firesight question' }),
      'a question',
    )
    await user.click(screen.getByRole('button', { name: 'Ask Firesight' }))
    return user
  }

  it('lists the fires an answer names, in its order, and focuses one when clicked', async () => {
    mockedAskFiresight.mockResolvedValue(
      answer({ wildfireExternalIds: ['2026_BC_K12', '2026_BC_K1'] }),
    )
    const user = await ask()

    const rows = await screen.findAllByRole('button', { name: /2026_BC_K/ })
    expect(rows.map((row) => within(row).getByText(/2026_BC_K/).textContent))
      .toEqual(['2026_BC_K12', '2026_BC_K1'])

    await user.click(rows[0])
    expect(onFocusWildfire).toHaveBeenCalledWith(fireK12)

    await user.click(
      screen.getByRole('button', { name: 'Show all 2 fires on map' }),
    )
    expect(onShowWildfires).toHaveBeenCalledWith([fireK12, fireK1])
  })

  it('offers the fire an answer names, even when it also searched an area', async () => {
    mockedAskFiresight.mockResolvedValue(
      answer({ mapContext: kamloops, wildfireExternalIds: ['2026_BC_K12'] }),
    )
    await ask()

    expect(
      await screen.findByRole('button', { name: 'Show fire on map' }),
    ).toBeInTheDocument()
    expect(
      screen.queryByRole('button', { name: /area on map/ }),
    ).not.toBeInTheDocument()
  })

  it('offers the searched area when the answer names no fires', async () => {
    mockedAskFiresight.mockResolvedValue(answer({ mapContext: kamloops }))
    await ask()

    expect(
      await screen.findByRole('button', { name: /area on map/ }),
    ).toBeInTheDocument()
    expect(
      screen.queryByRole('button', { name: /fires? on map/ }),
    ).not.toBeInTheDocument()
  })

  it('offers no map button when the answer has nothing to show', async () => {
    mockedAskFiresight.mockResolvedValue(answer({}))
    await ask()

    await screen.findByText('An answer.')
    expect(
      screen.queryByRole('button', { name: /on map/ }),
    ).not.toBeInTheDocument()
  })
})
