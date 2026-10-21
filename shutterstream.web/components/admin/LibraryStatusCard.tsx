'use client'

import { Badge, Button, Card, Code, Divider, Group, Paper, Progress, SimpleGrid, Stack, Text, ThemeIcon, Title } from '@mantine/core'
import { IconCheck, IconFolder, IconRefresh, IconX } from '@tabler/icons-react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useRef, useState } from 'react'
import { doQueryGet } from '@/helpers/apiClient'
import { useMutationPost } from '@/helpers/mutations/useMutationPost'
import notificationHelper from '@/helpers/notificationHelper'
import { formatTimestamp } from '@/helpers/dateHelper'
import { QueryKeys } from '@/helpers/QueryKeys'
import type { GetLibraryStatusDto } from '@/interfaces/api/admin/GetLibraryStatusDto'

export default function LibraryStatusCard() {
  const queryClient = useQueryClient()

  // Hangfire can take a few seconds to pick the scan up, so keep polling until it actually starts
  const [awaitingScanStart, setAwaitingScanStart] = useState(false)
  const lastSeenCompletedAt = useRef<string | null | undefined>(undefined)

  const { data: status } = useQuery({
    queryKey: [QueryKeys.LibraryStatus],
    queryFn: async () => await doQueryGet<GetLibraryStatusDto>('/api/Admin/GetLibraryStatus'),
    refetchInterval: (query) => query.state.data?.isScanRunning === true || awaitingScanStart ? 2000 : false
  })

  useEffect(() => {
    if (status === undefined) {
      return
    }

    if (status.isScanRunning) {
      setAwaitingScanStart(false)
    }

    // Once a scan finishes the album lists are out of date. A small scan can start and finish
    // between two polls without ever being seen as running, so watch the completion time instead
    const scanCompleted = lastSeenCompletedAt.current !== undefined &&
      status.lastScanCompletedAt !== lastSeenCompletedAt.current

    if (scanCompleted) {
      setAwaitingScanStart(false)
      queryClient.invalidateQueries({ queryKey: [QueryKeys.AdminAlbums] })
      queryClient.invalidateQueries({ queryKey: [QueryKeys.Albums] })
    }

    lastSeenCompletedAt.current = status.lastScanCompletedAt
  }, [status, queryClient])

  // Stop waiting eventually in case the scan finished between two polls
  useEffect(() => {
    if (!awaitingScanStart) {
      return
    }

    const timeout = setTimeout(() => setAwaitingScanStart(false), 60000)
    return () => clearTimeout(timeout)
  }, [awaitingScanStart])

  const { mutate: scanLibrary, isPending: isQueueingScan } = useMutationPost<void, void>({
    url: '/api/Admin/ScanLibrary',
    queryKey: [QueryKeys.LibraryStatus],
    invalidateQuery: true,
    onSuccess: () => {
      setAwaitingScanStart(true)
      notificationHelper.showSuccessNotification('Scan queued', 'The library scan will start shortly', 3000, <IconCheck />)
    },
    onError: (error) => {
      notificationHelper.showErrorNotification('Error', error.message || 'Failed to start the scan', 3000, <IconX />)
    }
  })

  if (status === undefined) {
    return null
  }

  const stats = [
    { label: 'Albums', value: status.totalAlbums },
    { label: 'Public albums', value: status.publicAlbums },
    { label: 'Photos', value: status.totalPhotos },
    { label: 'Thumbnails pending', value: status.pendingThumbnails }
  ]

  const isBusy = status.isScanRunning || awaitingScanStart

  return (
    <Card withBorder p="lg" radius="md" shadow="sm">
      <Group justify="space-between" mb="md">
        <Group gap="xs">
          <ThemeIcon size="lg" radius="md" variant="light" color="blue">
            <IconFolder size="1.2rem" />
          </ThemeIcon>
          <Title order={3} size="h4">Photo library</Title>
        </Group>
        <Button
          leftSection={<IconRefresh size="1rem" />}
          onClick={() => scanLibrary()}
          loading={isQueueingScan}
          disabled={isBusy}
        >
          {isBusy ? 'Scanning…' : 'Scan library'}
        </Button>
      </Group>

      <Divider mb="md" />

      <Stack gap="md">
        <Group gap="xs">
          <Code>{status.libraryPath}</Code>
          {status.libraryPathExists
            ? <Badge color="green" variant="light">Found</Badge>
            : <Badge color="red" variant="light">Folder missing</Badge>}
        </Group>

        <SimpleGrid cols={{ base: 2, sm: 4 }} spacing="sm">
          {stats.map(stat => (
            <Paper key={stat.label} withBorder p="sm" radius="md">
              <Text size="xs" c="dimmed">{stat.label}</Text>
              <Text size="xl" fw={700}>{stat.value.toLocaleString()}</Text>
            </Paper>
          ))}
        </SimpleGrid>

        {status.isScanRunning && (
          <div>
            <Text size="sm" mb={4}>
              {status.scanTotal > 0
                ? `Generating thumbnails: ${status.scanProcessed.toLocaleString()} of ${status.scanTotal.toLocaleString()}`
                : 'Scanning folders…'}
            </Text>
            <Progress
              value={status.scanTotal > 0 ? (status.scanProcessed / status.scanTotal) * 100 : 100}
              animated
              striped
            />
          </div>
        )}

        {!status.isScanRunning && status.lastScanCompletedAt !== null && (
          <Text size="sm" c="dimmed">
            Last scan finished {formatTimestamp(status.lastScanCompletedAt)}: {status.lastScanMessage}
          </Text>
        )}

        {!status.isScanRunning && status.lastScanCompletedAt === null && (
          <Text size="sm" c="dimmed">
            No scan has run since the server started. The library is scanned automatically every hour.
          </Text>
        )}
      </Stack>
    </Card>
  )
}
