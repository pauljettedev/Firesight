import { useState, type FormEvent } from 'react'
import {
  Alert,
  Box,
  Button,
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
import type { Wildfire } from '../services/wildfireService'
import { errorMessage } from '../utils/errorMessage'
import { AskFiresightAnswer } from './AskFiresightAnswer'
import { SectionHeading } from './SectionHeading'

const MaxQuestionLength = 500

const exampleQuestions = [
  'Are there any out-of-control fires near Ottawa?',
  'What are the largest fires currently being observed?',
  'Which fires have not been observed recently?',
]

// Asks the question. The answer itself is shown by AskFiresightAnswer, which
// gets these props passed straight through.
interface AskFiresightPanelProps {
  wildfires: Wildfire[]
  selectedWildfireId: string | null
  onShowAreaOnMap: (context: AskFiresightMapContext) => Promise<void>
  onShowWildfiresOnMap: (wildfires: Wildfire[]) => void
  onWildfireSelect: (wildfire: Wildfire) => void
}

export function AskFiresightPanel(answerProps: AskFiresightPanelProps) {
  const [question, setQuestion] = useState('')
  const [result, setResult] = useState<AskFiresightResult | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

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
      setError(errorMessage(err, 'Ask Firesight failed to answer the question.'))
    } finally {
      setLoading(false)
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
      <SectionHeading
        title="Ask Firesight"
        subtitle="Ask about the current wildfire dataset."
      />

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

          {result && <AskFiresightAnswer result={result} {...answerProps} />}
        </Stack>
      </Box>
    </Box>
  )
}
