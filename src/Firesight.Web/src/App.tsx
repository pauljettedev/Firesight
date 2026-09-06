import { useEffect, useState } from 'react'
import {
  Alert,
  AppBar,
  Box,
  CircularProgress,
  Container,
  Link,
  Stack,
  Toolbar,
  Typography,
} from '@mui/material'
import { WildfireMap } from './components/WildfireMap'
import { getHealth, type HealthStatus } from './services/healthService'
import { getWildfiresWithInitialSync, type Wildfire } from './services/wildfireService'

function App() {
  const [health, setHealth] = useState<HealthStatus | null>(null)
  const [wildfires, setWildfires] = useState<Wildfire[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    Promise.all([getHealth(), getWildfiresWithInitialSync()])
      .then(([healthStatus, wildfireData]) => {
        setHealth(healthStatus)
        setWildfires(wildfireData)
      })
      .catch((err: unknown) => {
        setError(err instanceof Error ? err.message : 'Firesight failed to load')
      })
      .finally(() => setLoading(false))
  }, [])

  return (
    <Box>
      <AppBar position="static">
        <Toolbar>
          <Typography variant="h6" component="h1">
            Firesight AI
          </Typography>
        </Toolbar>
      </AppBar>

      <Container maxWidth="xl" sx={{ py: 3 }}>
        <Stack spacing={2}>
          <Alert severity="warning">
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
            <Typography color="text.secondary">
              {health
                ? `API ${health.status} · Database ${health.database} · ${wildfires.length.toLocaleString()} fires loaded`
                : 'Loading system status…'}
            </Typography>
          </Box>

          {loading && (
            <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
              <CircularProgress />
            </Box>
          )}

          {error && <Alert severity="error">{error}</Alert>}

          {!loading && !error && <WildfireMap wildfires={wildfires} />}
        </Stack>
      </Container>
    </Box>
  )
}

export default App
