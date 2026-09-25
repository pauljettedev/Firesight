import { Box, Typography } from '@mui/material'

interface SummaryMetricProps {
  label: string
  value: number
  color: string
  divider?: boolean
}

export function SummaryMetric({
  label,
  value,
  color,
  divider = false,
}: SummaryMetricProps) {
  return (
    <Box
      sx={{
        px: { xs: 2, md: 2.5 },
        py: 1.75,
        borderLeft: {
          xs: 'none',
          md: divider ? '1px solid' : 'none',
        },
        borderColor: 'divider',
      }}
    >
      <Typography
        variant="caption"
        color="text.secondary"
        sx={{ textTransform: 'uppercase', letterSpacing: '0.08em' }}
      >
        {label}
      </Typography>
      <Typography
        variant="h5"
        sx={{
          mt: 0.25,
          color,
          fontVariantNumeric: 'tabular-nums',
        }}
      >
        {value.toLocaleString()}
      </Typography>
    </Box>
  )
}
