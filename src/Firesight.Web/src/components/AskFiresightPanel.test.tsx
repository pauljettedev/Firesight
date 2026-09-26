import { beforeEach, describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { AskFiresightPanel } from './AskFiresightPanel'
import {
  askFiresight,
  type AskFiresightResult,
} from '../services/askFiresightService'

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
  const onShowOnMap = vi.fn()
  const onShowWildfiresOnMap = vi.fn()

  beforeEach(() => {
    vi.clearAllMocks()
  })

  async function ask() {
    const user = userEvent.setup()
    render(
      <AskFiresightPanel
        onShowOnMap={onShowOnMap}
        onShowWildfiresOnMap={onShowWildfiresOnMap}
      />,
    )
    await user.type(
      screen.getByRole('textbox', { name: 'Ask Firesight question' }),
      'a question',
    )
    await user.click(screen.getByRole('button', { name: 'Ask Firesight' }))
    return user
  }

  it('offers the fire an answer names, even when it also searched an area', async () => {
    mockedAskFiresight.mockResolvedValue(
      answer({ mapContext: kamloops, wildfireExternalIds: ['2026_BC_K12'] }),
    )
    const user = await ask()

    await user.click(
      await screen.findByRole('button', { name: 'Show fire on map' }),
    )

    expect(onShowWildfiresOnMap).toHaveBeenCalledWith(['2026_BC_K12'])
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
