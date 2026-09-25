import { useState } from 'react'
import { Box, Button, ButtonGroup, Paper } from '@mui/material'
import type { AskFiresightMapContext } from '../services/askFiresightService'
import type { Wildfire } from '../services/wildfireService'
import { AskFiresightPanel } from './AskFiresightPanel'
import { NearbyWildfireSearch } from './NearbyWildfireSearch'
import { RecentWildfireList } from './RecentWildfireList'

type RailMode = 'recent' | 'nearby' | 'ask'

interface WildfireContextRailProps {
  recentWildfires: Wildfire[]
  selectedWildfireId: string | null
  onRecentWildfireSelect: (wildfire: Wildfire) => void
  onNearbyWildfireSelect: (wildfire: Wildfire) => void
  onShowAskResultOnMap: (context: AskFiresightMapContext) => Promise<void>
}

export function WildfireContextRail({
  recentWildfires,
  selectedWildfireId,
  onRecentWildfireSelect,
  onNearbyWildfireSelect,
  onShowAskResultOnMap,
}: WildfireContextRailProps) {
  const [mode, setMode] = useState<RailMode>('ask')

  return (
    <Paper variant="outlined" sx={{ overflow: 'hidden', minWidth: 0 }}>
      <Box sx={{ p: 1.25, pb: 0 }}>
        <ButtonGroup
          size="small"
          fullWidth
          variant="outlined"
          aria-label="Wildfire context"
        >
          <Button
            variant={mode === 'ask' ? 'contained' : 'outlined'}
            onClick={() => setMode('ask')}
          >
            Ask AI
          </Button>
          <Button
            variant={mode === 'recent' ? 'contained' : 'outlined'}
            onClick={() => setMode('recent')}
          >
            Recent
          </Button>
          <Button
            variant={mode === 'nearby' ? 'contained' : 'outlined'}
            onClick={() => setMode('nearby')}
          >
            Nearby
          </Button>
        </ButtonGroup>
      </Box>

      <Box sx={{ p: 2.25 }}>
        <Box sx={{ display: mode === 'recent' ? 'block' : 'none' }}>
          <RecentWildfireList
            wildfires={recentWildfires}
            selectedWildfireId={selectedWildfireId}
            onSelect={onRecentWildfireSelect}
          />
        </Box>

        <Box sx={{ display: mode === 'nearby' ? 'block' : 'none' }}>
          <NearbyWildfireSearch
            selectedWildfireId={selectedWildfireId}
            onSelect={onNearbyWildfireSelect}
          />
        </Box>

        <Box sx={{ display: mode === 'ask' ? 'block' : 'none' }}>
          <AskFiresightPanel onShowOnMap={onShowAskResultOnMap} />
        </Box>
      </Box>
    </Paper>
  )
}
