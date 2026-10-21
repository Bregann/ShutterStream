// context/AuthContext.tsx
'use client'

import { doPost } from '@/helpers/apiClient'
import { isTokenExpired, decodeJwt } from '@/helpers/jwtHelper'
import { useRouter } from 'next/navigation'
import { createContext, useContext, useState, useEffect, useRef } from 'react'

type User = {
  username: string
}

type AuthContextType = {
  isAuthenticated: boolean
  user: User | null
  login: (_username: string, _password: string) => Promise<boolean>
  logout: () => Promise<void>
}

const AuthContext = createContext<AuthContextType | undefined>(undefined)

export const AuthProvider = ({ children }: { children: React.ReactNode }) => {
  const [user, setUser] = useState<User | null>(null)
  const router = useRouter()
  const refreshTimeoutRef = useRef<NodeJS.Timeout | null>(null)

  // Function to get and decode current access token
  const getAccessToken = (): string | undefined => {
    const tokenRow = document.cookie
      .split('; ')
      .find(row => row.startsWith('accessToken='))
    return tokenRow !== undefined ? tokenRow.split('=')[1] : undefined
  }

  // Function to extract user from token
  const getUserFromToken = (token: string): User | null => {
    const payload = decodeJwt(token)
    if (!payload) return null

    const username = payload[
      'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'
    ]
    return typeof username === 'string' && username !== '' ? { username } : null
  }

  // Function to schedule token refresh before expiration
  const scheduleTokenRefresh = (token: string) => {
    // Clear any existing timeout
    if (refreshTimeoutRef.current) {
      clearTimeout(refreshTimeoutRef.current)
    }

    const payload = decodeJwt(token)
    if (!payload || !payload.exp) return

    const expirationTime = payload.exp * 1000
    const now = Date.now()
    const timeUntilExpiry = expirationTime - now

    // Refresh 2 minutes before expiration (or immediately if already expired/expiring soon)
    const refreshTime = Math.max(0, timeUntilExpiry - 120000)

    refreshTimeoutRef.current = setTimeout(async () => {
      try {
        // Trigger a page refresh which will cause the proxy to refresh the token
        router.refresh()
      } catch (error) {
        console.error('Failed to refresh token:', error)
      }
    }, refreshTime)
  }

  useEffect(() => {
    const token = getAccessToken()

    if (token !== undefined) {
      if (isTokenExpired(token)) {
        // Token is expired, trigger refresh via router
        router.refresh()
        return
      }

      const extractedUser = getUserFromToken(token)
      if (extractedUser) {
        setUser(extractedUser)
        scheduleTokenRefresh(token)
      }
    }

    // Cleanup timeout on unmount
    return () => {
      if (refreshTimeoutRef.current) {
        clearTimeout(refreshTimeoutRef.current)
      }
    }
  }, [])

  const login = async (username: string, password: string) => {
    const res = await doPost('/api/auth/LoginUser', { body: { username, password } })
    if (res.status === 200) {
      // Get the new token and schedule refresh
      const token = getAccessToken()
      if (token) {
        const extractedUser = getUserFromToken(token)
        if (extractedUser) {
          setUser(extractedUser)
          scheduleTokenRefresh(token)
        }
      }

      router.replace('/admin')
      return true
    }
    return false
  }

  const logout = async () => {
    // Clear refresh timeout
    if (refreshTimeoutRef.current) {
      clearTimeout(refreshTimeoutRef.current)
    }

    await doPost('/api/auth/DeleteUserSession', {})
    document.cookie = 'accessToken=; Max-Age=0; path=/'
    document.cookie = 'refreshToken=; Max-Age=0; path=/'
    setUser(null)

    router.replace('/')
    router.refresh()
  }

  // Unlike Orbit, children render straight away rather than waiting for the auth check:
  // the gallery is public, so its server rendered pages must not be held back for visitors
  return (
    <AuthContext.Provider value={{ isAuthenticated: user !== null, user, login, logout }}>
      {children}
    </AuthContext.Provider>
  )
}

export const useAuth = () => {
  const context = useContext(AuthContext)
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}
