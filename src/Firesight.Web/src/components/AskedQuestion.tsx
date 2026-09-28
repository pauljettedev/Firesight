import { Box, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'

interface AskedQuestionProps {
  question: string
}

// The question the user sent, shown as a right-aligned chat bubble above the
// answer, so the answer still makes sense after the input box is cleared.
export function AskedQuestion({ question }: AskedQuestionProps) {
  return (
    <Box sx={{ display: 'flex', justifyContent: 'flex-end' }}>
      <Typography
        variant="body2"
        sx={{
          maxWidth: '85%',
          px: 1.5,
          py: 1,
          borderRadius: 3,
          borderBottomRightRadius: 4,
          bgcolor: (theme) => alpha(theme.palette.primary.main, 0.14),
          overflowWrap: 'anywhere',
        }}
      >
        {question}
      </Typography>
    </Box>
  )
}
