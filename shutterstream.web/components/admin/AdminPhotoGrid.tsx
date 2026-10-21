'use client'

import { ActionIcon, AspectRatio, Badge, Card, Divider, Group, Image, SimpleGrid, Text, Title, Tooltip } from '@mantine/core'
import { IconCheck, IconEye, IconEyeOff, IconPencil, IconSparkles, IconStar, IconStarFilled, IconX } from '@tabler/icons-react'
import { useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import classes from '@/css/adminPhotoGrid.module.css'
import { useMutationPatch } from '@/helpers/mutations/useMutationPatch'
import notificationHelper from '@/helpers/notificationHelper'
import { getPhotoImageUrl } from '@/helpers/photoHelper'
import { QueryKeys } from '@/helpers/QueryKeys'
import { PhotoSize } from '@/interfaces/api/photos/PhotoSize'
import type { GetAdminAlbumDto } from '@/interfaces/api/admin/GetAdminAlbumDto'
import type { SetAlbumCoverRequest } from '@/interfaces/api/admin/SetAlbumCoverRequest'
import type { SetPhotoVisibilityRequest } from '@/interfaces/api/admin/SetPhotoVisibilityRequest'
import type { AdminPhotoItem } from '@/interfaces/api/admin/GetAdminAlbumDto'
import EditPhotoCaptionModal from './EditPhotoCaptionModal'

interface AdminPhotoGridProps {
  album: GetAdminAlbumDto
}

export default function AdminPhotoGrid({ album }: AdminPhotoGridProps) {
  const queryClient = useQueryClient()
  const [photoToEdit, setPhotoToEdit] = useState<AdminPhotoItem | null>(null)
  const albumQueryKey = [QueryKeys.AdminAlbum, album.albumId.toString()]

  const invalidatePublicViews = () => {
    queryClient.invalidateQueries({ queryKey: [QueryKeys.AdminAlbums] })
    queryClient.invalidateQueries({ queryKey: [QueryKeys.Albums] })
    queryClient.invalidateQueries({ queryKey: [QueryKeys.Album, album.albumId.toString()] })
  }

  const { mutate: setPhotoVisibility, isPending: isUpdatingVisibility, variables: visibilityVariables } = useMutationPatch<SetPhotoVisibilityRequest, void>({
    url: '/api/Admin/SetPhotoVisibility',
    queryKey: albumQueryKey,
    invalidateQuery: true,
    onSuccess: invalidatePublicViews,
    onError: (error) => {
      notificationHelper.showErrorNotification('Error', error.message || 'Failed to update the photo', 3000, <IconX />)
    }
  })

  const { mutate: setAlbumCover, isPending: isUpdatingCover, variables: coverVariables } = useMutationPatch<SetAlbumCoverRequest, void>({
    url: '/api/Admin/SetAlbumCover',
    queryKey: albumQueryKey,
    invalidateQuery: true,
    onSuccess: () => {
      invalidatePublicViews()
      notificationHelper.showSuccessNotification('Success', 'Album cover updated', 3000, <IconCheck />)
    },
    onError: (error) => {
      notificationHelper.showErrorNotification('Error', error.message || 'Failed to set the cover', 3000, <IconX />)
    }
  })

  const hiddenCount = album.photos.filter(p => p.isHidden).length

  return (
    <Card withBorder p="lg" radius="md" shadow="sm">
      <Group justify="space-between" mb="md">
        <Title order={3} size="h4">Photos</Title>
        <Text size="sm" c="dimmed">
          {album.photos.length - hiddenCount} visible{hiddenCount > 0 && `, ${hiddenCount} hidden`}
        </Text>
      </Group>
      <Divider mb="md" />

      {album.photos.length === 0 ? (
        <Text c="dimmed" ta="center" py="xl">
          This folder has no photos yet.
        </Text>
      ) : (
        <SimpleGrid cols={{ base: 2, sm: 3, md: 4, lg: 6 }} spacing="xs">
          {album.photos.map(photo => {
            const isCover = album.coverPhotoId === photo.photoId

            return (
              <div key={photo.photoId} className={`${classes.tile} ${photo.isHidden ? classes.hidden : ''}`}>
                <AspectRatio ratio={1}>
                  <Image
                    src={getPhotoImageUrl(photo.photoId, PhotoSize.Small, photo.version)}
                    alt={photo.fileName}
                    className={classes.image}
                    fit="cover"
                    loading="lazy"
                  />
                </AspectRatio>

                <Group gap={4} className={classes.badges}>
                  {isCover && <Badge size="sm" color="yellow" leftSection={<IconStarFilled size="0.7rem" />}>Cover</Badge>}
                  {photo.isHidden && <Badge size="sm" color="gray">Hidden</Badge>}
                </Group>

                <Group gap={4} className={classes.actions}>
                  <Tooltip label="Edit caption">
                    <ActionIcon
                      variant="filled"
                      color="dark"
                      size="md"
                      onClick={() => setPhotoToEdit(photo)}
                      aria-label="Edit caption"
                    >
                      <IconPencil size="1rem" />
                    </ActionIcon>
                  </Tooltip>
                  <Tooltip label={isCover ? 'Album cover' : 'Set as cover'}>
                    <ActionIcon
                      variant="filled"
                      color={isCover ? 'yellow' : 'dark'}
                      size="md"
                      onClick={() => setAlbumCover({ albumId: album.albumId, photoId: photo.photoId })}
                      loading={isUpdatingCover && coverVariables?.photoId === photo.photoId}
                      disabled={isCover || photo.isHidden}
                      aria-label="Set as cover"
                    >
                      {isCover ? <IconStarFilled size="1rem" /> : <IconStar size="1rem" />}
                    </ActionIcon>
                  </Tooltip>
                  <Tooltip label={photo.isHidden ? 'Show photo' : 'Hide photo'}>
                    <ActionIcon
                      variant="filled"
                      color="dark"
                      size="md"
                      onClick={() => setPhotoVisibility({ photoId: photo.photoId, isHidden: !photo.isHidden })}
                      loading={isUpdatingVisibility && visibilityVariables?.photoId === photo.photoId}
                      aria-label={photo.isHidden ? 'Show photo' : 'Hide photo'}
                    >
                      {photo.isHidden ? <IconEye size="1rem" /> : <IconEyeOff size="1rem" />}
                    </ActionIcon>
                  </Tooltip>
                </Group>

                <Group gap={4} wrap="nowrap" px={6} py={4}>
                  {photo.isAiCaption && (
                    <Tooltip label="Written by AI">
                      <IconSparkles size="0.8rem" color="var(--mantine-color-violet-4)" style={{ flexShrink: 0 }} />
                    </Tooltip>
                  )}
                  <Text size="xs" c={photo.title === null ? 'dimmed' : undefined} lineClamp={1}>
                    {photo.title ?? 'No caption'}
                  </Text>
                </Group>
              </div>
            )
          })}
        </SimpleGrid>
      )}

      <EditPhotoCaptionModal
        albumId={album.albumId}
        photo={photoToEdit}
        onClose={() => setPhotoToEdit(null)}
      />
    </Card>
  )
}
