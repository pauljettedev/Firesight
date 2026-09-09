import { useEffect, useState } from 'react'
import {
  Alert,
  AppBar,
  Box,
  CircularProgress,
  Container,
  Divider,
  Link,
  Paper,
  Stack,
  Toolbar,
  Typography,
} from '@mui/material'
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

  return (
    <Box sx={{ minHeight: '100vh' }}>
      <AppBar position="static">
        <Toolbar sx={{ minHeight: 64 }}>
          <Box>
            <Typography
              variant="h6"
              component="h1"
              sx={{ lineHeight: 1.1, textTransform: 'uppercase' }}
            >
              Firesight
            </Typography>
            <Typography
              variant="caption"
              color="text.secondary"
              sx={{ letterSpacing: '0.08em', textTransform: 'uppercase' }}
            >
              Canadian wildfire situational awareness
            </Typography>
          </Box>
        </Toolbar>
      </AppBar>

      <Container
        maxWidth={false}
        sx={{
          px: { xs: 2, md: 2.5 },
          py: { xs: 2, md: 3 },
        }}
      >
        <Stack spacing={2}>
          <Alert
            severity="warning"
            variant="outlined"
            sx={{
              color: 'text.secondary',
              '& .MuiAlert-icon': { color: 'warning.main' },
            }}
          >
            Demo only. CWFIS data is provided for situational awareness and may not reflect
            the most current fire situation. For operational decisions, consult the official{' '}
            <Link href="https://cwfis.cfs.nrcan.gc.ca/" target="_blank" rel="noreferrer">
              Canadian Wildland Fire Information System
            </Link>{' '}
            and the responsible provincial or territorial agency.
          </Alert>

          <Box>
            <Typography variant="h5" component="h2">
              Active wildfires in Canada
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Current and recently observed wildfire records from CWFIS
            </Typography>
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
                  lg: '280px minmax(0, 1fr)',
                },
                gap: 2,
                alignItems: 'stretch',
              }}
            >
              <Paper variant="outlined" sx={{ p: 2.25 }}>
                <Typography
                  variant="overline"
                  color="text.secondary"
                  sx={{ letterSpacing: '0.1em' }}
                >
                  Data status
                </Typography>

                <Stack spacing={2} sx={{ mt: 1 }}>
                  <StatusItem label="Source" value="CWFIS" />

                  <Divider />

                  <StatusItem
                    label="Last successful update"
                    value={lastSuccessfulSync}
                  />

                  <Divider />

                  <StatusItem
                    label="API"
                    value={
                      health
                        ? health.status
                        : healthUnavailable
                          ? 'Unavailable'
                          : 'Checking…'
                    }
                  />

                  <StatusItem
                    label="Database"
                    value={
                      health
                        ? health.database
                        : healthUnavailable
                          ? 'Unavailable'
                          : 'Checking…'
                    }
                  />

                  <Divider />

                  <StatusItem
                    label="Stale observations"
                    value={staleCount.toLocaleString()}
                  />
                </Stack>
              </Paper>

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
                <WildfireMap wildfires={wildfires} />
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

interface StatusItemProps {
  label: string
  value: string
}

function StatusItem({ label, value }: StatusItemProps) {
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
          mt: 0.25,
          fontWeight: 600,
          overflowWrap: 'anywhere',
        }}
      >
        {value}
      </Typography>
    </Box>
  )
}

export default App
