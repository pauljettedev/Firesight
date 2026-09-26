import { afterEach, describe, expect, it, vi } from 'vitest'
import { requestJson } from './http'

function respondWith(status: number, body?: unknown) {
  vi.stubGlobal(
    'fetch',
    vi.fn().mockResolvedValue(
      new Response(body === undefined ? null : JSON.stringify(body), { status }),
    ),
  )
}

describe('requestJson', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('returns the JSON body of a successful response', async () => {
    respondWith(200, { ok: true })

    await expect(requestJson('/api/x', 'X failed')).resolves.toEqual({ ok: true })
  })

  it('returns null for a status listed in nullWhen', async () => {
    respondWith(404)

    await expect(
      requestJson('/api/x', 'X failed', { nullWhen: [404] }),
    ).resolves.toBeNull()
  })

  it("uses the API's validation messages when a request fails", async () => {
    respondWith(400, {
      title: 'One or more validation errors occurred.',
      errors: { radiusKm: ['Radius must be at most 1000 km.'] },
    })

    await expect(requestJson('/api/x', 'X failed')).rejects.toThrow(
      'Radius must be at most 1000 km.',
    )
  })

  it('falls back to what failed and the status when there is no usable body', async () => {
    respondWith(500)

    await expect(requestJson('/api/x', 'X failed')).rejects.toThrow(
      'X failed: 500',
    )
  })
})
