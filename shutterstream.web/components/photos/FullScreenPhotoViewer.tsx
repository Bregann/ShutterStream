'use client'

import { ActionIcon, Group, Loader, Modal, Text, Tooltip } from '@mantine/core'
import { IconDownload, IconX, IconZoomIn, IconZoomOut } from '@tabler/icons-react'
import { useState } from 'react'
import classes from '@/css/photoViewer.module.css'
import { getPhotoDownloadUrl, getPhotoImageUrl } from '@/helpers/photoHelper'
import { PhotoSize } from '@/interfaces/api/photos/PhotoSize'
import type { GetPhotoDto } from '@/interfaces/api/photos/GetPhotoDto'

interface FullScreenPhotoViewerProps {
  photo: GetPhotoDto
  opened: boolean
  onClose: () => void
}

export default function FullScreenPhotoViewer({ photo, opened, onClose }: FullScreenPhotoViewerProps) {
  const [fullLoaded, setFullLoaded] = useState(false)
  const [zoomed, setZoomed] = useState(false)

  const close = () => {
    setZoomed(false)
    onClose()
  }

  return (
    <Modal
      opened={opened}
      onClose={close}
      fullScreen
      withCloseButton={false}
      padding={0}
      transitionProps={{ transition: 'fade', duration: 150 }}
      classNames={{ content: classes.viewerContent, body: classes.viewerBody }}
    >
      <div className={`${classes.viewerStage} ${zoomed ? classes.zoomed : ''}`}>
        {/* The large image is already cached from the page, so show it while the full size one downloads */}
        {!fullLoaded && (
          <img
            src={getPhotoImageUrl(photo.photoId, PhotoSize.Large, photo.version)}
            alt=""
            className={classes.viewerImage}
          />
        )}
        <img
          src={getPhotoImageUrl(photo.photoId, PhotoSize.Full, photo.version)}
          alt={photo.title ?? 'Full size photo'}
          className={classes.viewerImage}
          style={{ display: fullLoaded ? undefined : 'none', cursor: zoomed ? 'zoom-out' : 'zoom-in' }}
          onLoad={() => setFullLoaded(true)}
          onClick={() => setZoomed(z => !z)}
        />
      </div>

      <Group gap="xs" className={classes.viewerToolbar}>
        {!fullLoaded && (
          <Group gap={6} mr="xs">
            <Loader size="xs" color="gray.4" />
            <Text size="xs" c="gray.4">Loading full resolution</Text>
          </Group>
        )}
        {fullLoaded && (
          <Tooltip label={zoomed ? 'Fit to screen' : 'Actual size'}>
            <ActionIcon variant="filled" color="dark" size="lg" radius="xl" onClick={() => setZoomed(z => !z)} aria-label={zoomed ? 'Fit to screen' : 'Actual size'}>
              {zoomed ? <IconZoomOut size="1.2rem" /> : <IconZoomIn size="1.2rem" />}
            </ActionIcon>
          </Tooltip>
        )}
        <Tooltip label="Download">
          <ActionIcon component="a" href={getPhotoDownloadUrl(photo.photoId, photo.version)} variant="filled" color="dark" size="lg" radius="xl" aria-label="Download">
            <IconDownload size="1.2rem" />
          </ActionIcon>
        </Tooltip>
        <Tooltip label="Close">
          <ActionIcon variant="filled" color="dark" size="lg" radius="xl" onClick={close} aria-label="Close">
            <IconX size="1.2rem" />
          </ActionIcon>
        </Tooltip>
      </Group>
    </Modal>
  )
}
