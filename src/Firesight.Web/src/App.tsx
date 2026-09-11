import { useEffect, useMemo, useState } from 'react'
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Container,
  Paper,
  Stack,
  Typography,
} from '@mui/material'
import { AppHeader } from './components/AppHeader'
import { AskFiresightPanel } from './components/AskFiresightPanel'
import { ObservationFreshnessChart } from './components/ObservationFreshnessChart'
import { WildfireContextRail } from './components/WildfireContextRail'
import {
  WildfireMap,
  type WildfireMapFocusArea,
} from './components/WildfireMap'
import type { AskFiresightMapContext } from './services/askFiresightService'
import {
  getNearbyWildfires,
  getWildfires,
  type Wildfire,
} from './services/wildfireService'

interface MapView {
  focusArea: WildfireMapFocusArea
  wildfires: Wildfire[]
  label: string | null
}

function App() {
  const [wildfires, setWildfires] = useState<Wildfire[]>([])
  const [selectedWildfire, setSelectedWildfire] = useState<Wildfire | null>(null)
  const [mapView, setMapView] = useState<MapView | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    getWildfires()
      .then(setWildfires)
      .catch((err: unknown) => {
        setError(err instanceof Error ? err.message : 'Firesight failed to load')
      })
      .finally(() => setLoading(false))
  }, [])

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

  async function handleShowAskResultOnMap(
    context: AskFiresightMapContext,
  ) {
    const nearby = await getNearbyWildfires(
      context.latitude,
      context.longitude,
      context.radiusKm,
    )

    setSelectedWildfire(null)
    setMapView({
      focusArea: {
        latitude: context.latitude,
        longitude: context.longitude,
        radiusKm: context.radiusKm,
      },
      wildfires: nearby.map((result) => result.wildfire),
      label: context.label,
    })
  }

  function handleContextWildfireSelect(wildfire: Wildfire | null) {
    setMapView(null)
    setSelectedWildfire(wildfire)
  }

  const mapWildfires = mapView?.wildfires ?? wildfires

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

          <AskFiresightPanel onShowOnMap={handleShowAskResultOnMap} />

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
                alignItems: 'start',
              }}
            >
              <WildfireContextRail
                recentWildfires={recentWildfires}
                onWildfireSelect={handleContextWildfireSelect}
              />

              <Stack spacing={2} sx={{ minWidth: 0 }}>
                {mapView && (
                  <Paper
                    variant="outlined"
                    sx={{
                      px: 2,
                      py: 1.25,
                      display: 'flex',
                      gap: 2,
                      alignItems: 'center',
                      justifyContent: 'space-between',
                    }}
                  >
                    <Box>
                      <Typography variant="body2" sx={{ fontWeight: 700 }}>
                        AI map view
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        Showing {mapView.wildfires.length.toLocaleString()} fires
                        within {formatRadius(mapView.focusArea.radiusKm)} of{' '}
                        {mapView.label ?? 'the selected location'}.
                      </Typography>
                    </Box>

                    <Button
                      size="small"
                      onClick={() => setMapView(null)}
                      sx={{ whiteSpace: 'nowrap' }}
                    >
                      Show all fires
                    </Button>
                  </Paper>
                )}

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
                    wildfires={mapWildfires}
                    selectedWildfire={selectedWildfire}
                    onWildfireSelect={setSelectedWildfire}
                    focusArea={mapView?.focusArea}
                  />
                </Paper>

                <ObservationFreshnessChart wildfires={wildfires} />
              </Stack>
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

function formatRadius(radiusKm: number): string {
  return `${Math.round(radiusKm).toLocaleString()} km`
}

export default App
