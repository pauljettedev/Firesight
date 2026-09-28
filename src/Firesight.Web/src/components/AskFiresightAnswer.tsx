import { useState, type ReactNode } from 'react'
import {
  Alert,
  Button,
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
import { AnswerCard } from './AnswerCard'
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
  onShowArea: (context: AskFiresightMapContext) => Promise<void>
  onShowWildfires: (wildfires: Wildfire[]) => void
  onFocusWildfire: (wildfire: Wildfire) => void
}

// One Ask Firesight answer, styled as a reply: the text, the fires it names
// (clickable, like the Recent tab), a link to show them or the searched area
// on the map, and which data it was based on.
export function AskFiresightAnswer({
  result,
  wildfires,
  selectedWildfireId,
  onShowArea,
  onShowWildfires,
  onFocusWildfire,
}: AskFiresightAnswerProps) {
  const [mapLoading, setMapLoading] = useState(false)
  const [mapError, setMapError] = useState<string | null>(null)

  const answerWildfires = wildfiresWithExternalIds(
    wildfires,
    result.wildfireExternalIds,
  )

  async function handleShowArea(context: AskFiresightMapContext) {
    setMapLoading(true)
    setMapError(null)

    try {
      await onShowArea(context)
    } catch (err: unknown) {
      setMapError(errorMessage(err, 'Firesight failed to update the map.'))
    } finally {
      setMapLoading(false)
    }
  }

  return (
    <AnswerCard>
      <Typography
        variant="body2"
        sx={{ whiteSpace: 'pre-wrap', lineHeight: 1.6, overflowWrap: 'anywhere' }}
      >
        {result.answer}
      </Typography>

      {/* An answer that names fires lists them and can show exactly those.
          Otherwise an answer about a place can show the area it searched. */}
      {answerWildfires.length > 0 ? (
        <>
          <WildfireList
            wildfires={answerWildfires}
            selectedWildfireId={selectedWildfireId}
            onSelect={onFocusWildfire}
            limit={MaxListedWildfires}
            sx={{ mt: 1.5 }}
          />

          <ShowOnMapButton onClick={() => onShowWildfires(answerWildfires)}>
            {answerWildfires.length === 1
              ? 'Show fire on map'
              : `Show all ${answerWildfires.length} fires on map`}
          </ShowOnMapButton>
        </>
      ) : (
        result.mapContext && (
          <ShowOnMapButton
            disabled={mapLoading}
            onClick={() => void handleShowArea(result.mapContext!)}
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

      {/* Plain text rather than chips, so it reads as a footnote and not
          as more buttons. */}
      {result.toolsUsed.length > 0 && (
        <Typography
          variant="caption"
          color="textSecondary"
          sx={{ display: 'block', mt: 1.25 }}
        >
          Based on:{' '}
          {result.toolsUsed.map((tool) => toolLabels[tool] ?? tool).join(' · ')}
        </Typography>
      )}
    </AnswerCard>
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
      variant="text"
      disabled={disabled}
      onClick={onClick}
      sx={{
        mt: 1,
        px: 0,
        minWidth: 0,
        textTransform: 'none',
        fontWeight: 600,
      }}
    >
      {children}
    </Button>
  )
}
