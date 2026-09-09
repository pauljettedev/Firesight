import { Box, Paper, Stack, Typography } from '@mui/material'
import type { Wildfire } from '../services/wildfireService'

interface ObservationFreshnessChartProps {
  wildfires: Wildfire[]
}

interface FreshnessBucket {
  label: string
  description: string
  count: number
  color: string
}

export function ObservationFreshnessChart({
  wildfires,
}: ObservationFreshnessChartProps) {
  const buckets = buildBuckets(wildfires)
  const maxCount = Math.max(...buckets.map((bucket) => bucket.count), 1)

  return (
    <Paper variant="outlined" sx={{ p: { xs: 2, md: 2.25 } }}>
      <Box
        sx={{
          display: 'flex',
          flexDirection: { xs: 'column', md: 'row' },
          justifyContent: 'space-between',
          gap: 0.75,
        }}
      >
        <Box>
          <Typography
            variant="overline"
            color="text.secondary"
            sx={{ letterSpacing: '0.1em' }}
          >
            Observation freshness
          </Typography>
          <Typography variant="body2" color="text.secondary">
            How recently each wildfire was observed in the CWFIS feed
          </Typography>
        </Box>

        <Typography
          variant="caption"
          color="text.secondary"
          sx={{ alignSelf: { xs: 'flex-start', md: 'flex-end' } }}
        >
          {wildfires.length.toLocaleString()} observations
        </Typography>
      </Box>

      <Stack spacing={1.35} sx={{ mt: 2 }}>
        {buckets.map((bucket) => {
          const width = bucket.count === 0
            ? 0
            : Math.max((bucket.count / maxCount) * 100, 2)

          return (
            <Box
              key={bucket.label}
              sx={{
                display: 'grid',
                gridTemplateColumns: {
                  xs: '92px minmax(0, 1fr) 44px',
                  sm: '130px minmax(0, 1fr) 56px',
                },
                gap: 1.25,
                alignItems: 'center',
              }}
            >
              <Box>
                <Typography variant="body2" sx={{ fontWeight: 700 }}>
                  {bucket.label}
                </Typography>
                <Typography
                  variant="caption"
                  color="text.secondary"
                  sx={{ display: { xs: 'none', sm: 'block' } }}
                >
                  {bucket.description}
                </Typography>
              </Box>

              <Box
                sx={{
                  height: 10,
                  borderRadius: 999,
                  overflow: 'hidden',
                  backgroundColor: 'rgba(143, 164, 179, 0.12)',
                }}
              >
                <Box
                  sx={{
                    height: '100%',
                    width: `${width}%`,
                    borderRadius: 'inherit',
                    backgroundColor: bucket.color,
                  }}
                />
              </Box>

              <Typography
                variant="body2"
                sx={{
                  textAlign: 'right',
                  fontWeight: 700,
                  fontVariantNumeric: 'tabular-nums',
                }}
              >
                {bucket.count.toLocaleString()}
              </Typography>
            </Box>
          )
        })}
      </Stack>
    </Paper>
  )
}

function buildBuckets(wildfires: Wildfire[]): FreshnessBucket[] {
  const now = Date.now()
  const sixHours = 6 * 60 * 60 * 1000
  const oneDay = 24 * 60 * 60 * 1000

  const counts = {
    sixHours: 0,
    oneDay: 0,
    olderCurrent: 0,
    stale: 0,
  }

  for (const wildfire of wildfires) {
    if (wildfire.isStale) {
      counts.stale += 1
      continue
    }

    const observedAt = Date.parse(wildfire.lastSeenInFeedUtc)
    if (Number.isNaN(observedAt)) {
      counts.olderCurrent += 1
      continue
    }

    const age = Math.max(0, now - observedAt)

    if (age <= sixHours) {
      counts.sixHours += 1
    } else if (age <= oneDay) {
      counts.oneDay += 1
    } else {
      counts.olderCurrent += 1
    }
  }

  return [
    {
      label: '≤ 6 hours',
      description: 'Very recent',
      count: counts.sixHours,
      color: 'primary.main',
    },
    {
      label: '6–24 hours',
      description: 'Recent',
      count: counts.oneDay,
      color: 'success.main',
    },
    {
      label: '> 24 hours',
      description: 'Current, older observation',
      count: counts.olderCurrent,
      color: 'warning.main',
    },
    {
      label: 'Stale',
      description: 'Past configured freshness threshold',
      count: counts.stale,
      color: 'error.main',
    },
  ]
}
