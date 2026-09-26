import type { ReactNode } from 'react'
import { Divider, Stack, Typography, type SxProps, type Theme } from '@mui/material'
import type { Wildfire } from '../services/wildfireService'
import { wildfireUpdatedUtc } from '../utils/wildfirePresentation'
import { StatusDot } from './StatusDot'
import { WildfireListItem } from './WildfireListItem'

// How many fires a list in the side panel shows before it stops. Enough to be
// useful, short enough not to bury everything below it.
export const MaxListedWildfires = 12

interface WildfireListProps {
  wildfires: Wildfire[]
  selectedWildfireId: string | null
  onSelect: (wildfire: Wildfire) => void
  // Which timestamp each row shows as "Updated ...". Defaults to the fire's
  // latest update time.
  updatedUtc?: (wildfire: Wildfire) => string | null
  // What sits at the top right of each row. Defaults to a status dot.
  trailing?: (wildfire: Wildfire) => ReactNode
  // Show at most this many rows, with a note saying how many there are.
  limit?: number
  sx?: SxProps<Theme>
}

// A clickable list of fires. Every fire list in the app uses this, so the
// rows look and behave the same everywhere.
export function WildfireList({
  wildfires,
  selectedWildfireId,
  onSelect,
  updatedUtc = wildfireUpdatedUtc,
  trailing = (wildfire) => <StatusDot status={wildfire.status} />,
  limit,
  sx,
}: WildfireListProps) {
  const shown = limit === undefined ? wildfires : wildfires.slice(0, limit)

  return (
    <>
      <Stack divider={<Divider flexItem />} sx={sx}>
        {shown.map((wildfire) => (
          <WildfireListItem
            key={wildfire.id}
            wildfire={wildfire}
            updatedUtc={updatedUtc(wildfire)}
            trailing={trailing(wildfire)}
            selected={wildfire.id === selectedWildfireId}
            onSelect={onSelect}
          />
        ))}
      </Stack>

      {shown.length < wildfires.length && (
        <Typography
          variant="caption"
          color="text.secondary"
          sx={{ display: 'block', mt: 1 }}
        >
          Showing the first {shown.length} of {wildfires.length.toLocaleString()}.
        </Typography>
      )}
    </>
  )
}
