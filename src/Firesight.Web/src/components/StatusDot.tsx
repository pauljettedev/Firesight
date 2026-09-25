import { Box } from '@mui/material'

interface StatusDotProps {
  status: string
}

export function StatusDot({ status }: StatusDotProps) {
  return (
    <Box
      aria-hidden
      sx={{
        width: 8,
        height: 8,
        flex: '0 0 auto',
        borderRadius: '50%',
        backgroundColor: statusColor(status),
      }}
    />
  )
}

function statusColor(status: string): string {
  switch (status.toUpperCase()) {
    case 'OC': return 'error.main'
    case 'BH': return 'warning.main'
    case 'UC': return 'success.main'
    default: return 'text.secondary'
  }
}
