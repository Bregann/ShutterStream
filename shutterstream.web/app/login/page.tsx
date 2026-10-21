'use client'

import { useState } from 'react'
import {
  Paper,
  TextInput,
  PasswordInput,
  Button,
  Title,
  Text,
  Container,
  Center,
  Box,
  Stack,
  Anchor,
} from '@mantine/core'
import { IconUser, IconLock, IconExclamationCircle } from '@tabler/icons-react'
import Link from 'next/link'
import { useAuth } from '@/context/authContext'
import notificationHelper from '@/helpers/notificationHelper'

export default function LoginPage() {
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [loading, setLoading] = useState(false)

  const auth = useAuth()

  const handleLogin = async () => {
    setLoading(true)
    const res = await auth.login(username, password)

    if (!res) {
      setLoading(false)
      notificationHelper.showErrorNotification('Login failed', 'Check your username and password. After repeated failures, wait a few minutes before trying again.', 3000, <IconExclamationCircle size={16} />)
      return
    }
    setLoading(false)
  }

  const handleKeyPress = (event: React.KeyboardEvent) => {
    if (event.key === 'Enter' && username !== '' && password !== '' && !loading) {
      handleLogin()
    }
  }

  return (
    <Container size={420} my={40}>
      <Center>
        <Box w="100%">
          <Title
            ta="center"
            fw={900}
            mb="md"
          >
            ShutterStream
          </Title>

          <Text c="dimmed" size="sm" ta="center" mt={5} mb="xl">
            Sign in to manage your albums
          </Text>

          <Paper withBorder shadow="md" p={30} mt={30} radius="md">
            <Stack gap="md">
              <TextInput
                label="Username"
                placeholder="Your username"
                value={username}
                onChange={(event) => setUsername(event.currentTarget.value)}
                onKeyDown={handleKeyPress}
                leftSection={<IconUser size={16} />}
                autoComplete="username"
                required
              />

              <PasswordInput
                label="Password"
                placeholder="Your password"
                value={password}
                onChange={(event) => setPassword(event.currentTarget.value)}
                onKeyDown={handleKeyPress}
                leftSection={<IconLock size={16} />}
                autoComplete="current-password"
                required
              />

              <Button
                fullWidth
                mt="xl"
                onClick={handleLogin}
                loading={loading}
                disabled={username === '' || password === ''}
              >
                Sign in
              </Button>

              <Text ta="center" mt="md" size="sm" c="dimmed">
                <Anchor component={Link} href="/" size="sm">
                  Back to the gallery
                </Anchor>
              </Text>
            </Stack>
          </Paper>
        </Box>
      </Center>
    </Container>
  )
}
