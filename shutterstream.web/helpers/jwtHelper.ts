/**
 * Decodes a JWT token and returns the payload
 */
export function decodeJwt(token: string): any {
  try {
    const base64Url = token.split('.')[1]
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/')
    const jsonPayload = decodeURIComponent(
      atob(base64)
        .split('')
        .map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join('')
    )
    return JSON.parse(jsonPayload)
  } catch (error) {
    console.error('Failed to decode JWT:', error)
    return null
  }
}

/**
 * Checks if a JWT token is expired or will expire soon
 * @param token - The JWT token string
 * @param bufferSeconds - Number of seconds before expiration to consider token expired (default: 60)
 * @returns true if token is expired or will expire within buffer time
 */
export function isTokenExpired(token: string, bufferSeconds: number = 60): boolean {
  const payload = decodeJwt(token)
  if (!payload || !payload.exp) {
    return true // Consider invalid tokens as expired
  }

  const expirationTime = payload.exp * 1000 // Convert to milliseconds
  const now = Date.now()
  const bufferMs = bufferSeconds * 1000

  return now >= expirationTime - bufferMs
}

/**
 * Gets the expiration time from a JWT token
 * @param token - The JWT token string
 * @returns Date object of expiration time, or null if invalid
 */
export function getTokenExpiration(token: string): Date | null {
  const payload = decodeJwt(token)
  if (!payload || !payload.exp) {
    return null
  }

  return new Date(payload.exp * 1000)
}
