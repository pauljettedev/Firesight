import type { ReactNode } from 'react'
import { Box, Button, Typography } from '@mui/material'
import type { Wildfire } from '../services/wildfireService'
import {
  formatArea,
  formatRelativeTime,
  stageOfControlLabel,
} from '../utils/wildfirePresentation'

interface WildfireListItemProps {
  wildfire: Wildfire
  // Which timestamp to show as "Updated ...". Lists differ on purpose:
  // Recent shows the same update time it sorts by (wildfireUpdatedUtc),
  // Nearby shows when the fire was last seen in the feed.
  updatedUtc: string | null
  // Shown at the top right of the row, e.g. a status dot or a distance.
  trailing?: ReactNode
  selected?: boolean
  onSelect: (wildfire: Wildfire) => void
}

export function WildfireListItem({
  wildfire,
  updatedUtc,
  trailing,
  selected = false,
  onSelect,
}: WildfireListItemProps) {
  return (
    <Button
      onClick={() => onSelect(wildfire)}
      color="inherit"
      // aria-current, not aria-pressed: this marks the current item in a
      // list, it isn't a toggle (clicking it again doesn't deselect it).
      aria-current={selected ? 'true' : undefined}
      sx={{
        display: 'block',
        width: '100%',
        px: 1,
        py: 1.25,
        borderRadius: 0,
        textAlign: 'left',
        textTransform: 'none',
        // A thin accent bar marks the fire currently selected on the map.
        // Keeping the same left padding whether selected or not stops the
        // text from shifting sideways when the bar appears.
        borderLeft: '3px solid',
        borderLeftColor: selected ? 'primary.main' : 'transparent',
        backgroundColor: selected ? 'action.selected' : 'transparent',
      }}
    >
      <Box
        sx={{
          display: 'flex',
          gap: 1,
          alignItems: 'center',
          justifyContent: 'space-between',
        }}
      >
        <Typography variant="body2" sx={{ fontWeight: 700, lineHeight: 1.25 }}>
          {stageOfControlLabel(wildfire.status)}
        </Typography>
        {trailing}
      </Box>

      <Typography variant="body2" sx={{ mt: 0.35 }}>
        {formatArea(wildfire.areaHectares)}
      </Typography>

      <Typography
        variant="caption"
        color="text.secondary"
        sx={{ display: 'block', mt: 0.35 }}
      >
        {wildfire.agency} · {formatRelativeTime(updatedUtc)}
      </Typography>

      <Typography
        variant="caption"
        color="text.secondary"
        sx={{ display: 'block', mt: 0.15, opacity: 0.72 }}
      >
        {wildfire.externalId}
      </Typography>
    </Button>
  )
}
