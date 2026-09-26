import { useState, type FormEvent } from 'react'
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import {
  askFiresight,
  type AskFiresightMapContext,
  type AskFiresightResult,
} from '../services/askFiresightService'
import { formatRadius } from '../utils/wildfirePresentation'

const MaxQuestionLength = 500

const exampleQuestions = [
  'Are there any out-of-control fires near Ottawa?',
  'What are the largest fires currently being observed?',
  'Which fires have not been observed recently?',
]

const toolLabels: Record<string, string> = {
  geocode_location: 'Location lookup',
  get_active_wildfires: 'Current wildfire data',
  get_wildfire_by_external_id: 'Wildfire record',
  find_wildfires_near_location: 'Nearby search',
  get_feed_sync_state: 'Dataset freshness',
}

interface AskFiresightPanelProps {
  onShowOnMap: (context: AskFiresightMapContext) => Promise<void>
  onShowWildfiresOnMap: (externalIds: string[]) => void
}

export function AskFiresightPanel({
  onShowOnMap,
  onShowWildfiresOnMap,
}: AskFiresightPanelProps) {
  const [question, setQuestion] = useState('')
  const [result, setResult] = useState<AskFiresightResult | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)
  const [mapLoading, setMapLoading] = useState(false)

  const trimmedQuestion = question.trim()
  const canSubmit =
    trimmedQuestion.length > 0 &&
    trimmedQuestion.length <= MaxQuestionLength &&
    !loading

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (!canSubmit) {
      return
    }

    setLoading(true)
    setError(null)
    setResult(null)

    try {
      setResult(await askFiresight(trimmedQuestion))
    } catch (err: unknown) {
      setError(
        err instanceof Error
          ? err.message
          : 'Ask Firesight failed to answer the question.',
      )
    } finally {
      setLoading(false)
    }
  }

  async function handleShowOnMap(context: AskFiresightMapContext) {
    setMapLoading(true)
    setError(null)

    try {
      await onShowOnMap(context)
    } catch (err: unknown) {
      setError(
        err instanceof Error
          ? err.message
          : 'Firesight failed to update the map.',
      )
    } finally {
      setMapLoading(false)
    }
  }

  function selectExample(example: string) {
    setQuestion(example)
    setResult(null)
    setError(null)
  }

  function handleQuestionChange(value: string) {
    setQuestion(value)
    setResult(null)
    setError(null)
  }

  return (
    <Box>
      <Typography
        variant="overline"
        color="primary.main"
        sx={{ letterSpacing: '0.1em', fontWeight: 700 }}
      >
        Ask Firesight
      </Typography>

      <Typography variant="body2" color="text.secondary" sx={{ mt: 0.25 }}>
        Ask about the current wildfire dataset.
      </Typography>

      <Box component="form" onSubmit={handleSubmit} sx={{ mt: 1.5 }}>
        <Stack spacing={1.25}>
          <TextField
            value={question}
            onChange={(event) => handleQuestionChange(event.target.value)}
            placeholder="Ask about fires, locations, sizes, or data freshness..."
            multiline
            minRows={3}
            maxRows={6}
            fullWidth
            size="small"
            disabled={loading}
            slotProps={{
              htmlInput: {
                maxLength: MaxQuestionLength,
                'aria-label': 'Ask Firesight question',
              },
            }}
            helperText={`${question.length}/${MaxQuestionLength}`}
          />

          <Button
            type="submit"
            variant="contained"
            disabled={!canSubmit}
            fullWidth
          >
            {loading ? (
              <CircularProgress size={20} color="inherit" />
            ) : (
              'Ask Firesight'
            )}
          </Button>

          {!result && !loading && (
            <Box>
              <Typography
                variant="caption"
                color="text.secondary"
                sx={{ display: 'block', mb: 0.25 }}
              >
                Try:
              </Typography>
              <Stack spacing={0.25}>
                {exampleQuestions.map((example) => (
                  <Button
                    key={example}
                    type="button"
                    size="small"
                    variant="text"
                    onClick={() => selectExample(example)}
                    sx={{
                      minWidth: 0,
                      px: 0,
                      py: 0.25,
                      justifyContent: 'flex-start',
                      textAlign: 'left',
                      textTransform: 'none',
                      fontSize: '0.75rem',
                      lineHeight: 1.35,
                      color: 'text.secondary',
                    }}
                  >
                    {example}
                  </Button>
                ))}
              </Stack>
            </Box>
          )}

          {error && <Alert severity="error">{error}</Alert>}

          {result && (
            <Box
              sx={{
                borderTop: '1px solid',
                borderColor: 'divider',
                pt: 1.5,
              }}
            >
              <Typography
                variant="body2"
                sx={{
                  whiteSpace: 'pre-wrap',
                  lineHeight: 1.6,
                  overflowWrap: 'anywhere',
                }}
              >
                {result.answer}
              </Typography>

              {/* An answer that names fires shows exactly those. Otherwise an
                  answer about a place shows the area that was searched. */}
              {result.wildfireExternalIds.length > 0 ? (
                <Button
                  type="button"
                  size="small"
                  variant="outlined"
                  fullWidth
                  onClick={() =>
                    onShowWildfiresOnMap(result.wildfireExternalIds)
                  }
                  sx={{ mt: 1.5, textTransform: 'none' }}
                >
                  {result.wildfireExternalIds.length === 1
                    ? 'Show fire on map'
                    : `Show ${result.wildfireExternalIds.length} fires on map`}
                </Button>
              ) : result.mapContext && (
                <Button
                  type="button"
                  size="small"
                  variant="outlined"
                  fullWidth
                  disabled={mapLoading}
                  onClick={() => void handleShowOnMap(result.mapContext!)}
                  sx={{ mt: 1.5, textTransform: 'none' }}
                >
                  {mapLoading ? (
                    <CircularProgress size={16} color="inherit" />
                  ) : (
                    `Show ${formatRadius(result.mapContext.radiusKm)} area on map`
                  )}
                </Button>
              )}

              {result.toolsUsed.length > 0 && (
                <Box
                  sx={{
                    mt: 1.5,
                    display: 'flex',
                    gap: 0.5,
                    flexWrap: 'wrap',
                  }}
                >
                  {result.toolsUsed.map((tool) => (
                    <Chip
                      key={tool}
                      label={toolLabels[tool] ?? tool}
                      size="small"
                      variant="outlined"
                      sx={{
                        height: 22,
                        maxWidth: '100%',
                        '& .MuiChip-label': {
                          px: 0.75,
                          fontSize: '0.68rem',
                        },
                      }}
                    />
                  ))}
                </Box>
              )}

            </Box>
          )}
        </Stack>
      </Box>
    </Box>
  )
}
