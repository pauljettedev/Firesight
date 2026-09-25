import { useReducer, useRef } from 'react'
import type { AskFiresightMapContext } from '../services/askFiresightService'
import { getNearbyWildfires, type Wildfire } from '../services/wildfireService'
import {
  initialMapState,
  mapStateReducer,
  type MapAction,
} from './mapState'

// The map's state plus the user actions that change it. The reducer decides
// what each action does; this hook adds the one thing a reducer can't do on
// its own: the async Ask AI lookup, and making sure a slow one can't undo a
// choice the user made while it was still loading.
export function useMapState() {
  const [state, dispatch] = useReducer(mapStateReducer, initialMapState)

  // Bumped every time the user picks a new view. A pending Ask AI lookup
  // checks this when it finishes, and gives up if anything newer happened,
  // so the latest choice always wins.
  const viewRequestRef = useRef(0)

  function changeView(action: MapAction) {
    viewRequestRef.current += 1
    dispatch(action)
  }

  async function showAskResultOnMap(context: AskFiresightMapContext) {
    viewRequestRef.current += 1
    const requestId = viewRequestRef.current

    const nearby = await getNearbyWildfires(
      context.latitude,
      context.longitude,
      context.radiusKm,
    )

    if (requestId !== viewRequestRef.current) {
      return
    }

    dispatch({
      type: 'showArea',
      focusArea: {
        latitude: context.latitude,
        longitude: context.longitude,
        radiusKm: context.radiusKm,
      },
      wildfires: nearby.map((result) => result.wildfire),
      label: context.label,
    })
  }

  return {
    ...state,
    showAskResultOnMap,
    focusWildfire: (wildfire: Wildfire) =>
      changeView({ type: 'focusWildfire', wildfire }),
    selectWildfire: (wildfire: Wildfire) =>
      changeView({ type: 'selectWildfire', wildfire }),
    showAll: () => changeView({ type: 'showAll' }),
    // Picking a fire on the map is a selection within the current view, not
    // a new view, so it doesn't cancel a pending Ask AI lookup.
    changeMapSelection: (wildfire: Wildfire | null) =>
      dispatch({ type: 'mapSelectionChanged', wildfire }),
  }
}
