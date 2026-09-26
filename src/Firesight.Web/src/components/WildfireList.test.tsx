import { describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import { WildfireList } from './WildfireList'
import { buildWildfire } from '../test/fixtures'

function fires(count: number) {
  return Array.from({ length: count }, (_, index) =>
    buildWildfire({ id: `fire-${index}`, externalId: `FIRE_${index}` }),
  )
}

describe('WildfireList', () => {
  it('shows only up to the limit, and says how many there are', () => {
    render(
      <WildfireList
        wildfires={fires(14)}
        selectedWildfireId={null}
        onSelect={vi.fn()}
        limit={12}
      />,
    )

    expect(screen.getAllByRole('button')).toHaveLength(12)
    expect(screen.getByText('Showing the first 12 of 14.')).toBeInTheDocument()
  })

  it('shows every fire and no note when there are fewer than the limit', () => {
    render(
      <WildfireList
        wildfires={fires(3)}
        selectedWildfireId={null}
        onSelect={vi.fn()}
        limit={12}
      />,
    )

    expect(screen.getAllByRole('button')).toHaveLength(3)
    expect(screen.queryByText(/Showing the first/)).not.toBeInTheDocument()
  })
})
