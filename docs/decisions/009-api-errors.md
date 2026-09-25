# ADR 009: Only explicit validation errors are client errors

**Status:** Accepted

## Decision

- The Application layer throws `ApplicationValidationException` for bad input. The API turns it
  into a `400` with the field-level errors.
- Any other exception becomes a generic `500` with a trace ID and no internal details.
- A request the client cancelled becomes a `499`, so it isn't logged as a server error.

All of this is handled in one place: the API's exception handler.

## Why

Treating broad exception types like `ArgumentOutOfRangeException` as `400` would hide real
bugs behind "bad request". Hiding exception details stops internals from leaking to the
public.

## What this means

- To return a `400`, code must throw `ApplicationValidationException` on purpose.
- The Application layer decides what's valid. The API decides how that looks over HTTP.
