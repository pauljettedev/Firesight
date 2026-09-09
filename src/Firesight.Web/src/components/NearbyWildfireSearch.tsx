import { useState } from 'react'
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Divider,
  Link,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import { geocodeLocation } from '../services/locationService'
import {
  getNearbyWildfires,
  type NearbyWildfire,
  type Wildfire,
} from '../services/wildfireService'
import {
  formatArea,
  formatRelativeTime,
  stageOfControlLabel,
} from '../utils/wildfirePresentation'

interface NearbyWildfireSearchProps {
  onSelect: (wildfire: Wildfire) => void
}

export function NearbyWildfireSearch({
  onSelect,
}: NearbyWildfireSearchProps) {
  const [locationQuery, setLocationQuery] = useState('')
  const [radiusKm, setRadiusKm] = useState('25')
  const [resolvedLocation, setResolvedLocation] = useState<string | null>(null)
  const [results, setResults] = useState<NearbyWildfire[] | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function handleSearch() {
    const query = locationQuery.trim()
    const parsedRadiusKm = Number(radiusKm)

    if (!query) {
      setError('Enter a town or city.')
      return
    }

    if (!Number.isFinite(parsedRadiusKm) || parsedRadiusKm <= 0) {
      setError('Radius must be greater than 0 km.')
      return
    }

    setLoading(true)
    setError(null)

    try {
      const location = await geocodeLocation(query)

      if (!location) {
        setResolvedLocation(null)
        setResults(null)
        setError('Location not found in Canada.')
        return
      }

      const nearbyWildfires = await getNearbyWildfires(
        location.latitude,
        location.longitude,
        parsedRadiusKm,
      )

      setResolvedLocation(location.displayName)
      setResults(nearbyWildfires)
    } catch (err: unknown) {
      setError(
        err instanceof Error
          ? err.message
          : 'Nearby wildfire search failed.',
      )
    } finally {
      setLoading(false)
    }
  }

  return (
    <Box>
      <Typography
        variant="overline"
        color="text.secondary"
        sx={{ letterSpacing: '0.1em' }}
      >
        Nearby fires
      </Typography>

      <Typography variant="body2" color="text.secondary" sx={{ mt: 0.25 }}>
        Search for wildfires near a Canadian town or city.
      </Typography>

      <Stack spacing={1.25} sx={{ mt: 1.5 }}>
        <TextField
          label="Town or city"
          placeholder="Ottawa, ON"
          value={locationQuery}
          onChange={(event) => setLocationQuery(event.target.value)}
          onKeyDown={(event) => {
            if (event.key === 'Enter' && !loading) {
              void handleSearch()
            }
          }}
          size="small"
          autoComplete="off"
        />

        <TextField
          label="Radius (km)"
          value={radiusKm}
          onChange={(event) => setRadiusKm(event.target.value)}
          size="small"
          type="number"
          slotProps={{
            htmlInput: {
              min: 1,
              step: 1,
            },
          }}
        />

        <Button
          variant="contained"
          onClick={() => void handleSearch()}
          disabled={loading}
          fullWidth
        >
          {loading ? (
            <CircularProgress size={20} color="inherit" />
          ) : (
            'Search nearby fires'
          )}
        </Button>

        {error && <Alert severity="error">{error}</Alert>}

        <Typography variant="caption" color="text.secondary">
          Location search ©{' '}
          <Link
            href="https://www.openstreetmap.org/copyright"
            target="_blank"
            rel="noreferrer"
          >
            OpenStreetMap contributors
          </Link>
        </Typography>
      </Stack>

      {results && (
        <Box sx={{ mt: 2 }}>
          {resolvedLocation && (
            <Typography
              variant="caption"
              color="text.secondary"
              sx={{ display: 'block', mb: 0.5 }}
            >
              Near {resolvedLocation}
            </Typography>
          )}

          <Typography variant="body2" sx={{ fontWeight: 700 }}>
            {results.length.toLocaleString()} fires found
          </Typography>

          {results.length === 0 ? (
            <Typography
              variant="body2"
              color="text.secondary"
              sx={{ mt: 1 }}
            >
              No wildfires were found within this radius.
            </Typography>
          ) : (
            <Stack divider={<Divider flexItem />} sx={{ mt: 0.75 }}>
              {results.slice(0, 12).map((result) => (
                <Button
                  key={result.wildfire.id}
                  onClick={() => onSelect(result.wildfire)}
                  color="inherit"
                  sx={{
                    display: 'block',
                    width: '100%',
                    px: 0,
                    py: 1.25,
                    borderRadius: 0,
                    textAlign: 'left',
                    textTransform: 'none',
                  }}
                >
                  <Box
                    sx={{
                      display: 'flex',
                      justifyContent: 'space-between',
                      gap: 1,
                      alignItems: 'baseline',
                    }}
                  >
                    <Typography
                      variant="body2"
                      sx={{ fontWeight: 700 }}
                    >
                      {stageOfControlLabel(result.wildfire.status)}
                    </Typography>
                    <Typography
                      variant="caption"
                      color="primary.main"
                      sx={{ fontWeight: 700, whiteSpace: 'nowrap' }}
                    >
                      {formatDistance(result.distanceKm)}
                    </Typography>
                  </Box>

                  <Typography variant="body2" sx={{ mt: 0.25 }}>
                    {formatArea(result.wildfire.areaHectares)}
                  </Typography>

                  <Typography
                    variant="caption"
                    color="text.secondary"
                    sx={{ display: 'block', mt: 0.25 }}
                  >
                    {result.wildfire.agency} ·{' '}
                    {formatRelativeTime(result.wildfire.lastSeenInFeedUtc)}
                  </Typography>

                  <Typography
                    variant="caption"
                    color="text.secondary"
                    sx={{ display: 'block', mt: 0.1, opacity: 0.72 }}
                  >
                    {result.wildfire.externalId}
                  </Typography>
                </Button>
              ))}
            </Stack>
          )}

          {results.length > 12 && (
            <Typography
              variant="caption"
              color="text.secondary"
              sx={{ display: 'block', mt: 1 }}
            >
              Showing the 12 nearest results.
            </Typography>
          )}
        </Box>
      )}
    </Box>
  )
}

function formatDistance(distanceKm: number): string {
  if (distanceKm < 10) {
    return `${distanceKm.toFixed(1)} km`
  }

  return `${Math.round(distanceKm).toLocaleString()} km`
}
