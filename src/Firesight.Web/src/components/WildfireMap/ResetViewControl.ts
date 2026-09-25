import type { IControl } from 'maplibre-gl'

// The "Show all fires" button, as a native MapLibre control. That way it
// lives in the map's own control corners (MapLibre handles spacing and
// stacking) and picks up the same styling as the zoom buttons.
export class ResetViewControl implements IControl {
  private readonly container = document.createElement('div')
  private readonly onReset: () => void

  constructor(onReset: () => void) {
    this.onReset = onReset
  }

  onAdd(): HTMLElement {
    this.container.className =
      'maplibregl-ctrl maplibregl-ctrl-group firesight-reset-control'

    const button = document.createElement('button')
    button.type = 'button'
    button.textContent = 'Show all fires'
    button.addEventListener('click', () => this.onReset())

    this.container.appendChild(button)
    return this.container
  }

  onRemove(): void {
    this.container.remove()
  }
}
