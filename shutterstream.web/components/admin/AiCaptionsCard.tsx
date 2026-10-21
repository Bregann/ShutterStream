'use client'

import { Alert, Button, Card, Divider, Group, Menu, Progress, Stack, Text, ThemeIcon, Title } from '@mantine/core'
import { IconCheck, IconChevronDown, IconRefresh, IconSparkles, IconX } from '@tabler/icons-react'
import { useMutationPost } from '@/helpers/mutations/useMutationPost'
import notificationHelper from '@/helpers/notificationHelper'
import { formatTimestamp } from '@/helpers/dateHelper'
import { QueryKeys } from '@/helpers/QueryKeys'
import type { GetAdminAlbumDto } from '@/interfaces/api/admin/GetAdminAlbumDto'
import type { GenerateAlbumCaptionsRequest } from '@/interfaces/api/admin/GenerateAlbumCaptionsRequest'

interface AiCaptionsCardProps {
  album: GetAdminAlbumDto
  awaitingStart: boolean
  onQueued: () => void
}

export default function AiCaptionsCard({ album, awaitingStart, onQueued }: AiCaptionsCardProps) {
  const status = album.captionStatus
  const captionedCount = album.photos.filter(p => p.title !== null).length
  const uncaptionedCount = album.photos.length - captionedCount
  const aiCaptionedCount = album.photos.filter(p => p.isAiCaption).length

  const { mutate: generateCaptions, isPending } = useMutationPost<GenerateAlbumCaptionsRequest, void>({
    url: '/api/Admin/GenerateAlbumCaptions',
    queryKey: [QueryKeys.AdminAlbum, album.albumId.toString()],
    invalidateQuery: true,
    onSuccess: () => {
      onQueued()
      notificationHelper.showSuccessNotification('Captions queued', 'Gemini will start captioning shortly', 3000, <IconCheck />)
    },
    onError: (error) => {
      notificationHelper.showErrorNotification('Error', error.message || 'Failed to start captioning', 3000, <IconX />)
    }
  })

  const isBusy = status.isRunning || awaitingStart

  return (
    <Card withBorder p="lg" radius="md" shadow="sm">
      <Group gap="xs" mb="md">
        <ThemeIcon size="lg" radius="md" variant="light" color="violet">
          <IconSparkles size="1.2rem" />
        </ThemeIcon>
        <Title order={3} size="h4">AI captions</Title>
      </Group>
      <Divider mb="md" />

      {!status.aiCaptionsEnabled ? (
        <Text size="sm" c="dimmed">
          Optional. Add a Gemini API key to the <code>GeminiApiKey</code> environmental setting to have Gemini write a title and description for each photo.
        </Text>
      ) : (
        <Stack gap="md">
          <Text size="sm" c="dimmed">
            {captionedCount} of {album.photos.length} photos have a caption. Captions you edit by hand are never overwritten.
          </Text>

          {status.isRunning && (
            <div>
              <Text size="sm" mb={4}>
                Captioning {status.processed} of {status.total}
                {status.failed > 0 && ` (${status.failed} failed)`}
              </Text>
              <Progress value={status.total > 0 ? (status.processed / status.total) * 100 : 0} color="violet" animated striped />
            </div>
          )}

          {status.isOtherAlbumRunning && (
            <Alert color="yellow" variant="light">
              Another album is being captioned. Wait for it to finish before starting this one.
            </Alert>
          )}

          {!status.isRunning && status.lastMessage !== null && status.lastCompletedAt !== null && (
            <Text size="sm" c="dimmed">
              Last run {formatTimestamp(status.lastCompletedAt)}: {status.lastMessage}
            </Text>
          )}

          <Group gap={0} wrap="nowrap">
            <Button
              leftSection={<IconSparkles size="1rem" />}
              color="violet"
              onClick={() => generateCaptions({ albumId: album.albumId, regenerateExisting: false })}
              loading={isPending}
              disabled={isBusy || status.isOtherAlbumRunning || uncaptionedCount === 0}
              style={{ flex: 1, borderTopRightRadius: 0, borderBottomRightRadius: 0 }}
            >
              {isBusy ? 'Captioning…' : `Caption ${uncaptionedCount} ${uncaptionedCount === 1 ? 'photo' : 'photos'}`}
            </Button>
            <Menu position="bottom-end" withinPortal>
              <Menu.Target>
                <Button
                  color="violet"
                  disabled={isBusy || status.isOtherAlbumRunning}
                  style={{ borderTopLeftRadius: 0, borderBottomLeftRadius: 0, borderLeft: '1px solid var(--mantine-color-violet-8)' }}
                  px="xs"
                  aria-label="More caption options"
                >
                  <IconChevronDown size="1rem" />
                </Button>
              </Menu.Target>
              <Menu.Dropdown>
                <Menu.Item
                  leftSection={<IconRefresh size="1rem" />}
                  disabled={aiCaptionedCount === 0 && uncaptionedCount === 0}
                  onClick={() => generateCaptions({ albumId: album.albumId, regenerateExisting: true })}
                >
                  Regenerate AI captions
                </Menu.Item>
              </Menu.Dropdown>
            </Menu>
          </Group>
        </Stack>
      )}
    </Card>
  )
}
