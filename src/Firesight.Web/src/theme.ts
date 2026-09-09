import { createTheme } from '@mui/material/styles'

export const firesightTheme = createTheme({
  palette: {
    mode: 'dark',
    primary: { main: '#41d9e8' },
    secondary: { main: '#6f8ea6' },
    warning: { main: '#f0b45a' },
    error: { main: '#ef6a6a' },
    success: { main: '#69c58f' },
    background: {
      default: '#0d141b',
      paper: '#121c25',
    },
    text: {
      primary: '#eef4f7',
      secondary: '#8fa4b3',
    },
    divider: 'rgba(143, 164, 179, 0.18)',
  },
  shape: {
    borderRadius: 6,
  },
  typography: {
    fontFamily: [
      'Segoe UI',
      'Roboto',
      'Helvetica Neue',
      'Arial',
      'sans-serif',
    ].join(','),
    h5: {
      fontWeight: 600,
      letterSpacing: '-0.01em',
    },
    h6: {
      fontWeight: 700,
      letterSpacing: '0.02em',
    },
    body2: {
      lineHeight: 1.5,
    },
  },
  components: {
    MuiCssBaseline: {
      styleOverrides: {
        html: {
          backgroundColor: '#0d141b',
        },
        body: {
          margin: 0,
          minWidth: 320,
          minHeight: '100vh',
          background:
            'radial-gradient(circle at 18% 0%, rgba(65, 217, 232, 0.07), transparent 32%), #0d141b',
        },
        '#root': {
          minHeight: '100vh',
        },
      },
    },
    MuiAppBar: {
      styleOverrides: {
        root: {
          backgroundImage: 'none',
          backgroundColor: 'rgba(13, 20, 27, 0.94)',
          borderBottom: '1px solid rgba(143, 164, 179, 0.18)',
          boxShadow: 'none',
        },
      },
    },
    MuiPaper: {
      styleOverrides: {
        root: {
          backgroundImage: 'none',
        },
      },
    },
    MuiAlert: {
      styleOverrides: {
        root: {
          border: '1px solid rgba(143, 164, 179, 0.18)',
          backgroundImage: 'none',
        },
      },
    },
    MuiLink: {
      styleOverrides: {
        root: {
          fontWeight: 600,
        },
      },
    },
  },
})
