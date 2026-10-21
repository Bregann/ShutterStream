'use client'

import { ActionIcon, Badge, Card, Divider, Group, Image, Switch, Table, Text, ThemeIcon, Title, Tooltip } from '@mantine/core'
import { IconAlbum, IconCheck, IconExternalLink, IconPhoto, IconSettings, IconX } from '@tabler/icons-react'
import { useQueryClient } from '@tanstack/react-query'
import Link from 'next/link'
import { useMutationPatch } from '@/helpers/mutations/useMutationPatch'
import notificationHelper from '@/helpers/notificationHelper'
import { getPhotoImageUrl } from '@/helpers/photoHelper'
import { formatAlbumDate } from '@/helpers/dateHelper'
import { QueryKeys } from '@/helpers/QueryKeys'
import { PhotoSize } from '@/interfaces/api/photos/PhotoSize'
import type { AdminAlbumItem } from '@/interfaces/api/admin/GetAdminAlbumsDto'
import type { SetAlbumVisibilityRequest } from '@/interfaces/api/admin/SetAlbumVisibilityRequest'

interface AdminAlbumsTableProps {
  albums: AdminAlbumItem[]
}

export default function AdminAlbumsTable({ albums }: AdminAlbumsTableProps) {
  const queryClient = useQueryClient()

  const { mutate: setAlbumVisibility, isPending, variables } = useMutationPatch<SetAlbumVisibilityRequest, void>({
    url: '/api/Admin/SetAlbumVisibility',
    queryKey: [QueryKeys.AdminAlbums],
    invalidateQuery: true,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [QueryKeys.Albums] })
      queryClient.invalidateQueries({ queryKey: [QueryKeys.LibraryStatus] })
      notificationHelper.showSuccessNotification('Success', 'Album visibility updated', 3000, <IconCheck />)
    },
    onError: (error) => {
      notificationHelper.showErrorNotification('Error', error.message || 'Failed to update the album', 3000, <IconX />)
    }
  })

  return (
    <Card withBorder p="lg" radius="md" shadow="sm">
      <Group gap="xs" mb="md">
        <ThemeIcon size="lg" radius="md" variant="light" color="blue">
          <IconAlbum size="1.2rem" />
        </ThemeIcon>
        <Title order={3} size="h4">Albums</Title>
      </Group>

      <Divider mb="md" />

      {albums.length === 0 ? (
        <Text c="dimmed" ta="center" py="xl">
          No albums found. Add a folder of photos to the library and run a scan.
        </Text>
      ) : (
        <Table.ScrollContainer minWidth={650}>
          <Table verticalSpacing="sm" highlightOnHover>
            <Table.Thead>
              <Table.Tr>
                <Table.Th style={{ width: 90 }} />
                <Table.Th>Album</Table.Th>
                <Table.Th>Photos</Table.Th>
                <Table.Th>Views</Table.Th>
                <Table.Th>Public</Table.Th>
                <Table.Th style={{ width: 90 }}>Actions</Table.Th>
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {albums.map(album => (
                <Table.Tr key={album.albumId}>
                  <Table.Td>
                    {album.coverPhotoId !== null && album.coverPhotoVersion !== null ? (
                      <Image
                        src={getPhotoImageUrl(album.coverPhotoId, PhotoSize.Small, album.coverPhotoVersion)}
                        alt=""
                        w={72}
                        h={54}
                        radius="sm"
                        fit="cover"
                        loading="lazy"
                      />
                    ) : (
                      <ThemeIcon size={54} variant="light" color="gray" radius="sm">
                        <IconPhoto size="1.5rem" />
                      </ThemeIcon>
                    )}
                  </Table.Td>
                  <Table.Td>
                    <Text size="sm" fw={500} lineClamp={1}>{album.title}</Text>
                    <Text size="xs" c="dimmed" lineClamp={1}>
                      {album.folderName}
                      {album.albumDate !== null && ` · ${formatAlbumDate(album.albumDate)}`}
                    </Text>
                  </Table.Td>
                  <Table.Td>
                    <Group gap="xs" wrap="nowrap">
                      <Text size="sm">{album.photoCount.toLocaleString()}</Text>
                      {album.hiddenPhotoCount > 0 && (
                        <Badge size="sm" variant="light" color="gray">{album.hiddenPhotoCount} hidden</Badge>
                      )}
                    </Group>
                  </Table.Td>
                  <Table.Td>
                    <Text size="sm">{album.viewCount.toLocaleString()}</Text>
                  </Table.Td>
                  <Table.Td>
                    <Switch
                      checked={album.isPublic}
                      disabled={isPending && variables?.albumId === album.albumId}
                      onChange={(event) => setAlbumVisibility({ albumId: album.albumId, isPublic: event.currentTarget.checked })}
                      aria-label={`Make ${album.title} ${album.isPublic ? 'private' : 'public'}`}
                    />
                  </Table.Td>
                  <Table.Td>
                    <Group gap="xs" wrap="nowrap">
                      <Tooltip label="Manage">
                        <ActionIcon component={Link} href={`/admin/albums/${album.albumId}`} variant="subtle" size="sm">
                          <IconSettings size="1rem" />
                        </ActionIcon>
                      </Tooltip>
                      <Tooltip label="View album">
                        <ActionIcon component={Link} href={`/album/${album.albumId}`} variant="subtle" color="gray" size="sm">
                          <IconExternalLink size="1rem" />
                        </ActionIcon>
                      </Tooltip>
                    </Group>
                  </Table.Td>
                </Table.Tr>
              ))}
            </Table.Tbody>
          </Table>
        </Table.ScrollContainer>
      )}
    </Card>
  )
}
