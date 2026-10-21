import { NextRequest, NextResponse } from 'next/server'
import { isTokenExpired } from './helpers/jwtHelper'

// Configuration
// Use environment variables for configuration
let API_BASE_URL = process.env.API_BASE_URL

if (process.env.NODE_ENV === 'development') {
  API_BASE_URL = 'https://localhost:7250/api'
}

const ACCESS_TOKEN_COOKIE = 'accessToken'
const REFRESH_TOKEN_COOKIE = 'refreshToken'

// The gallery is public, only the admin area needs a login
const protectedRoutes = ['/admin']

export async function proxy(request: NextRequest) {
  const { pathname } = request.nextUrl
  const cookies = request.cookies
  const accessToken = cookies.get(ACCESS_TOKEN_COOKIE)?.value
  const refreshToken = cookies.get(REFRESH_TOKEN_COOKIE)?.value
  const isProtectedRoute = protectedRoutes.some(route => pathname.startsWith(route))

  // If user is on login page and already authenticated with valid token, redirect to the admin area
  if (pathname === '/login' && accessToken !== undefined && !isTokenExpired(accessToken)) {
    return NextResponse.redirect(new URL('/admin', request.url))
  }

  // Check if we need to refresh the token:
  // 1. Access token is missing OR expired
  // 2. Refresh token exists
  const needsRefresh =
    refreshToken !== undefined &&
    (accessToken === undefined || isTokenExpired(accessToken))

  if (needsRefresh) {
    try {
      const refreshRes = await fetch(`${API_BASE_URL}/Auth/RefreshToken`, {
        method: 'POST',
        headers: {
          // forward only the refreshToken cookie
          'Cookie': `${REFRESH_TOKEN_COOKIE}=${refreshToken}`,
        },
      })

      if (refreshRes.ok) {
        // Pull out all Set-Cookie headers
        const setCookieHeaders: string[] =
          refreshRes.headers.getSetCookie?.() ??
          (refreshRes.headers.get('set-cookie') !== null
            ? refreshRes.headers.get('set-cookie')!.split(/,(?=[^ ;]+=)/)
            : [])

        // Extract the new accessToken value
        let newAccessTokenValue: string | undefined
        for (const header of setCookieHeaders) {
          if (header.startsWith(`${ACCESS_TOKEN_COOKIE}=`)) {
            const [pair] = header.split('; ')
            newAccessTokenValue = pair.split('=')[1]
            break
          }
        }

        if (newAccessTokenValue !== undefined) {
          const originalCookies = request.headers.get('cookie')
          const filtered = originalCookies !== null
            ? originalCookies.split('; ').filter((c) => !c.startsWith(`${ACCESS_TOKEN_COOKIE}=`))
            : []

          filtered.push(`${ACCESS_TOKEN_COOKIE}=${newAccessTokenValue}`)
          const newCookieHeader = filtered.join('; ')

          // Clone request headers and override the Cookie header
          const newHeaders = new Headers(request.headers)
          newHeaders.set('cookie', newCookieHeader)

          // Prepare the proxy response:
          const response = NextResponse.next({
            request: {
              headers: newHeaders,
            },
          })

          // Also re-apply all Set-Cookie headers so the browser stores them
          for (const header of setCookieHeaders) {
            response.headers.append('set-cookie', header)
          }

          return response
        }
      } else {
        // Refresh failed - the session is over. Send the user to login if they were heading
        // to the admin area, otherwise let them carry on browsing as a visitor
        console.error('Token refresh failed in proxy')
        const response = isProtectedRoute
          ? NextResponse.redirect(new URL('/login', request.url))
          : NextResponse.next()

        response.cookies.delete(ACCESS_TOKEN_COOKIE)
        response.cookies.delete(REFRESH_TOKEN_COOKIE)
        return response
      }
    } catch (error) {
      console.error('Error during token refresh:', error)

      if (isProtectedRoute) {
        const response = NextResponse.redirect(new URL('/login', request.url))
        response.cookies.delete(ACCESS_TOKEN_COOKIE)
        response.cookies.delete(REFRESH_TOKEN_COOKIE)
        return response
      }
    }
  }

  // Check if user is authenticated for protected routes
  if (isProtectedRoute && accessToken === undefined && refreshToken === undefined) {
    return NextResponse.redirect(new URL('/login', request.url))
  }

  return NextResponse.next()
}


export const config = {
  matcher: [
    '/((?!_next/static|_next/image|api|favicon.ico).*)',
  ],
}
