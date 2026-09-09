import { Box, Typography } from '@mui/material'
import firesightLogo from '../assets/firesight-logo.png'

export function AppBrand() {
  return (
    <Box
      sx={{
        display: 'flex',
        alignItems: 'center',
        gap: 1.5,
      }}
    >
      <Box
        sx={{
          flex: '0 0 auto',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          p: 0.5,
          border: '1px solid',
          borderColor: 'divider',
          borderRadius: 1.25,
          backgroundColor: '#f4f6f8',
          boxShadow: '0 8px 18px rgba(0, 0, 0, 0.18)',
        }}
      >
        <Box
          component="img"
          src={firesightLogo}
          alt="Firesight owl logo"
          sx={{
            display: 'block',
            width: { xs: 46, md: 54 },
            height: { xs: 46, md: 54 },
            objectFit: 'contain',
            borderRadius: 0.75,
          }}
        />
      </Box>

      <Box>
        <Typography
          variant="h6"
          component="h1"
          sx={{ lineHeight: 1.05, textTransform: 'uppercase' }}
        >
          Firesight
        </Typography>
        <Typography
          variant="caption"
          color="text.secondary"
          sx={{ letterSpacing: '0.08em', textTransform: 'uppercase' }}
        >
          Canadian wildfire situational awareness
        </Typography>
      </Box>
    </Box>
  )
}
