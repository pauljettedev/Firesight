export function stageOfControlLabel(code: string): string {
  switch (code.toUpperCase()) {
    case 'OC': return 'Out of Control'
    case 'BH': return 'Being Held'
    case 'UC': return 'Under Control'
    case 'EX': return 'Extinguished'
    default: return code || 'Unknown status'
  }
}

export function formatArea(areaHectares: number | null): string {
  return areaHectares != null
    ? `${areaHectares.toLocaleString()} ha`
    : 'Area unavailable'
}

export function formatTimestamp(value: string | null): string {
  if (!value) {
    return 'Not provided'
  }

  const date = new Date(value)
  if (Number.isNaN(date.getTime())) {
    return 'Invalid timestamp'
  }

  return date.toLocaleString()
}

export function formatRelativeTime(value: string | null): string {
  if (!value) {
    return 'Update time unavailable'
  }

  const date = new Date(value)
  const milliseconds = Date.now() - date.getTime()

  if (Number.isNaN(milliseconds)) {
    return 'Update time unavailable'
  }

  const future = milliseconds < 0
  const elapsed = Math.abs(milliseconds)
  const minutes = Math.floor(elapsed / 60_000)

  if (minutes < 1) {
    return future ? 'Updates shortly' : 'Updated just now'
  }

  if (minutes < 60) {
    return future
      ? `Updates in ${minutes} min`
      : `Updated ${minutes} min ago`
  }

  const hours = Math.floor(minutes / 60)
  if (hours < 24) {
    return future
      ? `Updates in ${hours} hr`
      : `Updated ${hours} hr ago`
  }

  const days = Math.floor(hours / 24)
  return future
    ? `Updates in ${days} d`
    : `Updated ${days} d ago`
}
