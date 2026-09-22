import { afterEach } from 'vitest'
import { cleanup } from '@testing-library/react'
import '@testing-library/jest-dom/vitest'

// React Testing Library normally cleans this up itself, by looking for a
// global `afterEach` function. That only exists if Vitest's `test.globals`
// option is on, which it isn't here. Without this file, each rendered
// component would stay mounted, and the next test would silently see
// leftover elements from the one before it.
afterEach(() => {
  cleanup()
})
