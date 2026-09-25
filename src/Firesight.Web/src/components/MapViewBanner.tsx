import { Paper, Typography } from '@mui/material'
import type { MapView } from '../utils/mapView'
import {
  formatArea,
  formatRadius,
  stageOfControlLabel,
} from '../utils/wildfirePresentation'

interface MapViewBannerProps {
  mapView: MapView
}

// Says what the map is narrowed to. Going back to every fire is the map's
// own "Show all fires" button, which is always there.
export function MapViewBanner({ mapView }: MapViewBannerProps) {
  const { title, description } = describeMapView(mapView)

  return (
    // role="status" marks this as a status message for assistive tech (the
    // map itself says nothing when it's narrowed), and gives tests a
    // meaningful way to find it.
    <Paper
      role="status"
      aria-label="Map view"
      variant="outlined"
      sx={{ px: 2, py: 1.25 }}
    >
      <Typography variant="body2" sx={{ fontWeight: 700 }}>
        {title}
      </Typography>
      <Typography variant="caption" color="text.secondary">
        {description}
      </Typography>
    </Paper>
  )
}

function describeMapView(mapView: MapView): {
  title: string
  description: string
} {
  switch (mapView.kind) {
    case 'area':
      return {
        title: 'AI map view',
        description:
          `Showing ${mapView.wildfires.length.toLocaleString()} fires within ` +
          `${formatRadius(mapView.focusArea.radiusKm)} of ` +
          `${mapView.label ?? 'the selected location'}.`,
      }
    case 'wildfire': {
      const { wildfire } = mapView
      return {
        title: 'Single fire view',
        description:
          `${stageOfControlLabel(wildfire.status)} · ` +
          `${formatArea(wildfire.areaHectares)} · ${wildfire.agency} ` +
          `(${wildfire.externalId})`,
      }
    }
  }
}
