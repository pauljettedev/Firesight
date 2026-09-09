import { Alert, Link } from '@mui/material'

export function DemoNotice() {
  return (
    <Alert
      severity="warning"
      variant="outlined"
      sx={{
        py: 0.25,
        px: 1,
        minHeight: 34,
        flex: '0 1 auto',
        color: 'text.secondary',
        fontSize: '0.78rem',
        lineHeight: 1.35,
        backgroundColor: 'rgba(18, 28, 37, 0.54)',
        '& .MuiAlert-icon': {
          py: 0.25,
          mr: 0.75,
          color: 'warning.main',
          fontSize: '1rem',
        },
        '& .MuiAlert-message': {
          py: 0.25,
        },
      }}
    >
      Demo only. Data may not reflect the current fire situation.{' '}
      <Link
        href="https://cwfis.cfs.nrcan.gc.ca/"
        target="_blank"
        rel="noreferrer"
      >
        Official CWFIS
      </Link>
    </Alert>
  )
}
