import { useEffect, useMemo, useState } from 'react'
import {
  Alert,
  Box,
  CircularProgress,
  Container,
  Paper,
  Stack,
  Typography,
} from '@mui/material'
import { AppHeader } from './components/AppHeader'
import { DemoNotice } from './components/DemoNotice'
import { WildfireContextRail } from './components/WildfireContextRail'
import { WildfireMap } from './components/WildfireMap'
import { getHealth, type HealthStatus } from './services/healthService'
import {
  getWildfires,
  getWildfireSyncState,
  type Wildfire,
  type WildfireFeedSyncState,
} from './services/wildfireService'

function App() {
  const [health, setHealth] = useState<HealthStatus | null>(null)
  const [healthUnavailable, setHealthUnavailable] = useState(false)
  const [wildfires, setWildfires] = useState<Wildfire[]>([])
  const [syncState, setSyncState] = useState<WildfireFeedSyncState | null>(null)
  const [selectedWildfire, setSelectedWildfire] = useState<Wildfire | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    getHealth()
      .then(setHealth)
      .catch(() => setHealthUnavailable(true))

    getWildfireSyncState()
      .then(setSyncState)
      .catch(() => setSyncState(null))

    getWildfires()
      .then(setWildfires)
      .catch((err: unknown) => {
        setError(err instanceof Error ? err.message : 'Firesight failed to load')
      })
      .finally(() => setLoading(false))
  }, [])

  const lastSuccessfulSync = syncState?.lastSuccessfulFetchUtc
    ? new Date(syncState.lastSuccessfulFetchUtc).toLocaleString()
    : 'Not available'

  const statusCounts = wildfires.reduce(
    (counts, wildfire) => {
      const status = wildfire.status.toUpperCase()

      if (status === 'OC') counts.outOfControl += 1
      if (status === 'BH') counts.beingHeld += 1
      if (status === 'UC') counts.underControl += 1

      return counts
    },
    { outOfControl: 0, beingHeld: 0, underControl: 0 },
  )

  const staleCount = wildfires.filter((wildfire) => wildfire.isStale).length

  const recentWildfires = useMemo(
    () =>
      [...wildfires]
        .sort(
          (left, right) =>
            wildfireUpdateTime(right) - wildfireUpdateTime(left),
        )
        .slice(0, 5),
    [wildfires],
  )

  const apiStatus = health
    ? health.status
    : healthUnavailable
      ? 'Unavailable'
      : 'Checking…'

  const databaseStatus = health
    ? health.database
    : healthUnavailable
      ? 'Unavailable'
      : 'Checking…'

  return (
    <Box sx={{ minHeight: '100vh' }}>
      <AppHeader />

      <Container
        maxWidth={false}
        sx={{
          px: { xs: 2, md: 2.5 },
          py: { xs: 2, md: 2.25 },
        }}
      >
        <Stack spacing={2}>
          <Box
            sx={{
              display: 'flex',
              flexDirection: { xs: 'column', md: 'row' },
              alignItems: { xs: 'flex-start', md: 'center' },
              justifyContent: 'space-between',
              gap: 1.25,
            }}
          >
            <Box sx={{ flex: '0 0 auto' }}>
              <Typography variant="h5" component="h2">
                Active wildfires in Canada
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Current and recently observed wildfire records from CWFIS
              </Typography>
            </Box>

            <DemoNotice />
          </Box>

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

          {loading && (
            <Paper
              variant="outlined"
              sx={{ display: 'flex', justifyContent: 'center', py: 10 }}
            >
              <CircularProgress />
            </Paper>
          )}

          {error && <Alert severity="error">{error}</Alert>}

          {!loading && !error && (
            <Box
              sx={{
                display: 'grid',
                gridTemplateColumns: {
                  xs: '1fr',
                  lg: '300px minmax(0, 1fr)',
                },
                gap: 2,
                alignItems: 'stretch',
              }}
            >
              <WildfireContextRail
                selectedWildfire={selectedWildfire}
                recentWildfires={recentWildfires}
                lastSuccessfulSync={lastSuccessfulSync}
                apiStatus={apiStatus}
                databaseStatus={databaseStatus}
                staleCount={staleCount}
              />

              <Paper
                variant="outlined"
                sx={{
                  overflow: 'hidden',
                  minWidth: 0,
                  '& > div': {
                    borderRadius: 0,
                  },
                }}
              >
                <WildfireMap
                  wildfires={wildfires}
                  onWildfireSelect={setSelectedWildfire}
                />
              </Paper>
            </Box>
          )}
        </Stack>
      </Container>
    </Box>
  )
}

interface SummaryMetricProps {
  label: string
  value: number
  color: string
  divider?: boolean
}

function SummaryMetric({
  label,
  value,
  color,
  divider = false,
}: SummaryMetricProps) {
  return (
    <Box
      sx={{
        px: { xs: 2, md: 2.5 },
        py: 1.75,
        borderLeft: {
          xs: 'none',
          md: divider ? '1px solid' : 'none',
        },
        borderColor: 'divider',
      }}
    >
      <Typography
        variant="caption"
        color="text.secondary"
        sx={{ textTransform: 'uppercase', letterSpacing: '0.08em' }}
      >
        {label}
      </Typography>
      <Typography
        variant="h5"
        sx={{
          mt: 0.25,
          color,
          fontVariantNumeric: 'tabular-nums',
        }}
      >
        {value.toLocaleString()}
      </Typography>
    </Box>
  )
}

function wildfireUpdateTime(wildfire: Wildfire): number {
  const value = wildfire.statusDateUtc ?? wildfire.lastSeenInFeedUtc
  const timestamp = Date.parse(value)

  return Number.isNaN(timestamp) ? 0 : timestamp
}

export default App
