import { useState } from 'react'
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Divider,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
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
  const [latitude, setLatitude] = useState('45.4215')
  const [longitude, setLongitude] = useState('-75.6972')
  const [radiusKm, setRadiusKm] = useState('25')
  const [results, setResults] = useState<NearbyWildfire[] | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function handleSearch() {
    const parsedLatitude = Number(latitude)
    const parsedLongitude = Number(longitude)
    const parsedRadiusKm = Number(radiusKm)

    if (
      !Number.isFinite(parsedLatitude) ||
      parsedLatitude < -90 ||
      parsedLatitude > 90
    ) {
      setError('Latitude must be between -90 and 90.')
      return
    }

    if (
      !Number.isFinite(parsedLongitude) ||
      parsedLongitude < -180 ||
      parsedLongitude > 180
    ) {
      setError('Longitude must be between -180 and 180.')
      return
    }

    if (!Number.isFinite(parsedRadiusKm) || parsedRadiusKm <= 0) {
      setError('Radius must be greater than 0 km.')
      return
    }

    setLoading(true)
    setError(null)

    try {
      setResults(
        await getNearbyWildfires(
          parsedLatitude,
          parsedLongitude,
          parsedRadiusKm,
        ),
      )
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
        Search the existing wildfire dataset around a coordinate.
      </Typography>

      <Stack spacing={1.25} sx={{ mt: 1.5 }}>
        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: '1fr 1fr',
            gap: 1,
          }}
        >
          <TextField
            label="Latitude"
            value={latitude}
            onChange={(event) => setLatitude(event.target.value)}
            size="small"
            type="number"
            slotProps={{
              htmlInput: {
                step: 'any',
              },
            }}
          />
          <TextField
            label="Longitude"
            value={longitude}
            onChange={(event) => setLongitude(event.target.value)}
            size="small"
            type="number"
            slotProps={{
              htmlInput: {
                step: 'any',
              },
            }}
          />
        </Box>

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
          onClick={handleSearch}
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
      </Stack>

      {results && (
        <Box sx={{ mt: 2 }}>
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
