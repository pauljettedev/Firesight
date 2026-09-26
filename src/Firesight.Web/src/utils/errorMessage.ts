// The text to show for a caught error. Anything thrown can be caught, not
// just Error objects, so fall back to a message that suits where it happened.
export function errorMessage(err: unknown, fallback: string): string {
  return err instanceof Error ? err.message : fallback
}
