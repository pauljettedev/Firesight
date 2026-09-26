import { requestJson } from './http'

export interface HealthStatus {
  status: string
  database: string
}

export function getHealth(): Promise<HealthStatus> {
  return requestJson('/api/health', 'Health check failed')
}
