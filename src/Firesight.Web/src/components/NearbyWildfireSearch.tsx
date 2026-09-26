import { useState } from 'react'
import {
  Alert,
  Box,
  Button,
  CircularProgress,
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
import { errorMessage } from '../utils/errorMessage'
import { SectionHeading } from './SectionHeading'
import { MaxListedWildfires, WildfireList } from './WildfireList'

interface NearbyWildfireSearchProps {
  selectedWildfireId?: string | null
  onSelect: (wildfire: Wildfire) => void
}

export function NearbyWildfireSearch({
  selectedWildfireId = null,
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
      setError(errorMessage(err, 'Nearby wildfire search failed.'))
    } finally {
      setLoading(false)
    }
  }

  // Lets each list row look up its fire's distance.
  const distanceKmById = new Map(
    (results ?? []).map((result) => [result.wildfire.id, result.distanceKm]),
  )

  return (
    <Box>
      <SectionHeading
        title="Nearby fires"
        subtitle="Search for wildfires near a Canadian town or city."
      />

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
            <WildfireList
              wildfires={results.map((result) => result.wildfire)}
              selectedWildfireId={selectedWildfireId}
              onSelect={onSelect}
              // Nearby shows when each fire was last seen in the feed, and
              // how far away it is instead of a status dot.
              updatedUtc={(wildfire) => wildfire.lastSeenInFeedUtc}
              trailing={(wildfire) => (
                <Typography
                  variant="caption"
                  color="primary.main"
                  sx={{ fontWeight: 700, whiteSpace: 'nowrap' }}
                >
                  {/* Always found: the list and this lookup are built from
                      the same results. */}
                  {formatDistance(distanceKmById.get(wildfire.id)!)}
                </Typography>
              )}
              limit={MaxListedWildfires}
              sx={{ mt: 0.75 }}
            />
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
