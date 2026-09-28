import type { ReactNode } from 'react'
import { Box, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'

interface AnswerCardProps {
  children: ReactNode
}

// The reply surface for an answer: a slightly lighter card with a cyan edge
// and a "Firesight" label, so it looks different from the question bubble
// (AskedQuestion) and the form.
export function AnswerCard({ children }: AnswerCardProps) {
  return (
    <Box
      sx={{
        p: 1.5,
        borderRadius: 2,
        bgcolor: (theme) => alpha(theme.palette.text.secondary, 0.07),
        borderLeft: '3px solid',
        borderLeftColor: 'primary.main',
      }}
    >
      <Typography
        variant="caption"
        color="primary"
        sx={{
          display: 'block',
          mb: 0.5,
          fontWeight: 600,
          letterSpacing: '0.08em',
          textTransform: 'uppercase',
        }}
      >
        Firesight
      </Typography>

      {children}
    </Box>
  )
}
