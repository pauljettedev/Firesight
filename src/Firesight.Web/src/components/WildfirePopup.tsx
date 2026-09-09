import './WildfirePopup.css'

interface WildfirePopupProps {
  name: string
  agency: string
  status: string
  areaHectares: number | null
  statusDateUtc: string | null
  lastSeenInFeedUtc: string | null
  isStale: boolean
}

export function WildfirePopup({
  name,
  agency,
  status,
  areaHectares,
  statusDateUtc,
  lastSeenInFeedUtc,
  isStale,
}: WildfirePopupProps) {
  const statusLabel = stageOfControlLabel(status)
  const statusClass = statusToneClass(status)

  return (
    <article className="wildfire-popup-card">
      <header className="wildfire-popup-header">
        <div>
          <div className="wildfire-popup-eyebrow">Wildfire</div>
          <div className="wildfire-popup-title">{name}</div>
        </div>
        <span className={`wildfire-popup-status ${statusClass}`}>
          {statusLabel}
        </span>
      </header>

      <dl className="wildfire-popup-details">
        <Detail label="Agency" value={agency} />
        <Detail
          label="Area"
          value={
            areaHectares != null
              ? `${areaHectares.toLocaleString()} ha`
              : 'Unavailable'
          }
        />
        <Detail
          label="Last seen"
          value={formatTimestamp(lastSeenInFeedUtc)}
        />
        <Detail
          label="Status date"
          value={formatTimestamp(statusDateUtc)}
        />
      </dl>

      <div
        className={`wildfire-popup-freshness ${
          isStale ? 'is-stale' : 'is-recent'
        }`}
      >
        {isStale ? 'Stale observation' : 'Recent observation'}
      </div>
    </article>
  )
}

interface DetailProps {
  label: string
  value: string
}

function Detail({ label, value }: DetailProps) {
  return (
    <div className="wildfire-popup-detail">
      <dt>{label}</dt>
      <dd>{value}</dd>
    </div>
  )
}

function stageOfControlLabel(code: string): string {
  switch (code.toUpperCase()) {
    case 'OC': return 'Out of Control'
    case 'BH': return 'Being Held'
    case 'UC': return 'Under Control'
    case 'EX': return 'Extinguished'
    default: return code || 'Unknown status'
  }
}

function statusToneClass(code: string): string {
  switch (code.toUpperCase()) {
    case 'OC': return 'status-out-of-control'
    case 'BH': return 'status-being-held'
    case 'UC': return 'status-under-control'
    case 'EX': return 'status-extinguished'
    default: return 'status-unknown'
  }
}

function formatTimestamp(value: string | null): string {
  if (!value) {
    return 'Not provided'
  }

  const date = new Date(value)
  if (Number.isNaN(date.getTime())) {
    return 'Invalid timestamp'
  }

  return date.toLocaleString()
}
