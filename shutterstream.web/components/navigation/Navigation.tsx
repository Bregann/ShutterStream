'use client'

import {
  AppShell,
  Button,
  Group,
  Text,
  ActionIcon,
  Tooltip,
  useMantineColorScheme,
  useComputedColorScheme,
} from '@mantine/core'
import {
  IconCamera,
  IconLogout,
  IconLock,
  IconSettings,
  IconSun,
  IconMoon,
} from '@tabler/icons-react'
import { usePathname } from 'next/navigation'
import Link from 'next/link'
import classes from '@/css/navigation.module.css'
import { useAuth } from '@/context/authContext'

export default function Navigation({ children }: { children: React.ReactNode }) {
  const pathname = usePathname()
  const auth = useAuth()
  const { setColorScheme } = useMantineColorScheme()
  const computedColorScheme = useComputedColorScheme('dark', { getInitialValueInEffect: true })

  const toggleColorScheme = () => {
    setColorScheme(computedColorScheme === 'dark' ? 'light' : 'dark')
  }

  return (
    <AppShell
      header={{ height: 60 }}
      padding="md"
    >
      <AppShell.Header>
        <Group h="100%" px="md" justify="space-between" wrap="nowrap">
          <Link href="/" className={classes.logo}>
            <Group gap="xs" wrap="nowrap">
              <IconCamera size="1.5rem" />
              <Text size="lg" fw={700} style={{ whiteSpace: 'nowrap' }}>
                ShutterStream
              </Text>
            </Group>
          </Link>

          <Group wrap="nowrap" gap="xs">
            <Tooltip label="Toggle light/dark">
              <ActionIcon
                variant="subtle"
                color="gray"
                size="lg"
                onClick={toggleColorScheme}
                aria-label="Toggle colour scheme"
              >
                {computedColorScheme === 'dark' ? <IconSun size="1.1rem" /> : <IconMoon size="1.1rem" />}
              </ActionIcon>
            </Tooltip>

            {auth.isAuthenticated ? (
              <>
                <Button
                  component={Link}
                  href="/admin"
                  variant={pathname.startsWith('/admin') ? 'filled' : 'subtle'}
                  leftSection={<IconSettings size="1rem" />}
                  size="compact-sm"
                >
                  Admin
                </Button>

                <Button
                  variant="light"
                  color="red"
                  leftSection={<IconLogout size="1rem" />}
                  visibleFrom="sm"
                  size="compact-sm"
                  onClick={async () => { await auth.logout() }}
                >
                  Logout
                </Button>

                <ActionIcon
                  variant="light"
                  color="red"
                  hiddenFrom="sm"
                  title="Logout"
                  onClick={async () => { await auth.logout() }}
                >
                  <IconLogout size="1rem" />
                </ActionIcon>
              </>
            ) : (
              <Tooltip label="Admin login">
                <ActionIcon
                  component={Link}
                  href="/login"
                  variant="subtle"
                  color="gray"
                  size="lg"
                  aria-label="Admin login"
                >
                  <IconLock size="1.1rem" />
                </ActionIcon>
              </Tooltip>
            )}
          </Group>
        </Group>
      </AppShell.Header>

      <AppShell.Main>
        {children}
      </AppShell.Main>
    </AppShell>
  )
}
