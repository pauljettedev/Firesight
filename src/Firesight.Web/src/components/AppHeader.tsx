import { Box, Typography } from '@mui/material'
import firesightHeaderBackground from '../assets/firesight-header-background.png'
import firesightLogo from '../assets/firesight-logo.png'
import { DemoNotice } from './DemoNotice'

export function AppHeader() {
  return (
    <Box
      component="header"
      sx={{
        position: 'relative',
        minHeight: { xs: 110, md: 92 },
        borderBottom: '1px solid',
        borderColor: 'divider',
        backgroundColor: 'background.default',
        backgroundImage: `
          linear-gradient(
            90deg,
            rgba(8, 14, 20, 0.78) 0%,
            rgba(8, 14, 20, 0.28) 46%,
            rgba(8, 14, 20, 0.58) 100%
          ),
          url(${firesightHeaderBackground})
        `,
        backgroundPosition: 'center 58%',
        backgroundRepeat: 'no-repeat',
        backgroundSize: 'cover',
        overflow: 'hidden',
      }}
    >
      <Box
        sx={{
          minHeight: 'inherit',
          display: 'grid',
          gridTemplateColumns: {
            xs: 'auto 1fr',
            md: 'auto minmax(260px, 1fr) auto',
          },
          alignItems: 'center',
          gap: { xs: 0.75, md: 1 },
          px: { xs: 1.5, md: 2.5 },
          py: { xs: 1, md: 0 },
        }}
      >
        <Box
          component="img"
          src={firesightLogo}
          alt="Firesight"
          sx={{
            display: 'block',
            width: 'auto',
            height: { xs: 68, md: 84 },
            maxWidth: { xs: 110, md: 120 },
            objectFit: 'contain',
            filter: 'drop-shadow(0 4px 10px rgba(0, 0, 0, 0.42))',
          }}
        />

        <Box sx={{ minWidth: 0 }}>
          <Typography
            variant="h5"
            component="h1"
            sx={{
              fontSize: { xs: '1rem', md: '1.35rem' },
              lineHeight: 1.2,
              textShadow: '0 1px 4px rgba(0, 0, 0, 0.75)',
            }}
          >
            Active wildfires in Canada
          </Typography>
          <Typography
            variant="body2"
            sx={{
              mt: 0.25,
              color: 'rgba(238, 244, 247, 0.82)',
              fontSize: { xs: '0.72rem', md: '0.82rem' },
              textShadow: '0 1px 4px rgba(0, 0, 0, 0.75)',
            }}
          >
            Current and recently observed wildfire records from CWFIS
          </Typography>
        </Box>

        <Box
          sx={{
            display: { xs: 'none', md: 'block' },
            justifySelf: 'end',
          }}
        >
          <DemoNotice />
        </Box>
      </Box>

      <Box
        sx={{
          display: { xs: 'block', md: 'none' },
          px: 1.5,
          pb: 1,
        }}
      >
        <DemoNotice />
      </Box>
    </Box>
  )
}
