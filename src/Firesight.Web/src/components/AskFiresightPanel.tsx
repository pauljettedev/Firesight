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
import { AskedQuestion } from './AskedQuestion'
import { AskFiresightAnswer } from './AskFiresightAnswer'
import { SectionHeading } from './SectionHeading'

const MaxQuestionLength = 500

const exampleQuestions = [
  'Are there any out-of-control fires near Ottawa?',
  'What are the largest fires currently being observed?',
  'Which fires have not been observed recently?',
]

// Laid out like a chat: the question you asked, the answer below it, and the
// input box at the bottom. The answer itself is rendered by
// AskFiresightAnswer, which gets these props passed straight through.
interface AskFiresightPanelProps {
  wildfires: Wildfire[]
  selectedWildfireId: string | null
  onShowArea: (context: AskFiresightMapContext) => Promise<void>
  onShowWildfires: (wildfires: Wildfire[]) => void
  onFocusWildfire: (wildfire: Wildfire) => void
}

export function AskFiresightPanel(props: AskFiresightPanelProps) {
  // What's in the input box, and the question the shown answer belongs to.
  // They're kept apart so the answer stays up while you type the next one.
  const [question, setQuestion] = useState('')
  const [askedQuestion, setAskedQuestion] = useState<string | null>(null)
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

    setAskedQuestion(trimmedQuestion)
    setQuestion('')
    setLoading(true)
    setError(null)
    setResult(null)

    try {
      setResult(await askFiresight(trimmedQuestion))
    } catch (err: unknown) {
      setError(errorMessage(err, 'Ask Firesight failed to answer the question.'))
      // Put the question back so it can be retried without retyping it.
      setQuestion(trimmedQuestion)
    } finally {
      setLoading(false)
    }
  }

  return (
    <Box>
      <SectionHeading
        title="Ask Firesight"
        subtitle="Ask about the current wildfire dataset."
      />

      {/* aria-live makes screen readers announce the answer when it arrives. */}
      {askedQuestion && (
        <Stack
          spacing={1.25}
          sx={{ mt: 2 }}
          aria-live="polite"
          aria-busy={loading}
        >
          <AskedQuestion question={askedQuestion} />

          {loading && (
            <Box
              sx={{ display: 'flex', alignItems: 'center', gap: 1, px: 0.5 }}
            >
              <CircularProgress size={14} />
              <Typography variant="caption" color="textSecondary">
                Firesight is checking the data...
              </Typography>
            </Box>
          )}

          {error && <Alert severity="error">{error}</Alert>}

          {result && <AskFiresightAnswer result={result} {...props} />}
        </Stack>
      )}

      <Box component="form" onSubmit={handleSubmit} sx={{ mt: 2 }}>
        <Stack spacing={1.25}>
          <TextField
            value={question}
            onChange={(event) => setQuestion(event.target.value)}
            placeholder={
              askedQuestion
                ? 'Ask another question...'
                : 'Ask about fires, locations, sizes, or data freshness...'
            }
            multiline
            minRows={askedQuestion ? 2 : 3}
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

          {!askedQuestion && (
            <ExampleQuestions onSelect={(example) => setQuestion(example)} />
          )}
        </Stack>
      </Box>
    </Box>
  )
}

interface ExampleQuestionsProps {
  onSelect: (example: string) => void
}

// Suggested first questions, shown until something has been asked.
function ExampleQuestions({ onSelect }: ExampleQuestionsProps) {
  return (
    <Box>
      <Typography
        variant="caption"
        color="textSecondary"
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
            onClick={() => onSelect(example)}
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
  )
}
