import { Button, Center, Stack, Text, Title } from '@mantine/core'
import Link from 'next/link'

export default function NotFound() {
  return (
    <Center h={400}>
      <Stack align="center" gap="xs">
        <Title order={2}>Nothing to see here</Title>
        <Text c="dimmed">This page doesn&apos;t exist, or it isn&apos;t public yet.</Text>
        <Button component={Link} href="/" variant="light" mt="md">
          Back to albums
        </Button>
      </Stack>
    </Center>
  )
}
