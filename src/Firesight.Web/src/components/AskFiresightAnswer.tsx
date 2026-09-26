import { useState, type ReactNode } from 'react'
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Typography,
} from '@mui/material'
import type {
  AskFiresightMapContext,
  AskFiresightResult,
} from '../services/askFiresightService'
import type { Wildfire } from '../services/wildfireService'
import { errorMessage } from '../utils/errorMessage'
import { wildfiresWithExternalIds } from '../utils/mapView'
import { formatRadius } from '../utils/wildfirePresentation'
import { MaxListedWildfires, WildfireList } from './WildfireList'

const toolLabels: Record<string, string> = {
  geocode_location: 'Location lookup',
  get_active_wildfires: 'Current wildfire data',
  get_wildfire_by_external_id: 'Wildfire record',
  find_wildfires_near_location: 'Nearby search',
  get_feed_sync_state: 'Dataset freshness',
}

interface AskFiresightAnswerProps {
  result: AskFiresightResult
  // Every loaded fire. Fires the answer names are looked up here, so their
  // details come from the Firesight API rather than the answer text.
  wildfires: Wildfire[]
  selectedWildfireId: string | null
  onShowAreaOnMap: (context: AskFiresightMapContext) => Promise<void>
  onShowWildfiresOnMap: (wildfires: Wildfire[]) => void
  onWildfireSelect: (wildfire: Wildfire) => void
}

// One Ask Firesight answer: the text, the fires it names (clickable, like the
// Recent tab), a button to show them or the searched area on the map, and
// which tools were used.
export function AskFiresightAnswer({
  result,
  wildfires,
  selectedWildfireId,
  onShowAreaOnMap,
  onShowWildfiresOnMap,
  onWildfireSelect,
}: AskFiresightAnswerProps) {
  const [mapLoading, setMapLoading] = useState(false)
  const [mapError, setMapError] = useState<string | null>(null)

  const answerWildfires = wildfiresWithExternalIds(
    wildfires,
    result.wildfireExternalIds,
  )

  async function showAreaOnMap(context: AskFiresightMapContext) {
    setMapLoading(true)
    setMapError(null)

    try {
      await onShowAreaOnMap(context)
    } catch (err: unknown) {
      setMapError(errorMessage(err, 'Firesight failed to update the map.'))
    } finally {
      setMapLoading(false)
    }
  }

  return (
    <Box sx={{ borderTop: '1px solid', borderColor: 'divider', pt: 1.5 }}>
      <Typography
        variant="body2"
        sx={{ whiteSpace: 'pre-wrap', lineHeight: 1.6, overflowWrap: 'anywhere' }}
      >
        {result.answer}
      </Typography>

      {answerWildfires.length > 0 && (
        <WildfireList
          wildfires={answerWildfires}
          selectedWildfireId={selectedWildfireId}
          onSelect={onWildfireSelect}
          limit={MaxListedWildfires}
          sx={{ mt: 1.5 }}
        />
      )}

      {/* An answer that names fires shows exactly those. Otherwise an answer
          about a place shows the area that was searched. */}
      {answerWildfires.length > 0 ? (
        <ShowOnMapButton onClick={() => onShowWildfiresOnMap(answerWildfires)}>
          {answerWildfires.length === 1
            ? 'Show fire on map'
            : `Show all ${answerWildfires.length} fires on map`}
        </ShowOnMapButton>
      ) : (
        result.mapContext && (
          <ShowOnMapButton
            disabled={mapLoading}
            onClick={() => void showAreaOnMap(result.mapContext!)}
          >
            {mapLoading ? (
              <CircularProgress size={16} color="inherit" />
            ) : (
              `Show ${formatRadius(result.mapContext.radiusKm)} area on map`
            )}
          </ShowOnMapButton>
        )
      )}

      {mapError && (
        <Alert severity="error" sx={{ mt: 1.5 }}>
          {mapError}
        </Alert>
      )}

      {result.toolsUsed.length > 0 && (
        <Box sx={{ mt: 1.5, display: 'flex', gap: 0.5, flexWrap: 'wrap' }}>
          {result.toolsUsed.map((tool) => (
            <Chip
              key={tool}
              label={toolLabels[tool] ?? tool}
              size="small"
              variant="outlined"
              sx={{
                height: 22,
                maxWidth: '100%',
                '& .MuiChip-label': { px: 0.75, fontSize: '0.68rem' },
              }}
            />
          ))}
        </Box>
      )}
    </Box>
  )
}

interface ShowOnMapButtonProps {
  onClick: () => void
  disabled?: boolean
  children: ReactNode
}

function ShowOnMapButton({ onClick, disabled, children }: ShowOnMapButtonProps) {
  return (
    <Button
      type="button"
      size="small"
      variant="outlined"
      fullWidth
      disabled={disabled}
      onClick={onClick}
      sx={{ mt: 1.5, textTransform: 'none' }}
    >
      {children}
    </Button>
  )
}
