import { useState } from 'react'
import {
  Box,
  Button,
  ButtonGroup,
  Divider,
  Paper,
  Stack,
  Typography,
} from '@mui/material'
import type { Wildfire } from '../services/wildfireService'
import {
  formatArea,
  formatRelativeTime,
  stageOfControlLabel,
} from '../utils/wildfirePresentation'
import { NearbyWildfireSearch } from './NearbyWildfireSearch'

type RailMode = 'recent' | 'nearby'

interface WildfireContextRailProps {
  recentWildfires: Wildfire[]
  onWildfireSelect: (wildfire: Wildfire | null) => void
}

export function WildfireContextRail({
  recentWildfires,
  onWildfireSelect,
}: WildfireContextRailProps) {
  const [mode, setMode] = useState<RailMode>('recent')

  return (
    <Paper variant="outlined" sx={{ overflow: 'hidden', minWidth: 0 }}>
      <Box sx={{ p: 1.25, pb: 0 }}>
        <ButtonGroup
          size="small"
          fullWidth
          variant="outlined"
          aria-label="Wildfire context"
        >
          <Button
            variant={mode === 'recent' ? 'contained' : 'outlined'}
            onClick={() => setMode('recent')}
          >
            Recent
          </Button>
          <Button
            variant={mode === 'nearby' ? 'contained' : 'outlined'}
            onClick={() => setMode('nearby')}
          >
            Nearby
          </Button>
        </ButtonGroup>
      </Box>

      <Box sx={{ p: 2.25 }}>
        {mode === 'nearby' ? (
          <NearbyWildfireSearch onSelect={onWildfireSelect} />
        ) : (
          <RecentlyUpdated wildfires={recentWildfires} />
        )}
      </Box>
    </Paper>
  )
}

function RecentlyUpdated({ wildfires }: { wildfires: Wildfire[] }) {
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
          <Box key={wildfire.id} sx={{ py: 1.25 }}>
            <Box
              sx={{
                display: 'flex',
                gap: 1,
                alignItems: 'center',
                justifyContent: 'space-between',
              }}
            >
              <Typography
                variant="body2"
                sx={{ fontWeight: 700, lineHeight: 1.25 }}
              >
                {stageOfControlLabel(wildfire.status)}
              </Typography>
              <StatusDot status={wildfire.status} />
            </Box>

            <Typography variant="body2" sx={{ mt: 0.35 }}>
              {formatArea(wildfire.areaHectares)}
            </Typography>

            <Typography
              variant="caption"
              color="text.secondary"
              sx={{ display: 'block', mt: 0.35 }}
            >
              {wildfire.agency} · {formatRelativeTime(wildfire.statusDateUtc)}
            </Typography>

            <Typography
              variant="caption"
              color="text.secondary"
              sx={{ display: 'block', mt: 0.15, opacity: 0.72 }}
            >
              {wildfire.externalId}
            </Typography>
          </Box>
        ))}
      </Stack>
    </Box>
  )
}

function StatusDot({ status }: { status: string }) {
  return (
    <Box
      aria-hidden
      sx={{
        width: 8,
        height: 8,
        flex: '0 0 auto',
        borderRadius: '50%',
        backgroundColor: statusColor(status),
      }}
    />
  )
}

function statusColor(status: string): string {
  switch (status.toUpperCase()) {
    case 'OC': return 'error.main'
    case 'BH': return 'warning.main'
    case 'UC': return 'success.main'
    default: return 'text.secondary'
  }
}
