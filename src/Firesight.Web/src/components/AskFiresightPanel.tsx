import { useState, type FormEvent } from 'react'
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Paper,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import {
  askFiresight,
  type AskFiresightMapContext,
  type AskFiresightResult,
} from '../services/askFiresightService'

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
  find_wildfires_near_location: 'Nearby wildfire search',
  get_feed_sync_state: 'Dataset freshness',
}

interface AskFiresightPanelProps {
  onShowOnMap: (context: AskFiresightMapContext) => Promise<void>
}

export function AskFiresightPanel({
  onShowOnMap,
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
    <Paper variant="outlined" sx={{ overflow: 'hidden' }}>
      <Box
        sx={{
          px: { xs: 2, md: 2.5 },
          py: 2,
          display: 'grid',
          gridTemplateColumns: {
            xs: '1fr',
            lg: 'minmax(220px, 0.7fr) minmax(0, 2fr)',
          },
          gap: { xs: 2, lg: 3 },
          alignItems: 'start',
        }}
      >
        <Box>
          <Typography
            variant="overline"
            color="primary.main"
            sx={{ letterSpacing: '0.1em', fontWeight: 700 }}
          >
            Ask Firesight
          </Typography>

          <Typography variant="body2" sx={{ mt: 0.35, fontWeight: 600 }}>
            Query the current Firesight wildfire dataset
          </Typography>

          <Typography
            variant="caption"
            color="text.secondary"
            sx={{ display: 'block', mt: 0.75, lineHeight: 1.5 }}
          >
            Answers use Firesight&apos;s wildfire, location, and dataset
            freshness tools. This is a demo, not an emergency information
            service.
          </Typography>
        </Box>

        <Box component="form" onSubmit={handleSubmit}>
          <Stack spacing={1.25}>
            <Box
              sx={{
                display: 'grid',
                gridTemplateColumns: { xs: '1fr', sm: 'minmax(0, 1fr) auto' },
                gap: 1,
                alignItems: 'start',
              }}
            >
              <TextField
                value={question}
                onChange={(event) => handleQuestionChange(event.target.value)}
                placeholder="Ask about current wildfire conditions, locations, sizes, or data freshness..."
                multiline
                minRows={2}
                maxRows={4}
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
                sx={{
                  minWidth: { sm: 116 },
                  minHeight: 40,
                  mt: { sm: 0 },
                }}
              >
                {loading ? (
                  <CircularProgress size={20} color="inherit" />
                ) : (
                  'Ask'
                )}
              </Button>
            </Box>

            {!result && !loading && (
              <Box
                sx={{
                  display: 'flex',
                  gap: 0.75,
                  flexWrap: 'wrap',
                  alignItems: 'center',
                }}
              >
                <Typography variant="caption" color="text.secondary">
                  Try:
                </Typography>
                {exampleQuestions.map((example) => (
                  <Button
                    key={example}
                    type="button"
                    size="small"
                    variant="text"
                    onClick={() => selectExample(example)}
                    sx={{
                      minWidth: 0,
                      px: 0.75,
                      py: 0.25,
                      justifyContent: 'flex-start',
                      textTransform: 'none',
                      fontSize: '0.75rem',
                      color: 'text.secondary',
                    }}
                  >
                    {example}
                  </Button>
                ))}
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
                  sx={{ whiteSpace: 'pre-wrap', lineHeight: 1.65 }}
                >
                  {result.answer}
                </Typography>

                <Box
                  sx={{
                    mt: 1.5,
                    display: 'flex',
                    gap: 0.75,
                    flexWrap: 'wrap',
                    alignItems: 'center',
                  }}
                >
                  {result.toolsUsed.length > 0 && (
                    <>
                      <Typography variant="caption" color="text.secondary">
                        Used Firesight data:
                      </Typography>

                      {result.toolsUsed.map((tool) => (
                        <Chip
                          key={tool}
                          label={toolLabels[tool] ?? tool}
                          size="small"
                          variant="outlined"
                          sx={{ height: 24 }}
                        />
                      ))}
                    </>
                  )}

                  {result.mapContext && (
                    <Button
                      type="button"
                      size="small"
                      variant="outlined"
                      disabled={mapLoading}
                      onClick={() => void handleShowOnMap(result.mapContext!)}
                      sx={{
                        ml: { sm: 'auto' },
                        textTransform: 'none',
                      }}
                    >
                      {mapLoading ? (
                        <CircularProgress size={16} color="inherit" />
                      ) : (
                        `Show ${formatRadius(result.mapContext.radiusKm)} area on map`
                      )}
                    </Button>
                  )}
                </Box>
              </Box>
            )}
          </Stack>
        </Box>
      </Box>
    </Paper>
  )
}

function formatRadius(radiusKm: number): string {
  return `${Math.round(radiusKm).toLocaleString()} km`
}
