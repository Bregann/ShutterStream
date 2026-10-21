'use client'

import { ActionIcon, Button, Center, Container, Grid, Group, Loader, Stack, Text, Title } from '@mantine/core'
import { IconArrowLeft, IconChevronLeft, IconChevronRight } from '@tabler/icons-react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect } from 'react'
import { useRouter } from 'next/navigation'
import Link from 'next/link'
import classes from '@/css/photoViewer.module.css'
import { doPost, doQueryGet } from '@/helpers/apiClient'
import { QueryKeys } from '@/helpers/QueryKeys'
import { getPhotoImageUrl } from '@/helpers/photoHelper'
import { PhotoSize } from '@/interfaces/api/photos/PhotoSize'
import type { GetPhotoDto } from '@/interfaces/api/photos/GetPhotoDto'
import PhotoDetailsCard from '@/components/photos/PhotoDetailsCard'

interface PhotoComponentProps {
  photoId: number
}

const getPhoto = async (photoId: number) => await doQueryGet<GetPhotoDto>(`/api/Photos/GetPhoto?photoId=${photoId}`)

export default function PhotoComponent({ photoId }: PhotoComponentProps) {
  const router = useRouter()
  const queryClient = useQueryClient()

  const { data: photo, isLoading, isError } = useQuery({
    queryKey: [QueryKeys.Photo, photoId.toString()],
    queryFn: async () => await getPhoto(photoId)
  })

  // Count a view once per photo per browser session, so refreshing or stepping back and forth
  // doesn't inflate it. Recorded here rather than in GetPhoto, which also runs for prefetches
  useEffect(() => {
    if (photo === undefined) {
      return
    }

    const viewedKey = `photoViewed:${photo.photoId}`

    try {
      if (window.sessionStorage.getItem(viewedKey) !== null) {
        return
      }
      window.sessionStorage.setItem(viewedKey, '1')
    } catch {
      // Storage can be blocked, counting the view anyway is fine
    }

    doPost('/api/Photos/RecordPhotoView', { body: { photoId: photo.photoId } }).catch(() => {
      // View counts are best effort only
    })
  }, [photo])

  // Warm up the neighbouring photos so stepping through an album feels instant
  useEffect(() => {
    if (photo === undefined) {
      return
    }

    for (const neighbourId of [photo.nextPhotoId, photo.previousPhotoId]) {
      if (neighbourId === null) {
        continue
      }

      queryClient.query({
        queryKey: [QueryKeys.Photo, neighbourId.toString()],
        queryFn: async () => await getPhoto(neighbourId)
      }).then(neighbour => {
        const preload = new window.Image()
        preload.src = getPhotoImageUrl(neighbour.photoId, PhotoSize.Large, neighbour.version)
      }).catch(() => {
        // Preloading is best effort only
      })
    }
  }, [photo, queryClient])

  // Arrow keys step through the album, escape goes back to it
  useEffect(() => {
    if (photo === undefined) {
      return
    }

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.target instanceof HTMLInputElement || event.target instanceof HTMLTextAreaElement) {
        return
      }

      if (event.key === 'ArrowLeft' && photo.previousPhotoId !== null) {
        router.push(`/photo/${photo.previousPhotoId}`)
      } else if (event.key === 'ArrowRight' && photo.nextPhotoId !== null) {
        router.push(`/photo/${photo.nextPhotoId}`)
      } else if (event.key === 'Escape') {
        router.push(`/album/${photo.albumId}`)
      }
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [photo, router])

  if (isLoading) {
    return (
      <Center h={400}>
        <Loader size="lg" />
      </Center>
    )
  }

  if (isError || photo === undefined) {
    return (
      <Center h={400}>
        <Text c="dimmed">This photo could not be loaded.</Text>
      </Center>
    )
  }

  return (
    <Container size="xl" px={{ base: 'xs', sm: 'md' }}>
      <Stack gap="md">
        <Group justify="space-between">
          <Button
            component={Link}
            href={`/album/${photo.albumId}`}
            variant="subtle"
            color="gray"
            size="compact-sm"
            leftSection={<IconArrowLeft size="1rem" />}
          >
            {photo.albumTitle}
          </Button>
          <Text size="sm" c="dimmed">
            {photo.positionInAlbum} of {photo.albumPhotoCount}
          </Text>
        </Group>

        <Grid gap="md">
          <Grid.Col span={{ base: 12, lg: 9 }}>
            <div className={classes.stage}>
              {/* Keyed so the old photo is never shown while the next one loads */}
              <img
                key={photo.photoId}
                src={getPhotoImageUrl(photo.photoId, PhotoSize.Large, photo.version)}
                width={photo.width}
                height={photo.height}
                alt={photo.title ?? `Photo ${photo.positionInAlbum} from ${photo.albumTitle}`}
                className={classes.image}
              />

              {photo.previousPhotoId !== null && (
                <ActionIcon
                  component={Link}
                  href={`/photo/${photo.previousPhotoId}`}
                  className={`${classes.navButton} ${classes.previous}`}
                  variant="filled"
                  color="dark"
                  size="xl"
                  radius="xl"
                  aria-label="Previous photo"
                >
                  <IconChevronLeft size="1.5rem" />
                </ActionIcon>
              )}

              {photo.nextPhotoId !== null && (
                <ActionIcon
                  component={Link}
                  href={`/photo/${photo.nextPhotoId}`}
                  className={`${classes.navButton} ${classes.next}`}
                  variant="filled"
                  color="dark"
                  size="xl"
                  radius="xl"
                  aria-label="Next photo"
                >
                  <IconChevronRight size="1.5rem" />
                </ActionIcon>
              )}
            </div>
          </Grid.Col>

          <Grid.Col span={{ base: 12, lg: 3 }}>
            {(photo.title !== null || photo.description !== null) && (
              <Stack gap="xs" mb="md">
                {photo.title !== null && <Title order={2} size="h3">{photo.title}</Title>}
                {photo.description !== null && <Text size="sm" style={{ whiteSpace: 'pre-line' }}>{photo.description}</Text>}
              </Stack>
            )}
            <PhotoDetailsCard photo={photo} />
          </Grid.Col>
        </Grid>
      </Stack>
    </Container>
  )
}
