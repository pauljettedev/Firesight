import { Box } from '@mui/material'
import type { Wildfire } from '../services/wildfireService'
import { SectionHeading } from './SectionHeading'
import { WildfireList } from './WildfireList'

interface RecentWildfireListProps {
  wildfires: Wildfire[]
  selectedWildfireId: string | null
  onSelect: (wildfire: Wildfire) => void
}

export function RecentWildfireList({
  wildfires,
  selectedWildfireId,
  onSelect,
}: RecentWildfireListProps) {
  return (
    <Box>
      <SectionHeading
        title="Recently updated"
        subtitle="Latest source updates in the current dataset"
      />

      <WildfireList
        wildfires={wildfires}
        selectedWildfireId={selectedWildfireId}
        onSelect={onSelect}
        sx={{ mt: 1.5 }}
      />
    </Box>
  )
}
