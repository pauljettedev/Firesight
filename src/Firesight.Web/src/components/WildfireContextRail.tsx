import { useState } from 'react'
import { Box, Button, ButtonGroup, Paper } from '@mui/material'
import type { AskFiresightMapContext } from '../services/askFiresightService'
import type { Wildfire } from '../services/wildfireService'
import { AskFiresightPanel } from './AskFiresightPanel'
import { NearbyWildfireSearch } from './NearbyWildfireSearch'
import { RecentWildfireList } from './RecentWildfireList'

type RailMode = 'recent' | 'nearby' | 'ask'

interface WildfireContextRailProps {
  wildfires: Wildfire[]
  recentWildfires: Wildfire[]
  selectedWildfireId: string | null
  // Recent and Ask AI both narrow the map to the fire that was clicked.
  onFocusWildfire: (wildfire: Wildfire) => void
  onNearbyWildfireSelect: (wildfire: Wildfire) => void
  onShowAskAreaOnMap: (context: AskFiresightMapContext) => Promise<void>
  onShowAskWildfiresOnMap: (wildfires: Wildfire[]) => void
}

export function WildfireContextRail({
  wildfires,
  recentWildfires,
  selectedWildfireId,
  onFocusWildfire,
  onNearbyWildfireSelect,
  onShowAskAreaOnMap,
  onShowAskWildfiresOnMap,
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
            onSelect={onFocusWildfire}
          />
        </Box>

        <Box sx={{ display: mode === 'nearby' ? 'block' : 'none' }}>
          <NearbyWildfireSearch
            selectedWildfireId={selectedWildfireId}
            onSelect={onNearbyWildfireSelect}
          />
        </Box>

        <Box sx={{ display: mode === 'ask' ? 'block' : 'none' }}>
          <AskFiresightPanel
            wildfires={wildfires}
            selectedWildfireId={selectedWildfireId}
            onShowAreaOnMap={onShowAskAreaOnMap}
            onShowWildfiresOnMap={onShowAskWildfiresOnMap}
            onWildfireSelect={onFocusWildfire}
          />
        </Box>
      </Box>
    </Paper>
  )
}
