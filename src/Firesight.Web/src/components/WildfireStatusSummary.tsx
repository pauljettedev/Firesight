import { Paper } from '@mui/material'
import type { Wildfire } from '../services/wildfireService'
import { SummaryMetric } from './SummaryMetric'

interface WildfireStatusSummaryProps {
  wildfires: Wildfire[]
}

// The row of headline numbers above the map: total fires, then a count
// for each CWFIS stage of control.
export function WildfireStatusSummary({
  wildfires,
}: WildfireStatusSummaryProps) {
  const statusCounts = countByStatus(wildfires)

  return (
    <Paper
      variant="outlined"
      sx={{
        display: 'grid',
        gridTemplateColumns: {
          xs: 'repeat(2, minmax(0, 1fr))',
          md: 'repeat(4, minmax(0, 1fr))',
        },
        overflow: 'hidden',
      }}
    >
      <SummaryMetric
        label="Observed fires"
        value={wildfires.length}
        color="primary.main"
      />
      <SummaryMetric
        label="Out of control"
        value={statusCounts.outOfControl}
        color="error.main"
        divider
      />
      <SummaryMetric
        label="Being held"
        value={statusCounts.beingHeld}
        color="warning.main"
        divider
      />
      <SummaryMetric
        label="Under control"
        value={statusCounts.underControl}
        color="success.main"
        divider
      />
    </Paper>
  )
}

function countByStatus(wildfires: Wildfire[]) {
  return wildfires.reduce(
    (counts, wildfire) => {
      const status = wildfire.status.toUpperCase()

      if (status === 'OC') counts.outOfControl += 1
      if (status === 'BH') counts.beingHeld += 1
      if (status === 'UC') counts.underControl += 1

      return counts
    },
    { outOfControl: 0, beingHeld: 0, underControl: 0 },
  )
}
