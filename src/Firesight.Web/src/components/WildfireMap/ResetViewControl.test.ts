import { describe, expect, it, vi } from 'vitest'
import { ResetViewControl } from './ResetViewControl'

// The control is plain DOM, so unlike the map itself it can be tested
// without WebGL: build it the way MapLibre does and click its button.
describe('ResetViewControl', () => {
  it('calls onReset when its button is clicked', () => {
    const onReset = vi.fn()
    const container = new ResetViewControl(onReset).onAdd()

    container.querySelector('button')!.click()

    expect(onReset).toHaveBeenCalledOnce()
  })
})
