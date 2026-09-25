import { Box, Divider, Stack, Typography } from '@mui/material'
import type { Wildfire } from '../services/wildfireService'
import { wildfireUpdatedUtc } from '../utils/wildfirePresentation'
import { StatusDot } from './StatusDot'
import { WildfireListItem } from './WildfireListItem'

interface RecentWildfireListProps {
  wildfires: Wildfire[]
  selectedWildfireId: string | null
  onSelect: (wildfire: Wildfire) => void
}

export function RecentWildfireList({
  wildfires,
  selectedWildfireId,
  onSelect,
}: RecentWildfireListProps) {
  return (
    <Box>
      <Typography
        variant="overline"
        color="text.secondary"
        sx={{ letterSpacing: '0.1em' }}
      >
        Recently updated
      </Typography>

      <Typography variant="body2" color="text.secondary" sx={{ mt: 0.25 }}>
        Latest source updates in the current dataset
      </Typography>

      <Stack divider={<Divider flexItem />} sx={{ mt: 1.5 }}>
        {wildfires.map((wildfire) => (
          <WildfireListItem
            key={wildfire.id}
            wildfire={wildfire}
            updatedUtc={wildfireUpdatedUtc(wildfire)}
            trailing={<StatusDot status={wildfire.status} />}
            selected={wildfire.id === selectedWildfireId}
            onSelect={onSelect}
          />
        ))}
      </Stack>
    </Box>
  )
}
