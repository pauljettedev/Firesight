import { useEffect, useMemo, useState } from 'react'
import {
  Alert,
  Box,
  CircularProgress,
  Container,
  Paper,
  Stack,
} from '@mui/material'
import { AppHeader } from './components/AppHeader'
import { MapViewBanner } from './components/MapViewBanner'
import { ObservationFreshnessChart } from './components/ObservationFreshnessChart'
import { WildfireContextRail } from './components/WildfireContextRail'
import { WildfireMap } from './components/WildfireMap'
import { WildfireStatusSummary } from './components/WildfireStatusSummary'
import { getWildfires, type Wildfire } from './services/wildfireService'
import { useMapState } from './state/useMapState'
import { wildfiresInView } from './utils/mapView'
import { wildfireUpdatedUtc } from './utils/wildfirePresentation'

function App() {
  const [wildfires, setWildfires] = useState<Wildfire[]>([])
  const mapState = useMapState()
  const { mapView, selectedWildfire } = mapState
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

  // Memoized so the map only gets a new array when the view or data really
  // changes. Without it, the single-fire case would build a fresh one-item
  // array on every render (typing in the Ask box, say), and the map would
  // re-send its data to MapLibre each time.
  const mapWildfires = useMemo(
    () => wildfiresInView(mapView, wildfires),
    [mapView, wildfires],
  )

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
          <WildfireStatusSummary wildfires={wildfires} />

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
                selectedWildfireId={selectedWildfire?.id ?? null}
                onRecentWildfireSelect={mapState.focusWildfire}
                onNearbyWildfireSelect={mapState.selectWildfire}
                onShowAskResultOnMap={mapState.showAskResultOnMap}
              />

              <Stack spacing={2} sx={{ minWidth: 0 }}>
                {mapView && (
                  <MapViewBanner
                    mapView={mapView}
                    onShowAll={mapState.showAll}
                  />
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
                    onWildfireSelect={mapState.changeMapSelection}
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

function wildfireUpdateTime(wildfire: Wildfire): number {
  const timestamp = Date.parse(wildfireUpdatedUtc(wildfire))

  return Number.isNaN(timestamp) ? 0 : timestamp
}

export default App
