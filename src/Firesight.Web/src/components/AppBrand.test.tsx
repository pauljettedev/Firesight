import { describe, expect, it } from 'vitest'
import { render, screen } from '@testing-library/react'
import { AppBrand } from './AppBrand'

describe('AppBrand', () => {
  it('renders the Firesight name and tagline', () => {
    render(<AppBrand />)

    expect(
      screen.getByRole('heading', { name: 'Firesight' }),
    ).toBeInTheDocument()
    expect(
      screen.getByText('Canadian wildfire situational awareness'),
    ).toBeInTheDocument()
  })
})
