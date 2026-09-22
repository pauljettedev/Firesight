import { afterEach } from 'vitest'
import { cleanup } from '@testing-library/react'
import '@testing-library/jest-dom/vitest'

// React Testing Library normally wires this up itself by detecting a global
// `afterEach` — but that global only exists if Vitest's `test.globals` option
// is enabled, and it isn't here (this project uses explicit `import { it }
// from 'vitest'` everywhere instead of ambient test globals). So without this,
// each render stays mounted in jsdom's shared document for the rest of the
// file, and later tests silently see leftover elements from earlier ones.
afterEach(() => {
  cleanup()
})
