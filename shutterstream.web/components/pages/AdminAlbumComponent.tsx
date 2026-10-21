'use client'

import { Button, Center, Container, Grid, Group, Loader, Stack, Switch, Text, Title } from '@mantine/core'
import { IconArrowLeft, IconCheck, IconExternalLink, IconX } from '@tabler/icons-react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useRef, useState } from 'react'
import Link from 'next/link'
import { doQueryGet } from '@/helpers/apiClient'
import { useMutationPatch } from '@/helpers/mutations/useMutationPatch'
import notificationHelper from '@/helpers/notificationHelper'
import { QueryKeys } from '@/helpers/QueryKeys'
import type { GetAdminAlbumDto } from '@/interfaces/api/admin/GetAdminAlbumDto'
import type { SetAlbumVisibilityRequest } from '@/interfaces/api/admin/SetAlbumVisibilityRequest'
import AlbumDetailsCard from '@/components/admin/AlbumDetailsCard'
import AdminPhotoGrid from '@/components/admin/AdminPhotoGrid'
import AiCaptionsCard from '@/components/admin/AiCaptionsCard'

interface AdminAlbumComponentProps {
  albumId: number
}

export default function AdminAlbumComponent({ albumId }: AdminAlbumComponentProps) {
  const queryClient = useQueryClient()

  // Hangfire can take a few seconds to pick the caption job up, so keep polling until it starts
  const [awaitingCaptionStart, setAwaitingCaptionStart] = useState(false)
  const wasCaptioning = useRef(false)

  const { data: album, isLoading, isError } = useQuery({
    queryKey: [QueryKeys.AdminAlbum, albumId.toString()],
    queryFn: async () => await doQueryGet<GetAdminAlbumDto>(`/api/Admin/GetAdminAlbum?albumId=${albumId}`),
    // Poll while captions are being written so they appear in the grid as they arrive
    refetchInterval: (query) => query.state.data?.captionStatus.isRunning === true || awaitingCaptionStart ? 3000 : false
  })

  useEffect(() => {
    if (album === undefined) {
      return
    }

    if (album.captionStatus.isRunning) {
      setAwaitingCaptionStart(false)
    }

    // Once captioning finishes the public pages are out of date
    if (wasCaptioning.current && !album.captionStatus.isRunning) {
      queryClient.invalidateQueries({ queryKey: [QueryKeys.Album, albumId.toString()] })
    }

    wasCaptioning.current = album.captionStatus.isRunning
  }, [album, albumId, queryClient])

  // Stop waiting eventually in case the job finished between two polls
  useEffect(() => {
    if (!awaitingCaptionStart) {
      return
    }

    const timeout = setTimeout(() => setAwaitingCaptionStart(false), 60000)
    return () => clearTimeout(timeout)
  }, [awaitingCaptionStart])

  const { mutate: setAlbumVisibility, isPending: isUpdatingVisibility } = useMutationPatch<SetAlbumVisibilityRequest, void>({
    url: '/api/Admin/SetAlbumVisibility',
    queryKey: [QueryKeys.AdminAlbum, albumId.toString()],
    invalidateQuery: true,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [QueryKeys.AdminAlbums] })
      queryClient.invalidateQueries({ queryKey: [QueryKeys.Albums] })
      queryClient.invalidateQueries({ queryKey: [QueryKeys.Album, albumId.toString()] })
      queryClient.invalidateQueries({ queryKey: [QueryKeys.LibraryStatus] })
      notificationHelper.showSuccessNotification('Success', 'Album visibility updated', 3000, <IconCheck />)
    },
    onError: (error) => {
      notificationHelper.showErrorNotification('Error', error.message || 'Failed to update the album', 3000, <IconX />)
    }
  })

  if (isLoading) {
    return (
      <Center h={400}>
        <Loader size="lg" />
      </Center>
    )
  }

  if (isError || album === undefined) {
    return (
      <Center h={400}>
        <Text c="dimmed">This album could not be loaded.</Text>
      </Center>
    )
  }

  return (
    <Container size="xl" px={{ base: 'xs', sm: 'md' }}>
      <Stack gap="xl">
        <Group justify="space-between" align="flex-end" wrap="wrap">
          <div>
            <Button
              component={Link}
              href="/admin"
              variant="subtle"
              color="gray"
              size="compact-sm"
              leftSection={<IconArrowLeft size="1rem" />}
              mb="xs"
            >
              All albums
            </Button>
            <Title order={1}>{album.title}</Title>
          </div>

          <Group gap="lg">
            <Switch
              size="md"
              label={album.isPublic ? 'Public' : 'Private'}
              checked={album.isPublic}
              disabled={isUpdatingVisibility}
              onChange={(event) => setAlbumVisibility({ albumId: album.albumId, isPublic: event.currentTarget.checked })}
            />
            <Button
              component={Link}
              href={`/album/${album.albumId}`}
              variant="light"
              leftSection={<IconExternalLink size="1rem" />}
            >
              View album
            </Button>
          </Group>
        </Group>

        <Grid>
          <Grid.Col span={{ base: 12, md: 4 }}>
            <Stack gap="md">
              <AlbumDetailsCard album={album} />
              <AiCaptionsCard
                album={album}
                awaitingStart={awaitingCaptionStart}
                onQueued={() => setAwaitingCaptionStart(true)}
              />
            </Stack>
          </Grid.Col>
          <Grid.Col span={{ base: 12, md: 8 }}>
            <AdminPhotoGrid album={album} />
          </Grid.Col>
        </Grid>
      </Stack>
    </Container>
  )
}
