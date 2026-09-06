export interface HealthStatus {
  status: string
  database: string
}

export async function getHealth(): Promise<HealthStatus> {
  const response = await fetch('/api/health')

  if (!response.ok) {
    throw new Error(`Health check failed: ${response.status}`)
  }

  return response.json()
}
