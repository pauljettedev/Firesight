import {
  Box,
  Chip,
  Divider,
  Paper,
  Stack,
  Typography,
} from '@mui/material'
import type { ChipProps } from '@mui/material'
import type { Wildfire } from '../services/wildfireService'
import {
  formatArea,
  formatRelativeTime,
  formatTimestamp,
  stageOfControlLabel,
} from '../utils/wildfirePresentation'

interface WildfireContextRailProps {
  selectedWildfire: Wildfire | null
  recentWildfires: Wildfire[]
  lastSuccessfulSync: string
  apiStatus: string
  databaseStatus: string
  staleCount: number
}

export function WildfireContextRail({
  selectedWildfire,
  recentWildfires,
  lastSuccessfulSync,
  apiStatus,
  databaseStatus,
  staleCount,
}: WildfireContextRailProps) {
  return (
    <Paper variant="outlined" sx={{ overflow: 'hidden', minWidth: 0 }}>
      <Box sx={{ p: 2.25 }}>
        {selectedWildfire ? (
          <SelectedWildfire wildfire={selectedWildfire} />
        ) : (
          <RecentlyUpdated wildfires={recentWildfires} />
        )}
      </Box>

      <Divider />

      <Box sx={{ p: 2.25 }}>
        <Typography
          variant="overline"
          color="text.secondary"
          sx={{ letterSpacing: '0.1em' }}
        >
          Data status
        </Typography>

        <Stack spacing={1.75} sx={{ mt: 1 }}>
          <InfoItem label="Source" value="CWFIS" />
          <InfoItem label="Last successful update" value={lastSuccessfulSync} />

          <Box
            sx={{
              display: 'grid',
              gridTemplateColumns: '1fr 1fr',
              gap: 1.5,
            }}
          >
            <InfoItem label="API" value={apiStatus} />
            <InfoItem label="Database" value={databaseStatus} />
          </Box>

          <InfoItem
            label="Stale observations"
            value={staleCount.toLocaleString()}
          />
        </Stack>
      </Box>
    </Paper>
  )
}

function SelectedWildfire({ wildfire }: { wildfire: Wildfire }) {
  const title = wildfire.name ?? `${wildfire.agency} wildfire`

  return (
    <Box>
      <Typography
        variant="overline"
        color="text.secondary"
        sx={{ letterSpacing: '0.1em' }}
      >
        Selected fire
      </Typography>

      <Typography variant="h6" sx={{ mt: 0.5, lineHeight: 1.2 }}>
        {title}
      </Typography>

      <Typography
        variant="caption"
        color="text.secondary"
        sx={{ display: 'block', mt: 0.25 }}
      >
        {wildfire.externalId}
      </Typography>

      <Chip
        label={stageOfControlLabel(wildfire.status)}
        color={statusChipColor(wildfire.status)}
        variant="outlined"
        size="small"
        sx={{ mt: 1.5, fontWeight: 700 }}
      />

      <Stack spacing={1.5} sx={{ mt: 2 }}>
        <InfoItem label="Area" value={formatArea(wildfire.areaHectares)} />
        <InfoItem label="Agency" value={wildfire.agency} />
        <InfoItem
          label="Status updated"
          value={formatTimestamp(wildfire.statusDateUtc)}
        />
        <InfoItem
          label="Last seen in feed"
          value={formatTimestamp(wildfire.lastSeenInFeedUtc)}
        />
        <InfoItem
          label="Freshness"
          value={wildfire.isStale ? 'Stale observation' : 'Recent observation'}
          valueColor={wildfire.isStale ? 'warning.main' : 'success.main'}
        />
      </Stack>
    </Box>
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

function InfoItem({
  label,
  value,
  valueColor = 'text.primary',
}: {
  label: string
  value: string
  valueColor?: string
}) {
  return (
    <Box>
      <Typography
        variant="caption"
        color="text.secondary"
        sx={{ textTransform: 'uppercase', letterSpacing: '0.06em' }}
      >
        {label}
      </Typography>
      <Typography
        variant="body2"
        sx={{
          mt: 0.2,
          fontWeight: 600,
          color: valueColor,
          overflowWrap: 'anywhere',
        }}
      >
        {value}
      </Typography>
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

function statusChipColor(status: string): ChipProps['color'] {
  switch (status.toUpperCase()) {
    case 'OC': return 'error'
    case 'BH': return 'warning'
    case 'UC': return 'success'
    default: return 'default'
  }
}

function statusColor(status: string): string {
  switch (status.toUpperCase()) {
    case 'OC': return 'error.main'
    case 'BH': return 'warning.main'
    case 'UC': return 'success.main'
    default: return 'text.secondary'
  }
}
