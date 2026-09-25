# ADR 006: Every UI element is its own component

**Status:** Accepted

## Decision

Each piece of the UI (header, status summary, fire list item, map, map controls, search panel)
is its own component in `src/components/`. Everything else sits in its own folder too:

- `services/` fetches data from the API.
- `state/` holds the rules for how user actions change the app (for example, what the map
  shows and which fire is selected).
- `utils/` holds formatting and calculation helpers.

`App.tsx` only arranges components and passes data between them.

## Why

Small components are easier to read, test, and reuse. When state rules live in plain functions
instead of inside components, they can be tested without drawing any UI.

## What this means

- New UI goes into a new component rather than growing an existing one.
- A component that gets too big is split up (the map is split into a folder with its own camera
  hook, layers, popup, and controls).
- Shared colours live in the MUI theme or in CSS variables, not hard-coded in each component.
