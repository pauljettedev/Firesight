import { Box, Typography } from '@mui/material'

interface SectionHeadingProps {
  title: string
  subtitle: string
}

// The small uppercase title and one-line description at the top of each
// section, so every section is introduced the same way.
export function SectionHeading({ title, subtitle }: SectionHeadingProps) {
  return (
    <Box>
      <Typography
        variant="overline"
        color="text.secondary"
        sx={{ letterSpacing: '0.1em' }}
      >
        {title}
      </Typography>

      <Typography variant="body2" color="text.secondary" sx={{ mt: 0.25 }}>
        {subtitle}
      </Typography>
    </Box>
  )
}
