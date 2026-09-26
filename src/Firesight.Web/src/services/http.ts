interface RequestJsonOptions {
  // Statuses that mean "nothing here" rather than an error, e.g. 404 from a
  // lookup or 204 when there's no data yet. They return null.
  nullWhen?: number[]
  init?: RequestInit
}

// Every API call goes through here: send the request, return the JSON body,
// and turn a failure into an Error with a useful message.
export async function requestJson<T>(
  url: string,
  whatFailed: string,
  { nullWhen = [], init }: RequestJsonOptions = {},
): Promise<T> {
  const response = await fetch(url, init)

  if (nullWhen.includes(response.status)) {
    // Only reached for statuses the caller listed, and every caller that
    // lists one declares T as "X | null", so this null is expected.
    return null as T
  }

  if (!response.ok) {
    throw new Error(await describeError(response, whatFailed))
  }

  return response.json()
}

// A failed request like a bad radius comes back with a JSON body explaining
// what was wrong, for example "Radius must be at most 1000 km." We read that
// instead of just showing a status code, so the user knows what to fix.
async function describeError(
  response: Response,
  whatFailed: string,
): Promise<string> {
  try {
    const body = await response.json()
    const messages = Object.values(body.errors ?? {}).flat()

    if (messages.length > 0) {
      return messages.join(' ')
    }

    if (typeof body.title === 'string') {
      return body.title
    }
  } catch {
    // The response body wasn't JSON, or didn't have the shape we expected.
  }

  return `${whatFailed}: ${response.status}`
}
