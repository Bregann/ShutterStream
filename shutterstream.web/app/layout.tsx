import '@mantine/core/styles.css'
import '@mantine/notifications/styles.css'
import { ColorSchemeScript, MantineProvider, createTheme, mantineHtmlProps } from '@mantine/core'
import { Notifications } from '@mantine/notifications'
import Providers from './providers'
import { AuthProvider } from '@/context/authContext'
import ClientLayout from '@/components/navigation/ClientLayout'
import NextTopLoader from 'nextjs-toploader'
import type { Metadata } from 'next'
import { SITE_NAME, SITE_URL } from '@/helpers/seo'

export const metadata: Metadata = {
  // Required so that Next.js can resolve relative Open Graph image urls
  metadataBase: new URL(SITE_URL),
  title: {
    default: SITE_NAME,
    template: `%s | ${SITE_NAME}`
  },
  description: 'A photo gallery'
}

//override the background colour for mantine dark mode
const theme = createTheme({
  colors: {
    dark: [
      '#F3F4F6',
      '#DFE2E6',
      '#C3C7CD',
      '#9BA1A9',
      '#6E757E',
      '#4A4F57',
      '#2F333B',
      '#23272E',
      '#1D2026',
      '#15171C'
    ]
  }
})


export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en" {...mantineHtmlProps}>
      <head>
        <ColorSchemeScript defaultColorScheme="dark" />
      </head>
      <body>
        <NextTopLoader color="#339af0" showSpinner={false} />
        <Providers>
          <AuthProvider>
            {/* Photos look best on a dark background, so dark is the default rather than auto */}
            <MantineProvider defaultColorScheme="dark" theme={theme}>
              <Notifications />
              <ClientLayout>
                {children}
              </ClientLayout>
            </MantineProvider>
          </AuthProvider>
        </Providers>
      </body>
    </html>
  )
}
