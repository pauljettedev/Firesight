import { Box } from '@mui/material'
import firesightHeaderBackground from '../assets/firesight-header-background.png'
import firesightLogo from '../assets/firesight-logo.png'

export function AppHeader() {
  return (
    <Box
      component="header"
      sx={{
        position: 'relative',
        height: { xs: 76, md: 92 },
        borderBottom: '1px solid',
        borderColor: 'divider',
        backgroundColor: 'background.default',
        backgroundImage: `
          linear-gradient(
            90deg,
            rgba(8, 14, 20, 0.72) 0%,
            rgba(8, 14, 20, 0.22) 42%,
            rgba(8, 14, 20, 0.38) 100%
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
        component="img"
        src={firesightLogo}
        alt="Firesight"
        sx={{
          position: 'absolute',
          left: { xs: 12, md: 20 },
          top: '50%',
          transform: 'translateY(-50%)',
          display: 'block',
          width: 'auto',
          height: { xs: 68, md: 84 },
          maxWidth: '42vw',
          objectFit: 'contain',
          filter: 'drop-shadow(0 4px 10px rgba(0, 0, 0, 0.42))',
        }}
      />
    </Box>
  )
}
