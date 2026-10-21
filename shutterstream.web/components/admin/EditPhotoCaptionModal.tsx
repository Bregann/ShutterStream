'use client'

import { Button, Group, Image, Modal, Stack, Textarea, TextInput } from '@mantine/core'
import { IconCheck, IconDeviceFloppy, IconX } from '@tabler/icons-react'
import { useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { useMutationPatch } from '@/helpers/mutations/useMutationPatch'
import notificationHelper from '@/helpers/notificationHelper'
import { getPhotoImageUrl } from '@/helpers/photoHelper'
import { QueryKeys } from '@/helpers/QueryKeys'
import { PhotoSize } from '@/interfaces/api/photos/PhotoSize'
import type { AdminPhotoItem } from '@/interfaces/api/admin/GetAdminAlbumDto'
import type { UpdatePhotoCaptionRequest } from '@/interfaces/api/admin/UpdatePhotoCaptionRequest'

interface EditPhotoCaptionModalProps {
  albumId: number
  photo: AdminPhotoItem | null
  onClose: () => void
}

export default function EditPhotoCaptionModal({ albumId, photo, onClose }: EditPhotoCaptionModalProps) {
  const queryClient = useQueryClient()
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')

  useEffect(() => {
    setTitle(photo?.title ?? '')
    setDescription(photo?.description ?? '')
  }, [photo])

  const { mutate: updateCaption, isPending } = useMutationPatch<UpdatePhotoCaptionRequest, void>({
    url: '/api/Admin/UpdatePhotoCaption',
    queryKey: [QueryKeys.AdminAlbum, albumId.toString()],
    invalidateQuery: true,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [QueryKeys.Album, albumId.toString()] })
      if (photo !== null) {
        queryClient.invalidateQueries({ queryKey: [QueryKeys.Photo, photo.photoId.toString()] })
      }
      notificationHelper.showSuccessNotification('Success', 'Caption saved', 3000, <IconCheck />)
      onClose()
    },
    onError: (error) => {
      notificationHelper.showErrorNotification('Error', error.message || 'Failed to save the caption', 3000, <IconX />)
    }
  })

  return (
    <Modal opened={photo !== null} onClose={onClose} title="Edit caption" size="lg">
      {photo !== null && (
        <Stack gap="md">
          <Image
            src={getPhotoImageUrl(photo.photoId, PhotoSize.Small, photo.version)}
            alt={photo.fileName}
            mah={300}
            fit="contain"
            radius="sm"
          />
          <TextInput
            label="Title"
            value={title}
            onChange={(e) => setTitle(e.currentTarget.value)}
            maxLength={200}
          />
          <Textarea
            label="Description"
            value={description}
            onChange={(e) => setDescription(e.currentTarget.value)}
            maxLength={2000}
            autosize
            minRows={3}
            maxRows={8}
          />
          <Group justify="flex-end">
            <Button variant="light" onClick={onClose}>Cancel</Button>
            <Button
              leftSection={<IconDeviceFloppy size="1rem" />}
              onClick={() => updateCaption({ photoId: photo.photoId, title, description })}
              loading={isPending}
            >
              Save
            </Button>
          </Group>
        </Stack>
      )}
    </Modal>
  )
}
