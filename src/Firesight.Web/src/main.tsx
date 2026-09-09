import { StrictMode } from 'react'
import { CssBaseline, ThemeProvider } from '@mui/material'
import { createRoot } from 'react-dom/client'
import App from './App.tsx'
import { firesightTheme } from './theme'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ThemeProvider theme={firesightTheme}>
      <CssBaseline />
      <App />
    </ThemeProvider>
  </StrictMode>,
)
