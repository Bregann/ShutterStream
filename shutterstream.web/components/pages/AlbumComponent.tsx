'use client'

import { Alert, Button, Center, Container, Group, Loader, Stack, Text, Title } from '@mantine/core'
import { IconArrowLeft, IconEyeOff, IconSettings } from '@tabler/icons-react'
import { useQuery } from '@tanstack/react-query'
import Link from 'next/link'
import { doQueryGet } from '@/helpers/apiClient'
import { QueryKeys } from '@/helpers/QueryKeys'
import { formatAlbumDate } from '@/helpers/dateHelper'
import { useAuth } from '@/context/authContext'
import type { GetAlbumDto } from '@/interfaces/api/albums/GetAlbumDto'
import JustifiedPhotoGrid from '@/components/photos/JustifiedPhotoGrid'

interface AlbumComponentProps {
  albumId: number
}

export default function AlbumComponent({ albumId }: AlbumComponentProps) {
  const auth = useAuth()

  const { data: album, isLoading, isError } = useQuery({
    queryKey: [QueryKeys.Album, albumId.toString()],
    queryFn: async () => await doQueryGet<GetAlbumDto>(`/api/Albums/GetAlbum?albumId=${albumId}`)
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
      <Stack gap="lg">
        <Group justify="space-between" align="flex-start">
          <div>
            <Button
              component={Link}
              href="/"
              variant="subtle"
              color="gray"
              size="compact-sm"
              leftSection={<IconArrowLeft size="1rem" />}
              mb="xs"
            >
              All albums
            </Button>
            <Title order={1} mb="xs">
              {album.title}
            </Title>
            <Text c="dimmed" size="sm">
              {album.photos.length} {album.photos.length === 1 ? 'photo' : 'photos'}
              {album.albumDate !== null && ` · ${formatAlbumDate(album.albumDate)}`}
            </Text>
          </div>

          {auth.isAuthenticated && (
            <Button
              component={Link}
              href={`/admin/albums/${album.albumId}`}
              variant="light"
              leftSection={<IconSettings size="1rem" />}
            >
              Manage album
            </Button>
          )}
        </Group>

        {!album.isPublic && (
          <Alert color="yellow" variant="light" icon={<IconEyeOff size="1rem" />}>
            This album is private. Only you can see it until it is published.
          </Alert>
        )}

        {album.description !== '' && (
          <Text style={{ whiteSpace: 'pre-line' }} maw={800}>
            {album.description}
          </Text>
        )}

        {album.photos.length === 0 ? (
          <Center py={80}>
            <Text c="dimmed">There are no photos in this album yet.</Text>
          </Center>
        ) : (
          <JustifiedPhotoGrid photos={album.photos} />
        )}
      </Stack>
    </Container>
  )
}
