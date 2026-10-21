'use client'

import { Button, Card, Divider, Group, Stack, Text, Textarea, TextInput, Title } from '@mantine/core'
import { IconCheck, IconDeviceFloppy, IconX } from '@tabler/icons-react'
import { useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { useMutationPatch } from '@/helpers/mutations/useMutationPatch'
import notificationHelper from '@/helpers/notificationHelper'
import { QueryKeys } from '@/helpers/QueryKeys'
import type { GetAdminAlbumDto } from '@/interfaces/api/admin/GetAdminAlbumDto'
import type { UpdateAlbumDetailsRequest } from '@/interfaces/api/admin/UpdateAlbumDetailsRequest'

interface AlbumDetailsCardProps {
  album: GetAdminAlbumDto
}

export default function AlbumDetailsCard({ album }: AlbumDetailsCardProps) {
  const queryClient = useQueryClient()
  const [title, setTitle] = useState(album.title)
  const [description, setDescription] = useState(album.description)

  // Pick up the saved values once the album refetches
  useEffect(() => {
    setTitle(album.title)
    setDescription(album.description)
  }, [album.title, album.description])

  const { mutate: updateAlbumDetails, isPending } = useMutationPatch<UpdateAlbumDetailsRequest, void>({
    url: '/api/Admin/UpdateAlbumDetails',
    queryKey: [QueryKeys.AdminAlbum, album.albumId.toString()],
    invalidateQuery: true,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [QueryKeys.AdminAlbums] })
      queryClient.invalidateQueries({ queryKey: [QueryKeys.Albums] })
      queryClient.invalidateQueries({ queryKey: [QueryKeys.Album, album.albumId.toString()] })
      notificationHelper.showSuccessNotification('Success', 'Album details saved', 3000, <IconCheck />)
    },
    onError: (error) => {
      notificationHelper.showErrorNotification('Error', error.message || 'Failed to save the album', 3000, <IconX />)
    }
  })

  const isDirty = title !== album.title || description !== album.description

  return (
    <Card withBorder p="lg" radius="md" shadow="sm">
      <Title order={3} size="h4" mb="md">Details</Title>
      <Divider mb="md" />

      <Stack gap="md">
        <TextInput
          label="Title"
          value={title}
          onChange={(event) => setTitle(event.currentTarget.value)}
          maxLength={200}
          required
        />
        <Textarea
          label="Description"
          placeholder="Optional - shown at the top of the album"
          value={description}
          onChange={(event) => setDescription(event.currentTarget.value)}
          maxLength={5000}
          autosize
          minRows={3}
          maxRows={10}
        />
        <Text size="xs" c="dimmed">
          Folder: {album.folderName}
        </Text>
        <Group justify="flex-end">
          <Button
            leftSection={<IconDeviceFloppy size="1rem" />}
            onClick={() => updateAlbumDetails({ albumId: album.albumId, title, description })}
            loading={isPending}
            disabled={!isDirty || title.trim() === ''}
          >
            Save
          </Button>
        </Group>
      </Stack>
    </Card>
  )
}
