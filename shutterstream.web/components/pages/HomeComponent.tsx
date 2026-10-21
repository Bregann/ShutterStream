'use client'

import { Center, Container, Loader, SimpleGrid, Stack, Text, Title } from '@mantine/core'
import { IconPhotoOff } from '@tabler/icons-react'
import { useQuery } from '@tanstack/react-query'
import { doQueryGet } from '@/helpers/apiClient'
import { QueryKeys } from '@/helpers/QueryKeys'
import type { GetAlbumsDto } from '@/interfaces/api/albums/GetAlbumsDto'
import AlbumCard from '@/components/albums/AlbumCard'

export default function HomeComponent() {
  const { data, isLoading } = useQuery({
    queryKey: [QueryKeys.Albums],
    queryFn: async () => await doQueryGet<GetAlbumsDto>('/api/Albums/GetAlbums')
  })

  const albums = data?.albums ?? []

  if (isLoading) {
    return (
      <Center h={400}>
        <Loader size="lg" />
      </Center>
    )
  }

  return (
    <Container size="xl" px={{ base: 'xs', sm: 'md' }}>
      <Stack gap="xl">
        <div>
          <Title order={1} mb="xs">
            Albums
          </Title>
          <Text c="dimmed" size="sm">
            {albums.length} {albums.length === 1 ? 'album' : 'albums'}
          </Text>
        </div>

        {albums.length === 0 ? (
          <Center py={80}>
            <Stack align="center" gap="xs">
              <IconPhotoOff size="3rem" color="var(--mantine-color-dimmed)" />
              <Text c="dimmed">No albums have been published yet.</Text>
            </Stack>
          </Center>
        ) : (
          <SimpleGrid cols={{ base: 1, xs: 2, md: 3, xl: 4 }} spacing="md">
            {albums.map(album => (
              <AlbumCard key={album.albumId} album={album} />
            ))}
          </SimpleGrid>
        )}
      </Stack>
    </Container>
  )
}
