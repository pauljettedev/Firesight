import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import {
  formatArea,
  formatRelativeTime,
  formatTimestamp,
  stageOfControlLabel,
} from './wildfirePresentation'

describe('stageOfControlLabel', () => {
  it.each([
    ['OC', 'Out of Control'],
    ['BH', 'Being Held'],
    ['UC', 'Under Control'],
    ['EX', 'Extinguished'],
  ])('maps %s to %s', (code, expected) => {
    expect(stageOfControlLabel(code)).toBe(expected)
  })

  it('matches known codes case-insensitively', () => {
    expect(stageOfControlLabel('oc')).toBe('Out of Control')
  })

  it('falls back to the original code as-given for an unrecognized status', () => {
    // Unlike the known-code path above, the fallback does not re-case the
    // input — it returns whatever was passed in verbatim.
    expect(stageOfControlLabel('zz')).toBe('zz')
  })

  it('falls back to a generic label for an empty code', () => {
    expect(stageOfControlLabel('')).toBe('Unknown status')
  })
})

describe('formatArea', () => {
  it('formats a number with a unit, using whatever grouping the runtime locale applies', () => {
    // toLocaleString() output depends on the runner's locale (comma vs.
    // period vs. space as the thousands separator, or none at all) — same
    // portability trap as formatTimestamp below. Stripping non-digits before
    // comparing still proves grouping ran on the right number, without
    // hardcoding one locale's separator.
    const result = formatArea(1234)
    expect(result.endsWith(' ha')).toBe(true)
    expect(result.replace(/\D/g, '')).toBe('1234')
  })

  it('formats zero explicitly rather than treating it as missing', () => {
    expect(formatArea(0)).toBe('0 ha')
  })

  it('returns a placeholder for null', () => {
    expect(formatArea(null)).toBe('Area unavailable')
  })
})

describe('formatTimestamp', () => {
  it('returns a placeholder for null', () => {
    expect(formatTimestamp(null)).toBe('Not provided')
  })

  it('returns a placeholder for an unparseable string', () => {
    expect(formatTimestamp('not-a-date')).toBe('Invalid timestamp')
  })

  it('formats a valid timestamp instead of falling into an error branch', () => {
    // toLocaleString() output depends on the test runner's locale/timezone,
    // so assert the success path was taken rather than an exact string —
    // an exact match here would be a portability trap across machines/CI.
    const result = formatTimestamp('2026-01-15T12:00:00Z')
    expect(result).not.toBe('Invalid timestamp')
    expect(result).not.toBe('Not provided')
    expect(result.length).toBeGreaterThan(0)
  })
})

describe('formatRelativeTime', () => {
  const now = new Date('2026-01-15T12:00:00Z')

  beforeEach(() => {
    vi.useFakeTimers()
    vi.setSystemTime(now)
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  function isoMinutesFromNow(minutes: number): string {
    return new Date(now.getTime() + minutes * 60_000).toISOString()
  }

  it('returns a placeholder for null', () => {
    expect(formatRelativeTime(null)).toBe('Update time unavailable')
  })

  it('returns a placeholder for an unparseable string', () => {
    expect(formatRelativeTime('not-a-date')).toBe('Update time unavailable')
  })

  it('reports "just now" for a timestamp under a minute old', () => {
    expect(formatRelativeTime(isoMinutesFromNow(-0.5))).toBe('Updated just now')
  })

  it('reports elapsed minutes for a timestamp under an hour old', () => {
    expect(formatRelativeTime(isoMinutesFromNow(-15))).toBe('Updated 15 min ago')
  })

  it('reports elapsed hours for a timestamp under a day old', () => {
    expect(formatRelativeTime(isoMinutesFromNow(-3 * 60))).toBe('Updated 3 hr ago')
  })

  it('reports elapsed days for a timestamp a day or more old', () => {
    expect(formatRelativeTime(isoMinutesFromNow(-2 * 24 * 60))).toBe('Updated 2 d ago')
  })

  it('reports a future time using "in" phrasing', () => {
    expect(formatRelativeTime(isoMinutesFromNow(10))).toBe('Updates in 10 min')
  })
})
